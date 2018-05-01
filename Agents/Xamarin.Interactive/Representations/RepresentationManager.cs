//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    [Flags]
    public enum RepresentationManagerOptions
    {
        None = 0 << 0,
        EnforceMainThread = 1 << 0,
        YieldOriginal = 1 << 1,
        YieldInteractive = 1 << 2
    }

    public sealed class RepresentationManager
    {
        const string TAG = nameof (RepresentationManager);

        AgentRepresentationProvider agentRepresentationProvider;
        readonly List<RepresentationProvider> providers = new List<RepresentationProvider> (4);
        readonly HashSet<Type> activatedProviderTypes = new HashSet<Type> ();
        readonly RepresentationManagerOptions options;

        public RepresentationManager (RepresentationManagerOptions options =
            RepresentationManagerOptions.EnforceMainThread |
            RepresentationManagerOptions.YieldInteractive)
            => this.options = options;

        void MaybeEnsureMainThread ()
        {
            if (options.HasFlag (RepresentationManagerOptions.EnforceMainThread))
                MainThread.Ensure ();
        }

        public void AddProvider<TRepresentationProvider> ()
            where TRepresentationProvider : RepresentationProvider, new ()
        {
            MaybeEnsureMainThread ();

            if (activatedProviderTypes.Add (typeof (TRepresentationProvider)))
                AddProvider (new TRepresentationProvider ());
        }

        public void AddProvider (RepresentationProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException (nameof (provider));

            MaybeEnsureMainThread ();

            if (provider is AgentRepresentationProvider agentProvider) {
                if (agentRepresentationProvider != null)
                    throw new InvalidOperationException (
                        $"{agentRepresentationProvider.GetType ()} already registered; " +
                        $"only one {nameof (AgentRepresentationProvider)} can be registered");

                agentRepresentationProvider = agentProvider;
            }

            providers.Add (provider);
        }

        TypeMapRepresentationProvider EnsureTypeMapRepresentationProvider ()
        {
            var typeMapRepresentationProvider = providers
                .OfType<TypeMapRepresentationProvider> ()
                .FirstOrDefault ();

            if (typeMapRepresentationProvider == null) {
                typeMapRepresentationProvider = new TypeMapRepresentationProvider ();
                AddProvider (typeMapRepresentationProvider);
            }

            return typeMapRepresentationProvider;
        }

        public void AddProvider (string typeName, Func<object, object> handler)
            => EnsureTypeMapRepresentationProvider ().RegisterHandler (typeName, false, handler);

        public void AddProvider<T> (Func<T, object> handler)
            => EnsureTypeMapRepresentationProvider ().RegisterHandler (false, handler);

        internal InteractiveObject PrepareInteractiveObject (object obj)
        {
            if (obj == null)
                return null;

            var interactiveObject = new ReflectionInteractiveObject (
                0,
                obj,
                Prepare,
                InteractiveObjectMemberFilter);
            interactiveObject.Handle = ObjectCache.Shared.GetHandle (obj);
            interactiveObject.Initialize ();
            return interactiveObject;
        }

        /// <summary>
        /// Prepare serializable representations for an arbitrary object.
        /// </summary>
        public object Prepare (object obj)
        {
            if (obj == null)
                return null;

            MaybeEnsureMainThread ();

            var representations = new RepresentedObject (obj.GetType ());
            Prepare (representations, 0, obj);
            if (representations.Count == 0)
                return null;

            return representations;
        }

        /// <summary>
        /// Secondary entry point for all object representations. Called primarily by
        /// <see cref="Prepare(object,bool)"/>, but also recursively when preparing interactive
        /// reflected objects, etc. to prepare child member values.
        /// </summary>
        /// <param name="representations">The representations already produced for this object.</param>
        /// <param name="depth">The current object graph depth.</param>
        /// <param name="obj">The current object to represent.</param>
        internal void Prepare (RepresentedObject representations, int depth, object obj)
        {
            if (obj == null)
                return;

            MaybeEnsureMainThread ();

            if (options.HasFlag (RepresentationManagerOptions.YieldOriginal))
                representations.Add (obj);

            representations.Add (Normalize (obj));
            representations.Add (new ToStringRepresentation (obj));

            var skipInteractive = !options.HasFlag (RepresentationManagerOptions.YieldInteractive);
            var interactiveObj = obj;

            // It is not safe to provide Representation objects to providers
            // which can cause a stack overflow in Normalize if the provider
            // decides to directly pack the provied Representation object in
            // another Representation object for whatever reason.
            if (obj is Representation representationObj)
                obj = representationObj.Value;

            foreach (var provider in providers) {
                try {
                    foreach (var representation in provider.ProvideRepresentations (obj)) {
                        representations.Add (Normalize (representation));
                        skipInteractive |= representation
                            is InteractiveObject.GetMemberValueError;
                    }
                } catch (Exception e) {
                    Log.Error (TAG, $"provider {provider}.ProvideRepresentation", e);
                }
            }

            if (skipInteractive)
                return;

            foreach (var interactiveObject in PrepareInteractiveObjects (depth, interactiveObj)) {
                interactiveObject.Handle = ObjectCache.Shared.GetHandle (interactiveObject);
                interactiveObject.Initialize ();
                representations.Add (interactiveObject);
            }
        }

        /// <summary>
        /// Called on every representation to perform a normalization pass
        /// to convert many common object types to those that can be serialized
        /// in a normalized way. This provides a small safety layer as well,
        /// ensuring that we are only serializing objects that are safe/supported
        /// to do so across all agents*clients or explicitly declare themselves
        /// as an IRepresentationObject (an opt-in "contract" that says that
        /// clients should be able to deserialize it).
        /// </summary>
        /// <param name="obj">The object to normalize.</param>
        object Normalize (object obj)
        {
            switch (obj) {
            case null:
                return null;
            case Representation originalRepresentation:
                // an object may be boxed in a Representation object if it has metadata
                // associated with it, such as whether the representation provider supports
                // "editing" via TryConvertFromRepresentation. We must still normalize the
                // value inside to ensure we can safely serialize it. If the value differs
                // after normalization, but is serializable, we re-box it with the normalized
                // value and the canEdit flag unset.
                var normalizedRepresentationValue = Normalize (originalRepresentation.Value);

                if (Equals (originalRepresentation.Value, normalizedRepresentationValue))
                    return obj;

                if (normalizedRepresentationValue == null)
                    return null;

                return originalRepresentation.With (
                    normalizedRepresentationValue,
                    canEdit: false);
            }

            if (agentRepresentationProvider != null) {
                try {
                    var normalized = agentRepresentationProvider.NormalizeRepresentation (obj);
                    if (normalized != null)
                        return normalized;
                } catch (Exception e) {
                    Log.Error (TAG, "Agent-builtin normalizer raised an exception", e);
                }
            }

            switch (obj) {
            case Enum enumValue:
                return new EnumValue (enumValue);
            case IInteractiveObject interactive:
                interactive.Handle = ObjectCache.Shared.GetHandle (interactive);
                return interactive;
            case Exception exception:
                return ExceptionNode.Create (exception);
            case Type asType:
                return TypeNode.Create (asType);
            case MemberInfo memberInfo:
                try {
                    var remoteMemberInfo = TypeMember.Create (memberInfo);
                    if (remoteMemberInfo != null)
                        return remoteMemberInfo;
                } catch (Exception e) {
                    Log.Warning (TAG, "unable to create TypeMember from MemberInfo", e);
                }
                return null;
            case TimeSpan _:
            case Guid _:
                return obj;
            case IntPtr intptr:
                return new WordSizedNumber (
                    obj,
                    WordSizedNumberFlags.Pointer | WordSizedNumberFlags.Signed,
                    (ulong)intptr);
            case UIntPtr uintptr:
                return new WordSizedNumber (
                    obj,
                    WordSizedNumberFlags.Pointer,
                    (ulong)uintptr);
            default:
                var type = obj.GetType ();

                if (type.GetCustomAttribute<JsonObjectAttribute> () != null)
                    return obj;

                if (Type.GetTypeCode (type) != TypeCode.Object)
                    return obj;

                if (obj is byte [])
                    return obj;

                return null;
            }
        }

        /// <summary>
        /// Whether or not the object has an enumerator that is sensible. This
        /// implementation performs a few basic outer pass checks before asking
        /// representation providers. If a provider returns false, this method
        /// returns false and the object will not have an enumerable representation
        /// in the client.
        /// See <see cref="RepresentationProvider.HasSensibleEnumerator"/>.
        /// </summary>
        /// <param name="obj">The object to check for a sensible enumerator.</param>
        bool HasSensibleEnumerator (object obj)
        {
            var enumerable = obj as IEnumerable;
            if (enumerable == null)
                return false;

            if (obj is string)
                return false;

            try {
                foreach (var provider in providers) {
                    try {
                        if (!provider.HasSensibleEnumerator (enumerable))
                            return false;
                    } catch (Exception e) {
                        Log.Error (TAG, $"provider {provider}.HasSensibleEnumerator", e);
                    }
                }

                enumerable.GetEnumerator ();
                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Prepares interactive enumerables and reflection objects.
        /// </summary>
        /// <param name="depth">The current object graph depth.</param>
        /// <param name="obj">The current object to prepare for interaction.</param>
        IEnumerable<IInteractiveObject> PrepareInteractiveObjects (int depth, object obj)
        {
            // yield a sensible interactive enumerable
            if (HasSensibleEnumerator (obj))
                yield return new InteractiveEnumerable (
                    depth,
                    (IEnumerable)obj,
                    Prepare);

            switch (Type.GetTypeCode (obj.GetType ())) {
            case TypeCode.Object:
            case TypeCode.String:
            case TypeCode.DateTime:
                break;
            default:
                // do not reflect other TypeCoded objects
                yield break;
            }

            if (obj is IntPtr || obj is UIntPtr)
                yield break;

            foreach (var provider in providers) {
                try {
                    if (!provider.ShouldReflect (obj))
                        yield break;
                } catch (Exception e) {
                    Log.Error (TAG, $"provider {provider}.ShouldReflect", e);
                }
            }

            // the object is already represented
            if (obj is DictionaryInteractiveObject)
                yield break;

            // perform full interactive reflection of the object
            yield return new ReflectionInteractiveObject (
                depth,
                obj,
                Prepare,
                InteractiveObjectMemberFilter);
        }

        /// <summary>
        /// Filter reflected members of an object from an interactive representation.
        /// </summary>
        /// <returns><c>true</c> to include the member, <c>false</c> otherwise.</returns>
        /// <param name="memberInfo">The member to possibly filter out.</param>
        /// <remarks>
        /// We will expose this to RepresentationProvider at some point, but currently
        /// RepresentedMemberInfo and RepresentedType are too complicated for PCL
        /// inclusion. We either need to figure that out, or cull interfaces.
        /// </remarks>
        bool InteractiveObjectMemberFilter (RepresentedMemberInfo memberInfo, object obj)
        {
            // Dont evalute Task<>.Result. Creates deadlock on WPF, unnecessary wait elsewhere.
            if (memberInfo.Name == "Result") {
                var declaringType = memberInfo?.DeclaringType?.ResolvedType;
                if (declaringType != null &&
                    declaringType.IsGenericType &&
                    declaringType.GetGenericTypeDefinition () == typeof (Task<>))
                    return false;
            }

            var debuggerBrowsableAttr = memberInfo
                .ResolvedMemberInfo
                .GetCustomAttribute<DebuggerBrowsableAttribute> ();
            if (debuggerBrowsableAttr != null &&
                debuggerBrowsableAttr.State == DebuggerBrowsableState.Never)
                return false;

            // some of the libraries we consume use EditorBrowsableAttribute
            // without adding a DebuggerBrowsable attribute as well
            var editorBrowsableAttr = memberInfo
                .ResolvedMemberInfo
                .GetCustomAttribute<EditorBrowsableAttribute> ();
            if (editorBrowsableAttr != null)
                switch (editorBrowsableAttr.State) {
                case EditorBrowsableState.Never:
                case EditorBrowsableState.Advanced:
                    return false;
                }

            // Don't evaluate delegate types. It is not useful, and on iOS/Mac it can actually
            // unset existing delegates.
            if (typeof (Delegate).IsAssignableFrom (memberInfo.MemberType.ResolvedType))
                return false;

            foreach (var provider in providers) {
                try {
                    if (!provider.ShouldReadMemberValue (memberInfo, obj))
                        return false;
                } catch (Exception e) {
                    Log.Error (TAG, $"provider {provider}.", e);
                }
            }

            return true;
        }

        /// <summary>
        /// Converts from a representation object such as a <see cref="Color"/> to a
        /// represented object such as a UIKit.UIColor via the first successful call
        /// to a registered <see cref="RepresentationProvider"/>'s version of this method.
        /// </summary>
        internal bool TryConvertFromRepresentation (
            IRepresentedType representedType,
            object [] representations,
            out object represented)
        {
            foreach (var provider in providers) {
                try {
                    if (provider.TryConvertFromRepresentation (
                        representedType,
                        representations,
                        out represented))
                        return true;
                } catch (Exception e) {
                    Log.Error (TAG, $"provider {provider}.TryConvertFromRepresentation", e);
                }
            }

            represented = null;
            return false;
        }
    }
}