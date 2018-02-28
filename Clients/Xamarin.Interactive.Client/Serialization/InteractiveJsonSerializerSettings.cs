//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        public InteractiveJsonSerializerSettings ()
        {
            ContractResolver = new InteractiveJsonContractResolver ();

            Converters.Add (new CodeCellIdConverter ());
            Converters.Add (new StringEnumConverter ());
            Converters.Add (new ISerializableObjectConverter ());
            Converters.Add (new RepresentedReflectionConverter ());

            Formatting = Formatting.Indented;
        }
    }
}