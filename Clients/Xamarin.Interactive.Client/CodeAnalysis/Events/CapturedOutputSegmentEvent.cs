//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    sealed class CapturedOutputSegmentEvent : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public string Value { get; }

        public CapturedOutputSegmentEvent (
            CodeCellId codeCellId,
            string value)
        {
            CodeCellId = codeCellId;
            Value = value;
        }
    }
}