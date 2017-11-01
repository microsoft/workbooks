//
// UpdaterProduct.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Newtonsoft.Json;

namespace Xamarin.XamPub
{
	[JsonObject]
	public sealed class UpdaterProduct
	{
		[JsonProperty ("releaseId")]
		public string ReleaseId { get; set; }

		[JsonProperty ("productGuid")]
		public Guid ProductGuid { get; set; }

		[JsonProperty ("size")]
		public long Size { get; set; }

		[JsonProperty ("version")]
		public string Version { get; set; }

		[JsonProperty ("isMajorVersion")]
		public bool IsMajorVersion { get; set; }

		[JsonProperty ("releaseNotes")]
		public string ReleaseNotes { get; set; }

		[JsonProperty ("requiresInteractiveInstall")]
		public bool RequiresInteractiveInstall { get; set; }

		[JsonProperty ("requiresRestart")]
		public bool RequriesRestart { get; set; }

		[JsonProperty ("showEula")]
		public bool ShowEula { get; set; }

		[JsonProperty ("isAlpha")]
		public bool IsAlpha { get; set; }

		[JsonProperty ("isBeta")]
		public bool IsBeta { get; set; }

		[JsonProperty ("isStable")]
		public bool IsStable { get; set; }

		public static UpdaterProduct FromUpdateInfo (string contents)
		{
			if (String.IsNullOrEmpty (contents))
				return null;

			var parts = contents.Split (
				new [] { ' ', '\t', '\n', '\r' },
				StringSplitOptions.RemoveEmptyEntries);

			if (parts == null || parts.Length != 2)
				return null;

			return new UpdaterProduct {
				ProductGuid = Guid.Parse (parts [0]),
				ReleaseId = parts [1]
			};
		}
	}
}