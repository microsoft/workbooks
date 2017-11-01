//
// UnitOfMeasureRenderer.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.Rendering.Renderers
{
	enum UnitOfMeasure
	{
		Meters,
		MetersPerSecond,
		Degrees,
	}

	struct UnitOfMeasureMagnitude
	{
		public double Value { get; set; }
		public UnitOfMeasure Units { get; set; }
	}

	[Renderer (typeof (UnitOfMeasureMagnitude))]
	sealed class UnitOfMeasureRenderer : HtmlRendererBase
	{
		sealed class UomRep : RendererRepresentation
		{
			public string Unit { get; }

			public UomRep (string shortDisplayName, string unit, Func<double, string> toString)
				: base (shortDisplayName, toString)
			{
				Unit = unit;
			}

			public string ToString (double value)
				=> ((Func<double, string>)State) (value);
		}

		ImmutableArray<UomRep> representations;
		UnitOfMeasureMagnitude source;

		public override string CssClass => "renderer-uom";

		protected override void HandleBind ()
		{
			source = (UnitOfMeasureMagnitude)RenderState.Source;

			var reps = ImmutableArray<UomRep>.Empty.ToBuilder ();

			switch (this.source.Units) {
			case UnitOfMeasure.Meters:
				reps.Add (new UomRep ("Meters", " m", v => $"{v:F2}"));
				reps.Add (new UomRep ("Kilometers", " km", v => $"{v / 1000:F2}"));
				reps.Add (new UomRep ("👣", " 👣", v => $"{v / 0.3048:F2}"));
				reps.Add (new UomRep ("Miles", " mi.", v => $"{v / 1609.34:F2}"));
				break;
			case UnitOfMeasure.MetersPerSecond:
				reps.Add (new UomRep ("Meters / Second", " m/s", v => $"{v:F2}"));
				reps.Add (new UomRep ("Kilometers / Hour", " km/h", v => $"{v / 0.277778:F2}"));
				reps.Add (new UomRep ("Miles per hour", " mph", v => $"{v / 0.44704:F2}"));
				break;
			case UnitOfMeasure.Degrees:
				reps.Add (new UomRep ("Degrees", "°", v => $"{v:F2}"));
				break;
			default:
				throw new NotSupportedException ($"unsupport unit of measurement: {source.Units}");
			}

			representations = reps.ToImmutable ();
		}

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations () => representations;

		protected override void HandleRender (RenderTarget target)
		{
			var representation = (UomRep)target.Representation;

			var valueElem = Document.CreateElement ("code", @class: "csharp-number");
			valueElem.AppendTextNode (representation.ToString (source.Value));
			target.InlineTarget.AppendChild (valueElem);

			if (representation.Unit != null) {
				var unitElem = Document.CreateElement ("code");
				unitElem.AppendTextNode (representation.Unit);
				target.InlineTarget.AppendChild (unitElem);
			}
		}
	}
}