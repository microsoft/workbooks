//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Xamarin.Interactive.Serialization.Converters;

namespace Xamarin.Interactive.Serialization
{
    sealed class InteractiveJsonSerializerSettings : JsonSerializerSettings
    {
        sealed class InteractiveJsonContractResolver : DefaultContractResolver
        {
            public InteractiveJsonContractResolver ()
            {
                NamingStrategy = new CamelCaseNamingStrategy {
                    ProcessDictionaryKeys = true,
                    OverrideSpecifiedNames = true
                };
            }
        }

        sealed class InteractiveJsonBinder : ISerializationBinder
        {
            public void BindToName (Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.Name;
            }

            public Type BindToType (string assemblyName, string typeName)
                => throw new NotImplementedException ();
        }

        public InteractiveJsonSerializerSettings ()
        {
            ContractResolver = new InteractiveJsonContractResolver ();
            SerializationBinder = new InteractiveJsonBinder ();

            Converters.Add (new CodeCellIdConverter ());
            Converters.Add (new StringEnumConverter ());
            Converters.Add (new IsoDateTimeConverter ());
            Converters.Add (new ISerializableObjectConverter ());
            Converters.Add (new RepresentedReflectionConverter ());

            Formatting = Formatting.Indented;
            TypeNameHandling = TypeNameHandling.Objects;
        }
    }
}