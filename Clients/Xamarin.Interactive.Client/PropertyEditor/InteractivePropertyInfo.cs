//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;
using Xamarin.PropertyEditing;

namespace Xamarin.Interactive.PropertyEditor
{
    class InteractivePropertyInfo : IPropertyInfo
    {
        readonly int index;

        public object Value {
            get => Editor.Target.Values [index];
            private set => Editor.Target.Values [index] = value;
        }

        public InteractiveObjectEditor Editor { get; }

        public bool IsEnumValue { get; protected set; }

        public Type Type { get; }

        public string Category { get; }

        public bool CanWrite { get; }

        public RepresentedMemberInfo Member
            => Editor.Target.Members [index];

        public string Name
            => Member.Name;

        public ValueSources ValueSources
            => ValueSources.Local;

        public IReadOnlyList<PropertyVariation> Variations
            => Array.Empty<PropertyVariation> ();

        public IReadOnlyList<IAvailabilityConstraint> AvailabilityConstraints
            => Array.Empty<IAvailabilityConstraint> ();

        public static InteractivePropertyInfo CreateInstance (InteractiveObjectEditor editor, int index)
        {
            var representation = GetRepresentation (editor.Target.Values [index], editor);

            switch (GetRepresentation (editor.Target.Values [index], editor)) {
            case EnumValue enumValue:
                return (InteractivePropertyInfo)Activator.CreateInstance (
                    typeof (InteractiveEnumPropertyInfo<>).MakeGenericType (
                        enumValue.UnderlyingType.ResolvedType),
                    editor,
                    index);
            default:
                return new InteractivePropertyInfo (editor, index);
            }
        }

        public InteractivePropertyInfo (InteractiveObjectEditor editor, int index)
        {
            Editor = editor;
            this.index = index;
            var member = this.Member;

            Category = ShortTypeName (member.DeclaringType.Name);

            var info = member.Resolve ();
            Type type = null;
            switch (info) {
            case PropertyInfo prop:
                type = prop.PropertyType;
                break;
            case FieldInfo field:
                type = field.FieldType;
                break;
            }

            var representation = GetRepresentation (Value, Editor);
            var isResolvedEnum = type?.IsEnum ?? false;
            var isEnumValue = representation is EnumValue;
            var value = UnpackValue (representation, Editor);
            var valueType = value?.GetType ();

            if (type == null && isEnumValue)
                type = valueType;
            else if (type != valueType && valueType != null && !isResolvedEnum && valueType != typeof (string))
                type = valueType;

            var hasEditor = isResolvedEnum || isEnumValue || Editor.PropertyHelper.IsConvertable (type);

            var browsable = info?.GetCustomAttribute<DebuggerBrowsableAttribute> ();
            if (browsable != null && browsable.State == DebuggerBrowsableState.Never) {
                // clear the type so we won't inspect it
                type = null;
            }

            Type = type;
            CanWrite = member.CanWrite && type != null && (hasEditor || (member.MemberType.Name == type?.FullName));
        }

        public virtual TValue ToLocalValue<TValue> ()
        {
            var local = Editor.PropertyHelper.ToLocalValue (UnpackValue ());

            if (!(local is TValue)) {
                object thing = local;
                if (typeof (TValue) == typeof (string))
                    thing = local?.ToString ();

                try {
                    return (TValue)thing;
                } catch (InvalidCastException) {
                    return default (TValue);
                }
            }

            return (TValue)local;
        }

        protected static object GetRepresentation (object value, InteractiveObjectEditor editor)
        {
            var interactiveObject = value as InteractiveObject;
            if (interactiveObject != null) {
                if (interactiveObject.Values == null)
                    return interactiveObject.ToStringRepresentation;
            }

            var representedObject = value as RepresentedObject;
            if (representedObject == null)
                return value;

            object ChooseRepresentation (RepresentedObject ro)
            {
                if (ro.Count == 0)
                    return default (Representation).Value;

                Representation fallback = ro.GetRepresentation (0);
                for (int i = 0; i < ro.Count; i++) {
                    var current = ro.GetRepresentation (i);
                    if (current.Value is JsonPayload)
                        continue;

                    if (fallback.Value is JsonPayload)
                        fallback = current;

                    if (current.CanEdit)
                        return current.Value;
                }

                // If the only representation available is a JsonPayload treat it as an error in this case
                return fallback.Value is JsonPayload ?
                           new InteractiveObject.GetMemberValueError () :
                           fallback.Value;
            }

            return editor.PropertyHelper.ToLocalValue (ChooseRepresentation (representedObject));
        }

        static object UnpackValue (object value, InteractiveObjectEditor editor)
        {
            var representation = GetRepresentation (value, editor);

            switch (representation) {
            case EnumValue enumValue:
                return enumValue.Value;
            case WordSizedNumber wordSizedNumber:
                return wordSizedNumber.Value;
            case InteractiveObject.GetMemberValueError error:
                return error.Exception == null
                    ? Catalog.GetString ("not evaluated")
                    : Catalog.GetString ("cannot evaluate");
            case InteractiveObject interactiveRepresentation:
                return UnpackValue (interactiveRepresentation, editor);
            case InteractiveEnumerable source:
                var name = TypeHelper.GetCSharpTypeName (source.RepresentedType.Name);
                return Catalog.Format (Catalog.GetString (
                    "{0} - {1} items",
                    comment: "{0} is a CLR type name; {1} is a count (integer) of items"),
                    name,
                    source.Count);
            case Image image:
                return Catalog.GetString ("not represented");
            default:
                return representation;
            }
        }

        public object UnpackValue ()
            => UnpackValue (Value, Editor);

        public virtual object ToRemoteValue<TValue> (object local)
            => Editor.PropertyHelper.ToRemoteValue (local);

        static string ShortTypeName (string longTypeName)
            => longTypeName.Substring (
                longTypeName.LastIndexOf (".", StringComparison.InvariantCulture) + 1);
    }
}
