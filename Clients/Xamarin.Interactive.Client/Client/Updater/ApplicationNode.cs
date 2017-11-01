//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Xamarin.Interactive.Client.Updater
{
    public sealed class ApplicationNode
    {
        [XmlAttribute ("name")]
        public string Name { get; set; }

        [XmlAttribute ("id")]
        public string Id { get; set; }

        [XmlElement ("Update")]
        public List<UpdateNode> Updates { get; set; } = new List<UpdateNode> ();
    }
}