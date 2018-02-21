//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Workbook.Models;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Client.Web.Hosting
{
    sealed class WebCellView : ICodeCellView
    {
        readonly Cell cell;
        readonly TextWriter output;

        public IEditor Editor { get; } = new WebCellEditor ();

        bool ICodeCellView.IsOutdated { get; set; }
        bool ICodeCellView.IsDirty { get; set; }
        bool ICodeCellView.IsEvaluating { get; set; }
        bool ICodeCellView.HasErrorDiagnostics { get; set; }
        TimeSpan ICodeCellView.EvaluationDuration { get; set; }

        bool isFrozen;
        bool ICodeCellView.IsFrozen => isFrozen;

        public WebCellView (Cell cell, TextWriter output)
        {
            this.cell = cell ?? throw new ArgumentNullException (nameof (cell));
            this.output = output ?? throw new ArgumentNullException (nameof (output));
        }

        void ICodeCellView.Freeze () => isFrozen = true;

        public void Focus (bool scrollIntoView = true)
        {
        }


        void ICodeCellView.RenderDiagnostic (InteractiveDiagnostic diagnostic)
        {
        }

        void ICodeCellView.RenderResult (
            CultureInfo cultureInfo,
            object result,
            EvaluationResultHandling resultHandling)
        {
        }

        void ICodeCellView.RenderCapturedOutputSegment (CapturedOutputSegment segment)
        {
        }

        void ICodeCellView.Reset ()
        {
        }
    }
}