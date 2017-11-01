//
// PublicationInfo.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Xamarin.XamPub
{
	public sealed class PublicationInfo
	{
		[JsonProperty ("name")]
		public string Name { get; set; }
	}
}