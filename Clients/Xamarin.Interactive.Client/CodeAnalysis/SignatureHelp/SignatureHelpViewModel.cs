//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis.SignatureHelp
{
    sealed class ParameterViewModel
    {
        public string Label { get; }
        public string Documentation { get; } // Optional; unused for now

        public ParameterViewModel (IParameterSymbol parameter, string documentation = null)
        {
            Documentation = documentation;

            Label = parameter.ToDisplayString (Constants.SymbolDisplayFormat);
        }
    }

    sealed class SignatureViewModel
    {
        public string Label { get; }
        public string Documentation { get; } // Optional; unused for now
        public ParameterViewModel [] Parameters { get; }

        public SignatureViewModel (IMethodSymbol method, string documentation = null)
        {
            Documentation = documentation;

            Parameters = method
                .Parameters
                .Select (p => new ParameterViewModel (p))
                .ToArray ();

            Label = method.ToDisplayString (Constants.SymbolDisplayFormat);
        }
    }

    sealed class SignatureHelpViewModel
    {
        public SignatureViewModel [] Signatures { get; set; }
        public int ActiveSignature { get; set; }
        public int ActiveParameter { get; set; }
    }
}