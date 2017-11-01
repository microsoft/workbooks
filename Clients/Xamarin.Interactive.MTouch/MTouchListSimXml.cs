//
// MTouchListSimXml.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System.Xml.Serialization;

namespace Xamarin.Interactive.MTouch
{
    // Classes for deserializing from the XML output of `mtouch --listsim`
    [XmlRoot ("MTouch")]
    public class MTouchListSimXml
    {
        public class SimulatorElement
        {
            [XmlArrayItem ("SimRuntime")]
            public SimRuntimeElement [] SupportedRuntimes { get; set; }

            [XmlArrayItem ("SimDeviceType")]
            public SimDeviceTypeElement [] SupportedDeviceTypes { get; set; }

            [XmlArrayItem ("SimDevice")]
            public SimDeviceElement [] AvailableDevices { get; set; }
        }

        public class SimRuntimeElement
        {
            public string Name { get; set; }

            public string Identifier { get; set; }
        }

        public class SimDeviceTypeElement
        {
            public string Name { get; set; }

            public string Identifier { get; set; }

            public string ProductFamilyId { get; set; }

            public bool Supports64Bits { get; set; }
        }

        public class SimDeviceElement
        {
            [XmlAttribute]
            public string UDID { get; set; }

            [XmlAttribute]
            public string Name { get; set; }

            public string SimRuntime { get; set; }

            public string SimDeviceType { get; set; }
        }

        public string SdkRoot { get; set; }

        [XmlElement ("Simulator")]
        public SimulatorElement Simulator { get; set; }
    }
}

