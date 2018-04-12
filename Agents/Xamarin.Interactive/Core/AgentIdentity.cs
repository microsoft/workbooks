//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using Newtonsoft.Json;

using Xamarin.Versioning;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Core
{
    [JsonObject]
    sealed class AgentIdentity
    {
        const string needToObsoleteAgentType =
            "if you need this, it's really time to revisit obsoleting AgentType itself";

        [Obsolete (needToObsoleteAgentType)]
        internal static string GetFlavorId (AgentType agentType)
        {
            switch (agentType) {
            case AgentType.iOS:
                return "ios-xamarinios";
            case AgentType.MacNet45:
                return "mac-xamarinmac-full";
            case AgentType.MacMobile:
                return "mac-xamarinmac-modern";
            case AgentType.Android:
                return "android-xamarinandroid";
            case AgentType.WPF:
                return "wpf";
            case AgentType.Console:
                return "console";
            case AgentType.DotNetCore:
                return "console-netcore";
            default:
                return null;
            }
        }

        [Obsolete (needToObsoleteAgentType)]
        internal static AgentType GetAgentType (string flavorId)
        {
            switch (flavorId) {
            case "ios-xamarinios":
                return AgentType.iOS;
            case "mac-xamarinmac-full":
                return AgentType.MacNet45;
            case "mac-xamarinmac-modern":
                return AgentType.MacMobile;
            case "android-xamarinandroid":
                return AgentType.Android;
            case "wpf":
                return AgentType.WPF;
            case "console":
                return AgentType.Console;
            case "console-netcore":
                return AgentType.DotNetCore;
            default:
                return AgentType.Unknown;
            }
        }

        #pragma warning disable 0618
        public string FlavorId => GetFlavorId (AgentType);
        #pragma warning restore 0618

        public Guid Id { get; }
        public AgentType AgentType { get; }
        public Sdk Sdk { get; }
        public ReleaseVersion AgentVersion { get; }
        public string ApplicationName { get; }
        public string Host { get; }
        public ushort Port { get; }
        public string DeviceManufacturer { get; }
        public int ScreenWidth { get; }
        public int ScreenHeight { get; }

        [JsonConstructor]
        AgentIdentity (
            Guid id,
            AgentType agentType,
            Sdk sdk,
            string applicationName,
            string host,
            ushort port,
            string deviceManufacturer,
            int screenWidth,
            int screenHeight)
        {
            Id = id;
            AgentType = agentType;
            Sdk = sdk;
            AgentVersion = BuildInfo.Version;
            ApplicationName = applicationName;
            Host = host;
            Port = port;
            DeviceManufacturer = deviceManufacturer;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
        }

        public AgentIdentity (
            AgentType agentType,
            Sdk sdk,
            string applicationName,
            string deviceManufacturer = null,
            int screenWidth = 0,
            int screenHeight = 0) : this (
                Guid.NewGuid (),
                agentType,
                sdk,
                applicationName,
                null,
                0,
                deviceManufacturer,
                screenWidth,
                screenHeight)
        {
        }

        public AgentIdentity WithHost (string host)
        {
            if (Host == host)
                return this;

            return new AgentIdentity (
                Id,
                AgentType,
                Sdk,
                ApplicationName,
                host,
                Port,
                DeviceManufacturer,
                ScreenWidth,
                ScreenHeight);
        }


        public AgentIdentity WithPort (ushort port)
        {
            if (Port == port)
                return this;

            return new AgentIdentity (
                Id,
                AgentType,
                Sdk,
                ApplicationName,
                Host,
                port,
                DeviceManufacturer,
                ScreenWidth,
                ScreenHeight);
        }
    }
}