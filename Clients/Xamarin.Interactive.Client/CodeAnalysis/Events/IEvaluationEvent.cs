//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    interface IEvaluationEvent
    {
        CodeCellId CodeCellId { get; }
    }
}