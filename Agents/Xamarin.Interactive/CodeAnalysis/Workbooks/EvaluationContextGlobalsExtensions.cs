//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.CodeAnalysis.Workbooks
{
    public static class EvaluationContextGlobalsExtensions
    {
        public static VerbatimHtml AsHtml (this string str)
            => new VerbatimHtml (str);
    }
}