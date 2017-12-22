//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

using Newtonsoft.Json;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using Mono.Options;

namespace InstallerVerifier
{
    static class Program
    {
        enum VerifyResult
        {
            Signed,
            Unsigned,
            Skipped,
            Excluded
        }

        static readonly HashSet<string> extensionsToCheck = new HashSet<string>
        {
            ".dll",
            ".exe",
            ".js"
        };

        static int Main (string [] args)
        {
            var loggingLevelSwitch = new LoggingLevelSwitch {
                MinimumLevel = LogEventLevel.Information
            };

            Log.Logger = new LoggerConfiguration ()
                .WriteTo.Console ()
                .MinimumLevel.ControlledBy (loggingLevelSwitch)
                .CreateLogger ();

            Console.OutputEncoding = Encoding.UTF8;

            string workingPath = null;
            bool cleanWorkingPath = false;
            string exclusionsFile = null;
            bool updateExclusionsFile = false;
            string exclusionsBootstrapReason = null;
            bool showHelp = false;
            string msiFile = null;
            Exception optionsError = null;

            var optionSet = new OptionSet
            {
                { "o|output=", "Output path for intermediate work",
                    v => workingPath = v },
                { "x|exclude=", "Path to an exclusions JSON file (see below for format)",
                    v => exclusionsFile = v },
                { "exclude-update", "Update the exclusions file in place by removing " +
                    "entries in the exclusion file no longer present in the installer.",
                    v => updateExclusionsFile = true },
                { "exclude-bootstrap=", "Generate an initial exclusions file based on all unsigned files.",
                    v => exclusionsBootstrapReason = v },
                { "v|verbose", "Verbose", v => {
                    switch (loggingLevelSwitch.MinimumLevel) {
                        case LogEventLevel.Debug:
                            loggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
                            break;
                        case LogEventLevel.Verbose:
                            break;
                        default:
                            loggingLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                            break;
                    } } },
                { "h|?|help", "Show this help",
                    v => showHelp = true }
            };

            try {
                var remaining = optionSet.Parse (args);
                switch (remaining.Count) {
                case 0:
                    throw new Exception ("must specify an MSI file to process");
                case 1:
                    msiFile = remaining [0];
                    break;
                default:
                    throw new Exception ("only one MSI file may be specified");
                }
            } catch (Exception e) {
                showHelp = true;
                optionsError = e;
            }

            if (showHelp) {
                if (optionsError != null) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine ("Error: {0}", optionsError.Message);
                    Console.Error.WriteLine ();
                    Console.ResetColor ();
                }

                Console.Error.WriteLine (
                    "Usage: {0} [OPTIONS] MSI_FILE",
                    Path.GetFileName (typeof (Program).Assembly.Location));
                Console.Error.WriteLine ();
                Console.Error.WriteLine ("Options:");
                optionSet.WriteOptionDescriptions (Console.Error);
                return 1;
            }

            if (workingPath == null) {
                workingPath = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
                cleanWorkingPath = true;
            }

            ExclusionSet exclusions;
            try {
                exclusions = new ExclusionSet (exclusionsFile);
            } catch (Exception e) {
                Log.Fatal (e, "Error loading exclusions file {ExclusionsFile}", exclusionsFile);
                return 1;
            }

            int totalUnsigned = 0;

            try {
                var wxsOutputPath = Path.Combine (
                    workingPath,
                    Path.GetFileNameWithoutExtension (msiFile) + ".wxs");

                var extractPath = Path.Combine (workingPath, "contents");

                if (!Directory.Exists (extractPath))
                    Exec (
                        "wix/dark",
                        Path.Combine (FindWixTools (), "dark.exe"),
                        "-v",
                        "-sui",
                        "-o", wxsOutputPath,
                        "-x", extractPath,
                        msiFile);

                var fileMap = GenerateFileMap (wxsOutputPath);
                foreach (var entry in fileMap)
                    exclusions.MarkHandled (entry.Value.InstallPath);

                var stopwatch = new Stopwatch ();
                stopwatch.Start ();

                totalUnsigned = VerifySignatures (
                    fileMap,
                    exclusions,
                    exclusionsBootstrapReason);

                stopwatch.Stop ();

                Log.Debug ("Signature verification completed: {Duration}", stopwatch.Elapsed);

                if (exclusionsBootstrapReason != null || updateExclusionsFile)
                    exclusions.Save ();
            } finally {
                if (cleanWorkingPath) {
                    Log.Debug ("Removing working directory {WorkingPath}…", workingPath);
                    Directory.Delete (workingPath, recursive: true);
                }
            }
            
            if (totalUnsigned > 0) {
                Log.Error ("{TotalUnsigned} unsigned files", totalUnsigned);
                return 2;
            }

            return 0;
        }

        static int VerifySignatures (
            Dictionary<string, FileMapTarget> fileMap,
            ExclusionSet exclusions,
            string exclusionsBootstrapReason)
        {
            int totalUnsigned = 0;
            int i = 0;

            foreach (var entry in fileMap) {
                var extension = Path.GetExtension (entry.Value.SourcePath).ToLowerInvariant ();

                VerifyResult result;
                if (exclusions.Contains (entry.Value.InstallPath))
                    result = VerifyResult.Excluded;
                else if (extensionsToCheck.Contains (extension))
                    result = WinTrust.VerifyAuthenticodeTrust (
                        entry.Value.SourcePath) == SignatureVerificationResult.Valid
                        ? VerifyResult.Signed
                        : VerifyResult.Unsigned;
                else
                    result = VerifyResult.Skipped;

                var level = LogEventLevel.Verbose;

                switch (result) {
                case VerifyResult.Unsigned:
                    if (exclusionsBootstrapReason != null)
                        exclusions.Bootstrap (entry.Value.InstallPath, exclusionsBootstrapReason);
                    level = LogEventLevel.Error;
                    totalUnsigned++;
                    break;
                }

                Log.Write (
                    level,
                    "[{Is}/{Of}] {Result}: {Id} → {InstallPath}",
                    ++i,
                    fileMap.Count,
                    result,
                    entry.Key,
                    NormalizePath (entry.Value.InstallPath));
            }

            return totalUnsigned;
        }

        struct FileMapTarget
        {
            public string SourcePath;
            public string InstallPath;
        }

        static Dictionary<string, FileMapTarget> GenerateFileMap (string wxsFile)
        {
            XNamespace xmlns = "http://schemas.microsoft.com/wix/2006/wi";

            var fileMap = new Dictionary<string, FileMapTarget> ();

            var document = XDocument.Load (wxsFile);

            var fileQuery = document
                .Descendants (xmlns + "File")
                .Where (e => string.Equals (
                     (string)e.Attribute ("KeyPath"),
                     "yes",
                     StringComparison.OrdinalIgnoreCase));

            foreach (var fileElem in fileQuery) {
                var id = (string)fileElem.Attribute ("Id");

                var directories = new Stack<string> ();
                XElement directoryElem = fileElem.Parent?.Parent;
                var directoryElemName = xmlns + "Directory";
                while (directoryElem != null && directoryElem.Name == directoryElemName) {
                    var directoryName = (string)directoryElem.Attribute ("Name");
                    if (directoryName == null)
                        break;

                    directories.Push (directoryName);
                    directoryElem = directoryElem.Parent;
                }

                fileMap.Add (id, new FileMapTarget {
                    SourcePath = (string)fileElem.Attribute ("Source"),
                    InstallPath = Path.Combine (
                        string.Join (Path.DirectorySeparatorChar.ToString (), directories),
                        (string)fileElem.Attribute ("Name"))
                });
            }

            return fileMap;
        }

        sealed class ExclusionSet
        {
            readonly Dictionary<string, Exclusion> entries = new Dictionary<string, Exclusion> ();
            readonly string jsonPath;

            public ExclusionSet (string jsonPath = null)
            {
                this.jsonPath = jsonPath;

                if (jsonPath == null || !File.Exists (jsonPath))
                    return;

                using (var reader = new StreamReader (jsonPath)) {
                    var dict = JsonSerializer
                        .Create ()
                        .Deserialize<Dictionary<string, string>> (
                            new JsonTextReader (reader));

                    if (dict != null) {
                        foreach (var item in dict)
                            entries [item.Key] = new Exclusion {
                                Reason = item.Value
                            };
                    }
                }
            }

            public void Save ()
            {
                if (jsonPath == null)
                    return;

                using (var writer = new StreamWriter (jsonPath)) {
                    var jsonWriter = new JsonTextWriter (writer) {
                        Formatting = Formatting.Indented
                    };

                    jsonWriter.WriteStartObject ();

                    var query = entries
                            .Where (entry => entry.Value.Handled)
                            .Select (entry => new KeyValuePair<string, string> (
                                 entry.Key,
                                 entry.Value.Reason))
                            .OrderBy (entry => entry.Key);

                    foreach (var entry in query) {
                        jsonWriter.WritePropertyName (NormalizePath (entry.Key));
                        jsonWriter.WriteValue (entry.Value);
                    }

                    jsonWriter.WriteEndObject ();
                }
            }

            public bool Contains (string path)
                => entries.ContainsKey (NormalizePath (path));

            public void MarkHandled (string path)
            {
                if (entries.TryGetValue (path, out var exclusion))
                    exclusion.Handled = true;
            }

            public void Bootstrap (string path, string reason)
            {
                entries.Add (path, new Exclusion {
                    Reason = reason,
                    Handled = true
                });
            }
        }

        sealed class Exclusion
        {
            public bool Handled { get; set; }
            public string Reason { get; set; }
        }

        static string NormalizePath (string path)
            => path?.Replace ("\\", "/")
                .TrimStart ('.')
                .TrimStart ('/');

        static string FindWixTools ()
        {
            var path = Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages",
                "wix");

            return Path.Combine (
                Directory.EnumerateDirectories (path)
                    .OrderByDescending (Path.GetFileName)
                    .First (),
                "tools");
        }

        static void Exec (string logTag, string fileName, params string [] arguments)
        {
            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = string.Join (" ", arguments),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };

            process.OutputDataReceived += (o, e) => {
                if (e.Data != null)
                    Log.Verbose ("{Tag} → {Stdout}", logTag, e.Data);
            };

            process.ErrorDataReceived += (o, e) => {
                if (e.Data != null)
                    Log.Error ("{Tag} → {Stderr}", logTag, e.Data);
            };

            Log.Debug (
                "{Tag} § Exec {FileName} {Arguments}",
                logTag,
                process.StartInfo.FileName,
                process.StartInfo.Arguments);

            var stopwatch = new Stopwatch ();
            stopwatch.Start ();

            process.Start ();
            process.BeginOutputReadLine ();
            process.BeginErrorReadLine ();
            process.WaitForExit ();

            stopwatch.Stop ();

            Log.Debug ("{Tag} § Exec completed in {CompletionTime}", logTag, stopwatch.Elapsed);

            if (process.ExitCode != 0) {
                var exception = new Exception (
                    $"{process.StartInfo.FileName} {process.StartInfo.Arguments} " +
                    $"exited {process.ExitCode}");

                Log.Fatal (
                    exception,
                    "{Tag} § Exec {FileName} {Arguments} exited {ExitCode}",
                    logTag,
                    process.StartInfo.FileName,
                    process.StartInfo.Arguments,
                    process.ExitCode);

                throw exception;
            }
        }
    }
}