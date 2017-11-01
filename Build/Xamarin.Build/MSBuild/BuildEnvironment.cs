//
// BuildEnvironment.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
	public sealed class BuildEnvironment : Task
	{
		const string PullRequestBranchRegex = "refs/pull/(\\d+)/merge";

		[Output]
		public string DefinitionName { get; private set; }

		[Output]
		public string Revision { get; private set; }

		[Output]
		public string Branch { get; private set; }

		public override bool Execute ()
		{
			DefinitionName = GetDefinitionName ();
			Revision = GetRevision ();
			Branch = GetBranch ();

			if (Branch != null && Regex.IsMatch (Branch, PullRequestBranchRegex))
				Revision = null;

			return true;
		}

		public static string GetDefinitionName (string definitionName = null)
			=> GetEnvironment (
				definitionName,
				"BUILD_DEFINITIONNAME",
				"BUILD_LANE");

		public static string GetRevision (string revision = null)
			=> GetEnvironment (
				revision,
				"BUILD_SOURCEVERSION",
				"BUILD_REVISION");

		public static string GetBranch (string branch = null)
			=> GetEnvironment (
				branch,
				"BUILD_SOURCEBRANCH",
				"BUILD_LANE_MAX_REVISION")
				?.Replace ("remotes/origin/", string.Empty)
				?.Replace ("refs/heads/", string.Empty);

		static string GetEnvironment (string value, params string [] environmentVariables)
		{
			if (!string.IsNullOrEmpty (value))
				return value;

			foreach (var env in environmentVariables) {
				value = Environment.GetEnvironmentVariable (env);
				if (!string.IsNullOrEmpty (value))
					return value;
			}

			return null;
		}
	}
}