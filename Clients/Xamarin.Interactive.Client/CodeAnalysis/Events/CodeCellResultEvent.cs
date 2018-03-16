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
    sealed class CodeCellResultEvent : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public EvaluationResultHandling ResultHandling { get; }
        public RepresentedType Type { get; }
        public IReadOnlyList<object> ValueRepresentations { get; }

        public CodeCellResultEvent (
            CodeCellId codeCellId,
            EvaluationResultHandling resultHandling,
            object value)
        {
            CodeCellId = codeCellId;
            ResultHandling = resultHandling;

            switch (value) {
            case null:
                break;
            case RepresentedObject representedObject:
                Type = representedObject.RepresentedType;
                ValueRepresentations = representedObject;
                break;
            default:
                Type = RepresentedType.Lookup (value.GetType ());
                ValueRepresentations = new [] { value };
                break;
            }
        }
    }
}