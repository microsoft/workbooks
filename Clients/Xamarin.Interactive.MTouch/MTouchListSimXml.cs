//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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

        public class SimDeviceElement : IComparable<SimDeviceElement>
        {
            [XmlAttribute]
            public string UDID { get; set; }

            [XmlAttribute]
            public string Name { get; set; }

            public string SimRuntime { get; set; }

            public string SimDeviceType { get; set; }

            public int CompareTo (SimDeviceElement other)
            {
                if (other == null)
                    return -1;

                // NOTE: This is copied from IPhoneSimulatorExecutionTargetGroup in VSmac.
                //
                // Treating iPhone X (ten) as iPhone 10 makes it so 'iPhone 7 < iPhone 8 < iPhone X'
                var name = Name.Replace ("X", "10");
                var otherName = other.Name.Replace ("X", "10");
                return string.Compare (name, otherName, StringComparison.OrdinalIgnoreCase);
            }
        }

        public string SdkRoot { get; set; }

        [XmlElement ("Simulator")]
        public SimulatorElement Simulator { get; set; }
    }
}
