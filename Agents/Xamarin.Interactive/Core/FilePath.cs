//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Xamarin.Interactive.Core
{
    [TypeConverter (typeof (FilePathTypeConverter))]
    [Serializable]
    struct FilePath : IComparable<FilePath>, IComparable, IEquatable<FilePath>
    {
        class FilePathTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
                => sourceType == typeof (string) || base.CanConvertFrom (context, sourceType);

            public override object ConvertFrom (ITypeDescriptorContext context,
                CultureInfo culture, object value)
            {
                if (value == null)
                    return Empty;

                return value is string
                    ? (object)new FilePath ((string)value)
                    : base.ConvertFrom (context, culture, value);
            }
        }

        public static readonly FilePath Empty = new FilePath ();

        readonly string path;

        public FilePath (string path)
        {
            this.path = path == String.Empty ? null : path;
        }

        public bool IsNull => path == null;

        [DllImport ("libc")]
        static extern string realpath (string path, IntPtr resolvedName);

        static volatile bool haveRealpath = true;

        public string FullPath {
            get {
                if (String.IsNullOrEmpty (path))
                    return null;

                var fullPath = Path.GetFullPath (path);

                if (haveRealpath) {
                    try {
                        // Path.GetFullPath expands the path, but on Unix systems
                        // does not resolve symlinks. Always attempt to resolve
                        // symlinks via realpath.
                        var realPath = realpath (fullPath, IntPtr.Zero);
                        if (!String.IsNullOrEmpty (realPath))
                            fullPath = realPath;
                    } catch {
                        haveRealpath = false;
                    }
                }

                if (fullPath.Length == 0)
                    return null;

                if (fullPath [fullPath.Length - 1] == Path.DirectorySeparatorChar)
                    return fullPath.TrimEnd (Path.DirectorySeparatorChar);

                if (fullPath [fullPath.Length - 1] == Path.AltDirectorySeparatorChar)
                    return fullPath.TrimEnd (Path.AltDirectorySeparatorChar);

                return fullPath;
            }
        }

        public bool IsRooted => path != null && Path.IsPathRooted (path);
        public bool DirectoryExists => path != null && Directory.Exists (path);
        public bool FileExists => path != null && File.Exists (path);
        public bool Exists => path != null && (File.Exists (path) || Directory.Exists (path));
        public FilePath ParentDirectory => new FilePath (Path.GetDirectoryName (path));
        public string Extension => path != null ? Path.GetExtension (path) : null;
        public string Name => path != null ? Path.GetFileName (path) : null;
        public string NameWithoutExtension => path != null ? Path.GetFileNameWithoutExtension (path) : null;

        public string Checksum ()
        {
            if (path == null || DirectoryExists)
                return null;

            using (var sha256 = SHA256.Create ())
            using (var stream = OpenRead ())
                return sha256.ComputeHash (stream).ToHexString ();
        }

        public DirectoryInfo CreateDirectory ()
            => Directory.CreateDirectory (FullPath ?? ".");

        public IEnumerable<FilePath> EnumerateDirectories (string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            foreach (FilePath filePath in Directory.EnumerateDirectories (
                path, searchPattern, searchOption))
                yield return filePath;
        }

        public IEnumerable<FilePath> EnumerateFiles (string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            foreach (FilePath filePath in Directory.EnumerateFiles (
                path, searchPattern, searchOption))
                yield return filePath;
        }

        public FilePath ChangeExtension (string extension)
            => Path.ChangeExtension (path, extension);

        public FilePath Combine (params string [] paths)
            => new FilePath (Path.Combine (path ?? String.Empty, Path.Combine (paths)));

        public static FilePath Build (params string [] paths)
            => Empty.Combine (paths);

        public int CompareTo (FilePath other)
            => String.Compare (FullPath, other.FullPath, StringComparison.Ordinal);

        public int CompareTo (object obj)
            => obj is FilePath ? CompareTo ((FilePath)obj) : -1;

        public bool Equals (FilePath other)
            => String.Equals (FullPath, other.FullPath);

        public override bool Equals (object obj)
            => obj is FilePath && Equals ((FilePath)obj);

        public override int GetHashCode ()
        {
            var fp = FullPath;
            return fp == null ? 0 : fp.GetHashCode ();
        }

        public override string ToString () => path;

        public static implicit operator FilePath (string path)
            => new FilePath (path);

        public static implicit operator string (FilePath path)
            => String.IsNullOrEmpty (path.path) ? "." : path.path;

        public static bool operator == (FilePath a, FilePath b)
            => a.Equals (b);

        public static bool operator != (FilePath a, FilePath b)
            => !a.Equals (b);

        public static FilePath GetTempPath ()
            => Path.GetTempPath ();

        public FilePath GetTempFileName (string extension = null)
        {
            if (!String.IsNullOrWhiteSpace (extension)) {
                if (extension [0] == '.')
                    extension = extension.Substring (1);
            }

            if (String.IsNullOrWhiteSpace (extension))
                extension = "tmp";

            return Combine (Guid.NewGuid ().ToString ("N") + "." + extension);
        }

        public TemporaryFileStream GetTempFileStream (string extension = null)
        {
            var tempFileName = GetTempFileName (extension);
            tempFileName.ParentDirectory.CreateDirectory ();
            return new TemporaryFileStream (tempFileName);
        }

        public class TemporaryFileStream : FileStream
        {
            public FilePath FileName { get; }

            internal TemporaryFileStream (FilePath path)
                : base (path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
            {
                FileName = path;
            }
        }

        public FileStream OpenRead ()
            => File.OpenRead (this);

        /// <summary>
		/// Attempts to create a file with an exclusive lock on disk at the path
		/// represented by this instance. If the file cannot be created, a new
		/// similar file name will be chosen, up to <paramref name="maxAttempts"/>
		/// times before throwing an exception. For instance, if this instance
		/// represents the path '/tmp/image.jpeg' but that file already exists,
		/// the file '/tmp/image (1).jpeg' will be created, and so on.
		/// <paramref name="acquiredPath"/> will be set to the path of the file
		/// that was successfully created.
		/// </summary>
		/// <returns>
		/// An exclusive file stream whose access cannot be shared. Dispose the
		/// stream to release exclusivity.
		/// </returns>
        public FileStream ExclusiveCreateNewWithSimilarName (int maxAttempts, out FilePath acquiredPath)
        {
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException (nameof (maxAttempts), "must be >= 1");

            string fullPath = FullPath;
            string parentDirectory = null;
            string nameWithoutExtension = null;
            string extension = null;

            Exception lastException = null;

            for (int i = 1; i <= maxAttempts; i++) {
                if (!File.Exists (fullPath)) {
                    try {
                        var stream = File.Open (
                            fullPath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None);
                        acquiredPath = new FilePath (fullPath);
                        return stream;
                    } catch (Exception e) {
                        lastException = e;
                    }
                }

                // cache these for subsequent increments so we're only operating
                // on already computed string pieces instead of recomputing the
                // pieces from the original instance on each pass.
                if (parentDirectory == null) {
                    parentDirectory = ParentDirectory;
                    nameWithoutExtension = NameWithoutExtension;
                    extension = Extension;
                }

                fullPath = Path.Combine (parentDirectory, $"{nameWithoutExtension} ({i}){extension}");
            }

            acquiredPath = FilePath.Empty;
            throw new Exception ($"unable to create file after {maxAttempts} attempts", lastException);
        }

        public long FileSize
            => new FileInfo (this).Length;

        public bool IsChildOfDirectory (FilePath parentDirectory)
        {
            var childPath = this;
            var parentPath = parentDirectory.FullPath;
            var i = 0;

            while (!childPath.IsNull) {
                var fullChildPath = childPath.FullPath;
                if (fullChildPath.StartsWith (parentPath, StringComparison.Ordinal) &&
                        (i++ == 0 || fullChildPath != parentPath))
                    return true;

                childPath = childPath.ParentDirectory;
            }

            return false;
        }

        public FilePath GetRelativePath (FilePath relativeToPath)
        {
            if (relativeToPath.IsNull || IsNull)
                return Empty;

            var fullRelativeToPath = relativeToPath.FullPath + Path.DirectorySeparatorChar;
            var selfFullPath = FullPath;

            if (!selfFullPath.StartsWith (fullRelativeToPath, StringComparison.Ordinal))
                return Empty;

            return new FilePath (selfFullPath.Substring (fullRelativeToPath.Length));
        }
    }
}