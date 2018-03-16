//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;

using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.CodeAnalysis
{
    public sealed class CodeCellBuffer : SourceTextContainer, ICellBuffer
    {
        sealed class CodeCellBufferSourceText : SourceText
        {
            readonly SourceTextContainer container;
            readonly string value;

            public CodeCellBufferSourceText (SourceTextContainer container, string value = null)
            {
                this.container = container;
                this.value = value ?? String.Empty;
            }

            public override void CopyTo (int sourceIndex, char[] destination,
                int destinationIndex, int count)
                => value.CopyTo (sourceIndex, destination, destinationIndex, count);

            public override Encoding Encoding => Utf8.Encoding;

            public override int Length => value.Length;

            public override char this [int position] => value [position];

            public override SourceTextContainer Container => container;
        }

        SourceText currentText;

        internal CodeCellBuffer ()
        {
            currentText = new CodeCellBufferSourceText (this);
        }

        public override event EventHandler<TextChangeEventArgs> TextChanged;

        public override SourceText CurrentText => currentText;

        public int Length => CurrentText.Length;

        public string Value {
            get { return CurrentText.ToString (); }
            set {
                var oldText = currentText;
                currentText = new CodeCellBufferSourceText (this, value);

                TextChanged?.Invoke (this, new TextChangeEventArgs (
                    oldText, currentText, currentText.GetChangeRanges (oldText)));
            }
        }

        public void Invalidate ()
            => TextChanged?.Invoke (this, new TextChangeEventArgs (
                currentText, currentText, new TextChangeRange ()));
    }
}