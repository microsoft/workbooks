//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using Xamarin.Interactive.Collections.PropertyList;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.MTouch
{
    public class MlaunchNotFoundException : Exception {}

    public static class MTouchSdkTool
    {
        const string TAG = nameof (MTouchSdkTool);

        const string XamarinStudioMlaunchPath = "/Applications/Xamarin Studio.app/Contents/Resources/lib/monodevelop/" +
            "AddIns/MonoDevelop.IPhone/mlaunch.app/Contents/MacOS/mlaunch";
        const string XamariniOSMlaunchPath = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch";
        const string DefaultSdkRoot = "/Applications/Xcode.app";

        public static readonly Version RequiredMinimumXcodeVersion = new Version (9, 0);

        // These are in order of preference.
        static readonly string [] MlaunchPaths = {
            XamariniOSMlaunchPath,
            XamarinStudioMlaunchPath
        };

        public static string GetMlaunchPath ()
        {
            foreach (var mlaunchPath in MlaunchPaths)
                if (File.Exists (mlaunchPath))
                    return mlaunchPath;
            throw new MlaunchNotFoundException ();
        }

        public static async Task<string> GetXcodeSdkRootAsync ()
        {
            // Mimicking VSmac behavior, xcode-select is checked last
            var sdkRoot = GetXamarinStudioXcodeSdkRoot () ?? GetDefaultXCodeSdkRoot () ?? (await GetXcodeSelectXcodeSdkRootAsync ());
            if (sdkRoot == null)
                throw new Exception (Catalog.SharedStrings.XcodeNotFoundMessage);
            return sdkRoot;
        }

        public static Version GetXcodeVersion (string sdkRoot)
        {
            try {
                var pDict = PlistDictionary.Load (Path.Combine (
                    sdkRoot,
                    "Contents",
                    "Info.plist"));
                var shortVersion = (string)pDict ["CFBundleShortVersionString"];

                return Version.Parse (shortVersion);
            } catch (Exception e) {
                Log.Error (TAG, e);
                return null;
            }
        }

        static string GetDefaultXCodeSdkRoot ()
            => Directory.Exists (DefaultSdkRoot) ? DefaultSdkRoot : null;

        static async Task<string> GetXcodeSelectXcodeSdkRootAsync ()
        {
            try {
                var path = await RunToolWithRetriesAsync ("xcode-select", "-p");

                while (path != null && Path.GetExtension (path) != ".app")
                    path = Path.GetDirectoryName (path);

                if (Directory.Exists (path))
                    return path;
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            return null;
        }

        static string GetXamarinStudioXcodeSdkRoot ()
        {
            var settingsPlistPath = Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.Personal),
                "Library",
                "Preferences",
                "Xamarin",
                "Settings.plist");

            if (!File.Exists (settingsPlistPath))
                return null;

            try {
                var pDict = PlistDictionary.Load (settingsPlistPath);
                var path = (string)pDict ["AppleSdkRoot"];

                if (Directory.Exists (path))
                    return path;
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            return null;
        }

        public static async Task<MTouchListSimXml> MtouchListSimAsync (string sdkRoot)
        {
            if (sdkRoot == null)
                throw new ArgumentNullException (nameof (sdkRoot));

            var mlaunchPath = GetMlaunchPath ();
            var taskSource = new TaskCompletionSource<MTouchListSimXml> ();

            var tmpFile = Path.GetTempFileName ();
            var sdkRootArgs = $"-sdkroot \"{sdkRoot}\"";

            await RunToolWithRetriesAsync (
                mlaunchPath,
                $"{sdkRootArgs} --listsim=\"{tmpFile}\"");

            var mtouchInfo = ReadFromXml (File.OpenRead (tmpFile));
            File.Delete (tmpFile);

            return mtouchInfo;
        }

        public static MTouchListSimXml ReadFromXml (Stream xmlStream)
        {
            if (xmlStream == null)
                throw new ArgumentNullException (nameof (xmlStream));
            return new XmlSerializer (typeof (MTouchListSimXml)).Deserialize (xmlStream) as MTouchListSimXml;
        }

        public static List<MTouchListSimXml.SimDeviceElement> GetCompatibleDevices (MTouchListSimXml mtouchInfo)
        {
            if (mtouchInfo == null)
                throw new ArgumentNullException (nameof (mtouchInfo));

            var simInfo = mtouchInfo.Simulator;

            var iOSRuntime = simInfo.SupportedRuntimes
                .LastOrDefault (r => r.Name.StartsWith ("iOS", StringComparison.OrdinalIgnoreCase));

            var devices =
                from d in simInfo.AvailableDevices
                where d.SimRuntime == iOSRuntime.Identifier
                join t in simInfo.SupportedDeviceTypes on d.SimDeviceType equals t.Identifier
                where t.ProductFamilyId == "IPhone"
                where t.Supports64Bits
                select d;
            return devices.ToList ();
        }

        /// <summary>
        /// Runs a process, and returns its output
        /// </summary>
        /// <param name="timeout">How many milliseconds to wait before throwing TimeoutException.</param>
        static Task<string> RunToolAsync (string fileName, string arguments, int timeout = 5000)
        {
            var tcs = new TaskCompletionSource<string> ();

            var proc = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            proc.Exited += (o, args) => {
                if (proc.ExitCode == 0) {
                    try {
                        tcs.TrySetResult (proc.StandardOutput.ReadToEnd ());
                    } catch (Exception e) {
                        tcs.TrySetException (e);
                    }
                } else
                    tcs.TrySetException (new Exception ($"'{fileName} {arguments}' exited with exit code {proc.ExitCode}"));
            };

            Log.Info (TAG, $"{fileName} {arguments}");
            proc.Start ();

            if (timeout > 0) {
                Task.Run (() =>  {
                    if (!proc.WaitForExit (timeout)) {
                        Log.Info (TAG, $"TIMEOUT {fileName} {arguments}");
                        tcs.TrySetException (new TimeoutException ());
                        proc.Kill ();
                    }
                });
            }

            return tcs.Task;
        }

        /// <summary>
        /// Runs a process, and returns its output. Can retry execution if timeouts occur.
        /// Does not retry on non-timeout errors.
        /// </summary>
        /// <param name="timeoutRetries">How many times to retry execution before throwing TimeoutException.</param>
        /// <param name="timeout">How many milliseconds to wait before aborting and trying again.</param>
        static async Task<string> RunToolWithRetriesAsync (
            string fileName,
            string arguments,
            int timeoutRetries = 3,
            int timeout = 5000)
        {
            for (var i = 0; i < timeoutRetries; i++) {
                try {
                    return await RunToolAsync (fileName, arguments, timeout);
                } catch (TimeoutException e) {
                    if (i < (timeoutRetries - 1))
                        Log.Info (TAG, $"Failed with {e.GetType ()}, retrying");
                    else
                        throw;
                }
            }
            throw new Exception ($"Giving up on {fileName} after ${timeoutRetries} timeouts");
        }
    }
}