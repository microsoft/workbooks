//
// Publication.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.IO;

using Newtonsoft.Json;

namespace Xamarin.XamPub
{
	public sealed class Publication
	{
		[JsonProperty ("info")]
		public PublicationInfo Info { get; set; }

		[JsonProperty ("release")]
		public PublicationItem [] Release { get; set; }

		public void Write (TextWriter writer)
			=> new JsonSerializer {
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore
			}.Serialize (writer, this);
	}
}