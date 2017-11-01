//
// UpdateManifest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Xamarin.Interactive.Client.Updater
{
	[XmlRoot ("UpdateInfo")]
	public class UpdateManifest
	{
		[XmlElement ("Application")]
		public List<ApplicationNode> Applications { get; set; } = new List<ApplicationNode> ();

		public static UpdateManifest Deserialize (Stream stream)
			=> (UpdateManifest)new XmlSerializer (typeof (UpdateManifest))
				.Deserialize (new StreamReader (stream));
	}
}