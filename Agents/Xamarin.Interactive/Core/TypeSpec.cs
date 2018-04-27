//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Core
{
    [JsonObject]
    public sealed class TypeSpec
    {
        [JsonObject]
        public struct TypeName : IEquatable<TypeName>
        {
            public string Namespace { get; }
            public string Name { get; }
            public int TypeArgumentCount { get; }

            [JsonConstructor]
            public TypeName (string @namespace, string name, int typeArgumentCount = 0)
            {
                if (String.IsNullOrEmpty (name))
                    throw new ArgumentException ("must not be null or empty", nameof (name));

                Namespace = @namespace;
                Name = name;
                TypeArgumentCount = typeArgumentCount;
            }

            public bool Equals (TypeName other)
            {
                return other.Namespace == Namespace &&
                    other.Name == Name &&
                    other.TypeArgumentCount == TypeArgumentCount;
            }

            public override bool Equals (object obj)
            {
                return obj is TypeName && Equals ((TypeName)obj);
            }

            public override int GetHashCode ()
            {
                var hc = 17 * 23 + TypeArgumentCount;
                if (Namespace != null)
                    hc = hc * 23 + Namespace.GetHashCode ();
                if (Name != null)
                    hc = hc * 23 + Name.GetHashCode ();
                return hc;
            }

            static StringBuilder Append (StringBuilder builder, string name, bool escape)
            {
                if (String.IsNullOrEmpty (name))
                    return builder;

                if (!escape) {
                    builder.Append (name);
                    return builder;
                }

                foreach (var c in name) {
                    switch (c) {
                    case '+':
                    case ',':
                    case '[':
                    case ']':
                    case '*':
                    case '&':
                    case '\\':
                        builder.Append ('\\').Append (c);
                        break;
                    default:
                        builder.Append (c);
                        break;
                    }
                }

                return builder;
            }

            internal StringBuilder Append (StringBuilder builder, bool escape)
            {
                if (!String.IsNullOrEmpty (Namespace))
                    Append (builder, Namespace, escape).Append ('.');

                Append (builder, Name, escape);

                if (TypeArgumentCount > 0)
                    builder.Append ('`').Append (TypeArgumentCount);

                return builder;
            }

            public override string ToString ()
            {
                return Append (new StringBuilder (), false).ToString ();
            }

            internal static TypeName Parse (string name)
            {
                int arity = 0;
                string @namespace = null;

                var ofs = name.LastIndexOf ('`');
                if (ofs > 0) {
                    if (Int32.TryParse (name.Substring (ofs + 1), out arity))
                        name = name.Substring (0, ofs);
                }

                ofs = name.LastIndexOf ('.');
                if (ofs == 0)
                    throw new ArgumentException ("must not start with a '.'", nameof (name));

                if (ofs >= 0) {
                    @namespace = name.Substring (0, ofs);
                    name = name.Substring (ofs + 1);
                }

                return new TypeName (@namespace, name, arity);
            }
        }

        public interface IModifier
        {
            StringBuilder Append (StringBuilder builder);
        }

        [JsonObject]
        public struct ArrayModifier : IModifier
        {
            public bool IsBound { get; }
            public int Dimension { get; }

            [JsonConstructor]
            public ArrayModifier (bool isBound, int dimension)
            {
                IsBound = isBound;
                Dimension = dimension;
            }

            public StringBuilder Append (StringBuilder builder)
            {
                if (IsBound)
                    return builder.Append ("[*]");

                return builder
                    .Append ('[')
                    .Append (',', Dimension - 1)
                    .Append (']');
            }

            public override string ToString ()
                => Append (new StringBuilder ()).ToString ();
        }

        [JsonObject]
        public struct PointerModifier : IModifier
        {
            public StringBuilder Append (StringBuilder builder)
                => builder.Append ('*');

            public override string ToString ()
                => Append (new StringBuilder ()).ToString ();
        }

        public sealed class Builder
        {
            public TypeName Name { get; set; }
            public string AssemblyName { get; set; }
            public bool IsByRef { get; set; }
            public List<TypeName> NestedNames { get; private set; }
            public List<IModifier> Modifiers { get; private set; }
            public List<TypeSpec> TypeArguments { get; private set; }

            public bool HasModifiers => Modifiers != null && Modifiers.Count > 0;

            public void AddName (TypeName name)
            {
                if (name.Equals (default))
                    throw new ArgumentException ("must not be default value", nameof (name));

                if (Name.Equals (default))
                    Name = name;
                else
                    (NestedNames ?? (NestedNames = new List<TypeName> ())).Add (name);
            }

            public void AddModifier (IModifier modifier)
            {
                if (modifier == null)
                    throw new ArgumentNullException (nameof (modifier));

                (Modifiers ?? (Modifiers = new List<IModifier> ())).Add (modifier);
            }

            public void AddTypeArgument (TypeSpec typeArgument)
            {
                if (typeArgument == null)
                    throw new ArgumentNullException (nameof (typeArgument));

                (TypeArguments ?? (TypeArguments = new List<TypeSpec> ())).Add (typeArgument);
            }

            public TypeSpec Build ()
                => new TypeSpec (
                    Name,
                    AssemblyName,
                    IsByRef,
                    NestedNames,
                    Modifiers,
                    TypeArguments);
        }

        public TypeName Name { get; }
        public string AssemblyName { get; }
        public bool IsByRef { get; }
        public IReadOnlyList<TypeName> NestedNames { get; }
        public IReadOnlyList<IModifier> Modifiers { get; }
        public IReadOnlyList<TypeSpec> TypeArguments { get; }

        [JsonConstructor]
        public TypeSpec (
            TypeName name,
            string assemblyName,
            bool isByRef,
            IReadOnlyList<TypeName> nestedNames,
            IReadOnlyList<IModifier> modifiers,
            IReadOnlyList<TypeSpec> typeArguments)
        {
            Name = name;
            AssemblyName = assemblyName;
            IsByRef = isByRef;
            NestedNames = nestedNames ?? Array.Empty<TypeName> ();
            Modifiers = modifiers ?? Array.Empty<IModifier> ();
            TypeArguments = typeArguments ?? Array.Empty<TypeSpec> ();
        }

        public IEnumerable<TypeName> GetAllNames ()
        {
            if (!Name.Equals (default))
                yield return Name;

            foreach (var nestedName in NestedNames)
                yield return nestedName;
        }

        #region ToString

        StringBuilder AppendFullName (StringBuilder builder)
        {
            Name.Append (builder, true);

            foreach (var nestedName in NestedNames)
                nestedName.Append (builder.Append ('+'), true);

            return builder;
        }

        StringBuilder AppendTypeArguments (StringBuilder builder)
        {
            if (TypeArguments.Count == 0)
                return builder;

            builder.Append ('[');
            var startPos = builder.Length;

            foreach (var typeArgument in TypeArguments) {
                if (builder.Length > startPos)
                    builder.Append (',');

                if (String.IsNullOrEmpty (typeArgument.AssemblyName))
                    typeArgument.AppendFullTypeSpec (builder);
                else
                    typeArgument.AppendFullTypeSpec (builder.Append ('[')).Append (']');
            }

            builder.Append (']');
            return builder;
        }

        StringBuilder AppendModifiers (StringBuilder builder)
        {
            foreach (var modifier in Modifiers)
                modifier.Append (builder);

            if (IsByRef)
                builder.Append ('&');

            return builder;
        }

        StringBuilder AppendAssemblyName (StringBuilder builder)
        {
            if (!String.IsNullOrEmpty (AssemblyName))
                builder.Append (", ").Append (AssemblyName);

            return builder;
        }

        StringBuilder AppendFullTypeSpec (StringBuilder builder)
            => AppendAssemblyName (
                AppendModifiers (
                    AppendTypeArguments (
                        AppendFullName (
                            builder
                        )
                    )
                )
            );

        public override string ToString ()
            => AppendFullTypeSpec (new StringBuilder ()).ToString ();

        public string DumpToString ()
            => DumpToString (new StringBuilder (), 0).ToString ();

        public StringBuilder DumpToString (StringBuilder builder, int depth)
        {
            AppendFullName (builder.Append (' ', depth * 2));

            if (AssemblyName != null)
                builder.Append (", [").Append (AssemblyName).Append (']');

            builder.AppendLine ();

            foreach (var typeArgument in TypeArguments)
                typeArgument.DumpToString (builder, depth + 1);

            return builder;
        }

        #endregion

        #region Parsing

        public static TypeSpec Parse (Type type, bool assemblyQualified = false)
            => ParseBuilder (type, assemblyQualified).Build ();

        public static TypeSpec Parse (string typeSpec)
            => ParseBuilder (typeSpec).Build ();

        public static TypeSpec.Builder ParseBuilder (Type type, bool assemblyQualified = false)
            => ParseBuilder (
                assemblyQualified
                    ? type.AssemblyQualifiedName
                    : type.ToString ());

        // CLR type specification string parsing ported directly from the Mono runtime's
        // mono_reflection_parse_type (mono/metadata/reflection.c)

        public static TypeSpec.Builder ParseBuilder (string typeSpec)
        {
            if (typeSpec == null)
                throw new ArgumentNullException (nameof (typeSpec));

            return Parse (
                new StringReader (typeSpec),
                new StringBuilder (),
                false);
        }

        static TypeSpec.Builder Parse (TextReader reader, StringBuilder builder, bool isRecursed)
        {
            Exception Error (string message)
                => new ArgumentException (message, "typeSpec");

            var typeSpec = new TypeSpec.Builder ();
            var inModifiers = false;
            char c;

            while (Char.IsWhiteSpace ((char)reader.Peek ()))
                reader.Read ();

            while (!inModifiers && (c = (char)reader.Peek ()) != Char.MaxValue) {
                switch (c) {
                case '+':
                    reader.Read ();
                    typeSpec.AddName (TypeName.Parse (builder.ToString ()));
                    builder.Clear ();
                    break;
                case '\\':
                    reader.Read ();
                    if ((c = (char)reader.Read ()) != Char.MaxValue)
                        builder.Append (c);
                    break;
                case '&':
                case '*':
                case '[':
                case ',':
                case ']':
                    inModifiers = true;
                    break;
                default:
                    builder.Append (c);
                    reader.Read ();
                    break;
                }
            }

            if (builder.Length > 0) {
                typeSpec.AddName (TypeName.Parse (builder.ToString ()));
                builder.Clear ();
            }

            while ((c = (char)reader.Peek ()) != Char.MaxValue) {
                switch (c) {
                case '&':
                    reader.Read ();
                    if (typeSpec.IsByRef)
                        throw Error ("only one level of byref is allowed");
                    typeSpec.IsByRef = true;
                    break;
                case '*':
                    reader.Read ();
                    if (typeSpec.IsByRef)
                        throw Error ("pointer to byref is not allowed");
                    typeSpec.AddModifier (new PointerModifier ());
                    break;
                case '[':
                    reader.Read ();
                    if (typeSpec.IsByRef)
                        throw Error ("neither arrays nor generic types of byref are allowed");

                    if ((c = (char)reader.Peek ()) == Char.MaxValue)
                        throw Error ("unexpected end of type specification");

                    if (c == ',' || c == '*' || c == ']') {
                        var isBound = false;
                        var dimension = 1;
                        while ((c = (char)reader.Read ()) != Char.MaxValue) {
                            if (c == ']')
                                break;
                            else if (c == ',')
                                dimension++;
                            else if (c == '*')
                                isBound = true;
                            else
                                throw Error ("unexpected character in " +
                                    $"array specification: '{c}'");
                        }

                        if (c != ']')
                            throw Error ("expected ']' to close array specification");

                        if (isBound && dimension > 1)
                            throw Error ("multi-dimensional arrays cannot be bound");

                        typeSpec.AddModifier (new ArrayModifier (isBound, dimension));
                    } else {
                        if (typeSpec.HasModifiers)
                            throw Error ("generic type arguments after an " +
                                "array or pointer are not allowed");

                        while ((c = (char)reader.Peek ()) != Char.MaxValue) {
                            while (Char.IsWhiteSpace ((char)reader.Peek ()))
                                reader.Read ();

                            var fqname = false;
                            if (c == '[') {
                                fqname = true;
                                reader.Read ();
                            }

                            var typeArgument = Parse (reader, builder, true);

                            // MS is lenient on [] delimited parameters
                            // that aren't fqn - and F# uses them
                            if (fqname && (c = (char)reader.Peek ()) != ']') {
                                if (c != ',')
                                    throw Error ("expected ',' for fully qualified name");

                                reader.Read ();

                                while (Char.IsWhiteSpace ((char)reader.Peek ()))
                                    reader.Read ();

                                while ((c = (char)reader.Read ()) != Char.MaxValue && c != ']')
                                    builder.Append (c);

                                if (c != ']')
                                    throw Error ("expected ']' to terminate fully qualified name");

                                if (builder.Length == 0)
                                    throw Error ("missing assembly name after ','");

                                typeArgument.AssemblyName = builder.ToString ();
                                builder.Clear ();
                            } else if (fqname && c == ']')
                                reader.Read ();

                            typeSpec.AddTypeArgument (typeArgument.Build ());

                            if (reader.Read () == ']')
                                break;
                        }
                    }
                    break;
                case ']':
                    if (isRecursed)
                        return typeSpec;

                    throw Error ("unexpected ']' encountered");
                case ',':
                    if (isRecursed)
                        return typeSpec;

                    reader.Read ();

                    while (Char.IsWhiteSpace ((char)reader.Peek ()))
                        reader.Read ();

                    while ((c = (char)reader.Read ()) != Char.MaxValue)
                        builder.Append (c);

                    if (builder.Length == 0)
                        throw Error ("missing assembly name after ','");

                    typeSpec.AssemblyName = builder.ToString ();
                    builder.Clear ();
                    break;
                default:
                    throw Error ("unexpected '" + c + "' encountered");
                }

                if (typeSpec.AssemblyName != null)
                    break;
            }

            return typeSpec;
        }

        #endregion
    }
}