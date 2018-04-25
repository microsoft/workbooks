// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [JsonObject]
    public sealed class AssemblyContent
    {
        public FilePath Location { get; }
        public byte [] PEImage { get; }
        public byte [] DebugSymbols { get; }

        [JsonConstructor]
        internal AssemblyContent (FilePath location, byte [] peImage, byte [] debugSymbols)
        {
            Location = location;
            PEImage = peImage;
            DebugSymbols = debugSymbols;
        }

        public Stream OpenPEImage ()
        {
            if (PEImage != null)
                return new MemoryStream (
                    PEImage,
                    0,
                    PEImage.Length,
                    writable: false,
                    publiclyVisible: false);

            if (Location.FileExists)
                return File.OpenRead (Location);

            throw new IOException ("No image could be resolved for this assembly");
        }
    }
}