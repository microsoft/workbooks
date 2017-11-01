//
// TaskCompletionSource.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System.Threading.Tasks;

namespace Xamarin.Interactive
{
	sealed class TaskCompletionSource : TaskCompletionSource<TaskCompletionSource.Void>
	{
		internal struct Void
		{
		}

		public void SetResult () => SetResult (new Void ());
	}
}