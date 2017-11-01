//
// MapRenderer.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (GeoPolyline))]
	[Renderer (typeof (GeoLocation))]
	sealed class MapRenderer : HtmlRendererBase
	{
		static bool isElCapitanOrNewer;

		static MapRenderer ()
		{
			isElCapitanOrNewer = ClientApp.SharedInstance.Host.IsMac &&
				ClientApp.SharedInstance.Host.OSVersion >= new Version (10, 11);
		}

		GeoLocation location;
		GeoPolyline polyline;

		public override string CssClass => "renderer-map";

		protected override void HandleBind ()
		{
			location = RenderState.Source as GeoLocation;
			if (location == null) {
				polyline = (GeoPolyline)RenderState.Source;
				location = polyline.Points?.FirstOrDefault ();
			}
		}

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return new RendererRepresentation (
				"Map", options: RendererRepresentationOptions.ForceExpand);
		}

		void AppendMapFrame (HtmlElement container)
		{
			var loadingDiv = Document.CreateElement (
				"div",
				"loader");
			container.AppendChild (loadingDiv);

			var iframe = Document.CreateElement ("iframe");
			iframe.SetAttribute ("frameborder", "0"); // Needed for IE

			iframe.AddEventListener ("load", ev => container.RemoveChild (loadingDiv));

			long cacheHandle;
			if (polyline != null) {
				var wrapper = new ChangeableWrapper<GeoPolyline> (
					polyline,
					RenderState.RemoteMember != null
				);
				wrapper.PropertyChanged += OnPolylineChanged;
				cacheHandle = ObjectCache.Shared.GetHandle (wrapper);
			} else
				cacheHandle = ObjectCache.Shared.GetHandle (location);

			iframe.SetAttribute (
				"src",
				$"data:application/x-inspector-map-view,{cacheHandle}"
			);

			container.AppendChild (iframe);
		}

		async void OnPolylineChanged (object sender, PropertyChangedEventArgs args)
		{
			var wrapper = (ChangeableWrapper<GeoPolyline>)sender;
			try {
				await Context.SetMemberAsync (RenderState.RemoteMember, wrapper.Value);
			} catch (Exception e) {
				Log.Error ("MapRenderer", e);
			}
		}

		protected override void HandleRender (RenderTarget target)
		{
			if (location == null || !target.IsExpanded)
				return;

			var container = Document.CreateElement (
				"div",
				"renderer-map-container");

			if (isElCapitanOrNewer)
				AppendMapFrame (container);

			if (polyline == null)
				container.AppendChild (CreateTable ());

			target.ExpandedTarget.AppendChild (container);
		}

		HtmlElement CreateTable ()
		{
			var table = Document.CreateElement ("table");

			table.AppendChild (CreateRow ("Latitude", new CoordinateComponent {
				Value = location.Latitude,
				Type = CoordinateComponentType.Latitude,
			}));
			table.AppendChild (CreateRow ("Longitude", new CoordinateComponent {
				Value = location.Longitude,
				Type = CoordinateComponentType.Longitude,
			}));

			if (location.Altitude != null)
				table.AppendChild (CreateRow ("Altitude", new UnitOfMeasureMagnitude {
					Value = location.Altitude.Value,
					Units = UnitOfMeasure.Meters,
				}));
			if (location.VerticalAccuracy != null && location.VerticalAccuracy.Value >= 0)
				table.AppendChild (CreateRow ("Vertical Accuracy", new UnitOfMeasureMagnitude {
					Value = location.VerticalAccuracy.Value,
					Units = UnitOfMeasure.Meters,
				}));
			if (location.HorizontalAccuracy != null && location.HorizontalAccuracy.Value >= 0)
				table.AppendChild (CreateRow ("Horizontal Accuracy", new UnitOfMeasureMagnitude {
					Value = location.HorizontalAccuracy.Value,
					Units = UnitOfMeasure.Meters,
				}));
			if (location.Speed != null)
				table.AppendChild (CreateRow ("Speed", new UnitOfMeasureMagnitude {
					Value = location.Speed.Value,
					Units = UnitOfMeasure.MetersPerSecond,
				}));
			if (location.Bearing != null)
				table.AppendChild (CreateRow ("Bearing", new UnitOfMeasureMagnitude {
					Value = location.Bearing.Value,
					Units = UnitOfMeasure.Degrees,
				}));

			return table;
		}

		HtmlElement CreateRow (string label, object value)
		{
			var row = Document.CreateElement ("tr");

			row.AppendChild (Document.CreateElement ("th", innerHtml: label));

			var td = Document.CreateElement ("td");
			Context.Render (RenderState.CreateChild (value), td);
			row.AppendChild (td);

			return row;
		}
	}

	enum CoordinateComponentType
	{
		Latitude,
		Longitude
	}

	struct CoordinateComponent
	{
		public double Value { get; set; }
		public CoordinateComponentType Type { get; set; }
	}

	[Renderer (typeof (CoordinateComponent))]
	sealed class CoordinateComponentRenderer : HtmlRendererBase
	{
		static readonly RendererRepresentation decimalDegreesRepresentation
			= new RendererRepresentation ("Decimal Degrees");

		static readonly RendererRepresentation dddMmSssRepresentation
			= new RendererRepresentation ("DDD° MM' SS.S\"");

		public override string CssClass => "renderer-coordinate-component";

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return decimalDegreesRepresentation;
			yield return dddMmSssRepresentation;
		}

		protected override void HandleRender (RenderTarget target)
		{
			var source = (CoordinateComponent)RenderState.Source;

			if (target.Representation == decimalDegreesRepresentation)
				target.InlineTarget.AppendChild (
					Document.CreateElement (
						"code",
						@class: "csharp-number",
						innerText: source.Value.ToString ()));
			else if (target.Representation == dddMmSssRepresentation)
				target.InlineTarget.AppendChild (
					Document.CreateElement (
						"code",
						innerText: ToDegreesMinutesSeconds (source.Value, source.Type)));
			else
				throw new NotImplementedException (target.Representation.ToString ());
		}

		static string ToDegreesMinutesSeconds (double coordinate, CoordinateComponentType type)
		{
			var absCoordinate = Math.Abs (coordinate);

			var degrees = Math.Floor (absCoordinate);

			var doubleMinutes = 60 * (absCoordinate - degrees);
			var minutes = Math.Floor (doubleMinutes);

			var seconds = 60 * (doubleMinutes - minutes);

			return $"{degrees}° {minutes}' {seconds:F1}\" " +
				(type == CoordinateComponentType.Latitude ?
					(coordinate >= 0 ? "N" : "S") :
					(coordinate >= 0 ? "E" : "W"));
		}
	}
}