//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.MTouch;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Unified;

using AppKit;
using Foundation;

[assembly: AgentProcessRegistration (
    "ios-xamarinios",
    typeof (iOSAgentProcess))]

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class iOSAgentProcess : IAgentProcess
    {
        const string TAG = nameof (iOSAgentProcess);

        const string simulatorDevice = "iphone,64";

        FilePath sdkRoot;
        string defaultSdkVersion;

        NSTask mlaunchProcess;
        NSObject terminatedObserver;

        public IWorkbookAppInstallation WorkbookApp { get; }

        public event EventHandler UnexpectedlyTerminated;

        public iOSAgentProcess (IWorkbookAppInstallation workbookApp)
            => WorkbookApp = workbookApp ?? throw new ArgumentNullException (nameof (workbookApp));

        public async Task StartAgentProcessAsync (
            IdentifyAgentRequest identifyAgentRequest,
            IMessageService messageService,
            CancellationToken cancellationToken)
        {

            using (messageService.PushMessage (
                Message.CreateInfoStatus (
                    Catalog.GetString ("Checking for usable iOS simulators…"))))
                await InitializeXcodeSdkAsync (messageService);

            await MainThread.SendAsync (() => StartSimulatorOnMainThread (identifyAgentRequest));
        }

        void StartSimulatorOnMainThread (IdentifyAgentRequest identifyAgentRequest)
        {
            MainThread.Ensure ();

            var mlaunchPath = MTouchSdkTool.GetMlaunchPath ();
            var mlaunchArguments = new List<string> {
                "-vvvv",
                "-sdkroot", sdkRoot,
                "-launchsim", WorkbookApp.AppPath,
                "-sdk", defaultSdkVersion,
                "-device", simulatorDevice
            };

            mlaunchArguments.AddRange (identifyAgentRequest
                .ToCommandLineArguments ()
                .SelectMany (argument => new [] { "-argument", argument }));

            Log.Info (TAG, mlaunchPath + " " + string.Join (" ", mlaunchArguments));

            mlaunchProcess = new NSTask {
                LaunchPath = mlaunchPath,
                Arguments = mlaunchArguments.ToArray (),
                StandardInput = new NSPipe ()
            };

            terminatedObserver = mlaunchProcess.NotifyTerminated (
                NSNotificationCenter.DefaultCenter,
                task => {
                    terminatedObserver?.Dispose ();
                    terminatedObserver = null;

                    UnexpectedlyTerminated?.Invoke (this, EventArgs.Empty);
                });

            mlaunchProcess.Launch ();
        }

        async Task InitializeXcodeSdkAsync (IMessageService messageService)
        {
            try {
                sdkRoot = await MTouchSdkTool.GetXcodeSdkRootAsync ();
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            if (sdkRoot.IsNull || (Version.TryParse (
                (NSString)NSBundle.FromPath (sdkRoot).InfoDictionary.ValueForKey (
                    (NSString)"CFBundleShortVersionString"), out var xcodeVersion)
                && xcodeVersion < MTouchSdkTool.RequiredMinimumXcodeVersion)) {

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

            MTouchListSimXml mtouchList;
            try {
                mtouchList = await MTouchSdkTool.MtouchListSimAsync (sdkRoot);
            } catch (MlaunchNotFoundException) {
                var ex = new UserPresentableException (
                    Catalog.GetString ("Missing Xamarin.iOS"),
                    Catalog.GetString ("Check that Xamarin is installed and up to date."));

                messageService.PushMessage (CreateInstallAlert (
                    ex,
                    Catalog.GetString ("Install Xamarin"),
                    "https://aka.ms/xamint-install-xamarin"));

                throw ex;
            }

            defaultSdkVersion = mtouchList?.Simulator?.SupportedRuntimes
                ?.Where (r => r.Name.StartsWith ("iOS", StringComparison.OrdinalIgnoreCase))
                .Select (r => r.Name.Replace ("iOS ", ""))
                .LastOrDefault ();

            if (defaultSdkVersion != null)
                return;

            throw new UserPresentableException (Catalog.GetString ("No iOS simulators found."));
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
                        NSWorkspace.SharedWorkspace.OpenUrl (new NSUrl (installUrl));
                    message.Dispose ();
                });

        public async Task TerminateAgentProcessAsync ()
        {
            NSPipe stdin = null;

            if (terminatedObserver != null) {
                NSNotificationCenter.DefaultCenter.RemoveObserver (terminatedObserver);
                terminatedObserver.Dispose ();
                terminatedObserver = null;
            }

            try {
                NativeExceptionHandler.Trap ();

                if (mlaunchProcess == null || !mlaunchProcess.IsRunning)
                    return;

                var procName = $"mlaunch (pid={mlaunchProcess.ProcessIdentifier})";

                Log.Debug (TAG, $"Attempting to safely terminate {procName}...");

                // "Press enter to terminate the application"
                // (this is how XS does it in IPhoneDebuggerSession, anyway)
                try {
                    stdin = (NSPipe)mlaunchProcess.StandardInput;
                    stdin.WriteHandle.WriteData (NSData.FromString ("\n"));
                    stdin.WriteHandle.CloseFile ();
                } catch (Exception e) {
                    Log.Error (TAG, $"Exception trying to safely terminate {procName}", e);
                }

                const int totalTimeout = 10000;
                const int timeoutSteps = 20;
                const int singleTimeout = totalTimeout / timeoutSteps;

                for (int i = 0; i < timeoutSteps; i++) {
                    if (mlaunchProcess == null || !mlaunchProcess.IsRunning) {
                        Log.Debug (TAG, $"{procName} terminated safely");
                        return;
                    }

                    await Task.Delay (singleTimeout);
                }

                try {
                    Log.Warning (TAG, $"Force-terminating {procName} after timeout");
                    mlaunchProcess.Terminate ();
                    mlaunchProcess.WaitUntilExit ();
                } catch (Exception e) {
                    Log.Error (TAG, $"Exception trying to force-terminate {procName}", e);
                }
            } finally {
                stdin?.Dispose ();
                mlaunchProcess?.Dispose ();
                mlaunchProcess = null;

                NativeExceptionHandler.Release ();
            }
        }
    }
}