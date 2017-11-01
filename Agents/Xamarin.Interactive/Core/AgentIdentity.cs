//
// AgentIdentity.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.IO;

using Xamarin.Versioning;

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	sealed class AgentIdentity
	{
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

		public string SerializeToString ()
		{
			var stream = new MemoryStream ();
			new XipSerializer (stream).Serialize (this);
			return Convert.ToBase64String (stream.ToArray ());
		}

		public static AgentIdentity Deserialize (string serializedIdentity)
		{
			var identity = Convert.FromBase64String (serializedIdentity);
			return new XipSerializer (new MemoryStream (identity)).Deserialize () as AgentIdentity;
		}
	}
}