//
// AssemblyContent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
	[Serializable]
	sealed class AssemblyContent : IAssemblyContent
	{
		public FilePath Location { get; }
		public byte [] PEImage { get; }
		public byte [] DebugSymbols { get; }

		public AssemblyContent (FilePath location, byte [] peImage, byte [] debugSymbols)
		{
			Location = location;
			PEImage = peImage;
			DebugSymbols = debugSymbols;
		}

		Stream IAssemblyContent.OpenPEImage ()
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