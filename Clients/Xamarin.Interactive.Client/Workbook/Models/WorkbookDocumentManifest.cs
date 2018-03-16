//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using YamlDotNet.Serialization;

using NuGet.Versioning;

using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Workbook.Models
{
    sealed class WorkbookDocumentManifest
    {
        const string DefaultManifestUti = "com.xamarin.workbook";

        public OrderedDictionary Properties { get; private set; }
        public Guid Guid { get; set; }
        public string Title { get; set; }
        public ImmutableArray<AgentType> PlatformTargets { get; set; }
        public ImmutableArray<InteractivePackageDescription> Packages { get; set; }

        public WorkbookDocumentManifest ()
        {
            ReadProperties (null);
        }

        public void Write (TextWriter writer)
        {
            if (Packages.Length > 0)
                Properties.InsertOrUpdate (0, "packages", new List<object> (
                    Packages
                        .Where (package => package.IsExplicitlySelected)
                        .Select (package => new OrderedDictionary {
                            ["id"] = package.PackageId,
                            ["version"] = package.VersionRange
                        })));
            else
                Properties.Remove ("packages");

            Properties.Remove ("platform");
            if (PlatformTargets.Length > 0)
                Properties.InsertOrUpdate (0, "platforms", PlatformTargets);
            else
                Properties.Remove ("platforms");

            if (!string.IsNullOrWhiteSpace (Title))
                Properties.InsertOrUpdate (0, "title", Title.Trim ());
            else
                Properties.Remove ("title");

            Properties.InsertOrUpdate (0, "id", Guid.ToString ());
            Properties.InsertOrUpdate (0, "uti", DefaultManifestUti);

            new SerializerBuilder ().Build ().Serialize (writer, Properties);
        }

        public override string ToString ()
        {
            var writer = new StringWriter ();
            Write (writer);
            return writer.ToString ();
        }

        public void Read (WorkbookDocument document)
        {
            if (document == null)
                throw new ArgumentNullException (nameof (document));

            try {
                if (ReadYamlManifest (document) || ReadLegacyJsonManifest (document))
                    return;
            } catch (Exception e) when (!(e is InvalidManifestException)) {
                throw new InvalidManifestException (e);
            }

            throw new InvalidManifestException ();
        }

        bool ReadYamlManifest (WorkbookDocument document)
        {
            var manifestCell = document.FirstCell as YamlMetadataCell;
            if (manifestCell == null)
                return false;

            Read (new StringReader (manifestCell.Buffer.Value));

            document.RemoveCell (manifestCell);

            return true;
        }

        bool ReadLegacyJsonManifest (WorkbookDocument document)
        {
            var manifestCell = document.FirstCell as CodeCell;
            if (manifestCell?.LanguageName != "json")
                return false;

            ReadProperties (LegacyJsonWorkbookDocumentManifest.Read (manifestCell.Buffer.Value));

            document.RemoveCell (manifestCell);

            return true;
        }

        void Read (TextReader reader)
            => ReadProperties (new DeserializerBuilder ()
                .WithObjectFactory (OrderedDictionaryFactory.Instance)
                .Build ()
                .Deserialize<OrderedDictionary> (reader));

        string ReadStringProperty (string key)
        {
            object value;
            if (Properties.TryGetValue (key, out value))
                return value as string;
            return null;
        }

        void ReadProperties (OrderedDictionary properties)
        {
            var checkUti = true;

            if (properties == null) {
                properties = new OrderedDictionary ();
                checkUti = false;
            }

            Properties = properties;

            if (checkUti && !string.Equals (
                ReadStringProperty ("uti"),
                DefaultManifestUti,
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidManifestException ();

            Guid guid;
            var id = ReadStringProperty ("id");
            if (id != null && Guid.TryParse (id, out guid))
                Guid = guid;
            else
                Guid = Guid.NewGuid ();

            Title = ReadStringProperty ("title");
            PlatformTargets = ReadTargetPlatforms ().Distinct ().ToImmutableArray ();
            Packages = ReadPackages ().ToImmutableArray ();
        }

        static bool TryParseTargetPlatform (string targetPlatformName, out AgentType targetPlatform)
        {
            if (targetPlatformName == null ||
                !Enum.TryParse (targetPlatformName, true, out targetPlatform)) {
                targetPlatform = AgentType.Unknown;
                return false;
            }

            return targetPlatform != AgentType.Unknown;
        }

        IEnumerable<AgentType> ReadTargetPlatforms ()
        {
            foreach (var key in new [] { "platforms", "platform" }) {
                object value;
                if (!Properties.TryGetValue (key, out value))
                    continue;

                AgentType targetPlatform;

                if (TryParseTargetPlatform (value as string, out targetPlatform)) {
                    yield return targetPlatform;
                    continue;
                }

                var list = value as List<object>;
                for (int i = 0; list != null && i < list.Count; i++) {
                    if (TryParseTargetPlatform (list [i] as string, out targetPlatform))
                        yield return targetPlatform;
                }
            }
        }

        IEnumerable<InteractivePackageDescription> ReadPackages ()
        {
            IList list;
            if (!Properties.TryGetValueAs ("packages", out list) || list == null)
                yield break;

            foreach (var packageValue in list) {
                var package = packageValue as OrderedDictionary;
                if (package == null)
                    continue;

                string id;
                if (!package.TryGetValueAs ("id", out id))
                    continue;

                // A few VersionRange examples:
                //   1.2.3.4
                //   [1.2.3.4]
                //   1.2.*
                //   *
                //   [1.2, 1.3)
                // Read more at https://docs.microsoft.com/en-us/nuget/create-packages/dependency-versions#version-ranges
                package.TryGetValueAs ("version", out string versionRange);

                yield return new InteractivePackageDescription (id, versionRange: versionRange);
            }
        }

        sealed class OrderedDictionaryFactory : IObjectFactory
        {
            public static readonly OrderedDictionaryFactory Instance = new OrderedDictionaryFactory ();

            public object Create (Type type)
            {
                if (type == typeof (Dictionary<object, object>))
                    return new OrderedDictionary ();

                return Activator.CreateInstance (type);
            }
        }

        sealed class InvalidManifestException : Exception
        {
            public InvalidManifestException (Exception e = null)
                : base (Catalog.GetString ("The workbook does not contain a valid manifest."), e)
            {
            }
        }
    }

    [DataContract]
    /// <summary>
    /// Deprecated in 0.8.2 in favor of YAML. Do not use.
    /// </summary>
    sealed class LegacyJsonWorkbookDocumentManifest
    {
        #pragma warning disable 0649
        [DataContract]
        struct Package
        {
            [DataMember] public string id;
            [DataMember] public string version;
        }

        [DataMember] string uti;
        [DataMember] List<Package> packages;
        [DataMember] string platform;
        #pragma warning restore 0649

        public static OrderedDictionary Read (string json)
        {
            var manifest = (LegacyJsonWorkbookDocumentManifest)new DataContractJsonSerializer (
                typeof (LegacyJsonWorkbookDocumentManifest)).ReadObject (
                    new MemoryStream (Utf8.GetBytes (json)));

            var packageDictionaries = new List<object> ();
            if (manifest.packages != null) {
                foreach (var package in manifest.packages)
                    packageDictionaries.Add (new OrderedDictionary {
                        ["id"] = package.id,
                        ["version"] = package.version
                    });
            }

            return new OrderedDictionary {
                ["uti"] = manifest.uti,
                ["platform"] = manifest.platform,
                ["packages"] = packageDictionaries
            };
        }
    }
}