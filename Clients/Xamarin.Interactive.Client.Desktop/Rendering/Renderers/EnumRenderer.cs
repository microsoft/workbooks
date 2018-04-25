//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Rendering.Renderers
{
    [Renderer (typeof (EnumValue))]
    sealed class EnumRenderer : HtmlRendererBase
    {
        public override string CssClass => "renderer-enum";
        public override bool CanExpand => false;

        EnumValue source;

        protected override void HandleBind () => source = (EnumValue)RenderState.Source;

        protected override IEnumerable<RendererRepresentation> HandleGetRepresentations ()
        {
            yield return new RendererRepresentation ("Enum");
        }

        protected override void HandleRender (RenderTarget target)
        {
            var enumElem = Document.CreateElement ("code");

            target.InlineTarget.AppendChild (enumElem);

            ulong remainder;
            var first = true;

            foreach (var name in GetNames (out remainder)) {
                if (!first)
                    enumElem.AppendChild (Document.CreateElement ("span",
                        @class: "csharp-operator",
                        innerHtml: " | "));

                enumElem.AppendChild (Document.CreateElement ("span",
                    @class: "csharp-enum-name",
                    innerHtml: name.HtmlEscape ()));

                first = false;
            }

            if (remainder != 0 || first) {
                if (!first)
                    enumElem.AppendChild (Document.CreateElement ("span",
                        @class: "csharp-operator",
                        innerHtml: " | "));

                var number = FromUInt64 (remainder, source.UnderlyingType.ResolvedType);
                var numberStr = source.IsFlags
                    ? String.Format ("0x{0:x}", number)
                    : String.Format ("{0}", number);

                enumElem.AppendChild (Document.CreateElement ("span",
                    @class: "csharp-number",
                    innerHtml: numberStr));
            }
        }

        List<string> GetNames (out ulong remainder)
        {
            var names = new List<string> ();
            remainder = ToUInt64 (source.Value);
            var origValue = remainder;

            var index = source.Values.Count - 1;
            while (index >= 0) {
                var value = ToUInt64 (source.Values [index]);

                if (source.IsFlags && value != 0 && (remainder & value) == value) {
                    names.Add (source.Names [index]);
                    remainder -= value;
                } else if (!source.IsFlags && value == origValue) {
                    remainder = 0;
                    names.Add (source.Names [index]);
                    return names;
                }

                index--;
            }

            // special case the first enum value of '0' but only
            // show it by name when the value is actually 0
            if (names.Count == 0 && origValue == 0 &&
                source.Values.Count > 0 && ToUInt64 (source.Values [0]) == 0)
                names.Add (source.Names [0]);

            return names;
        }

        static ulong ToUInt64 (object value)
        {
            switch (Convert.GetTypeCode (value)) {
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
                return (ulong)Convert.ToInt64 (value, CultureInfo.InvariantCulture);
            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Boolean:
            case TypeCode.Char:
                return Convert.ToUInt64 (value, CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException ("Unknown enum type.");
        }

        static object FromUInt64 (ulong value, Type conversionType)
        {
            switch (Type.GetTypeCode (conversionType)) {
            case TypeCode.SByte:
                return (sbyte)value;
            case TypeCode.Int16:
                return (short)value;
            case TypeCode.Int32:
                return (int)value;
            case TypeCode.Int64:
                return (long)value;
            case TypeCode.Byte:
                return (byte)value;
            case TypeCode.UInt16:
                return (ushort)value;
            case TypeCode.UInt32:
                return (uint)value;
            case TypeCode.UInt64:
                return value;
            case TypeCode.Boolean:
                return value != 0;
            case TypeCode.Char:
                return (char)value;
            }

            throw new InvalidOperationException ("Unknown integer type.");
        }
    }
}