//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Workbook.Views
{
    interface ICodeCellView : ICellView
    {
        bool IsOutdated { get; set; }
        bool IsDirty { get; set; }
        bool IsEvaluating { get; set; }
        bool IsFrozen { get; }
        bool HasErrorDiagnostics { get; set; }
        TimeSpan EvaluationDuration { get; set; }

        void Freeze ();

        void RenderDiagnostic (InteractiveDiagnostic diagnostic);

        void RenderResult (
            CultureInfo cultureInfo,
            object result,
            EvaluationResultHandling resultHandling);

        void RenderCapturedOutputSegment (CapturedOutputSegment segment);

        void Reset ();
    }
}