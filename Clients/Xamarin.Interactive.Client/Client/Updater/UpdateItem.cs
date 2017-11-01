//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Versioning;

namespace Xamarin.Interactive.Client.Updater
{
    sealed class UpdateItem
    {
        public ReleaseVersion ReleaseVersion { get; }
        public string Version { get; }
        public string ReleaseNotes { get; }
        public string Channel { get; }
        public Uri DownloadUrl { get; }
        public string Md5Hash { get; }

        public UpdateItem (
            ReleaseVersion releaseVersion,
            string version,
            string releaseNotes,
            string channel,
            Uri downloadUrl,
            string md5Hash)
        {
            if (version == null)
                throw new ArgumentNullException (nameof (version));

            if (String.IsNullOrEmpty (releaseNotes))
                throw new ArgumentNullException (nameof (releaseNotes));

            if (channel == null)
                throw new ArgumentNullException (nameof (channel));

            if (downloadUrl == null)
                throw new ArgumentNullException (nameof (downloadUrl));

            ReleaseVersion = releaseVersion;
            Version = version;
            ReleaseNotes = releaseNotes;
            Channel = channel;
            DownloadUrl = downloadUrl;
            Md5Hash = md5Hash;
        }
    }
}