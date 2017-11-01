//
// WpfClientSessionViewControllers.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client.Windows
{
	sealed class WpfClientSessionViewControllers : IClientSessionViewControllers
	{
		public MessageViewController Messages { get; }
		public History ReplHistory { get; private set; }
		public WorkbookTargetsViewController WorkbookTargets { get; }

		public WpfClientSessionViewControllers (
			WpfMessageViewDelegate messageViewDelegate,
			WpfDialogMessageViewDelegate dialogMessageViewDelegate)
		{
			// Set up history. Don't provide any initial seed, History will load the saved history on its
			// own if the persist flag is true.
			ReplHistory = new History (history: null, persist: Prefs.Repl.SaveHistory.GetValue ());
			Messages = new MessageViewController (messageViewDelegate, dialogMessageViewDelegate);
			WorkbookTargets = new WorkbookTargetsViewController ();
			PreferenceStore.Default.Subscribe (ObservePreferenceChange);
		}

		void ObservePreferenceChange (PreferenceChange obj)
		{
			if (obj.Key == Prefs.Repl.SaveHistory.Key)
				ReplHistory = new History (history: ReplHistory?.Entries,
					persist: Prefs.Repl.SaveHistory.GetValue ());
		}
	}
}