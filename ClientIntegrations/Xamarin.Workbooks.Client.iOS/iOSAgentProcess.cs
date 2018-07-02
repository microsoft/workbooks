//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

using Newtonsoft.Json;

using Xamarin.ProcessControl;

using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.MTouch;
using Xamarin.Interactive.I18N;

[assembly: AgentProcessRegistration (
    "ios-xamarinios",
    typeof (iOSAgentProcess))]

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class iOSAgentProcess : IAgentProcess, IAgentAssociable
    {
        const string TAG = nameof (iOSAgentProcess);

        public AgentType AgentType { get; } = AgentType.iOS;
        public string [] AssemblySearchPaths { get; }

        string simulatorExePath;
        bool simulatorSupportsExtraArgs;
        string macTempPath;
        SshCommands sshCommands;
        string deviceUdid;

        public IWorkbookAppInstallation WorkbookApp { get; }

        // FIXME: implement?
        #pragma warning disable 0067
        public event EventHandler UnexpectedlyTerminated;
        #pragma warning restore 0067

        public iOSAgentProcess (IWorkbookAppInstallation workbookApp)
            => WorkbookApp = workbookApp ?? throw new ArgumentNullException (nameof (workbookApp));

        IEnumerable<RegistryKnownServer> GetXvsMacBuildHosts ()
        {
            IEnumerable<RegistryKnownServer> ReadFromRegistry (bool useKnownHosts)
            {
                var subKey = @"Software\Xamarin\VisualStudio\MacAgent\" +
                    (useKnownHosts ? "KnownHosts" : "KnownServers");

                using (var key = RegistryKey
                    .OpenBaseKey (RegistryHive.CurrentUser, RegistryView.Registry32)
                    .OpenSubKey (subKey)) {
                    if (key == null)
                        yield break;

                    foreach (var knownServerName in key.GetValueNames ()) {
                        var value = key.GetValue (knownServerName) as string;
                        if (value == null)
                            continue;

                        RegistryKnownServer knownServer;
                        try {
                            knownServer =
                                useKnownHosts ?
                                RegistryKnownServer.ConvertFromJsonRegistryValue (
                                    knownServerName, value) :
                                RegistryKnownServer.ConvertFromRegistryValue (
                                    knownServerName, value);
                        } catch (Exception e) {
                            Log.Error (TAG, e);
                            continue;
                        }

                        yield return knownServer;
                    }
                }
            }

            var anyFound = false;

            foreach (var knownServer in ReadFromRegistry (useKnownHosts: true)) {
                if (knownServer.Platform != RemoteServerPlatform.Mac)
                    continue;
                anyFound = true;
                yield return knownServer;
            }

            // KnownServers contains redundant data if KnownHosts is non-empty
            if (!anyFound)
                foreach (var knownServer in ReadFromRegistry (useKnownHosts: false))
                    if (knownServer.Platform == RemoteServerPlatform.Mac)
                        yield return knownServer;
        }

        async Task InitializeSimulatorAsync (
            IMessageService messageService,
            CancellationToken cancellationToken)
        {
            using (var simulatorKey = RegistryKey.OpenBaseKey (RegistryHive.LocalMachine, RegistryView.Registry32)
                .OpenSubKey (@"Software\Xamarin\Simulator\")) {
                simulatorExePath = simulatorKey?.GetValue ("Path") as string;
                simulatorSupportsExtraArgs =
                    Version.TryParse (
                        simulatorKey?.GetValue ("Version") as string,
                        out var version) &&
                    version >= new Version (1, 0, 2);
            }

            if (String.IsNullOrEmpty (simulatorExePath))
                throw new UserPresentableException (Catalog.GetString (
                    "The Xamarin Remote iOS Simulator is not installed."));

            string remoteHomeDir = null;
            foreach (var knownServer in GetXvsMacBuildHosts ().OrderByDescending (s => s.IsDefault)) {
                Log.Info (TAG, $"Trying known server {knownServer.BuildServer}");
                var ssh = new SshCommands (knownServer.Ip, knownServer.Username);
                try {
                    remoteHomeDir = await ssh.GetHomeDirectoryAsync ();
                    if (!String.IsNullOrEmpty (remoteHomeDir)) {
                        Log.Info (
                            TAG,
                            "Connected to Mac Build Host " +
                            $"{knownServer.BuildServer} at {ssh.Address}.");
                        sshCommands = ssh;
                        break;
                    }
                } catch (Exception e) {
                    Log.Error (TAG, "Could not connect to Mac build host", e);
                }
            }

            if (sshCommands == null)
                throw new UserPresentableException (
                    Catalog.GetString ("Unable to connect to the Mac build host."),
                    Catalog.GetString ("Confirm and test setup in Visual Studio."));

            const string monoPath =
                "/Library/Frameworks/Mono.framework/Versions/Current/bin/mono64";

            macTempPath = $"{remoteHomeDir}/Library/Caches/com.xamarin.Inspector/remote";

            var makeTempResult = await sshCommands.ExecuteCommandAsync ($"mkdir -p {macTempPath}");
            if (makeTempResult.ExitStatus != 0) {
                Log.Error (TAG, makeTempResult.Error);
                throw new UserPresentableException (Catalog.GetString ("Could not create temporary path on the Mac build host."));
            }

            var depCheckResult = await sshCommands.ExecuteCommandAsync ($"ls \"{monoPath}\"");
            if (depCheckResult.ExitStatus != 0) {
                Log.Error (TAG, depCheckResult.Error);
                throw new UserPresentableException (Catalog.GetString ("Mono must be installed on the Mac build host."));
            }

            var localSimChecker = InteractiveInstallation.Default.LocateSimChecker ();
            var simCheckerPath = $"{macTempPath}/{Path.GetFileName (localSimChecker)}";

            // This file is really small, so copying it fresh every time is OK.
            try {
                await sshCommands.CopyFileAsync (localSimChecker, simCheckerPath);
            } catch (Exception e) {
                Log.Error (TAG, "Could not copy SimChecker to Mac host.", e);
                throw new UserPresentableException (
                    e,
                    Catalog.GetString ("Could not copy files to Mac host."));
            }

            var simCheckerResult = await sshCommands.ExecuteCommandAsync ($"{monoPath} {simCheckerPath}");

            if (simCheckerResult.ExitStatus != 0 || String.IsNullOrEmpty (simCheckerResult.Result)) {
                // 100: Xcode not configured in XS or not installed at /Applications/Xcode.app
                // 105: Xcode too old
                if (simCheckerResult.ExitStatus == 100 || simCheckerResult.ExitStatus == 105) {
                    var ex = new UserPresentableException (
                        Catalog.GetString ("Required Xcode not found"),
                        Catalog.GetString (
                            $"Ensure Xcode {MTouchSdkTool.RequiredMinimumXcodeVersion} or " +
                            "later is installed and selected."));

                    messageService.PushMessage (CreateInstallAlert (
                        ex,
                        Catalog.GetString ("Install Xcode"),
                        "https://aka.ms/xamint-download-xcode"));

                    throw ex;
                }

                // 101: mlaunch (Xamarin Studio) not installed
                if (simCheckerResult.ExitStatus == 101) {
                    var ex = new UserPresentableException (
                        Catalog.GetString ("Missing Xamarin.iOS"),
                        Catalog.GetString ("Check that Xamarin is installed and up to date."));

                    messageService.PushMessage (CreateInstallAlert (
                        ex,
                        Catalog.GetString ("Install Xamarin"),
                        "https://aka.ms/xamint-install-xamarin"));

                    throw ex;
                }

                // 102: Error running mlaunch
                if (simCheckerResult.ExitStatus == 102)
                    throw new UserPresentableException (
                        Catalog.GetString (
                            "Unexpected error checking the Mac build host for compatible simulators."),
                        simCheckerResult.Error);

                // 103: Invalid mlaunch output
                if (simCheckerResult.ExitStatus == 103)
                    throw new UserPresentableException (
                        Catalog.GetString (
                            "Error checking the Mac build host for compatible simulators."),
                        Catalog.GetString (
                            "Check that your Xamarin Studio and configured Xcode " +
                            "are compatible with each other."));

                // 104: No compatible simulator device listed by mlaunch
                if (simCheckerResult.ExitStatus == 104)
                    throw new UserPresentableException (
                        Catalog.GetString (
                            "Unable to find a compatible simulator device " +
                            "on the Mac build host."),
                        Catalog.GetString (
                            "Check your Xcode installation."));

                throw new UserPresentableException (
                    Catalog.GetString (
                        "Unexpected error finding compatible simulator on Mac build host."),
                    simCheckerResult.Error);
            }

            var mtouchList = MTouchSdkTool.ReadFromXml (
                new MemoryStream (Encoding.UTF8.GetBytes (simCheckerResult.Result)));

            var compatibleDevices = MTouchSdkTool.GetCompatibleDevices (mtouchList);
            var simulatorDevice =
                compatibleDevices.FirstOrDefault (d => d.Name == "iPhone X") ?? compatibleDevices.FirstOrDefault ();
            deviceUdid = simulatorDevice?.UDID;

            if (deviceUdid == null)
                throw new UserPresentableException (
                    Catalog.GetString (
                        "Unexpected error communicating with the Mac build host."),
                    Catalog.GetString (
                        "Confirm you have the same version of Xamarin Workbooks " +
                        "installed on both Mac and Windows."));
        }

        static Message CreateInstallAlert (
            UserPresentableException ex,
            string installButtonTitle,
            string installUrl)
            => Message
                .CreateErrorAlert (ex)
                .WithAction (new MessageAction (
                    MessageActionKind.Negative,
                    "close",
                    Catalog.GetString ("Close")))
                .WithAction (new MessageAction (
                    MessageActionKind.Affirmative,
                    "install",
                    installButtonTitle))
                .WithActionResponseHandler ((message, action) => {
                    if (action.Id == "install")
                        Process.Start (installUrl);
                    message.Dispose ();
                });

        async Task<string> DeployWorkbooksAppAsync (CancellationToken cancellationToken)
        {
            const string fileToCheck = "Xamarin.Interactive.iOS.dll";

            var localWorkbookApp = new FilePath (WorkbookApp.AppPath);
            var targetPathForApp = $"{macTempPath}/{localWorkbookApp.Name}";

            var localWorkbookAppExecutablePath = localWorkbookApp.Combine (fileToCheck);

            if (!localWorkbookAppExecutablePath.FileExists || localWorkbookAppExecutablePath.FileSize == 0) {
                Log.Error (TAG, $"Could not deploy Workbook app, local copy is missing {fileToCheck} or it has size 0.");
                throw new UserPresentableException (Catalog.GetString ("Could not deploy Workbook app."));
            }

            var localWorkbookAppChecksum = localWorkbookAppExecutablePath.Checksum ();

            // Get the checksum for the remote workbook app. If we can't compute one or the file doesn't exist,
            // we'll copy it.
            var shouldCopy = false;
            var copyReason = String.Empty;
            var remoteAppChecksumResult = await sshCommands.ExecuteCommandAsync ($"shasum -a 256 {targetPathForApp}/{fileToCheck}");

            // We couldn't compute a checksum for some reason, copy the app.
            if (remoteAppChecksumResult.ExitStatus != 0) {
                shouldCopy = true;
                copyReason = "Could not take checksum of remote app, it probably doesn't exist.";
            }

            // If the exit status was 0, the file existed and was checksummed. The output of shasum looks like this:
            // e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  Xamarin.Workbooks.iOS
            var remoteWorkbookAppChecksum = remoteAppChecksumResult.Result.Split (new[] { ' ' }) [0];

            // If the checksums don't match, copy the workbook app. If shouldCopy is already set to true, skip this check.
            if (!shouldCopy && !localWorkbookAppChecksum.Equals (remoteWorkbookAppChecksum, StringComparison.OrdinalIgnoreCase)) {
                shouldCopy = true;
                copyReason =
                    $"Checksums were mismatched, local check file has {localWorkbookAppChecksum}, remote has {remoteWorkbookAppChecksum}.";
            }

            if (shouldCopy) {
                Log.Info (TAG, $"Copying Workbook app to Mac build host because: {copyReason}.");
                try {
                    await sshCommands.ExecuteCommandAsync ($"rm -fr {targetPathForApp}");
                    await sshCommands.CopyDirectoryAsync (localWorkbookApp, targetPathForApp);
                } catch (Exception e) {
                    throw new UserPresentableException (
                        e,
                        Catalog.GetString ("Could not copy Workbook app to Mac build host."));
                }
            }

            return targetPathForApp;
        }

        public async Task StartAgentProcessAsync (
            IdentifyAgentRequest identifyAgentRequest,
            IMessageService messageService,
            CancellationToken cancellationToken)
        {
            var remotePort = identifyAgentRequest.Uri.Port; // TODO: Pick free remote port (how? SimChecker?)

            if (sshCommands == null)
                using (messageService.PushMessage (Message.CreateInfoStatus (Catalog.GetString ("Connecting to Mac build host…"))))
                    await InitializeSimulatorAsync (messageService, cancellationToken);

            string agentAppPath;
            using (messageService.PushMessage (Message.CreateInfoStatus (Catalog.GetString ("Deploying Workbook app to Mac build host…"))))
                agentAppPath = await DeployWorkbooksAppAsync (cancellationToken);

            sshCommands.ForwardPort (
                boundPort: remotePort,
                port: identifyAgentRequest.Uri.Port,
                remoteForward: true);

            var arguments = ProcessArguments.Create (
                "-device", deviceUdid,
                "-launchsim", agentAppPath,
                "-h", sshCommands.Address,
                "-ssh", sshCommands.User
            );

            if (simulatorSupportsExtraArgs)
                arguments = arguments.InsertRange (0,
                    "--enable-telemetry",
                    $"--launched-by=com.xamarin.workbooks-{BuildInfo.Version}"
                );

            arguments = arguments.AddRange (identifyAgentRequest
                .ToCommandLineArguments ()
                .SelectMany (argument => new [] { "-argument", argument }));

            var agentProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = simulatorExePath,
                    Arguments = arguments.ToString (),
                    UseShellExecute = false
                },
            };

            // TODO: Add to remote simulator a process we can call that terminates when the app does, or something.
            //       If sim is already running, this process exits immediately, so we can't get notified if the user
            //       closes it or kills the app or anything like that.

            agentProcess.Start ();
        }

        public Task TerminateAgentProcessAsync ()
        {
            // TODO: How to actually kill agent with remote simulator?
            try {
                sshCommands.Dispose ();
            } catch (Exception e) {
                Log.Error (TAG, e);
            }
            sshCommands = null;
            return Task.CompletedTask;
        }

        public Task<AgentIdentity> GetAgentAssociationAsync (
            AgentIdentity agentIdentity,
            CancellationToken cancellationToken)
        {
            var remotePort = agentIdentity.Port;
            var localPort = ValidPortRange.GetRandom ();

            sshCommands.ForwardPort (boundPort: localPort, port: remotePort);

            return Task.FromResult (agentIdentity.WithPort ((ushort)localPort));
        }
    }

    enum RemoteServerPlatform
    {
        Mac = 1,
        IoT = 2,
        Unknown = 0
    }

    sealed class RegistryKnownServer
    {
        public RemoteServerPlatform Platform { get; set; }

        [JsonIgnore]
        public string BuildServer { get; set; }

        public string Ip { get; set; }

        public string OldIp { get; set; }

        [JsonIgnore]
        public int Port { get; set; }

        public string Username { get; set; }

        public string Fingerprint { get; set; }

        public bool IsDefault { get; set; }

        public static RegistryKnownServer ConvertFromJsonRegistryValue (string name, string value)
        {
            var knownServer = JsonConvert.DeserializeObject<RegistryKnownServer> (value);

            knownServer.BuildServer = name;
            knownServer.Port = GetPort (knownServer.Ip);
            knownServer.Ip = GetAddressOrHost (knownServer.Ip);

            return knownServer;
        }

        public static RegistryKnownServer ConvertFromRegistryValue (string name, string value)
        {
            var values = value.Split ("|".ToCharArray ());
            var knownServer = new RegistryKnownServer {
                BuildServer = name,
                Ip = GetAddressOrHost (values [0]),
                Port = GetPort (values [0]),
                Platform = RemoteServerPlatform.Mac,
                IsDefault = false
            };

            if (values.Length == 2) {
                knownServer.Username = values [1];
            }

            if (values.Length == 3) {
                knownServer.Fingerprint = values [1];
                knownServer.Username = values [2];
            }

            if (values.Length == 4) {
                knownServer.OldIp = values [1];
                knownServer.Fingerprint = values [2];
                knownServer.Username = values [3];
            }

            if (values.Length == 5) {
                knownServer.OldIp = values [1];
                knownServer.Fingerprint = values [2];
                knownServer.Username = values [3];

                var platform = default (RemoteServerPlatform);

                if (Enum.TryParse (values [4], out platform)) {
                    knownServer.Platform = platform;
                }
            }

            if (values.Length == 6) {
                knownServer.OldIp = values [1];
                knownServer.Fingerprint = values [2];
                knownServer.Username = values [3];

                var platform = default (RemoteServerPlatform);

                if (Enum.TryParse (values [4], out platform)) {
                    knownServer.Platform = platform;
                }

                var isDefault = false;

                if (bool.TryParse (values [5], out isDefault)) {
                    knownServer.IsDefault = isDefault;
                }
            }

            return knownServer;
        }

        static string GetAddressOrHost (string address)
        {
            if (string.IsNullOrEmpty (address)) {
                return address;
            }

            var addressParts = address.Split (':');

            return addressParts.Count () == 2 ? addressParts.First () : address;
        }

        static int GetPort (string address, int defaultPortNumber = 22)
        {
            if (string.IsNullOrEmpty (address)) {
                return defaultPortNumber;
            }

            var port = default (int);
            var addressParts = address.Split (":".ToCharArray (), StringSplitOptions.RemoveEmptyEntries);

            if (addressParts.Count () == 2) {
                if (!int.TryParse (addressParts.Last (), out port)) port = defaultPortNumber;
            } else {
                port = defaultPortNumber;
            }

            return port;
        }
    }
}
