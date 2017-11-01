//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Messages;

namespace Xamarin.Interactive.IO
{
    /// <summary>
	/// An instance of <see cref="T:QuarantineInfo"/> represents at a minimum that its
	/// <see cref="RepresentedFile"/> is to be considered under quarantine. Additional
	/// properties are optional but represent additional information that can help the
	/// user decide whether or not it is safe to unquarantine the file.
	/// </summary>
    sealed class QuarantineInfo
    {
        /// <summary>
		/// The file under quarantine.
		/// </summary>
        public FilePath RepresentedFile { get; }

        /// <summary>
		/// The date and time the item was quarantined.
		/// </summary>
        public DateTime TimeStamp { get; }

        /// <summary>
		/// The URL of the resource originally hosting the quarantined item, from the user's point of view.
		/// </summary>
        public Uri OriginUrl { get; }

        /// <summary>
		/// The name of the quarantining agent (application or program).
		/// </summary>
        public string AgentName { get; }

        public QuarantineInfo (
            FilePath representedFile,
            DateTime timeStamp = default (DateTime),
            Uri originUrl = null,
            string agentName = null)
        {
            if (representedFile.IsNull)
                throw new ArgumentNullException (nameof (representedFile));

            RepresentedFile = representedFile;

            TimeStamp = timeStamp;
            OriginUrl = originUrl;
            AgentName = agentName;
        }

        public Message CreateAlert ()
            => Message.CreateErrorAlert (
                    AlertMessageText,
                    AlertDetailsText)
                .WithAction (new MessageAction (
                    MessageActionKind.Negative,
                    "cancel",
                    Catalog.GetString ("Cancel"),
                    Catalog.GetString ("Close this Workbook")))
                .WithAction (new MessageAction (
                    MessageActionKind.Affirmative,
                    "open",
                    Catalog.GetString ("Open"),
                    Catalog.GetString ("Open this Workbook")));

        string AlertMessageText => Catalog.Format (Catalog.GetString (
            "The workbook “{0}” was downloaded from the Internet. " +
            "Are You sure you want to open it?",
            comment: "'{0}' is the file name of the workbook."),
            RepresentedFile.Name);

        string AlertDetailsText {
            get {
                var informativePreamble = Catalog.Format (Catalog.GetString (
                    "Only open workbooks from trusted sources.\n\n" +
                    "Opening “{0}” will always allow it to run on this computer.",
                    comment: "'{0}' is the file name of the workbook"),
                    RepresentedFile.Name);

                string relativeTimeStamp = null;

                var haveHost = !String.IsNullOrEmpty (OriginUrl?.Host);
                var haveAgentName = !String.IsNullOrEmpty (AgentName);
                var haveTimeStamp = TimeStamp > DateTime.MinValue;

                if (haveTimeStamp)
                    relativeTimeStamp = TimeStamp.ToShortDateString ();

                string informativeDetails = null;

                if (haveHost && haveAgentName && haveTimeStamp)
                    informativeDetails = Catalog.Format (Catalog.GetString (
                        "{0} downloaded this file {1} from {2}.",
                        comment: "'{0}' is the name of an app, " +
                            "'{1}' is a relative date/time string, " +
                            "'{2}' is the origin URL host"),
                        AgentName,
                        relativeTimeStamp,
                        OriginUrl.Host);
                else if (haveAgentName && haveTimeStamp)
                    informativeDetails = Catalog.Format (Catalog.GetString (
                        "{0} downloaded this file {1}.",
                        comment: "'{0}' is the name of an app, " +
                            "'{1}' is a relative date/time string"),
                        AgentName,
                        relativeTimeStamp);
                else if (haveTimeStamp)
                    informativeDetails = Catalog.Format (Catalog.GetString (
                        "This file was downloaded {0}.",
                        comment: "'{0}' is a relative date/time string"));

                // highly unlikely we'll ever encounter other permutations

                if (informativeDetails != null)
                    return informativePreamble + "\n\n" + informativeDetails;

                return informativePreamble;
            }
        }
    }
}