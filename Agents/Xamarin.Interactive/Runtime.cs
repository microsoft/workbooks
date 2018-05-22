// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

using Newtonsoft.Json;

namespace Xamarin.Interactive
{
    using static Architecture;

    [JsonObject]
    public struct Runtime : IEquatable<Runtime>
    {
        static OSPlatform GetOSPlatform ()
        {
            if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
                return OSPlatform.Windows;
            else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
                return OSPlatform.OSX;
            else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
                return OSPlatform.Linux;
            return default (OSPlatform);
        }

        public static Runtime CurrentProcessRuntime { get; } = new Runtime (
            GetOSPlatform (),
            RuntimeInformation.ProcessArchitecture,
            null);

        public OSPlatform OSPlatform { get; }
        public Architecture? Architecture { get; }
        public string RuntimeIdentifier { get; }

        [JsonConstructor]
        public Runtime (
            OSPlatform osPlatform,
            Architecture? architecture = null,
            string runtimeIdentifier = null)
        {
            OSPlatform = osPlatform;
            Architecture = architecture;

            RuntimeIdentifier = runtimeIdentifier;
            if (RuntimeIdentifier == null)
                RuntimeIdentifier = BuildRuntimeIdentifier ();
        }

        public Runtime WithRuntimeIdentifier (string runtimeIdentifier)
            => new Runtime (
                OSPlatform,
                Architecture,
                runtimeIdentifier);

        public bool Equals (Runtime other)
            => other.OSPlatform == OSPlatform &&
                other.Architecture == Architecture &&
                other.RuntimeIdentifier == RuntimeIdentifier;

        public override bool Equals (object obj)
            => obj is Runtime runtime && Equals (runtime);

        public override int GetHashCode ()
            => Hash.Combine (
                OSPlatform.GetHashCode (),
                Architecture == null ? 0 : Architecture.GetHashCode (),
                RuntimeIdentifier == null ? 0 : RuntimeIdentifier.GetHashCode ());

        public override string ToString ()
            => RuntimeIdentifier;

        string BuildRuntimeIdentifier ()
        {
            string rid;

            if (OSPlatform == OSPlatform.Windows)
                rid = "win";
            else if (OSPlatform == OSPlatform.OSX)
                rid = "osx";
            else if (OSPlatform == OSPlatform.Linux)
                rid = "linux";
            else
                rid = OSPlatform.ToString ().ToLowerInvariant ();

            if (Architecture == null)
                return rid;

            switch (Architecture.Value) {
            case X86:
                rid += "-x86";
                break;
            case X64:
                rid += "-x64";
                break;
            case Arm:
                rid += "-arm";
                break;
            case Arm64:
                rid += "-arm64";
                break;
            default:
                rid += "-" + Architecture.Value.ToString ().ToLowerInvariant ();
                break;
            }

            return rid;
        }
    }
}