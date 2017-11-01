//
// InspectorSupport.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Runtime.CompilerServices;

using Xamarin.Interactive.Core;

namespace Xamarin
{
	public static partial class InspectorSupport
	{
		static Agent agent;

		static partial void CreateAgent (AgentStartOptions startOptions);

		internal static Action<object> AgentStartedHandler;

		#if DEBUG
		public static void Start (AgentStartOptions startOptions = null) => CreateAgent (startOptions);
		#endif

		static void Start ()
		{
			try {
				CreateAgent (null);
			} catch (Exception e) {
				Console.Error.WriteLine (e);
			}
		}

		internal static void Stop ()
		{
			agent?.Dispose ();
			agent = null;
		}

		[MethodImpl (MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		static void BreakdanceStep ()
		{
		}

		#if !WPF

		[MethodImpl (MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		internal static void AgentStarted (string agentConnectUri)
		{
		}

		#endif
	}
}