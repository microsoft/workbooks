//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Linq;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Workbook.Models;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Client.Console
{
    sealed class ConsoleCellView : ICodeCellView
    {
        readonly Cell cell;
        readonly TextWriter output;

        bool haveRenderedBuffer;

        public IEditor Editor { get; } = new ConsoleEditor ();

        bool ICodeCellView.IsOutdated { get; set; }
        bool ICodeCellView.IsDirty { get; set; }
        bool ICodeCellView.IsEvaluating { get; set; }
        bool ICodeCellView.HasErrorDiagnostics { get; set; }
        TimeSpan ICodeCellView.EvaluationDuration { get; set; }

        bool isFrozen;
        bool ICodeCellView.IsFrozen => isFrozen;

        public ConsoleCellView (Cell cell, TextWriter output)
        {
            this.cell = cell ?? throw new ArgumentNullException (nameof (cell));
            this.output = output ?? throw new ArgumentNullException (nameof (output));
        }

        void ICodeCellView.Freeze () => isFrozen = true;

        public void Focus (bool scrollIntoView = true)
        {
        }

        void RenderBuffer ()
        {
            if (!haveRenderedBuffer) {
                haveRenderedBuffer = true;

                var resetConsole = false;
                try {
                    if (cell.GetPreviousCell<CodeCell> () != null)
                        output.WriteLine ();

                    if (output == System.Console.Out) {
                        resetConsole = true;
                        System.Console.ForegroundColor = ConsoleColor.Blue;
                    }

                    output.WriteLine ("> {0}", cell.Buffer.Value);
                } finally {
                    if (resetConsole)
                        System.Console.ResetColor ();
                }
            }
        }

        void ICodeCellView.RenderDiagnostic (InteractiveDiagnostic diagnostic)
        {
            RenderBuffer ();

            var resetConsole = false;
            try {
                if (cell.GetPreviousCell<CodeCell> () != null)
                    output.WriteLine ();

                if (output == System.Console.Out) {
                    resetConsole = true;
                    System.Console.ForegroundColor = ConsoleColor.Red;
                }

                output.WriteLine (diagnostic.Message);
            } finally {
                if (resetConsole)
                    System.Console.ResetColor ();
            }
        }

        void ICodeCellView.RenderResult (
            CultureInfo cultureInfo,
            object result,
            EvaluationResultHandling resultHandling)
        {
            RenderBuffer ();

            if (result is RepresentedObject representedObject) {
                var interactiveObject = representedObject
                    .OfType<InteractiveObject> ()
                    .FirstOrDefault ();
                if (interactiveObject != null)
                    result = interactiveObject.ToStringRepresentation;
                else
                    result = "<unable to represent>";
            }

            output.WriteLine (result);
        }

        void ICodeCellView.RenderCapturedOutputSegment (CapturedOutputSegment segment)
        {
            RenderBuffer ();
            output.Write (segment.Value);
        }

        void ICodeCellView.Reset ()
        {
            haveRenderedBuffer = false;
        }
    }
}