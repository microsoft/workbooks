//
// ReleaseThreadTool.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Mono.Options;

using Newtonsoft.Json;

namespace Xamarin.CITooling
{
    class ReleaseThreadTool
    {
        static readonly Dictionary<string, string []> profiles = new Dictionary<string, string []> {
            {
                "interactive",
                new [] {
                    "--relative-public-base-path", "interactive",
                    "--lane", "inspector-mac-master",
                    "--lane", "inspector-windows-master",
                    "--release-title", "XamarinInteractive",
                    // "--tag-github-repo", "xamarin/inspector"
                }
            }
        };

        bool verify;

        string commitHash;

        string tagGithubRepo;
        string tagName;
        string tagMessage;

        bool isAlpha;
        bool isBeta;
        bool isStable;

        bool showEula;
        bool requiresInteractiveInstall;
        bool requiresRestart;

        string releaseTitle;
        string releaseNotes;

        string relativePublicBasePath;

        List<string> lanes = new List<string> ();

        static string GitRevDereference (string rev)
        {
            var proc = Process.Start (new ProcessStartInfo {
                FileName = "git",
                Arguments = $"rev-list -n 1 '{rev}'",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            proc.WaitForExit ();

            if (proc.ExitCode != 0)
                throw new Exception ($"invalid revision: {rev}");

            return proc.StandardOutput.ReadToEnd ().Trim ();
        }

        public static int Run (string [] args)
        {
            var tool = new ReleaseThreadTool { verify = true };

            bool showHelp = false;
            bool verbose = false;

            OptionSet options = null;

            options = new OptionSet {
                { "h|help", "Show this help",
                    v => showHelp = true },

                { "v|verbose", "Show full exceptions on error",
                    v => verbose = true },

                { "no-verify", "Skip hashing and verify downloaded items (will omit a sha256 property from ingestion object)",
                    v => tool.verify = false },

                { "p|profile=", $"Use a profile to prefill options (available: {String.Join (", ", profiles.Keys)})",
                    v => {
                        string [] profileArgs;
                        if (!profiles.TryGetValue (v, out profileArgs)) {
                            Console.Error.WriteLine ("error: unknown profile `{0}`", v);
                            Console.Error.WriteLine ();
                            showHelp = true;
                        } else
                            options.Parse (profileArgs);
                    } },

                { "c|commit-hash=", "Commit hash to use for lane queries (defaults to HEAD dereference)",
                    v => tool.commitHash = v },

                { "relative-public-base-path=", "Relative public base path on the download server (e.g. interactive)",
                    v => tool.relativePublicBasePath = v },

                { "lane=", "Add a Wrench lane to scan",
                    v => tool.lanes.Add (v) },

                { "publish-all", "Publish to all channels (alpha, beta, stable)",
                    v => tool.isAlpha = tool.isBeta = tool.isStable = true },

                { "publish-alpha", "Publish to Alpha channel",
                    v => tool.isAlpha = true },

                { "publish-beta", "Publish to Beta channel",
                    v => tool.isBeta = true },

                { "publish-stable", "Publish to Stable channel",
                    v => tool.isStable = true },

                { "installer-show-eula", "Show EULA when installing update",
                    v => tool.showEula = true },

                { "installer-interactive", "Run installer interactively when applying update",
                    v => tool.requiresInteractiveInstall = true },

                { "installer-requires-restart", "Restart after installing",
                    v => tool.requiresRestart = true },

                { "release-notes=", "Small release notes blurb (plain text)",
                    v => tool.releaseNotes = v },

                { "release-title=", "Title for info.release in the JSON",
                    v => tool.releaseTitle = v },

                { "tag-github-repo=", "GitHub Repository for tagging release (e.g. xamarin/inspector)",
                    v => tool.tagGithubRepo = v },

                { "tag-name=", "Release tag name)",
                    v => tool.tagName = v },

                { "tag-message=", "Release tag message)",
                    v => tool.tagMessage = v }
            };

            options.Parse (args);

            if (showHelp) {
                Console.WriteLine ("Usage: xcit rt [OPTIONS] COMMIT");
                Console.WriteLine ();
                Console.WriteLine ("Options");
                Console.WriteLine ();
                options.WriteOptionDescriptions (Console.Out);
                return 1;
            }

            if (tool.lanes.Count == 0) {
                Console.Error.WriteLine ("At least one --lane must be specified.");
                return 3;
            }

            if (String.IsNullOrEmpty (tool.commitHash))
                tool.commitHash = "HEAD";

            tool.commitHash = GitRevDereference (tool.commitHash);

            try {
                tool.Run ().Wait ();
                return 0;
            } catch (Exception e) {
                var message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
                Console.Error.WriteLine ("error: {0}", message);

                if (verbose) {
                    Console.Error.WriteLine ();
                    Console.Error.WriteLine (e);
                }

                return 99;
            }
        }

        [JsonObject]
        class ReleaseInfo
        {
            [JsonProperty ("name")]
            public string Name { get; set; }
        }

        [JsonObject]
        class ReleaseDescription
        {
            [JsonProperty ("info")]
            public ReleaseInfo Info { get; set; }

            [JsonProperty ("release")]
            public object [] Items { get; set; }
        }

        async Task Run ()
        {
            var items = await WrenchIngestionItemPopulator.PopulateFromLanes (
                commitHash,
                lanes.ToArray (),
                isStable,
                verify: verify);

            string version = null;

            foreach (var item in items) {
                if (!String.IsNullOrEmpty (relativePublicBasePath)) {
                    if (item.RelativePublishUrl != null)
                        item.RelativePublishUrl = new Uri (
                            $"{relativePublicBasePath}/{item.RelativePublishUrl}",
                            UriKind.Relative);

                    if (item.RelativePublishEvergreenUrl != null)
                        item.RelativePublishEvergreenUrl = new Uri (
                            $"{relativePublicBasePath}/{item.RelativePublishEvergreenUrl}",
                            UriKind.Relative);
                }

                if (item.UpdaterProduct != null) {
                    version = item.UpdaterProduct.Version;

                    if (isAlpha || isBeta || isStable) {
                        item.UpdaterProduct.IsAlpha = isAlpha;
                        item.UpdaterProduct.IsBeta = isBeta;
                        item.UpdaterProduct.IsStable = isStable;
                        item.UpdaterProduct.RequriesRestart = requiresRestart;
                        item.UpdaterProduct.RequiresInteractiveInstall = requiresInteractiveInstall;
                        item.UpdaterProduct.ShowEula = showEula;
                        item.UpdaterProduct.ReleaseNotes = releaseNotes;
                    } else
                        item.UpdaterProduct = null;
                }
            }

            var allItems = new List<object> (items);

            if (!String.IsNullOrEmpty (tagGithubRepo)) {
                if (String.IsNullOrEmpty (tagName))
                    tagName = $"{relativePublicBasePath}-{version}-release";

                if (String.IsNullOrEmpty (tagMessage)) {
                    var name = Char.ToUpper (relativePublicBasePath [0]) +
                        relativePublicBasePath.Substring (1);
                    tagMessage = $"{name} {version} Release";
                }

                allItems.Add (new TagItem {
                    GitHubRepo = tagGithubRepo,
                    Tags = new [] { tagName },
                    CommitHash = commitHash,
                    Message = tagMessage
                });
            }

            if (!String.IsNullOrEmpty (releaseTitle) && version != null)
                releaseTitle = $"{releaseTitle}-{version}";

            new JsonSerializer {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            }.Serialize (Console.Out, new ReleaseDescription {
                Info = new ReleaseInfo { Name = releaseTitle },
                Items = allItems.ToArray ()
            });
        }
    }
}