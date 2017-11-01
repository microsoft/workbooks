//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using Foundation;
using WebKit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Client.Mac.WebDocument.MapView
{
    [Register]
    sealed class XIMapViewWebDocumentRepresentation : XIWebDocumentRepresentation
    {
        public bool LocationIsValid { get; private set; }
        public GeoLocation Location { get; private set; }
        public ChangeableWrapper<GeoPolyline> Polyline { get; private set; }

        public override void ReceivedData (NSData data, WebDataSource dataSource)
        {
            LocationIsValid = false;

            try {
                var handle = long.Parse (data.ToString ());
                var obj = ObjectCache.Shared.GetObject (handle);
                if (obj == null)
                    return;

                Polyline = obj as ChangeableWrapper<GeoPolyline>;
                if (Polyline != null) {
                    Location = Polyline.Value.Points?.FirstOrDefault ();
                    LocationIsValid = true;
                    return;
                }

                Location = obj as GeoLocation;
                if (Location != null)
                    LocationIsValid = true;
            } catch (Exception e) {
                Log.Error ("XIMapViewWebDocumentRepresentation", e);
                return;
            }
        }
    }
}