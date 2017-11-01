//
// TypeScriptConfig.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;

namespace Xamarin.MSBuild
{
	public sealed class TypeScriptConfig : Task
	{
		[Required]
		public ITaskItem TSConfigFile { get; set; }

		[Output]
		public string [] Files { get; set; }

		public override bool Execute ()
		{
			var tsconfig = TypeScriptConfigData.ReadFromFile (TSConfigFile.ItemSpec);
			Files = tsconfig.Files;
			return true;
		}

		public sealed class TypeScriptConfigData
		{
			[JsonProperty ("files")]
			public string [] Files { get; set; }

			public static TypeScriptConfig ReadFromFile (string path)
			{
				using (var reader = new StreamReader (path))
					return new JsonSerializer ().Deserialize<TypeScriptConfig> (
						new JsonTextReader (reader));
			}
		}
	}
}