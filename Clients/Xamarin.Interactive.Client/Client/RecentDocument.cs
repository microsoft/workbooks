//
// RecentDocument.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client
{
	struct RecentDocument : IEquatable<RecentDocument>
	{
		// FIXME: would rather this be of type FilePath, but then the
		// YAML Serializer explodes, and I cannot get it to respect
		// either this type implementing IYamlConvertible or an
		// IYamlTypeConverter registered on the serializer...
		public string Path { get; set; }

		public string Title { get; set; }

		public RecentDocument (string path, string title = null)
		{
			Path = path;
			Title = title;
		}

		public bool Equals (RecentDocument other)
			=> new FilePath (other.Path) == new FilePath (Path);

		public override bool Equals (object obj)
			=> obj is RecentDocument && Equals ((RecentDocument)obj);

		public override int GetHashCode ()
			=> Path == null ? 0 : Path.GetHashCode ();

		public override string ToString ()
			=> string.IsNullOrEmpty (Title) ? Path : $"{Path} ({Title})";
	}
}