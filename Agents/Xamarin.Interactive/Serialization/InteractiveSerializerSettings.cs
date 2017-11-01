//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Serialization
{
    sealed class InteractiveSerializerSettings : XipSerializerSettings
    {
        public static readonly XipSerializerSettings SharedInstance = new InteractiveSerializerSettings ();

        InteractiveSerializerSettings ()
            => base.Binder = new Binder ();

        new sealed class Binder : XipSerializationBinder
        {
            public override string BindToName (Type serializedType)
            {
                if (typeof (InspectView).IsAssignableFrom (serializedType))
                    return typeof (InspectView).ToSerializableName ();

                return base.BindToName (serializedType);
            }
        }
    }
}