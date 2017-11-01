//
// IdentifyAgentRequest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Core
{
	sealed class IdentifyAgentRequest : IEquatable<IdentifyAgentRequest>
	{
		static readonly int clientProcessId = System.Diagnostics.Process.GetCurrentProcess ().Id;

		const string tokenUriParameter = "token";
		const string clientPidUriParameter = "clientPid";

		public static IdentifyAgentRequest CreateWithBaseConnectUri (Uri baseUri)
		{
			var token = Guid.NewGuid ();
			return new IdentifyAgentRequest (
				new Uri (
					baseUri,
					$"?{clientPidUriParameter}={clientProcessId}&{tokenUriParameter}={token}"),
				token,
				clientProcessId);
		}

		public Uri Uri { get; }
		public Guid RequestToken { get; }
		public int ProcessId { get; }

		IdentifyAgentRequest (
			Uri uri,
			Guid requestToken,
			int clientProcessId)
		{
			Uri = uri;
			RequestToken = requestToken;
			ProcessId = clientProcessId;
		}

		public IdentifyAgentRequest (Uri uri)
		{
			Uri = uri ?? throw new ArgumentNullException (nameof (uri));

			ProcessId = -1;

			foreach (var item in Client.ClientSessionUri.ParseQueryString (uri.Query)) {
				switch (item.Key) {
				case tokenUriParameter:
					RequestToken = Guid.Parse (item.Value);
					break;
				case clientPidUriParameter:
					ProcessId = int.Parse (item.Value);
					break;
				}
			}
		}

		public override bool Equals (object obj)
			=> obj is IdentifyAgentRequest iar && Equals (iar);

		public bool Equals (IdentifyAgentRequest other)
			=> Uri == other?.Uri;

		public override int GetHashCode ()
			=> Uri.GetHashCode ();

		public override string ToString ()
			=> $"[IdentifyAgentRequest: {Uri}]";

		const string requestUriArgument = "-xiais-request-uri";

		public string [] ToCommandLineArguments ()
			=> new [] {
				requestUriArgument, Uri.ToString ()
			};

		public static IdentifyAgentRequest FromCommandLineArguments (string [] arguments)
		{
			if (arguments == null)
				return null;

			for (int i = 0; i < arguments.Length - 1; i++) {
				switch (arguments [i]) {
				case requestUriArgument:
					return new IdentifyAgentRequest (new Uri (arguments [i + 1]));
				}
			}

			return null;
		}
	}
}