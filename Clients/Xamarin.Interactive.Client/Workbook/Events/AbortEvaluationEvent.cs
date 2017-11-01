//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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