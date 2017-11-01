//
// TestDriver.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Reflection;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Client.CommandLineTool
{
	static class TestDriver
	{
		static readonly List<string> arguments = new List<string> ();

		public static bool ShouldRun { get; private set; }

		public static void Initialize (string [] args)
		{
			ShouldRun = true;
			arguments.Clear ();
			arguments.AddRange (args);
		}

		public static void Run (Action<Action> dispatcher)
		{
			const string testRunnerTypeName = "Xamarin.Interactive.Tests.TestRunner";
			const string testRunnerExecuteMethodName = "Execute";

			MethodInfo testDriverRunMethod = null;

			for (int i = 0; i < arguments.Count; i++) {
				var filePath = new FilePath (arguments [i]);
				if (filePath.Exists) {
					// fix up file paths in the arguments list to be absolute since
					// the test runner will change working directories before running
					arguments [i] = filePath.FullPath;
					if (filePath.Extension != ".dll")
						continue;
				}

				if (testDriverRunMethod != null)
					continue;

				var testDriverAssemblyPath = filePath
					.ParentDirectory
					.Combine ("Xamarin.Interactive.Tests.dll");

				testDriverRunMethod = Assembly
					.LoadFrom (testDriverAssemblyPath)
					.GetType (testRunnerTypeName, false)
					?.GetMethod (
						testRunnerExecuteMethodName,
						BindingFlags.Public | BindingFlags.Static,
						new [] {
							typeof (Action<Action>),
							typeof (IEnumerable<string>)
						},
						typeof (void));
			}

			if (testDriverRunMethod == null)
				throw new Exception (
					$"Unable to resolve {testRunnerTypeName}::{testRunnerExecuteMethodName}");

			testDriverRunMethod.Invoke (
				null,
				new object [] {
					dispatcher,
					arguments
				});
		}
	}
}