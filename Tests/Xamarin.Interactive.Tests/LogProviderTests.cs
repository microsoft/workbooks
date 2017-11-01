//
// LogProviderTests.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

using Should;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class LogProviderTests
	{
		[TestCase (LogLevel.Debug)]
		[TestCase (LogLevel.Verbose)]
		public void Log_provider_drops_higher_verbosity (LogLevel newEntryLevel)
		{
			var lp = new LogProvider (LogLevel.Info);
			var sentEntry = default (LogEntry);
			lp.EntryAdded += (sender, logEntry) => { sentEntry = logEntry; };

			var newLogEntry = new LogEntry (
				null,
				DateTime.UtcNow,
				TimeSpan.Zero,
				newEntryLevel,
				LogFlags.None,
				null,
				"Test log.",
				null,
				null,
				0);

			lp.Commit (newLogEntry);

			sentEntry.ShouldNotEqual (newLogEntry);
			sentEntry.ShouldEqual (default (LogEntry));
		}

		static IEnumerable<ITestCaseData> GetPassthroughTestCases ()
		{
			var enumValues = Enum.GetValues (typeof (LogLevel)).Cast<LogLevel> ().ToArray ();

			// These are pairs of logger level vs. log levels that should pass through.
			yield return new TestCaseData (
				LogLevel.Debug,
				enumValues.Where (ev => ev > LogLevel.Debug).ToArray ());
			yield return new TestCaseData (LogLevel.Verbose, enumValues);
		}

		[TestCaseSource (nameof (GetPassthroughTestCases))]
		public void Log_provider_passes_through_correct_verbosity (LogLevel logProviderLevel, LogLevel [] allowedLevels)
		{
			var lp = new LogProvider (logProviderLevel);
			var sentEntry = default (LogEntry);
			lp.EntryAdded += (sender, logEntry) => { sentEntry = logEntry; };

			var allLevels = Enum.GetValues (typeof (LogLevel)).Cast<LogLevel> ().ToArray ();

			foreach (var level in allLevels) {
				var newLogEntry = new LogEntry (
					null,
					DateTime.UtcNow,
					TimeSpan.Zero,
					level,
					LogFlags.None,
					null,
					"Test log.",
					null,
					null,
					0);

				lp.Commit (newLogEntry);

				if (allowedLevels.Contains (level)) {
					sentEntry.ShouldEqual (newLogEntry);
					sentEntry.ShouldNotEqual (default (LogEntry));
				}
			}
		}
	}
}
