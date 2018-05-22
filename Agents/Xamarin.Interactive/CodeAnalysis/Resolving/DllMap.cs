// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    sealed class DllMap : IEnumerable<KeyValuePair<DllMap.Entity, DllMap.Entity>>
    {
        public struct Entity : IEquatable<Entity>
        {
            public string LibraryName { get; }
            public string SymbolName { get; }

            public Entity (
                string libraryName,
                string symbolName = null)
            {
                LibraryName = libraryName;
                SymbolName = symbolName;
            }

            public bool Equals (Entity other)
                => LibraryName == other.LibraryName && SymbolName == other.SymbolName;

            public override bool Equals (object obj)
                => obj is Entity entity && Equals (entity);

            public override int GetHashCode ()
                => Hash.Combine (LibraryName, SymbolName);

            public override string ToString ()
                => $"({LibraryName ?? "(null)"}, {SymbolName ?? "(null)"})";
        }

        internal struct Filter
        {
            public string OperatingSystem { get; }
            public string Cpu { get; }
            public string WordSize { get; }

            public Filter (
                string operatingSystem,
                string cpu,
                string wordSize)
            {
                OperatingSystem = operatingSystem;
                Cpu = cpu;
                WordSize = wordSize;
            }

            public bool Matches (Filter targetFilter)
            {
                if (OperatingSystem != null &&
                    targetFilter.OperatingSystem != null &&
                    !Matches (targetFilter.OperatingSystem, OperatingSystem))
                    return false;

                if (Cpu != null &&
                    targetFilter.Cpu != null &&
                    !Matches (targetFilter.Cpu, Cpu))
                    return false;

                if (WordSize != null &&
                    targetFilter.WordSize != null &&
                    !Matches (targetFilter.WordSize, WordSize))
                    return false;

                return true;
            }

            static bool Matches (string value, string predicate)
            {
                if (string.IsNullOrEmpty (predicate) || string.IsNullOrEmpty (value))
                    return false;

                var invert = false;

                if (predicate [0] == '!') {
                    invert = true;
                    predicate = predicate.Substring (1);
                }

                foreach (var predicateItem in predicate.Split (',')) {
                    if (predicateItem == value)
                        return !invert;
                }

                return invert;
            }
        }

        readonly Filter targetFilter;
        readonly Dictionary<Entity, Entity> map = new Dictionary<Entity, Entity> ();

        public DllMap () : this (Runtime.CurrentProcessRuntime)
        {
        }

        public DllMap (Runtime targetRuntime)
        {
            var os = targetRuntime.OSPlatform.ToString ().ToLowerInvariant ();

            string cpu = null;
            string wordSize = null;
            switch (targetRuntime.Architecture) {
            case Architecture.X86:
                cpu = "x86";
                wordSize = "32";
                break;
            case Architecture.X64:
                cpu = "x86-64";
                wordSize = "64";
                break;
            case Architecture.Arm:
                cpu = "arm";
                wordSize = "32";
                break;
            case Architecture.Arm64:
                cpu = "armv8";
                wordSize = "64";
                break;
            }

            targetFilter = new Filter (os, cpu, wordSize);
        }

        public bool TryMap (Entity source, out Entity target)
        {
            if (!map.TryGetValue (source, out target) &&
                !map.TryGetValue (new Entity (source.LibraryName), out target)) {
                target = source;
                return false;
            }

            target = new Entity (target.LibraryName, target.SymbolName ?? source.SymbolName);
            return true;
        }

        public bool TryMap (Entity source, string basePath, out Entity target)
        {
            if (basePath == null)
                throw new ArgumentNullException (nameof (basePath));

            if (TryMap (source, out target)) {
                target = new Entity (
                    Path.Combine (basePath, target.LibraryName),
                    target.SymbolName);
                return true;
            }

            return false;
        }

        public DllMap Add (Entity source, Entity target)
        {
            map.Add (source, target);
            return this;
        }

        public DllMap LoadXml (string configurationXml)
        {
            var document = new XmlDocument ();
            document.LoadXml (configurationXml);
            return Load (document);
        }

        public DllMap Load (string configurationFile)
        {
            var document = new XmlDocument ();
            document.Load (configurationFile);
            return Load (document);
        }

        public DllMap Load (XmlDocument document)
        {
            if (document == null)
                throw new ArgumentNullException (nameof (document));

            if (document.DocumentElement == null)
                throw new ArgumentException (
                    "document is not loaded or has no root element",
                    nameof (document));

            if (document.DocumentElement.Name != "configuration")
                return this;

            string GetAttribute (XmlElement elem, string attributeName)
            {
                var value = elem.GetAttribute (attributeName);
                return string.IsNullOrEmpty (value) ? null : value;
            }

            Filter CreateFilter (XmlElement elem)
                => new Filter (
                    GetAttribute (elem, "os"),
                    GetAttribute (elem, "cpu"),
                    GetAttribute (elem, "wordsize"));

            foreach (var node in document.DocumentElement.ChildNodes) {
                if (node is XmlElement elem && elem.Name == "dllmap") {
                    if (!CreateFilter (elem).Matches (targetFilter))
                        continue;

                    var sourceLibrary = GetAttribute (elem, "dll");
                    var targetLibrary = GetAttribute (elem, "target");

                    foreach (var childNode in elem.ChildNodes) {
                        if (childNode is XmlElement childElem && childElem.Name == "dllentry") {
                            if (!CreateFilter (childElem).Matches (targetFilter))
                                continue;

                            var symbolTargetLibrary = GetAttribute (childElem, "dll") ?? targetLibrary;
                            var sourceSymbol = GetAttribute (childElem, "name");
                            var targetSymbol = GetAttribute (childElem, "target");

                            if (symbolTargetLibrary != null)
                                map [new Entity (sourceLibrary, sourceSymbol)]
                                    = new Entity (symbolTargetLibrary, targetSymbol);
                        }
                    }

                    if (!elem.HasChildNodes && targetLibrary != null)
                        map [new Entity (sourceLibrary)] = new Entity (targetLibrary);
                }
            }

            return this;
        }

        public IEnumerator<KeyValuePair<Entity, Entity>> GetEnumerator ()
            => map.GetEnumerator ();

        IEnumerator IEnumerable.GetEnumerator ()
            => GetEnumerator ();
    }
}