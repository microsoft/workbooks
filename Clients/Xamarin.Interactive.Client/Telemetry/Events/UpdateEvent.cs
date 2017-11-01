//
// UpdateEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Client.Updater;

namespace Xamarin.Interactive.Telemetry.Events
{
    sealed class UpdateEvent : Event
    {
        public static UpdateEvent CheckFailed ()
            => new UpdateEvent (null, "update.checkFailed");

        public static UpdateEvent Available (UpdateItem updateItem)
            => updateItem == null ? null : new UpdateEvent (updateItem, "update.available");

        public static UpdateEvent Ignored (UpdateItem updateItem)
            => updateItem == null ? null : new UpdateEvent (updateItem, "update.ignored");

        public static UpdateEvent Downloading (UpdateItem updateItem)
            => updateItem == null ? null : new UpdateEvent (updateItem, "update.downloading");

        public static UpdateEvent Installing (UpdateItem updateItem)
            => updateItem == null ? null : new UpdateEvent (updateItem, "update.installing");

        public static UpdateEvent Failed (UpdateItem updateItem)
            => updateItem == null ? null : new UpdateEvent (updateItem, "update.failed");

        public static UpdateEvent Canceled (UpdateItem updateItem = null)
            => new UpdateEvent (updateItem, "update.canceled");

        readonly UpdateItem updateItem;

        UpdateEvent (UpdateItem updateItem, string key) : base (key)
        {
            this.updateItem = updateItem;
        }

        protected override Task SerializePropertiesAsync (JsonTextWriter writer)
        {
            if (updateItem == null)
                return Task.CompletedTask;

            writer.WritePropertyName ("version");
            writer.WriteValue (
                updateItem.ReleaseVersion.IsValid
                    ? updateItem.ReleaseVersion.ToString ()
                    : updateItem.Version);

            writer.WritePropertyName ("channel");
            writer.WriteValue (updateItem.Channel);

            return Task.CompletedTask;
        }
    }
}