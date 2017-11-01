//
// PublicationItem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Xamarin.XamPub
{
    [JsonObject]
    public sealed class PublicationItem
    {
        [JsonProperty ("file")]
        public Uri IngestionUri { get; set; }

        [JsonProperty ("size")]
        public long Size { get; set; }

        [JsonProperty ("md5")]
        public string Md5 { get; set; }

        [JsonProperty ("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty ("publishUrl")]
        public Uri RelativePublishUrl { get; set; }

        [JsonProperty ("evergreen")]
        public Uri RelativePublishEvergreenUrl { get; set; }

        [JsonProperty ("updaterProduct")]
        public UpdaterProduct UpdaterProduct { get; set; }

        public static PublicationItem CreateFromFile (string path)
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

                return new PublicationItem {
                    Md5 = ToHexString (md5.Hash),
                    Sha256 = ToHexString (sha256.Hash),
                    Size = totalRead
                };
            }
        }

        static string ToHexString (byte [] bytes)
        {
            var sb = new StringBuilder (bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++)
                sb.AppendFormat ("{0:x2}", bytes [i]);
            return sb.ToString ();
        }

        public static async Task<PublicationItem []> CreateFromPublishedManifestsAsync (params Uri [] uris)
        {
            var items = new List<PublicationItem> ();
            var client = new HttpClient ();
            foreach (var uri in uris)
                items.AddRange (JsonConvert.DeserializeObject<PublicationItem []> (
                    await client.GetStringAsync (uri)));
            return items.ToArray ();
        }
    }
}