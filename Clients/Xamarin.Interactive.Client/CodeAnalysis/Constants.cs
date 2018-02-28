//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis
{
    public static class Constants
    {
        public static readonly SymbolDisplayFormat SymbolDisplayFormat = SymbolDisplayFormat.CSharpErrorMessageFormat
            .WithParameterOptions (
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeDefaultValue |
                SymbolDisplayParameterOptions.IncludeParamsRefOut)
            .WithMemberOptions (
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeContainingType |
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeRef |
                SymbolDisplayMemberOptions.IncludeExplicitInterface);
    }
}
