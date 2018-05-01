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

            public static TypeName Parse (string name)
                => Parse (null, name);

            public static TypeName Parse (string @namespace, string name)
            {
                int arity = 0;

                var ofs = name.LastIndexOf ('`');
                if (ofs > 0) {
                    if (Int32.TryParse (name.Substring (ofs + 1), out arity))
                        name = name.Substring (0, ofs);
                }

                ofs = name.LastIndexOf ('.');
                if (ofs == 0)
                    throw new ArgumentException ("must not start with a '.'", nameof (name));

                if (ofs >= 0) {
                    if (@namespace != null)
                        throw new ArgumentException (
                            "parsing name yielded a namespace but one was explicitly provided",
                            nameof (name));

                    @namespace = name.Substring (0, ofs);
                    name = name.Substring (ofs + 1);
                }

                return new TypeName (@namespace, name, arity);
            }
        }

        public sealed class Builder
        {
            public TypeName Name { get; set; }
            public string AssemblyName { get; set; }
            public List<TypeName> NestedNames { get; private set; }
            public List<Modifier> Modifiers { get; private set; }
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

            public void AddModifier (Modifier modifier)
                => (Modifiers ?? (Modifiers = new List<Modifier> ())).Add (modifier);

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
                    NestedNames,
                    Modifiers,
                    TypeArguments);
        }

        /// <summary>
        /// A modifier is either an array rank ([1,32] when cast to byte) or
        /// a specified enum value (excluding 'None').
        /// </summary>
        /// <remarks>
        /// This enum is designed to serialize nicely via Newtonsoft.Json's StringEnumConverter
        /// for representation within TypeScript. It takes advantage of the fact that
        /// array ranks greater than 32 are illegal, so we can pack other modifiers beyond
        /// that value. Values for Pointer, ByRef, and BoundArray are arbitray yet inspired.
        /// The only requirement is that they are greater 32.
        ///
        /// The TypeScript equivalent of this enum is:
        /// <code>
        /// type Modifier = number | 'Pointer' | 'ByRef' | 'BoundArray'
        /// </code>
        /// </remarks>
        public enum Modifier : byte
        {
            None,
            Pointer = (byte)'*',
            ByRef = (byte)'&',
            BoundArray = (byte)'@'
        }

        public TypeName Name { get; }
        public string AssemblyName { get; }
        public IReadOnlyList<TypeName> NestedNames { get; }
        public IReadOnlyList<Modifier> Modifiers { get; }
        public IReadOnlyList<TypeSpec> TypeArguments { get; }

        [JsonConstructor]
        public TypeSpec (
            TypeName name,
            string assemblyName = null,
            IReadOnlyList<TypeName> nestedNames = null,
            IReadOnlyList<Modifier> modifiers = null,
            IReadOnlyList<TypeSpec> typeArguments = null)
        {
            Name = name;
            AssemblyName = assemblyName;
            NestedNames = nestedNames;
            Modifiers = modifiers;
            TypeArguments = typeArguments;
        }

        public bool IsByRef ()
            => Modifiers != null && Modifiers.Count > 0 && Modifiers [Modifiers.Count - 1] == Modifier.ByRef;

        public IEnumerable<TypeName> GetAllNames ()
        {
            if (!Name.Equals (default))
                yield return Name;

            if (NestedNames != null) {
                foreach (var nestedName in NestedNames)
                    yield return nestedName;
            }
        }

        #region ToString

        StringBuilder AppendFullName (StringBuilder builder)
        {
            Name.Append (builder, true);

            if (NestedNames != null) {
                foreach (var nestedName in NestedNames)
                    nestedName.Append (builder.Append ('+'), true);
            }

            return builder;
        }

        StringBuilder AppendTypeArguments (StringBuilder builder)
        {
            if (TypeArguments == null || TypeArguments.Count == 0)
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
            if (Modifiers == null)
                return builder;

            foreach (var modifier in Modifiers) {
                // 32 is the max array dimension in CTS/CLR
                if ((byte)modifier <= 32)
                    builder.Append ('[').Append (',', (byte)modifier - 1).Append (']');
                else if (modifier == Modifier.BoundArray)
                    builder.Append ("[*]");
                else if (modifier == Modifier.Pointer || modifier == Modifier.ByRef)
                    builder.Append ((char)modifier);
                else
                    throw new InvalidOperationException (
                        $"Invalid modifier '{(char)modifier}' = {modifier}");
            }

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

        public static TypeSpec Create (Type type, bool withAssemblyQualifiedNames = false)
        {
            if (type == null)
                throw new ArgumentNullException (nameof (type));

            TypeName outerTypeName = default;
            List<TypeName> nestedNames = null;
            List<Modifier> modifiers = null;
            List<TypeSpec> typeArgumentList = null;
            var assemblyQualifiedName = withAssemblyQualifiedNames
                ? type.Assembly.FullName
                : null;

            // handle generic type arguments for open (e.g <,>) and closed (e.g. <int, string>) types
            if (type.IsGenericType) {
                typeArgumentList = new List<TypeSpec> ();

                foreach (var typeArgument in type.GetGenericArguments ()) {
                    if (type.IsConstructedGenericType)
                        // recurse into constructed type arguments
                        typeArgumentList.Add (Create (
                            typeArgument,
                            withAssemblyQualifiedNames));
                    else
                        // open generics simply have a type name
                        typeArgumentList.Add (new TypeSpec (
                            new TypeName (null, typeArgument.Name)));
                }
            }

            // walk nested type chain and desugar
            while (type != null) {
                // only provide the namespace on the outer-most type
                var @namespace = type.DeclaringType == null
                    ? type.Namespace
                    : null;

                // desugar the type into modifiers and desugared type
                while (type.HasElementType) {
                    if (modifiers == null)
                        modifiers = new List<Modifier> ();

                    if (type.IsByRef)
                        modifiers.Insert (0, Modifier.ByRef);
                    else if (type.IsPointer)
                        modifiers.Insert (0, Modifier.Pointer);
                    else if (type.IsArray)
                        modifiers.Insert (0, (Modifier)(byte)type.GetArrayRank ());

                    type = type.GetElementType ();
                }

                // when fully desugared we will have just the root type name
                // (e.g. 'System.Int32' and not 'System.Int32**[,,]&')
                var typeName = TypeName.Parse (@namespace, type.Name);

                if (type.DeclaringType == null) {
                    outerTypeName = typeName;
                } else {
                    if (nestedNames == null)
                        nestedNames = new List<TypeName> ();
                    nestedNames.Insert (0, typeName);
                }

                // move up the nested chain
                type = type.DeclaringType;
            }

            return new TypeSpec (
                outerTypeName,
                assemblyQualifiedName,
                nestedNames,
                modifiers,
                typeArgumentList);
        }

        public static TypeSpec Parse (string typeSpec)
            => ParseBuilder (typeSpec).Build ();

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
            var isByRef = false;
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
                    if (isByRef)
                        throw Error ("only one level of byref is allowed");
                    typeSpec.AddModifier (Modifier.ByRef);
                    break;
                case '*':
                    reader.Read ();
                    if (isByRef)
                        throw Error ("pointer to byref is not allowed");
                    typeSpec.AddModifier (Modifier.Pointer);
                    break;
                case '[':
                    reader.Read ();
                    if (isByRef)
                        throw Error ("neither arrays nor generic types of byref are allowed");

                    if ((c = (char)reader.Peek ()) == Char.MaxValue)
                        throw Error ("unexpected end of type specification");

                    if (c == ',' || c == '*' || c == ']') {
                        var isBound = false;
                        byte dimension = 1;
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

                        if (isBound) {
                            if (dimension > 1)
                                throw Error ("multi-dimensional arrays cannot be bound");
                            typeSpec.AddModifier (Modifier.BoundArray);
                        } else {
                            typeSpec.AddModifier ((Modifier)dimension);
                        }
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