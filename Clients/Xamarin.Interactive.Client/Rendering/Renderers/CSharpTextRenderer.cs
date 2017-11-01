//
// CSharpTextRenderer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.IO;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Rendering.Renderers
{
    sealed class CSharpTextRenderer : CSharpWriter
    {
        int exceptionDepth;

        public Predicate<StackFrame> StackFrameFilter { get; set; }

        new HtmlWriter Writer {
            get { return (HtmlWriter)base.Writer; }
        }

        public CSharpTextRenderer (TextWriter writer) : base (new HtmlWriter (writer))
        {
            if (writer == null)
                throw new ArgumentNullException (nameof (writer));

            WriteLanguageKeywords = true;
        }

        class HtmlWriter : CSharpWriter.TokenWriter
        {
            readonly TextWriter writer;

            public bool ShouldWriteNamespace { get; set; }

            public HtmlWriter (TextWriter writer) : base (writer)
            {
                this.writer = writer;
            }

            public void WriteEscaped (string str)
            {
                writer.WriteHtmlEscaped (str, newlineToBr: true);
            }

            public void Write (string str)
            {
                writer.Write (str);
            }

            public override void WriteNamespace (string @namespace)
            {
                if (ShouldWriteNamespace)
                    WriteEscaped (@namespace);
            }

            public override void WriteTypeName (string typeName)
            {
                WriteEscaped (typeName);
            }

            public override void WriteMemberName (string memberName)
            {
                WriteEscaped (memberName);
            }

            public override void WriteParameterName (string parameterName)
            {
                WriteEscaped (parameterName);
            }

            public override void Write (char c)
            {
                switch (c) {
                case '<':
                    writer.Write ("&lt;");
                    break;
                case '>':
                    writer.Write ("&gt;");
                    break;
                case '&':
                    writer.Write ("&amp;");
                    break;
                default:
                    writer.Write (c);
                    break;
                }
            }

            public override void WriteKeyword (string keyword)
            {
                WriteSpan ("keyword", keyword);
            }

            public void WriteSpan (string cssClass, string contents)
            {
                WriteSpan (cssClass, () => WriteEscaped (contents));
            }

            public void WriteSpan (string cssClass, Action contents)
            {
                writer.Write ("<span class=\"" + cssClass + "\">");
                contents ();
                writer.Write ("</span>");
            }
        }

        public override void VisitExceptionNode (ExceptionNode exception)
        {
            if (exceptionDepth == 0)
                Writer.Write ("<div class=\"exception root\">");
            else
                Writer.Write ("<div class=\"exception inner\">");

            Writer.Write ("<h1 onclick=\"exceptionToggle(this)\">");

            VisitTypeSpec (exception.Type);

            if (!String.IsNullOrEmpty (exception.Message)) {
                Writer.Write (": ");
                Writer.WriteSpan ("message", exception.Message);
            }

            Writer.Write ("</h1>");
            Writer.Write ("<div class=\"contents\">");

            exception.StackTrace.AcceptVisitor (this);

            if (exception.InnerException != null) {
                exceptionDepth++;
                exception.InnerException.AcceptVisitor (this);
                exceptionDepth--;
            }

            Writer.Write ("</div>");
            Writer.Write ("</div>");
        }

        protected override void WriteCapturedStackTraceDelimiter ()
        {
        }

        public override void VisitStackFrame (StackFrame stackFrame)
        {
            var filter = StackFrameFilter;
            if (filter != null && !filter (stackFrame))
                return;

            Writer.Write ("<div class=\"stack-frame\">");
            Writer.WriteSpan ("at", "at");
            Writer.Write (' ');

            if (stackFrame.Member != null)
                stackFrame.Member.AcceptVisitor (this);
            else if (stackFrame.InternalMethod != null) {
                Writer.WriteSpan ("wrapper", $"(wrapper {stackFrame.InternalMethod.WrapperType})");
                Writer.Write (' ');
                stackFrame.InternalMethod.AcceptVisitor (this);
            } else
                Writer.WriteEscaped (String.Format ("<0x{0:x5} + 0x{1:x5}> <unknown method>",
                    stackFrame.NativeAddress, stackFrame.NativeOffset));

            if (stackFrame.FileName != null) {
                Writer.WriteSpan ("sloc", () => {
                    Writer.Write (' ');
                    Writer.WriteSpan ("in", "in");
                    Writer.Write (' ');
                    Writer.Write ("<span class=\"filename\" title=\"");
                    Writer.WriteEscaped (stackFrame.FileName);
                    Writer.Write ("\">");

                    var exists = File.Exists (stackFrame.FileName);
                    if (exists) {
                        Writer.Write ("<a href=\"monodevelop://open?file=");
                        Writer.WriteEscaped (stackFrame.FileName);
                        Writer.Write ("&line=");
                        Writer.Write (stackFrame.Line);
                        Writer.Write ("&column=");
                        Writer.Write (stackFrame.Column);
                        Writer.Write ("\">");
                    }

                    Writer.WriteEscaped (Path.GetFileName (stackFrame.FileName));

                    if (exists)
                        Writer.Write ("</a>");

                    Writer.Write ("</span>");
                    Writer.Write (':');
                    Writer.WriteSpan ("line", stackFrame.Line.ToString ());
                });
            }

            Writer.Write ("</div>");
        }

        public override void VisitMethod (Method method)
        {
            Writer.WriteSpan ("method", () => base.VisitMethod (method));
        }

        public override void VisitProperty (Property property)
        {
            Writer.WriteSpan ("property", () => base.VisitProperty (property));
        }

        public override void VisitParameter (Parameter parameter)
        {
            Writer.WriteSpan ("parameter", () => base.VisitParameter (parameter));
        }

        public override void VisitDeclaringTypeSpec (TypeSpec typeSpec)
        {
            Writer.ShouldWriteNamespace = true;
            base.VisitDeclaringTypeSpec (typeSpec);
            Writer.ShouldWriteNamespace = false;
        }

        public override void VisitTypeSpec (TypeSpec typeSpec, bool writeByRefModifier)
        {
            var stringWriter = new StringWriter ();
            var csharpWriter = new CSharpWriter (stringWriter);
            csharpWriter.VisitTypeSpec (typeSpec, writeByRefModifier);

            Writer.Write ("<span class=\"typespec\" title=\"");
            Writer.WriteEscaped (stringWriter.ToString ());
            Writer.Write ("\">");

            base.VisitTypeSpec (typeSpec, writeByRefModifier);

            Writer.Write ("</span>");
        }
    }
}