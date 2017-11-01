//
// UpdateNode.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Xamarin.Interactive.Client.Updater
{
	public sealed class UpdateNode : IXmlSerializable
	{
		public bool IsValid => Version != null && VersionId > 0 && Url != null && Size > 0;

		public string Id { get; set; }
		public string Level { get; set; }
		public string Version { get; set; }
		public long VersionId { get; set; }
		public DateTime Date { get; set; }
		public Uri Url { get; set; }
		public string Hash { get; set; }
		public long Size { get; set; }
		public bool Restart { get; set; }
		public bool Interactive { get; set; }
		public string ReleaseNotes { get; set; }

		XmlSchema IXmlSerializable.GetSchema () => null;

		void IXmlSerializable.ReadXml (XmlReader reader)
		{
			reader.MoveToContent ();

			Id = reader.GetAttribute ("id");
			Level = reader.GetAttribute ("level");
			Version = reader.GetAttribute ("version");

			long versionId;
			long.TryParse (reader.GetAttribute ("versionId"), out versionId);
			VersionId = versionId;

			try {
				Date = XmlConvert.ToDateTime (
					reader.GetAttribute ("date"),
					XmlDateTimeSerializationMode.RoundtripKind);
			} catch {
				Date = DateTime.MinValue;
			}

			try {
				Url = new Uri (reader.GetAttribute ("url"));
			} catch {
				Url = null;
			}

			Hash = reader.GetAttribute ("hash");

			long size;
			long.TryParse (reader.GetAttribute ("size"), out size);
			Size = size;

			Restart = String.Equals (
				reader.GetAttribute ("restart"),
				"true",
				StringComparison.InvariantCultureIgnoreCase);

			Interactive = String.Equals (
				reader.GetAttribute ("interactive"),
				"true",
				StringComparison.InvariantCultureIgnoreCase);

			ReleaseNotes = reader.Value;

			var isEmptyElement = reader.IsEmptyElement;

			reader.ReadStartElement ();

			if (isEmptyElement) {
				ReleaseNotes = null;
			} else {
				ReleaseNotes = reader.ReadContentAsString ();
				reader.ReadEndElement ();
			}
		}

		void IXmlSerializable.WriteXml (XmlWriter writer)
		{
			throw new NotImplementedException ();
		}
	}
}