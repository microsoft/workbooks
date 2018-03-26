//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Security.Cryptography;

using Newtonsoft.Json;

namespace Xamarin.XamPub.Models
{
    abstract class FileBase
    {
        [JsonProperty ("sourceUri")]
        [JsonRequired]
        public string SourceUri { get; set; }

        [JsonProperty ("md5")]
        [JsonRequired]
        public string Md5 { get; set; }

        [JsonProperty ("sha256")]
        [JsonRequired]
        public string Sha256 { get; set; }

        [JsonProperty ("size")]
        public long Size { get; set; }

        public void PopulateFromFile (string path)
        {
            var hashes = new HashAlgorithm [2];
            using (var md5 = hashes [0] = MD5.Create ())
            using (var sha256 = hashes [1] = SHA256.Create ())
            using (var stream = File.OpenRead (path)) {
                var buffer = new byte [16 * 1024];
                long totalRead = 0;
                int read;

                while ((read = stream.Read (buffer, 0, buffer.Length)) != 0) {
                    totalRead += read;
                    foreach (var hash in hashes)
                        hash.TransformBlock (buffer, 0, read, buffer, 0);
                }

                foreach (var hash in hashes)
                    hash.TransformFinalBlock (buffer, 0, 0);

                var expectedSize = new FileInfo (path).Length;
                if (totalRead != expectedSize)
                    throw new IOException ($"expected {expectedSize} bytes but read {totalRead}");

                Md5 = ToHexString (md5.Hash);
                Sha256 = ToHexString (sha256.Hash);
                Size = totalRead;
            }

            string ToHexString (byte [] bytes)
            {
                var sb = new System.Text.StringBuilder (bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++)
                    sb.AppendFormat ("{0:x2}", bytes [i]);
                return sb.ToString ();
            }
        }
    }
}