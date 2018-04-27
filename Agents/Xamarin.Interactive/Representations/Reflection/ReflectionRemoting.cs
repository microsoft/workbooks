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

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Representations.Reflection
{
    interface IReflectionRemotingVisitor
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

    class CSharpWriter : IReflectionRemotingVisitor
    {
        public class TokenWriter
        {
            readonly TextWriter writer;

            public TokenWriter (TextWriter writer)
            {
                if (writer == null)
                    throw new ArgumentNullException (nameof (writer));

                this.writer = writer;
            }

            public virtual void WriteKeyword (string keyword)
            {
                writer.Write (keyword);
            }

            public virtual void WriteNamespace (string @namespace)
            {
                writer.Write (@namespace);
            }

            public virtual void WriteMemberName (string memberName)
            {
                writer.Write (memberName);
            }

            public virtual void WriteParameterName (string parameterName)
            {
                writer.Write (parameterName);
            }

            public virtual void WriteTypeName (string typeName)
            {
                writer.Write (typeName);
            }

            public virtual void WriteTypeModifier (string modifier)
            {
                writer.Write (modifier);
            }

            public virtual void Write (char c)
            {
                writer.Write (c);
            }

            public virtual void Write (int n)
            {
                writer.Write (n);
            }

            public virtual void Write (string s, params object [] formatArgs)
            {
                writer.Write (s, formatArgs);
            }

            public virtual void WriteLine (string s, params object [] formatArgs)
            {
                writer.WriteLine (s, formatArgs);
            }

            public virtual void WriteLine ()
            {
                writer.WriteLine ();
            }
        }

        readonly TokenWriter writer;

        protected TokenWriter Writer {
            get { return writer; }
        }

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
            } else if (parameter.Type.IsByRef) {
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
        {
            VisitTypeSpec (typeSpec, true);
        }

        public void VisitTypeSpec (TypeSpec typeSpec)
        {
            VisitTypeSpec (typeSpec, true);
        }

        public virtual void VisitTypeSpec (TypeSpec typeSpec, bool writeByRefModifier)
        {
            WriteTypeName (typeSpec);

            foreach (var modifier in typeSpec.Modifiers)
                writer.WriteTypeModifier (modifier.ToString ());

            if (typeSpec.IsByRef && writeByRefModifier)
                writer.WriteTypeModifier ("&");
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

    [Serializable]
    abstract class Node
    {
        internal Node ()
        {
        }

        public abstract void AcceptVisitor (IReflectionRemotingVisitor visitor);
    }

    [Serializable]
    sealed class ExceptionNode : Node
    {
        public TypeSpec Type { get; set; }
        public string Message { get; set; }
        public StackTrace StackTrace { get; set; }
        public ExceptionNode InnerException { get; set; }

        public static ExceptionNode Create (Exception exception)
        {
            if (exception == null)
                return null;

            return new ExceptionNode {
                Type = TypeSpec.Parse (exception.GetType ()),
                Message = exception.Message,
                InnerException = ExceptionNode.Create (exception.InnerException),
                StackTrace = StackTrace.Create (new System.Diagnostics.StackTrace (exception, true))
            };
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
        {
            visitor.VisitExceptionNode (this);
        }

        public override string ToString ()
        {
            var writer = new StringWriter ();
            AcceptVisitor (new CSharpWriter (writer));
            return writer.ToString ();
        }
    }

    [Serializable]
    sealed class StackTrace : Node
    {
        StackFrame [] frames;
        public IReadOnlyList<StackFrame> Frames {
            get { return frames; }
        }

        StackTrace [] capturedTraces;
        public IReadOnlyList<StackTrace> CapturedTraces {
            get { return capturedTraces; }
        }

        public static StackTrace Create (System.Diagnostics.StackTrace trace)
        {
            return new StackTrace {
                frames = trace.GetFrames ()?.Select (StackFrame.Create)?.ToArray (),
                capturedTraces = trace.GetCapturedTraces ()?.Select (StackTrace.Create)?.ToArray ()
            };
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
        {
            visitor.VisitStackTrace (this);
        }

        public StackTrace WithFramesAndCapturedTraces (IEnumerable<StackFrame> frames,
            IEnumerable<StackTrace> capturedTraces)
        {
            return new StackTrace {
                frames = frames?.ToArray (),
                capturedTraces = capturedTraces?.ToArray ()
            };
        }

        public StackTrace WithFrames (IEnumerable<StackFrame> frames)
        {
            return new StackTrace {
                frames = frames?.ToArray (),
                capturedTraces = this.capturedTraces?.ToArray ()
            };
        }

        public StackTrace WithCapturedTraces (IEnumerable<StackTrace> capturedTraces)
        {
            return new StackTrace {
                frames = this.frames?.ToArray (),
                capturedTraces = capturedTraces?.ToArray ()
            };
        }
    }

    [Serializable]
    sealed class StackFrame : Node
    {
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int ILOffset { get; set; }
        public ITypeMember Member { get; set; }
        public Method InternalMethod { get; set; }
        public int NativeOffset { get; set; }
        public long NativeAddress { get; set; }
        public uint MethodIndex { get; set; }
        public bool IsTaskAwaiter { get; set; }

        public static StackFrame Create (System.Diagnostics.StackFrame frame)
        {
            if (frame == null)
                return null;

            var frameNode = new StackFrame {
                Line = frame.GetFileLineNumber (),
                Column = frame.GetFileColumnNumber (),
                ILOffset = frame.GetILOffset (),
                NativeOffset = frame.GetNativeOffset (),
                NativeAddress = frame.GetMethodAddress (),
                InternalMethod = ParseInternalMethodName (frame.GetInternalMethodName ()),
                MethodIndex = frame.GetMethodIndex ()
            };

            try {
                frameNode.FileName = frame.GetFileName ();
            } catch (SecurityException) {
                // CAS check failure
            }

            var method = frame.GetMethod ();
            if (method == null)
                return frameNode;

            frameNode.IsTaskAwaiter = method.DeclaringType.IsTaskAwaiter () ||
                method.DeclaringType.DeclaringType.IsTaskAwaiter ();

            var property = GetPropertyForMethodAccessor (method);
            if (property != null)
                frameNode.Member = Property.Create (property);
            else
                frameNode.Member = Method.Create (method);

            return frameNode;
        }

        static readonly Regex wrapperMethodPreamble = new Regex (@"^\(wrapper ([a-z\-]+)\) ");

        // parses mono_method_get_name_full
        static Method ParseInternalMethodName (string name)
        {
            if (name == null)
                return null;

            var method = new Method ();

            name = wrapperMethodPreamble.Replace (name, ev => {
                method.WrapperType = ev.Groups [1].Value;
                return String.Empty;
            });

            var builder = new StringBuilder ();
            var depth = 0;

            for (int i = 0; i < name.Length; i++) {
                var c = name [i];
                if (method.DeclaringType == null && c == ':') {
                    method.DeclaringType = TypeSpec.Parse (builder.ToString ());
                    builder.Clear ();
                } else if (method.DeclaringType != null && method.Name == null && c == '(') {
                    method.Name = builder.ToString ().Trim ();
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

                    if (method.ReturnType == null) {
                        method.ReturnType = typeSpec.Build ();
                    } else {
                        if (method.Parameters == null)
                            method.Parameters = new List<Parameter> ();
                        method.Parameters.Add (new Parameter {
                            Type = typeSpec.Build ()
                        });
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

            if (method.Name == null)
                return null;

            return method;
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
        {
            visitor.VisitStackFrame (this);
        }
    }

    interface ITypeMember
    {
        TypeSpec DeclaringType { get; }
        string Name { get; }
        void AcceptVisitor (IReflectionRemotingVisitor visitor);
    }

    static class TypeMember
    {
        public static ITypeMember Create (MemberInfo memberInfo)
        {
            if (memberInfo is MethodBase)
                return Method.Create ((MethodBase)memberInfo);

            if (memberInfo is PropertyInfo)
                return Property.Create ((PropertyInfo)memberInfo);

            if (memberInfo is FieldInfo)
                return Field.Create ((FieldInfo)memberInfo);

            throw new ArgumentException (
                $"cannot convert {memberInfo.GetType()} to ITypeMember", nameof(memberInfo));
        }
    }

    [Serializable]
    sealed class Parameter : Node
    {
        public string Name { get; set; }
        public TypeSpec Type { get; set; }
        public bool IsOut { get; set; }
        public bool IsRetval { get; set; }
        public bool HasDefaultValue { get; set; }
        public object DefaultValue { get; set; }

        public static Parameter Create (ParameterInfo parameter)
        {
            if (parameter == null)
                return null;

            return new Parameter {
                Name = parameter.Name,
                Type = TypeSpec.Parse (parameter.ParameterType),
                IsOut = parameter.IsOut,
                IsRetval = parameter.IsRetval,
                HasDefaultValue = parameter.HasDefaultValue,
                // DefaultValue can be DBNull when HasDefaultValue is false. We should avoid
                // serializing that type as it is not serializable in .NET Core 2.0. In general,
                // there is no need to serialize whatever is in DefaultValue in this case.
                DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : null
            };
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
        {
            visitor.VisitParameter (this);
        }
    }

    [Serializable]
    sealed class Method : Node, ITypeMember
    {
        public string Name { get; set; }
        public string WrapperType { get; set; }
        public TypeSpec DeclaringType { get; set; }
        public TypeSpec ReturnType { get; set; }
        public List<TypeSpec> TypeArguments { get; set; }
        public List<Parameter> Parameters { get; set; }

        public static Method Create (MethodBase method)
        {
            if (method == null)
                return null;

            var methodNode = new Method {
                Name = method.Name,
                DeclaringType = TypeSpec.Parse (method.DeclaringType)
            };

            var methodInfo = method as MethodInfo;
            if (methodInfo != null)
                methodNode.ReturnType = TypeSpec.Parse (methodInfo.ReturnType);

            if (method.IsGenericMethod)
                methodNode.TypeArguments = method
                    .GetGenericArguments ()
                    .Select (t => TypeSpec.Parse (t))
                    .ToListOrNullIfEmpty ();

            var parameters = method.GetParameters ();
            if (parameters?.Length > 0)
                methodNode.Parameters = parameters.Select (Parameter.Create).ToListOrNullIfEmpty ();

            return methodNode;
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
        {
            visitor.VisitMethod (this);
        }
    }

    [Serializable]
    sealed class Field : Node, ITypeMember
    {
        public string Name { get; set; }
        public TypeSpec DeclaringType { get; set; }
        public TypeSpec FieldType { get; set; }
        public FieldAttributes Attributes { get; set; }

        public static Field Create (FieldInfo field)
        {
            if (field == null)
                return null;

            return new Field {
                Name = field.Name,
                DeclaringType = TypeSpec.Parse (field.DeclaringType),
                FieldType = TypeSpec.Parse (field.FieldType),
                Attributes = field.Attributes
            };
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
        {
            visitor.VisitField (this);
        }
    }

    [Serializable]
    sealed class Property : Node, ITypeMember
    {
        public string Name { get; set; }
        public TypeSpec DeclaringType { get; set; }
        public TypeSpec PropertyType { get; set; }
        public Method Getter { get; set; }
        public Method Setter { get; set; }

        public static Property Create (PropertyInfo property)
        {
            if (property == null)
                return null;

            return new Property {
                Name = property.Name,
                DeclaringType = TypeSpec.Parse (property.DeclaringType),
                PropertyType = TypeSpec.Parse (property.PropertyType),
                Getter = Method.Create (property.GetGetMethod (true)),
                Setter = Method.Create (property.GetSetMethod (true))
            };
        }

        public override void AcceptVisitor (IReflectionRemotingVisitor visitor)
        {
            visitor.VisitProperty (this);
        }
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