//
// CommonMarkToHtml.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using CommonMark;

namespace Xamarin.MSBuild
{
	public sealed class CommonMarkToHtml : Task
	{
		[Required]
		public string CommonMarkFile { get; set; }

		public string HtmlTemplateFile { get; set; }

		[Required]
		public string HtmlOutputFile { get; set; }

		public override bool Execute ()
		{
			var html = CommonMarkConverter.Convert (File.ReadAllText (CommonMarkFile));

			if (!string.IsNullOrEmpty (HtmlTemplateFile))
				html = File.ReadAllText (HtmlTemplateFile).Replace ("@COMMONMARK@", html);

			Directory.CreateDirectory (Path.GetDirectoryName (HtmlOutputFile));

			File.WriteAllText (HtmlOutputFile, html);

			return true;
		}
	}
}