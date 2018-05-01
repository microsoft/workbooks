//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Representations.Reflection
{
    public interface IReflectionRemotingVisitor
    {
        void VisitExceptionNode (ExceptionNode exception);
        void VisitStackTrace (StackTrace stackTrace);
        void VisitStackFrame (StackFrame stackFrame);
        void VisitMethod (Method method);
        void VisitProperty (Property property);
        void VisitField (Field field);
        void VisitParameter (Parameter parameter);
        void VisitTypeSpec (TypeSpec typeSpec);
    }

    public class CSharpWriter : IReflectionRemotingVisitor
    {
        public class TokenWriter
        {
            readonly TextWriter writer;

            public TokenWriter (TextWriter writer)
                => this.writer = writer
                    ?? throw new ArgumentNullException (nameof (writer));

            public virtual void WriteKeyword (string keyword)
                => writer.Write (keyword);

            public virtual void WriteNamespace (string @namespace)
                => writer.Write (@namespace);

            public virtual void WriteMemberName (string memberName)
                => writer.Write (memberName);

            public virtual void WriteParameterName (string parameterName)
                => writer.Write (parameterName);

            public virtual void WriteTypeName (string typeName)
                => writer.Write (typeName);

            public virtual void WriteTypeModifier (string modifier)
                => writer.Write (modifier);

            public virtual void Write (char c)
                => writer.Write (c);

            public virtual void Write (int n)
                => writer.Write (n);

            public virtual void Write (string s, params object [] formatArgs)
                => writer.Write (s, formatArgs);

            public virtual void WriteLine (string s, params object [] formatArgs)
                => writer.WriteLine (s, formatArgs);

            public virtual void WriteLine ()
                => writer.WriteLine ();
        }

        readonly TokenWriter writer;
        protected TokenWriter Writer => writer;

        public bool WriteLanguageKeywords { get; set; }
        public bool WriteTypeBeforeMemberName { get; set; } = true;
        public bool WriteReturnTypes { get; set; } = true;
        public bool WriteMemberTypes { get; set; } = true;

        public CSharpWriter (TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException (nameof (writer));

            this.writer = new TokenWriter (writer);
        }

        public CSharpWriter (TokenWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException (nameof (writer));

            this.writer = writer;
        }

        public virtual void VisitExceptionNode (ExceptionNode exception)
        {
            VisitTypeSpec (exception.Type);

            if (exception.Message != null)
                writer.Write (": {0}", exception.Message);

            if (exception.InnerException != null) {
                writer.Write (" ---> ");
                exception.InnerException.AcceptVisitor (this);
                writer.WriteLine ();
                writer.Write ("  --- End of inner exception stack trace ---");
            }

            if (exception.StackTrace != null) {
                writer.WriteLine ();
                exception.StackTrace.AcceptVisitor (this);
            }
        }

        protected virtual void WriteCapturedStackTraceDelimiter ()
        {
            writer.WriteLine ("--- End of stack trace from previous " +
                "location where exception was thrown ---");
        }

        public virtual void VisitStackTrace (StackTrace stackTrace)
        {
            for (int i = 0; stackTrace.CapturedTraces != null && i < stackTrace.CapturedTraces.Count; i++) {
                // note: we do not call Accept on the captured traces because
                // the inner-most trace would end up getting its frames dumped
                // twice due to the capturing - hence VisitStackFrames ()
                VisitStackFrames (stackTrace.CapturedTraces [i].Frames);
                WriteCapturedStackTraceDelimiter ();
            }

            VisitStackFrames (stackTrace.Frames);
        }

        void VisitStackFrames (IReadOnlyList<StackFrame> frames)
        {
            for (int i = 0; frames != null && i < frames.Count; i++)
                frames [i].AcceptVisitor (this);
        }

        public virtual void VisitStackFrame (StackFrame stackFrame)
        {
            writer.Write ("  at ");

            if (stackFrame.Member != null) {
                stackFrame.Member.AcceptVisitor (this);

                if (stackFrame.ILOffset == -1) {
                    writer.Write ("<0x{0:x5} + 0x{1:x5}>",
                        stackFrame.NativeAddress, stackFrame.NativeOffset);
                    if (stackFrame.MethodIndex != 0xffffff)
                        writer.Write (" {0}", stackFrame.MethodIndex);
                } else
                    writer.Write (" [0x{0:x5}]", stackFrame.ILOffset);

                writer.WriteLine (" in {0}:{1}",
                    stackFrame.FileName ?? "<filename unknown>",
                    stackFrame.Line
                );
            } else if (stackFrame.InternalMethod != null) {
                writer.Write ("(wrapper {0}) ", stackFrame.InternalMethod.WrapperType);
                stackFrame.InternalMethod.AcceptVisitor (this);
                writer.WriteLine ();
            } else
                writer.WriteLine ("<0x{0:x5} + 0x{1:x5}> <unknown method>",
                    stackFrame.NativeAddress, stackFrame.NativeOffset);
        }

        public virtual void VisitMethod (Method method)
        {
            if (WriteReturnTypes && method.ReturnType != null) {
                VisitTypeSpec (method.ReturnType);
                writer.Write (' ');
            }

            if (WriteTypeBeforeMemberName) {
                VisitDeclaringTypeSpec (method.DeclaringType);
                writer.Write ('.');
            }

            writer.WriteMemberName (method.Name);

            if (method.TypeArguments != null && method.TypeArguments.Count > 0) {
                writer.Write ('<');
                for (var i = 0; i < method.TypeArguments.Count; i++) {
                    if (i > 0) {
                        writer.Write (',');
                        writer.Write (' ');
                    }

                    VisitTypeSpec (method.TypeArguments [i]);
                }
                writer.Write ('>');
            }

            writer.Write (' ');
            writer.Write ('(');

            for (var i = 0; method.Parameters != null && i < method.Parameters.Count; i++) {
                if (i > 0) {
                    writer.Write (',');
                    writer.Write (' ');
                }

                method.Parameters [i].AcceptVisitor (this);
            }

            writer.Write (')');
        }

        public virtual void VisitField (Field field)
        {
            if (WriteMemberTypes) {
                VisitTypeSpec (field.FieldType);
                writer.Write (' ');
            }

            if (WriteTypeBeforeMemberName) {
                VisitDeclaringTypeSpec (field.DeclaringType);
                writer.Write ('.');
            }

            writer.WriteMemberName (field.Name);
        }

        public virtual void VisitProperty (Property property)
        {
            if (WriteMemberTypes) {
                VisitTypeSpec (property.PropertyType);
                writer.Write (' ');
            }

            if (WriteTypeBeforeMemberName) {
                VisitDeclaringTypeSpec (property.DeclaringType);
                writer.Write ('.');
            }

            writer.WriteMemberName (property.Name);
            writer.Write (' ');
            writer.Write ('{');
            writer.Write (' ');

            if (property.Getter != null) {
                writer.WriteKeyword ("get");
                writer.Write (';');
                writer.Write (' ');
            }

            if (property.Setter != null) {
                writer.WriteKeyword ("set");
                writer.Write (';');
                writer.Write (' ');
            }

            writer.Write ('}');
        }

        public virtual void VisitParameter (Parameter parameter)
        {
            if (parameter.IsOut) {
                writer.WriteKeyword ("out");
                writer.Write (' ');
            } else if (parameter.Type.IsByRef ()) {
                writer.WriteKeyword ("ref");
                writer.Write (' ');
            }

            VisitTypeSpec (parameter.Type, false);

            if (parameter.Name != null) {
                writer.Write (' ');
                writer.WriteParameterName (parameter.Name);
            }
        }

        public virtual void VisitDeclaringTypeSpec (TypeSpec typeSpec)
            => VisitTypeSpec (typeSpec, true);

        public void VisitTypeSpec (TypeSpec typeSpec)
            => VisitTypeSpec (typeSpec, true);

        public virtual void VisitTypeSpec (TypeSpec typeSpec, bool writeByRefModifier)
        {
            WriteTypeName (typeSpec);

            foreach (var modifier in typeSpec.Modifiers) {
                switch (modifier) {
                case TypeSpec.Modifier.Pointer:
                    writer.WriteTypeModifier ("*");
                    break;
                case TypeSpec.Modifier.ByRef:
                    if (writeByRefModifier)
                        writer.WriteTypeModifier ("&");
                    break;
                case TypeSpec.Modifier.BoundArray:
                    writer.WriteTypeModifier ("[*]");
                    break;
                default:
                    var rank = (byte)modifier;
                    if (rank >= 1 && rank <= 32)
                        writer.WriteTypeModifier ($"[{new string(',', rank - 1)}]");
                    else
                        throw new ArgumentOutOfRangeException (
                            $"invalid modifier: {modifier}",
                            nameof (typeSpec));
                    break;
                }
            }
        }

        void WriteTypeName (TypeSpec typeSpec)
        {
            if (WriteLanguageKeywords && typeSpec.Name.Namespace == "System") {
                switch (typeSpec.Name.Name) {
                case "Void":
                case "SByte":
                case "Byte":
                case "Double":
                case "Decimal":
                case "Char":
                case "String":
                case "Object":
                    writer.WriteKeyword (typeSpec.Name.Name.ToLowerInvariant ());
                    return;
                case "Boolean":
                    writer.WriteKeyword ("bool");
                    return;
                case "Int16":
                    writer.WriteKeyword ("short");
                    return;
                case "UInt16":
                    writer.WriteKeyword ("ushort");
                    return;
                case "Int32":
                    writer.WriteKeyword ("int");
                    return;
                case "UInt32":
                    writer.WriteKeyword ("uint");
                    return;
                case "Int64":
                    writer.WriteKeyword ("long");
                    return;
                case "UInt64":
                    writer.WriteKeyword ("ulong");
                    return;
                case "Single":
                    writer.WriteKeyword ("float");
                    return;
                case "nint":
                    writer.WriteKeyword ("nint");
                    return;
                case "nuint":
                    writer.WriteKeyword ("nuint");
                    return;
                case "nfloat":
                    writer.WriteKeyword ("nfloat");
                    return;
                }
            }

            if (typeSpec.Name.Namespace != null)
                writer.WriteNamespace (typeSpec.Name.Namespace + ".");

            int typeNamesConsumed = 0;
            int typeArgIndex = 0;

            foreach (var name in typeSpec.GetAllNames ()) {
                if (!name.Name.StartsWith ("🐵", StringComparison.Ordinal)) {
                    if (typeNamesConsumed++ > 0)
                        writer.Write ('.');

                    writer.WriteTypeName (name.Name);
                }

                if (name.TypeArgumentCount > 0) {
                    writer.Write ('<');
                    for (var i = 0; i < name.TypeArgumentCount; i++, typeArgIndex++) {
                        if (i > 0) {
                            writer.Write (',');
                            writer.Write (' ');
                        }

                        VisitTypeSpec (typeSpec.TypeArguments [typeArgIndex]);
                    }
                    writer.Write ('>');
                }
            }
        }
    }

    public abstract class Node
    {
        internal Node ()
        {
        }

        public abstract void AcceptVisitor (IReflectionRemotingVisitor visitor);
    }

    [JsonObject]
    public sealed class ExceptionNode : Node
    {
        public TypeSpec Type { get; }
        public string Message { get; }
        public StackTrace StackTrace { get; }
        public ExceptionNode InnerException { get; }

        [JsonConstructor]
        public ExceptionNode (
            TypeSpec type,
            string message,
            StackTrace stackTrace,
            ExceptionNode innerException)
        {
            Type = type;
            Message = message;
            StackTrace = stackTrace;
            InnerException = innerException;
        }

        public static ExceptionNode Create (Exception exception)
        {
            if (exception == null)
                return null;

            return new ExceptionNode (
                TypeSpec.Parse (exception.GetType ()),
                exception.Message,
                StackTrace.Create (new System.Diagnostics.StackTrace (exception, true)),
                ExceptionNode.Create (exception.InnerException));
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
            => visitor.VisitExceptionNode (this);

        public override string ToString ()
        {
            var writer = new StringWriter ();
            AcceptVisitor (new CSharpWriter (writer));
            return writer.ToString ();
        }
    }

    [JsonObject]
    public sealed class StackTrace : Node
    {
        public IReadOnlyList<StackFrame> Frames { get; }
        public IReadOnlyList<StackTrace> CapturedTraces { get; }

        [JsonConstructor]
        public StackTrace (
            IReadOnlyList<StackFrame> frames,
            IReadOnlyList<StackTrace> capturedTraces)
        {
            Frames = frames;
            CapturedTraces = capturedTraces;
        }

        public static StackTrace Create (System.Diagnostics.StackTrace trace)
            => new StackTrace (
                trace.GetFrames ()?.Select (StackFrame.Create)?.ToArray (),
                trace.GetCapturedTraces ()?.Select (StackTrace.Create)?.ToArray ());

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
            => visitor.VisitStackTrace (this);

        public StackTrace WithFrames (IEnumerable<StackFrame> frames)
            => new StackTrace (frames?.ToArray (), CapturedTraces);

        public StackTrace WithCapturedTraces (IEnumerable<StackTrace> capturedTraces)
            => new StackTrace (Frames, capturedTraces?.ToArray ());
    }

    [JsonObject]
    public sealed class StackFrame : Node
    {
        public string FileName { get; }
        public int Line { get; }
        public int Column { get; }
        public int ILOffset { get; }
        public int NativeOffset { get; }
        public long NativeAddress { get; }
        public uint MethodIndex { get; }
        public bool IsTaskAwaiter { get; }
        public Method InternalMethod { get; }
        public ITypeMember Member { get; }

        [JsonConstructor]
        public StackFrame (
            string fileName,
            int line,
            int column,
            int ilOffset,
            int nativeOffset,
            long nativeAddress,
            uint methodIndex,
            bool isTaskAwaiter,
            Method internalMethod,
            ITypeMember member)
        {
            FileName = fileName;
            Line = line;
            Column = column;
            ILOffset = ilOffset;
            NativeOffset = nativeOffset;
            NativeAddress = nativeAddress;
            MethodIndex = methodIndex;
            IsTaskAwaiter = isTaskAwaiter;
            InternalMethod = internalMethod;
            Member = member;
        }

        public static StackFrame Create (System.Diagnostics.StackFrame frame)
        {
            if (frame == null)
                return null;

            string fileName = null;
            bool isTaskAwaiter = false;
            ITypeMember member = null;

            try {
                fileName = frame.GetFileName ();
            } catch (SecurityException) {
                // CAS check failure
            }

            var method = frame.GetMethod ();
            if (method != null) {
                isTaskAwaiter = method.DeclaringType.IsTaskAwaiter () ||
                    method.DeclaringType.DeclaringType.IsTaskAwaiter ();

                var property = GetPropertyForMethodAccessor (method);
                if (property != null)
                    member = Property.Create (property);
                else
                    member = Method.Create (method);
            }

            return new StackFrame (
                fileName,
                frame.GetFileLineNumber (),
                frame.GetFileColumnNumber (),
                frame.GetILOffset (),
                frame.GetNativeOffset (),
                frame.GetMethodAddress (),
                frame.GetMethodIndex (),
                isTaskAwaiter,
                ParseInternalMethodName (frame.GetInternalMethodName ()),
                member);
        }

        static readonly Regex wrapperMethodPreamble = new Regex (@"^\(wrapper ([a-z\-]+)\) ");

        // parses mono_method_get_name_full
        static Method ParseInternalMethodName (string name)
        {
            if (name == null)
                return null;

            string methodName = null;
            string methodWrapperType = null;
            List<Parameter> methodParameters = null;
            TypeSpec methodReturnType = null;
            TypeSpec methodDeclaringType = null;

            name = wrapperMethodPreamble.Replace (name, ev => {
                methodWrapperType = ev.Groups [1].Value;
                return String.Empty;
            });

            var builder = new StringBuilder ();
            var depth = 0;

            for (int i = 0; i < name.Length; i++) {
                var c = name [i];
                if (methodDeclaringType == null && c == ':') {
                    methodDeclaringType = TypeSpec.Parse (builder.ToString ());
                    builder.Clear ();
                } else if (methodDeclaringType != null && methodName == null && c == '(') {
                    methodName = builder.ToString ().Trim ();
                    builder.Clear ();
                } else if ((c == ',' && depth == 0) || (i == name.Length - 1 && c == ')')) {
                    var typeSpec = TypeSpec.ParseBuilder (builder.ToString ().Trim ());
                    // mono_method_get_name_full writes C# keywords, so
                    // we need to translate them back to full type names
                    switch (typeSpec.Name.ToString ()) {
                    case "void":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Void");
                        break;
                    case "object":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Object");
                        break;
                    case "sbyte":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "SByte");
                        break;
                    case "byte":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Byte");
                        break;
                    case "short":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Int16");
                        break;
                    case "ushort":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "UInt16");
                        break;
                    case "int":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Int32");
                        break;
                    case "uint":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "UInt32");
                        break;
                    case "long":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Int64");
                        break;
                    case "ulong":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "UInt64");
                        break;
                    case "float":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Single");
                        break;
                    case "double":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Double");
                        break;
                    case "decimal":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Decimal");
                        break;
                    case "bool":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Boolean");
                        break;
                    case "char":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Char");
                        break;
                    case "string":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "Sbject");
                        break;
                    case "intptr":
                        typeSpec.Name = new TypeSpec.TypeName ("System", "IntPtr");
                        break;
                    }

                    if (methodReturnType == null) {
                        methodReturnType = typeSpec.Build ();
                    } else {
                        if (methodParameters == null)
                            methodParameters = new List<Parameter> ();
                        methodParameters.Add (new Parameter (typeSpec.Build ()));
                    }
                    builder.Clear ();
                } else {
                    if (c == '[')
                        depth++;
                    else if (c == ']')
                        depth--;
                    builder.Append (c);
                }
            }

            if (methodName == null)
                return null;

            return new Method (
                methodName,
                methodWrapperType,
                methodDeclaringType,
                methodReturnType,
                null,
                methodParameters);
        }

        static PropertyInfo GetPropertyForMethodAccessor (MethodBase method)
        {
            if (method == null || !method.IsSpecialName || method.Name.Length <= 4)
                return null;

            var bindingFlags = BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            var prop = method.DeclaringType.GetProperty (method.Name.Substring (4), bindingFlags);
            if (prop != null && (prop.GetMethod == method || prop.SetMethod == method))
                return prop;

            return null;
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
            => visitor.VisitStackFrame (this);
    }

    public interface ITypeMember
    {
        TypeSpec DeclaringType { get; }
        string Name { get; }
        void AcceptVisitor (IReflectionRemotingVisitor visitor);
    }

    public static class TypeMember
    {
        public static ITypeMember Create (MemberInfo memberInfo)
        {
            switch (memberInfo) {
            case MethodBase methodBase:
                return Method.Create (methodBase);
            case PropertyInfo propertyInfo:
                return Property.Create (propertyInfo);
            case FieldInfo fieldInfo:
                return Field.Create (fieldInfo);
            }

            throw new ArgumentException (
                $"cannot convert {memberInfo.GetType()} to {nameof (ITypeMember)}", nameof(memberInfo));
        }
    }

    [JsonObject]
    public sealed class Parameter : Node
    {
        public string Name { get; }
        public TypeSpec Type { get; }
        public bool IsOut { get; }
        public bool IsRetval { get; }
        public bool HasDefaultValue { get; }
        public object DefaultValue { get; }

        [JsonConstructor]
        public Parameter (
            TypeSpec type,
            string name = default,
            bool isOut = default,
            bool isRetval = default,
            bool hasDefaultValue = default,
            object defaultValue = default)
        {
            Name = name;
            Type = type;
            IsOut = isOut;
            IsRetval = isRetval;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        public static Parameter Create (ParameterInfo parameter)
        {
            if (parameter == null)
                return null;

            return new Parameter (
                TypeSpec.Parse (parameter.ParameterType),
                parameter.Name,
                parameter.IsOut,
                parameter.IsRetval,
                parameter.HasDefaultValue,
                // DefaultValue can be DBNull when HasDefaultValue is false. We should avoid
                // serializing that type as it is not serializable in .NET Core 2.0. In general,
                // there is no need to serialize whatever is in DefaultValue in this case.
                parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : null);
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
            => visitor.VisitParameter (this);
    }

    [JsonObject]
    public sealed class Method : Node, ITypeMember
    {
        public string Name { get; }
        public string WrapperType { get; }
        public TypeSpec DeclaringType { get; }
        public TypeSpec ReturnType { get; }
        public IReadOnlyList<TypeSpec> TypeArguments { get; }
        public IReadOnlyList<Parameter> Parameters { get; }

        [JsonConstructor]
        public Method (
            string name,
            string wrapperType,
            TypeSpec declaringType,
            TypeSpec returnType,
            IReadOnlyList<TypeSpec> typeArguments,
            IReadOnlyList<Parameter> parameters)
        {
            Name = name;
            WrapperType = wrapperType;
            DeclaringType = declaringType;
            ReturnType = returnType;
            TypeArguments = typeArguments;
            Parameters = parameters;
        }

        public static Method Create (MethodBase method)
        {
            if (method == null)
                return null;

            TypeSpec returnType = null;
            if (method is MethodInfo methodInfo)
                returnType = TypeSpec.Parse (methodInfo.ReturnType);

            List<TypeSpec> typeArguments = null;
            if (method.IsGenericMethod)
                typeArguments = method
                    .GetGenericArguments ()
                    .Select (t => TypeSpec.Parse (t))
                    .ToListOrNullIfEmpty ();

            return new Method (
                method.Name,
                null,
                TypeSpec.Parse (method.DeclaringType),
                returnType,
                typeArguments,
                method
                    .GetParameters ()
                    ?.Select (Parameter.Create)
                    .ToListOrNullIfEmpty ());
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
            => visitor.VisitMethod (this);
    }

    [JsonObject]
    public sealed class Field : Node, ITypeMember
    {
        public string Name { get; }
        public TypeSpec DeclaringType { get; }
        public TypeSpec FieldType { get; }
        public FieldAttributes Attributes { get; }

        [JsonConstructor]
        public Field (
            string name,
            TypeSpec declaringType,
            TypeSpec fieldType,
            FieldAttributes attributes)
        {
            Name = name;
            DeclaringType = declaringType;
            FieldType = fieldType;
            Attributes = attributes;
        }

        public static Field Create (FieldInfo field)
        {
            if (field == null)
                return null;

            return new Field (
                field.Name,
                TypeSpec.Parse (field.DeclaringType),
                TypeSpec.Parse (field.FieldType),
                field.Attributes);
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
            => visitor.VisitField (this);
    }

    [JsonObject]
    public sealed class Property : Node, ITypeMember
    {
        public string Name { get; }
        public TypeSpec DeclaringType { get; }
        public TypeSpec PropertyType { get; }
        public Method Getter { get; }
        public Method Setter { get; }

        [JsonConstructor]
        public Property (
            string name,
            TypeSpec declaringType,
            TypeSpec propertyType,
            Method getter,
            Method setter)
        {
            Name = name;
            DeclaringType = declaringType;
            PropertyType = propertyType;
            Getter = getter;
            Setter = setter;
        }

        public static Property Create (PropertyInfo property)
        {
            if (property == null)
                return null;

            return new Property (
                property.Name,
                TypeSpec.Parse (property.DeclaringType),
                TypeSpec.Parse (property.PropertyType),
                Method.Create (property.GetGetMethod (true)),
                Method.Create (property.GetSetMethod (true)));
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
            => visitor.VisitProperty (this);
    }

    static class EnumerableExtensions
    {
        public static List<T> ToListOrNullIfEmpty<T> (this IEnumerable<T> source)
        {
            List<T> list = null;
            foreach (var item in source)
                (list ?? (list = new List<T> ())).Add (item);
            return list;
        }
    }
}