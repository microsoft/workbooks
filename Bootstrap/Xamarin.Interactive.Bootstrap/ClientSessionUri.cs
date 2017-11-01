//
// ClientSessionUri.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xamarin.Interactive.Client
{
	/// <summary>
	/// URI that represents any and all initial state for opening a workbook, creating a new
	/// workbook, or connecting to a live-inspect agent already running. Further state may
	/// be derived asynchronously and persisted on the represented <see cref="ClientSession"/>
	/// instance. The URI intentionally only represents initial state.
	/// </summary>
	/// <remarks>
	/// <![CDATA[
	/// Many forms of URI are supported that encompass everything the ClientSession may need
	/// to open a workbook, create a new workbook, or connect to an already running session:
	///   xamarin-interactive://host:port/v1?agentAssemblyPath=...
	///   xamarin-interactive:///v1?agentType=MacNet45&sessionKind=Workbook
	///   file:///path/to/workbook
	/// ]]>
	/// </remarks>
	sealed class ClientSessionUri : IEquatable<ClientSessionUri>, IEquatable<Uri>
	{
		const string xamarinInteractiveScheme = "xamarin-interactive";

		public static bool IsSchemeSupported (string scheme)
			=> scheme == xamarinInteractiveScheme || scheme == "file";

		static bool UriStartsWithSupportedSchemeAndAuthority (string uriString)
		{
			if (string.IsNullOrEmpty (uriString))
				return false;

			// no authority, absolute path
			if (uriString.StartsWith ("file:///", StringComparison.Ordinal))
				return true;

			// our own scheme, authority may or may not be present
			if (uriString.StartsWith (xamarinInteractiveScheme + "://", StringComparison.Ordinal))
				return true;

			return false;
		}

		/// <summary>
		/// Literal 'localhost' is problematic on Windows.
		/// </summary>
		const string localhost = "127.0.0.1";

		public AgentType AgentType { get; }
		public ClientSessionKind SessionKind { get; }
		public string Host { get; }
		public ushort Port { get; }
		public string [] AssemblySearchPaths { get; } = EmptyArray<string>.Instance;
		public string WorkbookPath { get; }
		public string WorkingDirectory { get; }
		public KeyValuePair<string, string> [] Parameters { get; }
			= EmptyArray<KeyValuePair<string, string>>.Instance;

		public ClientSessionUri (AgentType agentType, ClientSessionKind sessionKind)
		{
			AgentType = agentType;
			SessionKind = sessionKind;
		}

		public ClientSessionUri (string host, ushort port, IEnumerable<string> assemblySearchPaths = null)
			: this (AgentType.Unknown, ClientSessionKind.Unknown, host, port, assemblySearchPaths)
		{
		}

		public ClientSessionUri (
			AgentType agentType,
			ClientSessionKind sessionKind,
			string host,
			ushort port,
			IEnumerable<string> assemblySearchPaths = null,
			string workbookPath = null,
			string workingDirectory = null,
			IEnumerable<KeyValuePair<string, string>> parameters = null)
		{
			if (port != 0)
				ValidPortRange.Assert (port);

			AgentType = agentType;
			SessionKind = sessionKind;
			Host = host ?? localhost;
			Port = port;

			if (assemblySearchPaths != null)
				AssemblySearchPaths = assemblySearchPaths.ToArray ();

			WorkbookPath = NormalizePath (workbookPath);
			WorkingDirectory = NormalizePath (workingDirectory);

			if (parameters != null)
				Parameters = parameters.ToArray ();
		}

		public ClientSessionUri (Uri uri)
		{
			if (uri == null)
				throw new ArgumentNullException (nameof (uri));

			if (!uri.IsAbsoluteUri)
				throw new ArgumentException ("only absolute URIs are supported", nameof (uri));

			if (uri.IsFile) {
				SessionKind = ClientSessionKind.Workbook;
				WorkbookPath = NormalizePath (uri.LocalPath);
				return;
			}

			if (uri.Scheme != xamarinInteractiveScheme)
				throw new ArgumentException ($"unsupported scheme: {uri}", nameof (uri));

			if (String.IsNullOrEmpty (uri.Host)) {
				SessionKind = ClientSessionKind.Workbook;
			} else {
				// keep in sync with .ctor(host,port,...) above
				if (uri.Port != 0)
					ValidPortRange.Assert (uri.Port);

				Host = uri.Host ?? localhost;
				Port = (ushort)uri.Port;
			}

			if (String.IsNullOrEmpty (uri.Query))
				return;

			if (uri.AbsolutePath != "/v1")
				throw new ArgumentException (
					$"uri path must be 'v1' (it is '{uri.AbsolutePath}')",
					nameof (uri));

			List<string> assemblySearchPaths = null;
			List<KeyValuePair<string, string>> parameters = null;

			foreach (var property in ParseQueryString (uri.Query)) {
				switch (property.Key) {
				case "agentType":
					AgentType = (AgentType)Enum.Parse (
						typeof (AgentType), property.Value, true);
					break;
				case "sessionKind":
					SessionKind = (ClientSessionKind)Enum.Parse (
						typeof (ClientSessionKind), property.Value, true);
					break;
				case "assemblySearchPath":
					(assemblySearchPaths ?? (assemblySearchPaths = new List<string> ()))
					 	.Add (property.Value);
					break;
				case "workingDirectory":
					WorkingDirectory = property.Value;
					break;
				default:
					(parameters ?? (parameters = new List<KeyValuePair<string, string>> ()))
						.Add (property);
					break;
				}
			}

			if (assemblySearchPaths != null)
				AssemblySearchPaths = assemblySearchPaths.ToArray ();

			if (parameters != null)
				Parameters = parameters.ToArray ();
		}

		static readonly char [] trailingSlashes = { '/', '\\' };

		static string NormalizePath (string path)
			=> path?.TrimEnd (trailingSlashes);

		public ClientSessionUri WithSessionKind (ClientSessionKind sessionKind)
		{
			if (sessionKind == SessionKind)
				return this;

			return new ClientSessionUri (
				AgentType,
				sessionKind,
				Host,
				Port,
				AssemblySearchPaths,
				WorkbookPath,
				WorkingDirectory,
				Parameters);
		}

		public ClientSessionUri WithAssemblySearchPaths (IEnumerable<string> assemblySearchPaths)
		{
			if (AssemblySearchPaths.Length == 0 && assemblySearchPaths == null)
				return this;

			if (assemblySearchPaths != null && AssemblySearchPaths.SequenceEqual (assemblySearchPaths))
				return this;

			return new ClientSessionUri (
				AgentType,
				SessionKind,
				Host,
				Port,
				assemblySearchPaths,
				WorkbookPath,
				WorkingDirectory,
				Parameters);
		}

		public ClientSessionUri WithParameters (IEnumerable<KeyValuePair<string, string>> parameters)
		{
			if (Parameters.Length == 0 && parameters == null)
				return this;

			if (parameters != null && Parameters.SequenceEqual (parameters))
				return this;

			return new ClientSessionUri (
				AgentType,
				SessionKind,
				Host,
				Port,
				AssemblySearchPaths,
				WorkbookPath,
				WorkingDirectory,
				parameters);
		}

		public ClientSessionUri WithHostAndPort (string host, ushort? port)
		{
			if ((host == null || host == Host) && (port == null || port == Port))
				return this;

			return new ClientSessionUri (
				AgentType,
				SessionKind,
				host ?? Host,
				port ?? Port,
				AssemblySearchPaths,
				WorkbookPath,
				WorkingDirectory,
				Parameters);
		}

		public ClientSessionUri WithWorkingDirectory (string workingDirectory)
		{
			if (workingDirectory == WorkingDirectory)
				return this;

			return new ClientSessionUri (
				AgentType,
				SessionKind,
				Host,
				Port,
				AssemblySearchPaths,
				WorkbookPath,
				workingDirectory,
				Parameters);
		}

		public override string ToString ()
		{
			// `new Uri ("/")` and `new Uri ("/a")` (but not `new Uri ("/aa")`)
			// throws UriFormatException on Mono, and Windows really does not
			// like it if you just prepend 'file://' to an absolute path...
			if (WorkbookPath != null) {
				var uri = new Uri (WorkbookPath, UriKind.RelativeOrAbsolute);
				if (uri.IsAbsoluteUri)
					return uri.ToString ();

				// this should be okay for Mono for the '/' and '/a' cases...
				return "file://" + uri;
			}

			var builder = new StringBuilder (128 * (AssemblySearchPaths.Length + 2));

			builder.Append (xamarinInteractiveScheme).Append ("://");

			// if Port is non-zero we will have explicitly
			// set both Port and Host in a constructor
			if (Port > 0)
				builder.Append (Host).Append (':').Append (Port);

			var qpCount = 0;

			void QP (string k, object v)
				=> builder
					.Append (qpCount++ == 0 ? "/v1?" : "&")
					.Append (k)
					.Append ('=')
					.Append (v);

			if (AgentType != AgentType.Unknown)
				QP ("agentType", AgentType.ToString ());

			if (SessionKind != ClientSessionKind.Unknown)
				QP ("sessionKind", SessionKind.ToString ());

			foreach (var path in AssemblySearchPaths) {
				if (!String.IsNullOrEmpty (path))
					QP ("assemblySearchPath", Uri.EscapeDataString (path));
			}

			if (!String.IsNullOrEmpty (WorkingDirectory))
				QP ("workingDirectory", Uri.EscapeDataString (WorkingDirectory));

			foreach (var parameter in Parameters) {
				if (!String.IsNullOrEmpty (parameter.Key))
					QP (
						Uri.EscapeDataString (parameter.Key),
						Uri.EscapeDataString (parameter.Value));
			}

			return builder.ToString ();
		}

		public override int GetHashCode ()
			=> ToString ().GetHashCode ();

		public override bool Equals (object obj)
			=> Equals (obj as ClientSessionUri);

		public bool Equals (ClientSessionUri other)
			=> string.Equals (ToString (), other?.ToString ());

		public bool Equals (Uri other)
		{
			try {
				if (other.IsFile && other.LocalPath == WorkbookPath)
					return true;

				return other.Scheme == xamarinInteractiveScheme &&
					Equals (new ClientSessionUri (other));
			} catch {
				return false;
			}
		}

		public static bool operator == (ClientSessionUri a, ClientSessionUri b)
		{
			if (ReferenceEquals (a, b))
				return true;

			if ((object)a == null || (object)b == null)
				return false;

			return a.Equals (b);
		}

		public static bool operator != (ClientSessionUri a, ClientSessionUri b)
		{
			return !(a == b);
		}

		public static implicit operator string (ClientSessionUri uri)
			=> uri?.ToString ();

		public static explicit operator ClientSessionUri (Uri uri)
			=> new ClientSessionUri (uri);

		public static bool TryParse (string uriString, out ClientSessionUri uri)
		{
			uriString = uriString?.Trim ();

			if (string.IsNullOrWhiteSpace (uriString)) {
				uri = null;
				return false;
			}

			var startsWithSupportedScheme = UriStartsWithSupportedSchemeAndAuthority (uriString);

			try {
				if (startsWithSupportedScheme) {
					uri = new ClientSessionUri (new Uri (uriString));
					return true;
				}
			} catch {
			}

			try {
				// parse [host:]port
				string portStr;
				string host = null;
				ushort port;

				var portOffset = uriString.IndexOf (':');
				if (portOffset >= 0) {
					host = uriString.Substring (0, portOffset);
					portStr = uriString.Substring (portOffset + 1);
				} else
					portStr = uriString;

				if (!ushort.TryParse (portStr, out port))
					host = null;
				else
					host = string.IsNullOrWhiteSpace (host) ? localhost : host;

				if (host != null) {
					uri = new ClientSessionUri (host, port);
					return true;
				}
			} catch {
			}

			if (startsWithSupportedScheme) {
				uri = null;
				return false;
			}

			// try again with a prepended scheme. assume starting with / is an absolute
			// file path, so long as we don't start with /v1? which would indicate a
			// CSU without an authority.
			if (uriString [0] == '/' && !uriString.StartsWith ("/v1?", StringComparison.Ordinal))
				return TryParse ("file://" + uriString, out uri);

			return TryParse (xamarinInteractiveScheme + "://" + uriString, out uri);
		}

		internal static IReadOnlyList<KeyValuePair<string, string>> ParseQueryString (string query)
		{
			if (String.IsNullOrEmpty (query))
				return EmptyArray<KeyValuePair<string, string>>.Instance;

			var items = new List<KeyValuePair<string, string>> (8);

			var builder = new StringBuilder (query.Length);
			string key = null;
			string value = null;

			Action ProduceValue = () => {
				if (builder.Length == 0)
					return;

				if (key == null) {
					key = builder.ToString ();
					value = null;
				} else
					value = builder.ToString ();

				items.Add (new KeyValuePair<string, string> (
					Uri.UnescapeDataString (key),
					value == null ? null : Uri.UnescapeDataString (value)));

				key = null;
				value = null;
				builder.Clear ();
			};

			for (int i = 0, lastIndex = query.Length - 1; i <= lastIndex; i++) {
				if (i == 0 && query [i] == '?')
					continue;

				switch (query [i]) {
				case '=':
					// empty, but specified value
					if (i == lastIndex || (i < lastIndex && query [i + 1] == '&')) {
						ProduceValue ();
						break;
					}

					key = builder.ToString ();
					value = null;
					builder.Clear ();
					break;
				case '&':
					ProduceValue ();
					break;
				default:
					builder.Append (query [i]);
					break;
				}
			}

			ProduceValue ();

			return items;
		}
	}
}
