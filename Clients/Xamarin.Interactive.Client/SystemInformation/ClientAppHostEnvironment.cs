//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Xamarin.Interactive.SystemInformation
{
    abstract class ClientAppHostEnvironment
    {
        readonly TaskCompletionSource<IReadOnlyList<ISoftwareEnvironment>> loadSoftwareEnvironmentsTcs
            = new TaskCompletionSource<IReadOnlyList<ISoftwareEnvironment>> ();

        public bool IsMac => OSName == HostOS.macOS;

        public abstract HostOS OSName { get; }
        public abstract string OSVersionString { get; }
        public abstract Version OSVersion { get; }

        public virtual ulong? PhysicalMemory => null;
        public virtual int? ProcessorCount => null;
        public virtual int? ActiveProcessorCount => null;

        protected ClientAppHostEnvironment (
            Func<Task<IReadOnlyList<ISoftwareEnvironment>>> loadSoftwareEnvironmentsAsync = null)
        {
            if (loadSoftwareEnvironmentsAsync == null) {
                loadSoftwareEnvironmentsTcs.SetResult (Array.Empty<ISoftwareEnvironment> ());
                return;
            }

            loadSoftwareEnvironmentsAsync ().ContinueWith (task => {
                if (task.IsFaulted)
                    loadSoftwareEnvironmentsTcs.SetException (task.Exception);
                else
                    loadSoftwareEnvironmentsTcs.SetResult (task.Result);
            });
        }

        public Task<IReadOnlyList<ISoftwareEnvironment>> GetSoftwareEnvironmentsAsync ()
            => loadSoftwareEnvironmentsTcs.Task;

        public async Task WriteJsonAsync (JsonTextWriter writer)
        {
            writer.WriteStartObject ();

            writer.WritePropertyName ("os");
            writer.WriteValue ($"{OSName} {OSVersionString}");

            writer.WritePropertyName ("ws");
            writer.WriteValue (IntPtr.Size);

            writer.WritePropertyName ("cpuws");
            writer.WriteValue (Environment.Is64BitOperatingSystem ? 8 : 4);

            if (ProcessorCount != null) {
                writer.WritePropertyName ("ncpus");
                writer.WriteValue (ProcessorCount.Value);
            }

            if (ActiveProcessorCount != null) {
                writer.WritePropertyName ("acpus");
                writer.WriteValue (ActiveProcessorCount.Value);
            }

            if (PhysicalMemory != null) {
                writer.WritePropertyName ("physmem");
                writer.WriteValue (PhysicalMemory.Value / 1_073_741_824.0);
            }

            var softwareEnvironments = await GetSoftwareEnvironmentsAsync ();
            if (softwareEnvironments != null) {
                writer.WritePropertyName ("environments");
                writer.WriteStartObject ();

                foreach (var env in softwareEnvironments) {
                    var components = env.Where (c => c.IsInstalled).ToArray ();
                    if (components.Length > 0) {
                        writer.WritePropertyName (env.Name);
                        writer.WriteStartObject ();

                        foreach (var component in components) {
                            writer.WritePropertyName (component.Name);
                            writer.WriteStartObject ();

                            writer.WritePropertyName ("version");
                            writer.WriteValue (component.Version);

                            component.SerializeExtraProperties (writer);

                            writer.WriteEndObject ();
                        }

                        writer.WriteEndObject ();
                    }
                }

                writer.WriteEndObject ();
            }

            writer.WriteEndObject ();
        }

        public override string ToString ()
        {
            var writer = new StringWriter ();
            WriteJsonAsync (new JsonTextWriter (writer) {
                Formatting = Formatting.Indented
            }).GetAwaiter ().GetResult ();
            return writer.ToString ();
        }
    }
}