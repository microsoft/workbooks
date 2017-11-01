//
// PackageDotJson.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;

namespace Xamarin.MSBuild
{
	public sealed class PackageDotJson : Task
	{
		[Required]
		public string FileName { get; set; }

		[Output]
		public string Version { get; private set; }

		public override bool Execute ()
		{
			Version = PackageDotJsonData.ReadFromFile (FileName).Version;
			return true;
		}

		public sealed class PackageDotJsonData
		{
			[JsonProperty ("version")]
			public string Version { get; set; }

			public static PackageDotJsonData ReadFromFile (string path)
			{
				using (var reader = new StreamReader (path))
					return new JsonSerializer ().Deserialize<PackageDotJsonData> (
						new JsonTextReader (reader));
			}
		}
	}
}