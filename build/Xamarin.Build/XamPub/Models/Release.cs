//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Xamarin.XamPub.Models
{
    sealed class Release
    {
        [JsonProperty ("info")]
        [JsonRequired]
        public ReleaseInfo Info { get; set; }

        [JsonProperty ("release")]
        [JsonRequired]
        public List<ReleaseFile> ReleaseFiles { get; set; }

        public override string ToString ()
        {
            var writer = new StringWriter ();
            Serialize (writer);
            return writer.ToString ();
        }

        #region Serialization

        static readonly JsonSerializerSettings settings = new JsonSerializerSettings {
            ContractResolver = new ContractResolver (),
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        sealed class ContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty (
                MemberInfo member,
                MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty (member, memberSerialization);

                if (property.PropertyType == typeof (string))
                    property.DefaultValue = string.Empty;

                return property;
            }
        }

        public void Serialize (TextWriter writer)
        {
            var serializer = JsonSerializer.Create (settings);

            var obj = JObject.FromObject (this, serializer);

            // perform a depth-first recursive flattening of all tokens and
            // remove any properties with values that are empty containers

            IEnumerable<JToken> DepthFirst (IEnumerable<JToken> tokens)
                => tokens.SelectMany (token => DepthFirst (token.Children ()).Concat (token));

            var emptyContainers = DepthFirst (obj.Children ())
                .OfType<JProperty> ()
                .Where (p => p.Value is JContainer c && !c.HasValues)
                .ToList ();

            foreach (var prop in emptyContainers)
                prop.Remove ();

            serializer.Serialize (writer, obj);
        }

        public static Release Deserialize (TextReader reader)
            => JsonSerializer.Create (settings).Deserialize<Release> (new JsonTextReader (reader));

        #endregion
    }
}