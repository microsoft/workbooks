//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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