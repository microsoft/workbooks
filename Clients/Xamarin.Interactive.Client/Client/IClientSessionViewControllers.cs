//
// IClientSessionViewControllers.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client
{
	interface IClientSessionViewControllers
	{
		MessageViewController Messages { get; }
		History ReplHistory { get; }
		WorkbookTargetsViewController WorkbookTargets { get; }
	}
}