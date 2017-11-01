//
// DateTimeRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using Xamarin.CrossBrowser;

namespace Xamarin.Interactive.Rendering.Renderers
{
	[Renderer (typeof (DateTime))]
	sealed class DateTimeRenderer : HtmlRendererBase
	{
		public override string CssClass => "renderer-datetime";

		public override bool CanExpand => false;

		DateTime source;

		protected override void HandleBind () => source = (DateTime)RenderState.Source;

		protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
		{
			yield return new RendererRepresentation ("Default");
			yield return new RendererRepresentation ("RFC1123", "R");
			yield return new RendererRepresentation ("Universal Sortable", "u");
			yield return new RendererRepresentation ("Sortable", "s");
			yield return new RendererRepresentation ("Round-trip", "o");
			yield return new RendererRepresentation ("Calendar", (Func<HtmlElement>)RenderCalendar,
				options: RendererRepresentationOptions.ForceExpand);
	 	}

		protected override void HandleRender (RenderTarget target)
		{
			string format = null;

			var func = target.Representation.State as Func<HtmlElement>;
			if (func == null)
				format = (string)target.Representation.State;

			target.InlineTarget.AppendChild (CreateToStringRepresentationElement (
				format, source.ToString (format, RenderState.CultureInfo)));

			if (target.IsExpanded && func != null)
				target.ExpandedTarget.AppendChild (func ());
		}

		HtmlElement RenderCalendar ()
		{
			var calendar = new CalendarRenderer {
				CultureInfo = RenderState.CultureInfo
			};

			var tableElem = Document.CreateElement ("table");

			var tableHeadElem = Document.CreateElement ("thead");
			tableElem.AppendChild (tableHeadElem);

			var headerRowElem = Document.CreateElement ("tr");
			tableHeadElem.AppendChild (headerRowElem);

			var headerElem = Document.CreateElement ("th",
				innerHtml: calendar.GetMonthRenderTitle (source));
			headerElem.SetAttribute ("colspan", "7");
			headerRowElem.AppendChild (headerElem);

			headerRowElem = Document.CreateElement ("tr");
			tableElem.AppendChild (headerRowElem);

			foreach (var dayOfWeek in calendar.GetRenderableDaysOfWeek ())
				headerRowElem.AppendChild (Document.CreateElement ("th",
					innerHtml: calendar.GetAbbreviatedDayName (dayOfWeek)));

			HtmlElement rowElem = null;

			var cellNumber = 0;
			var targetDate = calendar.GetDate (source);

			foreach (var currentDate in calendar.GetRenderableMonthDates (source)) {
				if (rowElem == null) {
					rowElem = Document.CreateElement ("tr");
					tableElem.AppendChild (rowElem);
				}

				var cell = Document.CreateElement ("td");
				rowElem.AppendChild (cell);

				if (currentDate.HasValue) {
					cell.InnerHTML = $"<span>{currentDate.Value.Day}</span>";

					if (targetDate.Month < currentDate.Value.Month)
						cell.AddCssClass ("previous-month-day");
					else if (targetDate.Month > currentDate.Value.Month)
						cell.AddCssClass ("next-month-day");
					else {
						cell.AddCssClass ("current-month-day");
						if (targetDate.Day == currentDate.Value.Day)
							cell.AddCssClass ("selected-date");
					}

					if (currentDate.Value.DayOfWeek == DayOfWeek.Saturday ||
						currentDate.Value.DayOfWeek == DayOfWeek.Sunday)
						cell.AddCssClass ("weekend");
				}

				if (++cellNumber % 7 == 0)
					rowElem = null;
			}

			return tableElem;
		}
	}
}