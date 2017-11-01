//
// Authors:
//   Mauro Agnoletti <mauro.agnoletti@gmail.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Renci.SshNet;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    //mag: This class already exists as part of XMA assemblies on VSX. It is created here until VSX exposes it (Nuget, etc)
    class SshCommands : ISshCommands
    {
        static readonly byte[] Entropy = Encoding.Unicode.GetBytes ("Xamarin Pass Phrase Entropy");

        SshClient sshClient;
        ScpClient scpClient;

        public string Address { get; }
        public string User { get; }

        public SshCommands (string address, string user)
        {
            Address = address;
            User = user;
        }

        public async Task<string> GetHomeDirectoryAsync ()
        {
            EnsureSshConnection ();

            var commandText = "printenv HOME";
            var command = await ExecuteCommandAsync (commandText).ConfigureAwait (continueOnCapturedContext: false);

            return command.ExitStatus == 0 && !string.IsNullOrEmpty (command.Result) ?
                command.Result.Trim () :
                string.Empty;
        }

        public void ForwardPort (int boundPort, int port, bool remoteForward = false)
        {
            EnsureSshConnection ();

            var forwardedPort = default (ForwardedPort);

            if (remoteForward) {
                forwardedPort = new ForwardedPortRemote (IPAddress.Parse ("127.0.0.1"), (uint)boundPort, IPAddress.Parse ("127.0.0.1"), (uint)port);
            } else {
                // IPAddress.Loopback.ToString() is the string 127.0.0.1. Don't change this unless you've also changed
                // port forwarding in XMA/XVS to match--you'll break inspection if you make things dependent on this
                // behavior without fixing XMA/XVS too.
                forwardedPort = new ForwardedPortLocal (IPAddress.Loopback.ToString (), (uint)boundPort, IPAddress.Loopback.ToString (), (uint)port);
            }

            sshClient.AddForwardedPort (forwardedPort);

            forwardedPort.Start ();
        }

        public void Dispose ()
        {
            sshClient?.Disconnect ();
            sshClient?.Dispose ();

            scpClient?.Disconnect ();
            scpClient?.Dispose ();
        }

        void EnsureSshConnection ()
        {
            if (sshClient != null && sshClient.IsConnected) {
                return;
            }

            if (!File.Exists (GetPrivateKeyFilePath ())) {
                throw new Exception ("XMA Private Key has not been found");
            }

            var privateKey = default (PrivateKeyFile);
            var privateKeyPath = GetPrivateKeyFilePath ();
            var passPhrasePath = GetPassPhraseFilePath ();

            if (File.Exists (passPhrasePath)) {
                var protectedPassPhrase = File.ReadAllText (passPhrasePath);
                var unprotectedPassPhrase = ProtectedData.Unprotect (Convert.FromBase64String (protectedPassPhrase), Entropy, DataProtectionScope.CurrentUser);

                privateKey = new PrivateKeyFile (privateKeyPath, Encoding.Unicode.GetString (unprotectedPassPhrase));
            } else {
                privateKey = new PrivateKeyFile (privateKeyPath);
            }

            var authenticationMethod = new PrivateKeyAuthenticationMethod (User, privateKey);
            var connectionInfo = new ConnectionInfo (Address, User, authenticationMethod);

            sshClient = new SshClient (connectionInfo);

            sshClient.Connect ();

            if (sshClient.IsConnected) {
                scpClient = new ScpClient (connectionInfo);
            }
        }

        public async Task CopyDirectoryAsync (FilePath sourcePath, string targetPath)
        {
            ValidateScpConnection ();

            await Task.Run (() => scpClient.Upload (sourcePath.CreateDirectory (), targetPath));
        }

        public async Task CopyFileAsync (FilePath sourcePath, string targetPath)
        {
            ValidateScpConnection ();

            await Task.Run (() => scpClient.Upload (sourcePath.OpenRead (), targetPath));
        }

        public async Task<SshCommand> ExecuteCommandAsync (string commandText)
        {
            ValidateConnection ();

            var command = sshClient.CreateCommand (commandText);

            try {
                await Task.Factory.FromAsync (
                    (callback, state) => command.BeginExecute (callback),
                    result => command.EndExecute (result),
                    state: null,
                    creationOptions: TaskCreationOptions.None).ConfigureAwait (continueOnCapturedContext: false);

                return command;
            } catch (Exception) {
                //TODO: Log according to Inspector practices

                return command;
            }
        }

        void ValidateScpConnection ()
        {
            if (!scpClient.IsConnected)
                scpClient.Connect ();

            if (!scpClient.IsConnected)
                throw new Exception (Catalog.Format (Catalog.GetString (
                    "Unable to connect to the Mac server {0} in order to copy a file.",
                    "{0} is a server address--it may be a host name or an IP address."),
                    Address));
        }

        void ValidateConnection ()
        {
            if (!sshClient.IsConnected) {
                sshClient.Connect ();
            }

            if (!sshClient.IsConnected) {
                throw new Exception ($"Unable to connect to the Mac server {Address} in order to execute the SSH command");
            }
        }

        string GetPrivateKeyFilePath ()
        {
            return Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
                "Xamarin",
                "MonoTouch",
                "id_rsa");
        }

        string GetPassPhraseFilePath ()
        {
            return Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
                "Xamarin",
                "MonoTouch",
                "passphrase.key");
        }
    }
}