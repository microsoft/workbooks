//
// FindReplaceInFiles.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.IO;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
	public sealed class FindReplaceInFiles : Task
	{
		static readonly Encoding encoding = new UTF8Encoding (true);

		[Required]
		public ITaskItem [] Files { get; set; }

		[Required]
		public ITaskItem [] Replacements { get; set; }

		public override bool Execute ()
		{
			foreach (var file in Files) {
				var contents = File.ReadAllText (file.ItemSpec);

				foreach (var repl in Replacements)
					contents = contents.Replace (repl.ItemSpec, repl.GetMetadata ("Value"));

				using (var writer = new StreamWriter (file.ItemSpec, false, encoding)) {
					writer.Write (contents);
					writer.Flush ();
				}
			}

			return true;
		}
	}
}