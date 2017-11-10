//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Xamarin.Interactive.Representations.Reflection
{
    [Serializable]
    sealed class RepresentedMemberInfo : IRepresentedMemberInfo, ISerializable
    {
        readonly object resolveLock = new object ();
        bool resolved;
        bool checkedNativeProperty;
        string nativePropertyError;

        public RepresentedType DeclaringType { get; private set; }
        public RepresentedMemberKind MemberKind { get; private set; }
        public RepresentedType MemberType { get; private set; }
        public string Name { get; private set; }
        public MemberInfo ResolvedMemberInfo { get; private set; }
        public bool CanWrite { get; private set; }

        IRepresentedType IRepresentedMemberInfo.DeclaringType => DeclaringType;
        IRepresentedType IRepresentedMemberInfo.MemberType => MemberType;

        public RepresentedMemberInfo (MemberInfo memberInfo)
            : this (RepresentedType.Lookup (memberInfo?.DeclaringType), memberInfo)
        {
        }

        internal RepresentedMemberInfo (RepresentedType declaringType, MemberInfo memberInfo)
        {
            if (declaringType == null)
                throw new ArgumentNullException (nameof(declaringType));

            if (memberInfo == null)
                throw new ArgumentNullException (nameof(memberInfo));

            resolved = true;

            if (memberInfo is FieldInfo) {
                MemberKind = RepresentedMemberKind.Field;
                MemberType = RepresentedType.Lookup (((FieldInfo)memberInfo).FieldType);
                CanWrite = true; // TODO: Or only if public?
            } else if (memberInfo is PropertyInfo) {
                var propertyInfo = (PropertyInfo)memberInfo;
                MemberKind = RepresentedMemberKind.Property;
                MemberType = RepresentedType.Lookup (propertyInfo.PropertyType);
                CanWrite = propertyInfo.CanWrite && propertyInfo.GetSetMethod () != null;
            } else
                throw new ArgumentException ("must be FieldInfo or PropertyInfo", nameof (memberInfo));

            Name = memberInfo.Name;
            ResolvedMemberInfo = memberInfo;
            DeclaringType = declaringType;
        }

        internal RepresentedMemberInfo (SerializationInfo info, StreamingContext context)
        {
            MemberKind = (RepresentedMemberKind)info.GetValue (nameof(MemberKind), typeof(RepresentedMemberKind));
            Name = info.GetString (nameof(Name));
            CanWrite = info.GetBoolean (nameof(CanWrite));

            var declaringTypeName = info.GetString (nameof(DeclaringType));
            if (declaringTypeName != null)
                DeclaringType = RepresentedType.Lookup (declaringTypeName);

            var memberType = info.GetString (nameof(MemberType));
            if (memberType != null)
                MemberType = RepresentedType.Lookup (memberType);
        }

        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue (nameof(MemberKind), MemberKind);
            info.AddValue (nameof(Name), Name);
            info.AddValue (nameof(CanWrite), CanWrite);

            if (DeclaringType?.Name != null)
                info.AddValue (nameof(DeclaringType), DeclaringType.Name);

            if (MemberType?.Name != null)
                info.AddValue (nameof(MemberType), MemberType.Name);
        }

        public void SetValue (object target, object value)
        {
            EnsureResolved ();

            var field = ResolvedMemberInfo as FieldInfo;
            if (field != null) {
                field.SetValue (target, value);
                return;
            }

            var property = ResolvedMemberInfo as PropertyInfo;
            if (property != null) {
                property.SetValue (target, value);
                return;
            }

            throw new NotImplementedException ("should not be reached; " +
                $"unsupported MemberInfo {ResolvedMemberInfo.GetType ()}");
        }

        public object GetValue (object target)
        {
            EnsureResolved ();

            var field = ResolvedMemberInfo as FieldInfo;
            if (field != null)
                return field.GetValue (target);

            var property = ResolvedMemberInfo as PropertyInfo;
            if (property != null) {
                if (!checkedNativeProperty) {
                    checkedNativeProperty = true;
                    nativePropertyError = NativeHelper.SharedInstance.CheckProperty (
                        property,
                        target,
                        DeclaringType);
                }

                if (nativePropertyError != null)
                    throw new Exception (nativePropertyError);

                return property.GetValue (target);
            }

            throw new NotImplementedException ("should not be reached; " +
                $"unsupported MemberInfo {ResolvedMemberInfo.GetType ()}");
        }

        void EnsureResolved ()
        {
            if (Resolve () == null)
                throw new InvalidOperationException (
                    $"Resolve() failed to resolve {DeclaringType.Name}::{Name} as a {MemberType}");
        }

        public MemberInfo Resolve ()
        {
            const BindingFlags bindingFlags =
                BindingFlags.DeclaredOnly |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            lock (resolveLock) {
                if (resolved)
                    return ResolvedMemberInfo;

                resolved = true;

                var type = DeclaringType?.ResolvedType;
                if (type == null)
                    return null;

                if (MemberKind == RepresentedMemberKind.Field)
                    ResolvedMemberInfo = type.GetField (Name, bindingFlags);
                else if (MemberKind == RepresentedMemberKind.Property)
                    ResolvedMemberInfo = type.GetProperty (Name, bindingFlags);

                return ResolvedMemberInfo;
            }
        }
    }
}