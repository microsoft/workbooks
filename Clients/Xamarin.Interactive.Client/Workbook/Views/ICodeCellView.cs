//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Models;

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

        void RenderDiagnostic (Diagnostic diagnostic);

        void RenderResult (
            CultureInfo cultureInfo,
            object result,
            EvaluationResultHandling resultHandling);

        void RenderCapturedOutputSegment (CapturedOutputSegment segment);

        void Reset ();
    }
}