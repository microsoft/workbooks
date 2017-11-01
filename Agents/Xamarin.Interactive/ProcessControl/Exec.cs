//
// Exec.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.ProcessControl
{
	using static ExecFlags;

	public sealed class Exec
	{
		public sealed class ExecLog : EventArgs
		{
			public int ExecId { get; }
			public ExecFlags Flags { get; }
			public ProcessArguments Arguments { get; }
			public int? ExitCode { get; }

			internal ExecLog (
				int execId,
				ExecFlags flags,
				ProcessArguments arguments,
				int? exitCode = null)
			{
				ExecId = execId;
				Flags = flags;
				Arguments = arguments;
				ExitCode = exitCode;
			}
		}

		public static event EventHandler<ExecLog> Log;

		static volatile int lastId;

		readonly int id;

		public ProcessArguments Arguments { get; }
		public ConsoleRedirection OutputRedirection { get; }
		public Action<StreamWriter> InputHandler { get; }
		public ExecFlags Flags { get; }
		public string WorkingDirectory { get; }

		public Exec (
			ProcessArguments arguments,
			ExecFlags flags = None,
			Action<ConsoleRedirection.Segment> outputHandler = null,
			Action<StreamWriter> inputHandler = null,
			string workingDirectory = null)
			: this (
				arguments,
				flags,
				outputHandler == null ? null : new ConsoleRedirection (outputHandler),
				inputHandler,
				workingDirectory)
		{
		}

		public Exec (
			ProcessArguments arguments,
			ExecFlags flags = None,
			ConsoleRedirection outputRedirection = null,
			Action<StreamWriter> inputHandler = null,
			string workingDirectory = null)
		{
			Arguments = arguments;

			if (Arguments.Count < 1)
				throw new ArgumentOutOfRangeException (
					nameof (arguments),
					"must have at least one argument (the file name to execute)");

			Flags = flags;
			InputHandler = inputHandler;
			OutputRedirection = outputRedirection;

			if (Flags.HasFlag (RedirectStdin) && InputHandler == null)
				throw new ArgumentException (
					$"{nameof (RedirectStdin)} was specified " +
					$"but {nameof (InputHandler)} is null",
					nameof (flags));

			if (Flags.HasFlag (RedirectStdout) && OutputRedirection == null)
				throw new ArgumentException (
					$"{nameof (RedirectStdout)} was specified " +
					$"but {nameof (OutputRedirection)} is null",
					nameof (flags));

			if (Flags.HasFlag (RedirectStderr) && OutputRedirection == null)
				throw new ArgumentException (
					$"{nameof (RedirectStderr)} was specified " +
					$"but {nameof (OutputRedirection)} is null",
					nameof (flags));

			WorkingDirectory = workingDirectory;

			id = lastId++;
		}

		public sealed class ExitException : Exception
		{
			public Exec Exec { get; }
			public int ExitCode { get; }

			public ExitException (Exec exec, int exitCode)
				: base ($"{exec.Arguments [0]} terminated with exit code {exitCode}")
			{
				Exec = exec;
				ExitCode = exitCode;
			}
		}

		static readonly bool isWindows = Environment.OSVersion.Platform != PlatformID.Unix;

		public Task RunAsync ()
		{
			var arguments = Arguments;

			if (!isWindows) {
				if (Path.GetExtension (arguments [0]) == ".exe")
					arguments = arguments.Insert (0, "mono");

				if (Flags.HasFlag (Elevate))
					arguments = arguments.Insert (0, "/usr/bin/sudo");
			}

			var proc = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = arguments [0],
					Arguments = string.Join (
						" ",
						arguments.Skip (1).Select (ProcessArguments.Quote)),
					UseShellExecute = false,
					CreateNoWindow = true,
					WorkingDirectory = WorkingDirectory
				}
			};

			Log?.Invoke (null, new ExecLog (id, Flags, arguments));

			if (isWindows && Flags.HasFlag (Elevate)) {
				proc.StartInfo.UseShellExecute = true;
				proc.StartInfo.Verb = "runas";
			} else {
				proc.StartInfo.RedirectStandardInput = Flags.HasFlag (RedirectStdin);
				proc.StartInfo.RedirectStandardOutput = Flags.HasFlag (RedirectStdout);
				proc.StartInfo.RedirectStandardError = Flags.HasFlag (RedirectStderr);
			}

			if (proc.StartInfo.RedirectStandardOutput)
				proc.OutputDataReceived += (sender, e)
					=> WriteOutput (e.Data, OutputRedirection.StandardOutput);

			if (proc.StartInfo.RedirectStandardError)
				proc.ErrorDataReceived += (sender, e)
					=> WriteOutput (e.Data, OutputRedirection.StandardError);

			void WriteOutput (string data, TextWriter writer)
			{
				if (string.IsNullOrEmpty (data))
					return;

				data += Environment.NewLine;

				if (Flags.HasFlag (OutputOnSynchronizationContext) &&
					SynchronizationContext.Current != null)
					SynchronizationContext.Current.Post (writer.Write, data);
				else
					writer.Write (data);
			}

			var tcs = new TaskCompletionSource<int> ();

			Task.Run (() => {
				try {
					proc.Start ();

					if (proc.StartInfo.RedirectStandardOutput)
						proc.BeginOutputReadLine ();

					if (proc.StartInfo.RedirectStandardError)
						proc.BeginErrorReadLine ();

					InputHandler?.Invoke (proc.StandardInput);

					proc.WaitForExit ();

					Log?.Invoke (null, new ExecLog (id, Flags, arguments, proc.ExitCode));

					if (proc.ExitCode != 0)
						tcs.SetException (new ExitException (this, proc.ExitCode));
					else
						tcs.SetResult (proc.ExitCode);
				} catch (Exception e) {
					tcs.SetException (e);
				} finally {
					proc.Close ();
				}
			});

			return tcs.Task;
		}

		#region Convenience Methods

		public static Task RunAsync (
			Action<ConsoleRedirection.Segment> outputHandler,
			string command,
			params string [] arguments)
			=> RunAsync (Default, outputHandler, command, arguments);

		public static Task RunAsync (
			ExecFlags flags,
			Action<ConsoleRedirection.Segment> outputHandler,
			string command,
			params string [] arguments)
			=> new Exec (
				ProcessArguments.FromCommandAndArguments (command, arguments),
				flags | RedirectStdout | RedirectStderr,
				outputHandler).RunAsync ();

		public static IReadOnlyList<string> Run (
			string command,
			params string [] arguments)
			=> Run (Default, command, arguments);

		public static IReadOnlyList<string> Run (
			ExecFlags flags,
			string command,
			params string [] arguments)
		{
			var lines = new List<string> ();

			new Exec (
				ProcessArguments.FromCommandAndArguments (command, arguments),
				flags | RedirectStdout | RedirectStderr,
				segment => lines.Add (segment.Data.TrimEnd ('\r', '\n')))
				.RunAsync ()
				.GetAwaiter ()
				.GetResult ();

			return lines;
		}

		#endregion
	}
}