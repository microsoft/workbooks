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

using Xamarin.Interactive.Markdown;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client
{
    sealed class IssueReport
    {
        readonly HostEnvironment host;

        public IssueReport (HostEnvironment host)
            => this.host = host ?? throw new ArgumentNullException (nameof (host));

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