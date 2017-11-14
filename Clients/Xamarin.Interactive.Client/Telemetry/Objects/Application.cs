//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Client;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Telemetry.Objects
{
    sealed class Application
    {
        public static Application Instance { get; private set; }

        // Do not change this--this gets called on the main thread
        // so that Application can read the update channel property
        // ahead of serialization, which can be on any thread.
        public static void Initialize ()
            => Instance = Instance ?? new Application ();

        readonly string flavor = ClientInfo.Flavor.ToString ().ToLowerInvariant ();
        readonly string updateChannel = ClientApp.SharedInstance.Updater?.UpdateChannel;

        Application ()
        {
        }

        public void Serialize (JsonTextWriter writer)
        {
            writer.WriteStartObject ();

            writer.WritePropertyName ("flavor");
            writer.WriteValue (flavor);

            writer.WritePropertyName ("build");
            Build.Instance.Serialize (writer);

            if (updateChannel != null) {
                writer.WritePropertyName ("updateChannel");
                writer.WriteValue (updateChannel);
            }

            writer.WriteEndObject ();
        }
    }
}