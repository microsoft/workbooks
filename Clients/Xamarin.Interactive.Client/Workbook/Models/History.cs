//
// History.cs: a bash-like history recorder
//
// Authors:
//   Miguel de Icaza <miguel@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2008 Novell, Inc.
// Copyright 2013 Xamarin, Inc.
// Copyright 2017 Microsoft
//
// Dual-licensed under the terms of the MIT X11 license or the
// Apache License 2.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Xml;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Workbook.Models
{
    class History
    {
        const string TAG = nameof (History);

        string [] history;
        int head, tail;
        int cursor, count;
        int maxEntries;
        bool persist;

        public IEnumerable<string> Entries => history;

        public static readonly FilePath HistoryFile = ClientApp
            .SharedInstance
            .Paths
            .PreferencesDirectory
            .Combine ("history.xml");

        public History (IEnumerable<string> history, bool persist, int maxEntries = 300)
        {
            Init (history, persist, maxEntries);
        }

        public void Clear ()
        {
            history = new string [maxEntries];
            Save ();
        }

        void Init (IEnumerable<string> entries, bool persist, int maxEntries)
        {
            if (maxEntries < 1)
                throw new ArgumentException ("must be at least 1", nameof (maxEntries));

            this.maxEntries = maxEntries;
            this.persist = persist;

            history = new string [maxEntries];
            head = tail = cursor = 0;

            // If we were passed history to create ourselves with, append it. Otherwise, load history from
            // disk if persistent history is enabled.
            if (entries == null && persist)
                entries = Load ();

            if (entries != null) {
                AppendEntries (entries);
                cursor = head;
            }

            if (!persist)
                try {
                    File.Delete (HistoryFile);
                } catch (FileNotFoundException) {
                } catch (DirectoryNotFoundException) {
                } catch (Exception e) {
                    Log.Warning (
                        TAG,
                        "Could not delete existing history when switching to non-persistent history.",
                    e);
                }
        }

        void AppendEntries (IEnumerable<string> entries)
        {
            foreach (var entry in entries)
                if (!String.IsNullOrEmpty (entry))
                    Append (entry);
        }

        IEnumerable<string> Serialize ()
        {
            int start = (count == history.Length) ? head : tail;
            for (int i = start; i < start + count; i++)
                yield return history [i % history.Length];
        }

        public void Save ()
        {
            if (persist) {
                try {
                    HistoryFile.ParentDirectory.CreateDirectory ();
                    using (var historyStream = File.Open (HistoryFile, FileMode.Create))
                    using (var writer = new XmlTextWriter (historyStream, Utf8.Encoding)) {
                        writer.Formatting = Formatting.Indented;
                        writer.WriteStartDocument ();
                        writer.WriteStartElement ("ArrayOfString");

                        foreach (var historyItem in history.Where (hi => !String.IsNullOrWhiteSpace (hi)))
                            writer.WriteElementString ("string", historyItem);

                        writer.WriteEndElement ();
                        writer.WriteEndDocument ();
                    }
                } catch (Exception e) {
                    Log.Error (TAG, "Could not save history.", e);
                }
            }
        }

        public IEnumerable<string> Load ()
        {
            if (!HistoryFile.FileExists)
                return EmptyArray<string>.Instance;

            try {
                var entries = new List<string> ();
                using (var historyStream = HistoryFile.OpenRead ())
                using (var reader = new XmlTextReader (historyStream)) {
                    while (reader.Read ()) {
                        if (reader.Name == "string")
                            entries.Add (reader.ReadString ());
                    }
                }
                return entries;
            } catch (Exception e) {
                Log.Error (TAG, "Could not load history.", e);
                return EmptyArray<string>.Instance;
            }
        }

        public void Close ()
        {
            Save ();
        }

        /// <summary>
        /// Appends a value to the history
        /// </summary>
        public void Append (string s)
        {
            history [head] = s;
            head = (head + 1) % history.Length;

            if (head == tail)
                tail = (tail + 1 % history.Length);

            if (count != history.Length)
                count++;
        }

        /// <summary>
        /// Pushes the value to the history, on the slot that was created by PrepareLine
        /// </summary>
        /// <returns>The history.</returns>
        /// <param name="text">The text to update at the last slot</param>
        public void UpdateLastAppended (string text)
        {
            int location = head - 1;
            if (location < 0)
                location = history.Length - 1;
            history [location] = text;
        }

        /// <summary>
        /// Updates the current cursor location with the string,
        /// to support editing of history items.   For the current
        /// line to participate, an Append must be done before.
        /// </summary>
        public void Update (string s)
        {
            history [cursor] = s;
        }

        public void RemoveLast ()
        {
            head = head - 1;
            if (head < 0)
                head = history.Length - 1;
        }

        public void Accept (string s)
        {
            int t = head - 1;
            if (t < 0)
                t = history.Length - 1;

            history [t] = s;
        }

        public bool IsPreviousAvailable {
            get {
                if (count == 0)
                    return false;

                int next = cursor - 1;
                if (next < 0)
                    next = count - 1;

                return next < count && next != head;
            }
        }

        public bool IsNextAvailable {
            get {
                if (count == 0)
                    return false;

                int next = (cursor + 1) % history.Length;
                return next < count && next != head;
            }
        }

        /// <summary>
        /// Returns a string with the previous line contents, or
        /// null if there is no data in the history to move to.
        /// </summary>
        public string Previous ()
        {
            if (!IsPreviousAvailable)
                return null;

            cursor--;
            if (cursor < 0)
                cursor = history.Length - 1;

            return history [cursor];
        }

        public string Next ()
        {
            if (!IsNextAvailable)
                return null;

            cursor = (cursor + 1) % history.Length;
            return history [cursor];
        }

        public void CursorToEnd ()
        {
            if (head == tail)
                return;

            cursor = head;
        }

        public void Dump (TextWriter writer = null)
        {
            if (writer == null)
                writer = Console.Out;

            writer.WriteLine ("Head={0} Tail={1} Cursor={2} count={3}", head, tail, cursor, count);
            for (int i = 0; i < history.Length; i++) {
                writer.WriteLine (" {0} {1}: {2}", i == cursor ? "==>" : "   ", i, history [i]);
            }
        }

        public string SearchBackward (string term)
        {
            for (int i = 0; i < count; i++) {
                int slot = cursor - i - 1;

                if (slot < 0)
                    slot = history.Length + slot;

                if (slot >= history.Length)
                    slot = 0;

                if (history [slot] != null && history [slot].IndexOf (term, StringComparison.Ordinal) != -1) {
                    cursor = slot;
                    return history [slot];
                }
            }

            return null;
        }

        public string ToHtml (string text)
        {
            var sb = new StringBuilder ("<html><style>body { font-family: Courier; }</style><body>");
            sb.AppendFormat ("<p>From: {0}", text);
            sb.AppendFormat ("<p>Head={0}, Tail={1}, Cursor={2}, Count={3}", head, tail, cursor, count);
            for (int i = 0; i < history.Length; i++) {
                var e = history [i];
                if (e == null)
                    e = "[null]";

                sb.AppendFormat ("<p>{0} {1}: {2}", i == cursor? ">" : "&nbsp", i, e);
            }
            return sb.ToString ();
        }
    }
}