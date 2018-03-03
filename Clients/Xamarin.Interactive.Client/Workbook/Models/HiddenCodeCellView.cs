//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Workbook.Models
{
    sealed class HiddenCodeCellView : ICodeCellView
    {
        public IEditor Editor { get; set; }
        public bool IsOutdated { get; set; }
        public bool IsDirty { get; set; }
        public bool IsEvaluating { get; set; }

        public bool IsFrozen { get; private set; }

        public bool HasErrorDiagnostics { get; set; }
        public TimeSpan EvaluationDuration { get; set; }

        public void Focus (bool scrollIntoView = true)
        {
        }

        public void Freeze ()
            => IsFrozen = true;

        public void RenderCapturedOutputSegment (CapturedOutputSegment segment)
        {
        }

        public void RenderDiagnostic (InteractiveDiagnostic diagnostic)
        {
        }

        public void RenderResult (CultureInfo cultureInfo, object result, EvaluationResultHandling resultHandling)
        {
        }

        public void Reset ()
        {
        }
    }
}