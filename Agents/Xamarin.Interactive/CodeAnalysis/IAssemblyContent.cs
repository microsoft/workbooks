//
// IAssemblyContent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.IO;

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IAssemblyContent
    {
        Stream OpenPEImage ();
    }
}