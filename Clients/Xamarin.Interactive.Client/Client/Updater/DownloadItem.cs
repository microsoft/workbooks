//
// DownloadItem.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.Updater
{
    sealed class DownloadItem : INotifyPropertyChanged
    {
        const int bufferSize = 16 * 1024;

        static readonly TimeSpan propertyUpdateThreshold = TimeSpan.FromSeconds (1 / 30.0);

        readonly Stopwatch stopwatch = new Stopwatch ();

        public TimeSpan ElapsedTime => stopwatch.Elapsed;

        public Uri SourceUri { get; }
        public FilePath TargetDirectory { get; }
        public string Md5Hash { get; }
        public bool UseExactFileName { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        Uri actualSourceUri;
        public Uri ActualSourceUri {
            get { return actualSourceUri; }
            set {
                if (actualSourceUri != value) {
                    actualSourceUri = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        FilePath targetFile;
        public FilePath TargetFile {
            get { return targetFile; }
            set {
                if (targetFile != value) {
                    targetFile = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        public ulong TotalBytes { get; private set; }
        public ulong ProgressBytes { get; private set; }

        DateTime progressLastNotify;
        double progress;
        public double Progress {
            get { return progress; }
            set {
                if (progress == value)
                    return;
                
                progress = value;

                if (progress == 0 ||progress == 1 ||
                    DateTime.UtcNow - progressLastNotify >= propertyUpdateThreshold) {
                    progressLastNotify = DateTime.UtcNow;
                    MainThread.Post (() => NotifyPropertyChanged (nameof (Progress)));
                }
            }
        }

        public DownloadItem (
            Uri sourceUri,
            FilePath targetDirectory,
            string md5Hash = null,
            bool useExactFileName = false)
        {
            if (sourceUri == null)
                throw new ArgumentNullException (nameof (sourceUri));

            if (targetDirectory.IsNull)
                throw new ArgumentNullException (nameof (targetDirectory));

            SourceUri = sourceUri;
            TargetDirectory = targetDirectory;
            Md5Hash = md5Hash;
            UseExactFileName = useExactFileName;
        }

        public async Task DownloadAsync (CancellationToken cancellationToken = default (CancellationToken))
        {
            ActualSourceUri = SourceUri;
            TargetFile = TargetFile;
            TotalBytes = 0;
            ProgressBytes = 0;
            Progress = 0;

            try {
                stopwatch.Restart ();

                using (var httpClient = ClientApp.SharedInstance.Updater.CreateHttpClient ())
                    await DownloadAsync (httpClient, cancellationToken);
            } finally {
                stopwatch.Stop ();
            }
        }

        FileStream CreateOutputStream (ref FilePath path)
        {
            if (UseExactFileName)
                return File.Open (path, FileMode.Create, FileAccess.Write, FileShare.None);

            return path.ExclusiveCreateNewWithSimilarName (100, out path);
        }

        async Task DownloadAsync (HttpClient httpClient, CancellationToken cancellationToken)
        {
            var response = (await httpClient.GetAsync (
                    SourceUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)).EnsureSuccessStatusCode ();

            cancellationToken.ThrowIfCancellationRequested ();

            ActualSourceUri = response.Content.Headers.ContentLocation ?? SourceUri;
            TotalBytes = (ulong)response.Content.Headers.ContentLength.GetValueOrDefault ();

            var _targetFile = TargetDirectory.Combine (Path.GetFileName (ActualSourceUri.LocalPath));

            using (var hash = MD5.Create ())
            using (var sourceStream = await response.Content.ReadAsStreamAsync ().ConfigureAwait (false))
            using (var outputStream = CreateOutputStream (ref _targetFile)) {
                cancellationToken.ThrowIfCancellationRequested ();

                TargetFile = _targetFile;

                var buffer = new byte [bufferSize];
                int read;

                while ((read = await sourceStream
                    .ReadAsync (buffer, 0, buffer.Length, cancellationToken)
                    .ConfigureAwait (false)) != 0) {
                    cancellationToken.ThrowIfCancellationRequested ();

                    await outputStream
                        .WriteAsync (buffer, 0, read, cancellationToken)
                        .ConfigureAwait (false);

                    hash.TransformBlock (buffer, 0, read, buffer, 0);

                    ProgressBytes += (ulong)read;
                    Progress = TotalBytes == 0 ? 0.0 : ProgressBytes / (double)TotalBytes;
                }

                hash.TransformFinalBlock (buffer, 0, 0);

                await outputStream
                    .FlushAsync (cancellationToken)
                    .ConfigureAwait (false);

                var fileLength = new FileInfo (TargetFile).Length;
                if (fileLength != (long)TotalBytes)
                    throw new DamagedDownloadException (
                        $"expected {TotalBytes} bytes for {TargetFile} " +
                        $"but size on disk is {fileLength} bytes");
                
                if (Md5Hash != null) {
                    var actualHash = hash.Hash.ToHexString ();
                    if (!String.Equals (Md5Hash, actualHash, StringComparison.OrdinalIgnoreCase))
                        throw new DamagedDownloadException (
                            $"checksum ({actualHash}) for {TargetFile} does " +
                            $"not match expected checksum {Md5Hash}");
                }
            }
        }
    }
}