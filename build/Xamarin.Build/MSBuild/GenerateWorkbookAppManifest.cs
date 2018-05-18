//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xamarin.MSBuild
{
    public sealed class GenerateWorkbookAppManifest : Task
    {
        [Required]
        public string Flavor { get; set; }

        public string Icon { get; set; }

        public string AppPath { get; set; }

        public string AppManagerAssembly { get; set; }
        public string AgentTicketAssembly { get; set; }

        public string [] AssemblySearchPaths { get; set; }

        public string [] OptionalFeatures { get; set; }

        public string SdkName { get; set; }

        public string SdkProfile { get; set; }

        public string SdkVersion { get; set; }

        public int Order { get; set; }

        [Required]
        public string TargetFrameworkIdentifier { get; set; }

        [Required]
        public string TargetFrameworkVersion { get; set; }

        [Required]
        public string ManifestOutputPath { get; set;  }

        public override bool Execute ()
        {
            string Pathify (string path)
                => string.IsNullOrEmpty (path) ? null : Regex.Replace (
                    path,
                    @"[\/\\]+",
                    Path.DirectorySeparatorChar.ToString ());

            string sdkVersion = null;
            if (SdkVersion != null) {
                if (SdkVersion.StartsWith ("@", StringComparison.Ordinal)) {
                    var parts = SdkVersion.Substring (1).Split (new [] { ',' }, 2);
                    switch (parts [0]) {
                    case "GlobalJsonSdkVersion":
                        sdkVersion = ReadGlobalJsonSdkVersion (Pathify (parts [1]));
                        break;
                    case "AssemblyInformationalVersion":
                        sdkVersion = AssemblyInformationalVersion (parts [1]);
                        break;
                    default:
                        throw new NotImplementedException ($"Unable to handle SdkVersion style @{parts [0]}");
                    }

                    if (sdkVersion == null)
                        return false;
                }
            }

            string targetFramework = null;
            TargetFrameworkVersion = TargetFrameworkVersion?.Trim ().TrimStart ('v');
            if (TargetFrameworkIdentifier != null && TargetFrameworkVersion != null)
                targetFramework = $"{TargetFrameworkIdentifier},Version={TargetFrameworkVersion}";

            var idParts = new List<string> (3) { Flavor };
            if (!string.IsNullOrEmpty (SdkName))
                idParts.Add (SdkName);
            if (!string.IsNullOrEmpty (SdkProfile))
                idParts.Add (SdkProfile);
            var id = string
                .Join ("-", idParts.Select (p => Regex.Replace (p, @"[^A-Za-z0-9]", "")))
                .ToLowerInvariant ();

            var manifest = new JObject {
                [id] = JObject.FromObject (new {
                    flavor = Flavor,
                    order = Order,
                    icon = Icon,
                    appPath = Pathify (AppPath),
                    appManagerAssembly = Pathify (AppManagerAssembly),
                    agentTicketAssembly = Pathify (AgentTicketAssembly),
                    sdk = new {
                        name = SdkName,
                        profile = SdkProfile,
                        version = sdkVersion,
                        targetFramework,
                        assemblySearchPaths = AssemblySearchPaths?.Select (Pathify)
                    },
                    optionalFeatures = OptionalFeatures
                })
            };

            var toRemove = manifest
                .Descendants ()
                .Where (value => value.Type == JTokenType.Null)
                .Select (value => value.Parent is JProperty property ? property : value)
                .ToArray ();

            foreach (var nullToken in toRemove)
                nullToken.Remove ();

            Directory.CreateDirectory (Path.GetDirectoryName (ManifestOutputPath));

            var workbooks = File.Exists (ManifestOutputPath)
                ? JObject.Parse (File.ReadAllText (ManifestOutputPath))
                : new JObject ();

            workbooks.Merge (manifest, new JsonMergeSettings {
                MergeArrayHandling = MergeArrayHandling.Union
            });

            Log.LogMessage (
                MessageImportance.Normal,
                $"Writing manifest entry ({id}): {ManifestOutputPath}");

            File.WriteAllText (
                ManifestOutputPath,
                workbooks.ToString (Formatting.Indented));

            return true;
        }

        string AssemblyInformationalVersion (string assemblyFile)
        {
            var fileVersion = FileVersionInfo.GetVersionInfo (assemblyFile);
            var version = fileVersion.ProductVersion;
            if (string.IsNullOrEmpty (version))
                version = fileVersion.FileVersion;

            return version
                ?.Split (';')
                .Select (v => v.Trim ())
                .FirstOrDefault (v => v.Length > 0 && char.IsDigit (v [0]));
        }

        string ReadGlobalJsonSdkVersion (string globalJsonFile)
        {
            if (!File.Exists (globalJsonFile)) {
                Log.LogError ($"File does not exist: {globalJsonFile}");
                return null;
            }

            try {
                string version = null;

                var obj = JObject.Parse (File.ReadAllText (globalJsonFile));
                if (obj.TryGetValue ("sdk", out var sdkToken) &&
                    sdkToken is JObject sdk &&
                    sdk.TryGetValue ("version", out var versionToken))
                    version = versionToken.Value<string> ();

                if (string.IsNullOrEmpty (version)) {
                    Log.LogError ($"sdk.version does not exist as a string in {globalJsonFile}");
                    return null;
                }

                return version;
            } catch {
                Log.LogError ($"Unable to parse file as JSON: {globalJsonFile}");
                return null;
            }
        }
    }
}