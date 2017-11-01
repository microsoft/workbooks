//
// AbortEvaluationEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Events;

namespace Xamarin.Interactive.Workbook.Events
{
	sealed class AbortEvaluationEvent : IEvent
	{
		public object Source { get; }
		public DateTime Timestamp { get; } = DateTime.UtcNow;

		public AbortEvaluationEvent (object source)
			=> Source = source ?? throw new ArgumentNullException (nameof (source));
	}
}