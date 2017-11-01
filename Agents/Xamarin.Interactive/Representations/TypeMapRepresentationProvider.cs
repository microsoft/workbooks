//
// TypeMapRepresentationProvider.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Representations
{
    class TypeMapRepresentationProvider : RepresentationProvider
    {
        const string TAG = nameof (TypeMapRepresentationProvider);

        readonly TypeMap<Delegate> typeMap = new TypeMap<Delegate> ();

        public bool HasRegisteredHandler (Type type)
        {
            if (type == null)
                throw new ArgumentNullException (nameof (type));

            return typeMap.GetValues (type).Length > 0;
        }

        public void RegisterHandler<T> (Func<T, object> handler)
            => RegisterHandler (false, handler);

        public void RegisterHandler<T> (bool exactMatchRequired, Func<T, object> handler)
        {
            if (handler == null)
                throw new ArgumentNullException (nameof (handler));
            
            typeMap.Add (typeof (T), exactMatchRequired, handler);
        }

        public void RegisterHandler (string typeName, Func<dynamic, object> handler)
            => RegisterHandler (typeName, false, handler);

        public void RegisterHandler (string typeName, bool exactMatchRequired, Func<dynamic, object> handler)
        {
            if (typeName == null)
                throw new ArgumentNullException (nameof (typeName));

            if (handler == null)
                throw new ArgumentNullException (nameof (handler));

            typeMap.Add (typeName, exactMatchRequired, handler);
        }

        public sealed override IEnumerable<object> ProvideRepresentations (object obj)
        {
            if (obj == null || !typeMap.HasAny)
                yield break;

            var type = obj.GetType ();

            foreach (var handler in typeMap.GetValues (type)) {
                object normalizedValue = null;

                try {
                    normalizedValue = handler.DynamicInvoke (obj);
                } catch (Exception e) {
                    Log.Error (TAG, $"Handler for '{type}' threw an exception", e);
                }

                if (normalizedValue != null)
                    yield return normalizedValue;
            }
        }
    }
}