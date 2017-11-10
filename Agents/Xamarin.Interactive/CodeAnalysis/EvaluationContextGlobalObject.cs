//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis
{
    public class EvaluationContextGlobalObject
    {
        /// <summary>
        /// This will be invoked when applying
        /// <see cref="Xamarin.Interactive.Scripting.AssignmentMonitorSyntaxRewriter"/>
        /// to the compilation from InteractiveSession before emission. All assignments
        /// will be written to capture the assigned value at the point of assignment.
        /// This is the entrypoint to produce a Sketches/Playground-like live result
        /// environment.
        /// </summary>
        [EditorBrowsable (EditorBrowsableState.Never)]
        public T __AssignmentMonitor<T> (T value,
            string symbolName, string nodeType, int spanStart, int spanEnd)
        {
            return value;
        }

        /// <summary>
        /// Set this attribute on public static members to get an entry in the `help` table.
        /// </summary>
        protected class InteractiveHelpAttribute : Attribute
        {
            public string Description { get; set; }
            public bool ShowReturnType { get; set; } = true;
            public bool LiveInspectOnly { get; set; }
        }

        readonly Agent agent;

        internal EvaluationContextGlobalObject (Agent agent)
        {
            if (agent == null)
                throw new ArgumentNullException (nameof(agent));

            this.agent = agent;
        }

        internal EvaluationContext EvaluationContext { get; set; }

        ReplHelp cachedHelp;

        ReplHelp GetCachedHelp ()
        {
            if (cachedHelp != null)
                return cachedHelp;

            cachedHelp = new ReplHelp ();

            var bindingFlags =
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy;

            var attrMembers = GetType ()
                .GetMembers (bindingFlags)
                .Select (member => new {
                    Member = member,
                    Attribute = member.GetCustomAttribute<InteractiveHelpAttribute> (true)
                })
                .Where (pair => {
                    if (pair.Attribute == null)
                        return false;

                    if (pair.Attribute.LiveInspectOnly &&
                        agent.ClientSessionUri.SessionKind != ClientSessionKind.LiveInspection)
                        return false;

                    return true;
                });

            foreach (var m in attrMembers)
                cachedHelp.Add (new ReplHelp.Item (
                    TypeMember.Create (m.Member),
                    m.Attribute.Description,
                    m.Attribute.ShowReturnType
                ));

            return cachedHelp;
        }

        [InteractiveHelp (Description = "This help text", ShowReturnType = false)]
        public object help => GetCachedHelp ();

        [InteractiveHelp (Description = "Direct public access to the agent powering the interactive session")]
        public IAgent InteractiveAgent => agent;

        [InteractiveHelp (Description = "Uses a Stopwatch to time the specified action delegate")]
        public static TimeSpan Time (Action action)
        {
            var stopwatch = new Stopwatch ();
            stopwatch.Start ();
            action ();
            stopwatch.Stop ();
            return stopwatch.Elapsed;
        }

        public T GetObject<T> (long handle)
        {
            var obj = ObjectCache.Shared.GetObject (handle);
            if (obj == null)
                throw new ArgumentException ("Invalid handle");
            return (T)obj;
        }

        [InteractiveHelp (Description = "Shows declared global variables", ShowReturnType = false)]
        public object GetVars ()
        {
            var variables = EvaluationContext.GetGlobalVariables ();
            if (variables == null || variables.Count == 0)
                return null;

            var variablesTable = new DictionaryInteractiveObject (
                0,
                agent.RepresentationManager.Prepare,
                title: "Declared Global Variables");

            foreach (var variable in variables) {
                if (variable.ValueReadException != null)
                    variablesTable.Add (variable.Field, variable.ValueReadException, true);
                else
                    variablesTable.Add (variable.Field, variable.Value);
            }

            variablesTable.Initialize ();
            return variablesTable;
        }

        [InteractiveHelp (
            Description = "Clear all previous REPL results",
            ShowReturnType = false,
            LiveInspectOnly = true)]
        public static readonly Guid clear = new Guid ("c29f6037-d321-4911-b38a-6c1707df07bb");

        [InteractiveHelp (Description = "Get or set the current thread culture by name")]
        public static string CurrentCultureName {
            get { return Thread.CurrentThread.CurrentCulture.Name; }
            set { InteractiveCulture.CurrentCulture = CultureInfo.GetCultureInfo (value); }
        }

        [InteractiveHelp (Description = "Get or set the current thread culture")]
        public static CultureInfo CurrentCulture {
            get { return Thread.CurrentThread.CurrentCulture; }
            set { InteractiveCulture.CurrentCulture = value; }
        }

        [InteractiveHelp (Description = "Render an image from a path or URI, or SVG data")]
        public static Image Image (string uriOrTextData)
        {
            if (uriOrTextData == null)
                throw new ArgumentNullException (nameof (uriOrTextData));

            if (File.Exists (uriOrTextData)) {
                var data = File.ReadAllBytes (uriOrTextData);
                switch (Path.GetExtension (uriOrTextData)) {
                case ".png":
                    return new Image (ImageFormat.Png, data);
                case ".jpg":
                case ".jpeg":
                    return new Image (ImageFormat.Jpeg, data);
                case ".gif":
                    return new Image (ImageFormat.Gif, data);
                case ".svg":
                    return new Image (ImageFormat.Svg, data);
                default:
                    return new Image (ImageFormat.Unknown, data);
                }
            }

            return Representations.Image.FromData (uriOrTextData);
        }

        [InteractiveHelp (Description = "Render an image from a path or URI")]
        public static Image Image (Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException (nameof (uri));

            return Image (uri.OriginalString);
        }

        [InteractiveHelp (Description = "Render an image from raw data")]
        public static Image Image (byte [] data)
        {
            if (data == null)
                throw new ArgumentNullException (nameof (data));

            return Representations.Image.FromData (data);
        }

        [InteractiveHelp (Description = "Render an image from a stream")]
        public static Task<Image> ImageAsync (Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException (nameof (stream));

            return Representations.Image.FromStreamAsync (stream);
        }
    }
}