//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommonMark;
using CommonMark.Formatters;
using CommonMark.Syntax;

using Xamarin.Interactive.Markdown;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client
{
    sealed class IssueReport
    {
        readonly ClientAppHostEnvironment host;

        public IssueReport (ClientAppHostEnvironment host)
            => this.host = host ?? throw new ArgumentNullException (nameof (host));

        public string GetIssueReportUrlForGitHub (bool includeReport = true)
        {
            var builder = new System.Text.StringBuilder ();
            builder.Append ("https://github.com/Microsoft/workbooks/issues/new");
            if (includeReport)
                builder.Append ("?body=").Append (System.Net.WebUtility.UrlEncode (GetIssueReportForGitHub ()));
            return builder.ToString ();
        }

        public string GetIssueReportForGitHub ()
        {
            var writer = new StringWriter ();
            WriteIssueReportForGitHub (writer);
            return writer.ToString ().TrimEnd ();
        }

        public void WriteIssueReportForGitHub (TextWriter writer)
        {
            var environment = GetEnvironmentMarkdown ();

            using (var stream = typeof (ClientApp).Assembly.GetManifestResourceStream ("ISSUE_TEMPLATE.md")) {
                var reader = new StreamReader (stream);
                var template = CommonMarkConverter.Parse (reader);
                var block = template.FirstChild;
                while (block != null) {
                    // Find the following structure in the markdown template and replace the
                    // HTML comment with our fancy environment markdown tables:
                    //    ### Environment
                    //    <!-- ... --->
                    // (The heading level is intentionally ignored in the match)
                    if (block.Tag == BlockTag.AtxHeading &&
                        block.InlineContent?.LiteralContent?.Trim () == "Environment" &&
                        block.NextSibling?.Tag == BlockTag.HtmlBlock) {
                        block.NextSibling.StringContent.Replace (environment, 0, environment.Length);
                        break;
                    }

                    block = block.NextSibling;
                }

                new MarkdownFormatter (writer).WriteBlock (template.Top);
            }
        }

        public string GetEnvironmentMarkdown (bool padTableCells = false)
        {
            var writer = new StringWriter ();
            WriteEnvironmentMarkdownAsync (writer, padTableCells).GetAwaiter ().GetResult ();
            return writer.ToString ().TrimEnd ();
        }

        public async Task WriteEnvironmentMarkdownAsync (TextWriter writer, bool padTableCells = false)
        {
            var osArch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

            new MarkdownTable (ClientInfo.FullProductName, "Detail", "Value") {
                { "Version", BuildInfo.Version.ToString () },
                { "Git Branch", BuildInfo.Branch },
                { "Git Hash", BuildInfo.HashShort },
                { "VSTS Definition", BuildInfo.BuildHostLane }
            }.Render (writer, padTableCells);

            writer.WriteLine ();

            string cores = null;

            if (host.ActiveProcessorCount != null)
                cores = $"{host.ActiveProcessorCount}";

            if (host.ProcessorCount != null) {
                if (cores != null)
                    cores += " / ";
                cores += $"{host.ProcessorCount}";
            }

            cores = cores ?? "_Unknown_";

            string memory = null;

            if (host.PhysicalMemory != null)
                memory = $"{host.PhysicalMemory.Value / 1_073_741_824.0:N0} GB";

            memory = memory ?? "_Unknown_";

            new MarkdownTable ("System Info", "Component", "Value") {
                { $"{host.OSName}", $"{host.OSVersionString} ({osArch})" },
                { "CPU Cores", cores },
                { "Physical Memory", memory }
            }.Render (writer, padTableCells);

            var softwareEnvironments = await host.GetSoftwareEnvironmentsAsync ();
            if (softwareEnvironments == null)
                return;

            foreach (var environment in softwareEnvironments) {
                var name = environment is SystemSoftwareEnvironment
                    ? "System-Installed Software"
                    : $"{environment.Name} Components";

                MarkdownTable table = null;

                foreach (var component in environment.Where (c => c.IsInstalled)) {
                    if (table == null)
                        table = new MarkdownTable (name, "Component", "Version");
                    table.Add (component.Name, component.Version);
                }

                if (table != null) {
                    writer.WriteLine ();
                    table.Render (writer, padTableCells);
                }
            }
        }
    }
}