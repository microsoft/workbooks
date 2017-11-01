//
// CalendarRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace Xamarin.Interactive.Rendering.Renderers
{
	sealed class CalendarRenderer
	{
		public DayOfWeek FirstDayOfWeek {
			get { return CultureInfo.DateTimeFormat.FirstDayOfWeek; }
			set { CultureInfo.DateTimeFormat.FirstDayOfWeek = value; }
		}

		CultureInfo cultureInfo;
		public CultureInfo CultureInfo {
			get { return cultureInfo; }
			set { cultureInfo = (CultureInfo)value.Clone (); }
		}

		public CalendarRenderer ()
		{
			CultureInfo = CultureInfo.CurrentCulture;
		}

		public DateTime GetDate (DateTime date)
		{
			return new DateTime (date.Year, date.Month, date.Day, CultureInfo.Calendar);
		}

		public DateTime GetDate (int year, int month, int day = 1)
		{
			return new DateTime (year, month, day, CultureInfo.Calendar);
		}

		public string GetMonthRenderTitle (DateTime date)
		{
			return GetMonthRenderTitle (date.Year, date.Month);
		}

		public string GetMonthRenderTitle (int year, int month)
		{
			return GetDate (year, month).ToString (
				CultureInfo.DateTimeFormat.YearMonthPattern,
				CultureInfo.DateTimeFormat
			);
		}

		public string GetDayName (DayOfWeek dayOfWeek)
		{
			return CultureInfo.DateTimeFormat.GetDayName (dayOfWeek);
		}

		public string GetAbbreviatedDayName (DayOfWeek dayOfWeek)
		{
			return CultureInfo.DateTimeFormat.GetAbbreviatedDayName (dayOfWeek);
		}

		public string GetShortestDayName (DayOfWeek dayOfWeek)
		{
			return CultureInfo.DateTimeFormat.GetShortestDayName (dayOfWeek);
		}

		public IEnumerable<DayOfWeek> GetRenderableDaysOfWeek ()
		{
			var first = (int)FirstDayOfWeek;
			for (var i = first; i < first + 7; i++)
				yield return (DayOfWeek)(i % 7);
		}

		public IEnumerable<Nullable<DateTime>> GetRenderableMonthDates (DateTime date)
		{
			return GetRenderableMonthDates (date.Year, date.Month);
		}

		public IEnumerable<Nullable<DateTime>> GetRenderableMonthDates (int year, int month)
		{
			var date = GetDate (year, month);

			var addend = -((date.DayOfWeek - FirstDayOfWeek + 7) % 7);
			do {
				try {
					date = CultureInfo.Calendar.AddDays (date, addend);
					break;
				} catch (ArgumentException) {
					// throws if outside the min range for the calendar
					addend++;
				}
				yield return null;
			} while (true);

			do {
				yield return date;
				try {
					date = CultureInfo.Calendar.AddDays (date, 1);
				} catch (ArgumentException) {
					// throws if outside the max range for the calendar
					break;
				}
			} while (date.Month == month || date.DayOfWeek != FirstDayOfWeek);
		}

		public void RenderTextCalendar (DateTime targetDate, TextWriter writer)
		{
			const int width = 7 * 3 - 1;

			var title = GetMonthRenderTitle (targetDate);
			writer.WriteLine (title.PadLeft ((width - title.Length) / 2 + title.Length));

			foreach (var day in GetRenderableDaysOfWeek ()) {
				var twoLetter = GetAbbreviatedDayName (day);
				twoLetter = twoLetter.Substring (0, Math.Min (twoLetter.Length, 2));
				writer.Write ("{0} ", twoLetter.PadRight (2));
			}

			writer.WriteLine ();

			int cell = 0;
			foreach (var date in GetRenderableMonthDates (targetDate)) {
				if (date.HasValue && date.Value.Month == targetDate.Month)
					writer.Write ("{0} ", date.Value.Day.ToString ().PadLeft (2));
				else
					writer.Write ("   ");
				if (++cell % 7 == 0)
					writer.WriteLine ();
			}
		}
	}
}