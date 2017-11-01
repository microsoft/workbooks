//
// HttpServer.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2017 Xamarin Inc. All rights reserved.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Core
{
	abstract class HttpServer : IDisposable
	{
		const string TAG = nameof (HttpServer);

		readonly object mutex = new object ();

		CancellationTokenSource cts;
		Semaphore semaphore;
		ManualResetEventSlim stopEvent;
		HttpListener listener;

		protected Uri BaseUri { get; private set; }
		protected int MaxConnections { get; set; } = 4;

		bool IsListening => listener != null;

		protected abstract Task PerformHttpAsync (
			HttpListenerContext context,
			CancellationToken cancellationToken);

		public void Dispose ()
		{
			GC.SuppressFinalize (this);
			Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing)
				Stop ();
		}

		void StopListener ()
		{
			listener?.Stop ();
			listener = null;
		}

		protected void Start ()
		{
			lock (mutex) {
				if (IsListening)
					return;

				var timeout = DateTime.Now.AddSeconds (1);
				SocketException exception = null;

				while (true) {
					if (DateTime.Now >= timeout) {
						StopListener ();
						throw new Exception (
							"unable to bind to a port within an acceptable amount of time",
							exception);
					}

					var port = (ushort)ValidPortRange.GetRandom ();

					try {
						// Fall back to 127.0.0.1 instead of "localhost" to avoid IPv6/IPv4 ambiguity.
						// On Windows < 10, we only have permission to use "localhost".
						var osVersion = Environment.OSVersion;
						var host = osVersion.Platform == PlatformID.Win32NT && osVersion.Version.Major < 10
							? "localhost"
							: "127.0.0.1";

						BaseUri = new Uri ($"http://{host}:{port}");

						listener = new HttpListener ();
						listener.Prefixes.Add (BaseUri.AbsoluteUri);
						listener.Start ();

						cts = new CancellationTokenSource ();
						semaphore = new Semaphore (MaxConnections, MaxConnections);
						stopEvent = new ManualResetEventSlim (false);

						using (var startEvent = new ManualResetEventSlim (false)) {
							new Thread (() => {
								startEvent.Set ();
								AcceptLoop ();
							}) { IsBackground = true }.Start ();

							startEvent.Wait ();
						}

						break;
					} catch (SocketException e) {
						exception = e;
					}
				}
			}
		}

		protected void Stop ()
		{
			lock (mutex) {
				if (!IsListening)
					return;

				semaphore.Release ();
				cts.Cancel ();
			}

			stopEvent.Wait ();
			Log.Debug (TAG, "Stopped");
		}

		void AcceptLoop ()
		{
			Log.Debug (TAG, $"Listening on prefix {BaseUri}");

			while (true) {
				semaphore.WaitOne ();

				CancellationToken cancellationToken;

				lock (mutex) {
					if (cts == null || cts.Token.IsCancellationRequested) {
						Log.Debug (TAG, "AcceptLoop: cancellation requested");
						semaphore.Dispose ();

						StopListener ();
						stopEvent.Set ();
						return;
					}

					cancellationToken = cts.Token;
				}

				Log.Debug (TAG, "AcceptLoop: AcceptClientConnectionAsync");

				// do not await this in order to return control immediately back to
				// the loop so we can hit the semaphore wait for the next connection
				listener.GetContextAsync ().ContinueWith (async t1 => {
					Log.Debug (TAG, "AcceptLoop: AcceptClientConnectionAsync continuation");
					semaphore.Release ();

					if (t1.Exception != null) {
						Log.Error (TAG, "AcceptClientConnectionAsync failed", t1.Exception);
						return;
					}

					var context = t1.Result;
					if (context == null)
						return;

					try {
						await PerformHttpAsync (context, cancellationToken)
							.ConfigureAwait (false);

						context.Response.Close ();
					} catch (Exception e) {
						Log.Error (TAG, "HandleClientConnectionAsync failed", e);
					}
				}).ConfigureAwait (false);
			}
		}
	}
}