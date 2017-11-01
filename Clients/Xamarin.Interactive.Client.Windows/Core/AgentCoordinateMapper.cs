// AgentCoordinateMapper.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015 Xamarin Inc.

using System;
using System.Windows;
using System.Windows.Automation;

namespace Xamarin.Interactive.Core
{
	abstract class AgentCoordinateMapper
	{
		public abstract bool TryGetLocalCoordinate (Point hostCoordinate, out Point localCoordinate);

		public abstract Rect GetHostRect (Rect localRect);

		public static AgentCoordinateMapper Create (AgentIdentity agentIdentity, Window window)
		{
			if (agentIdentity == null)
				throw new ArgumentNullException (nameof (agentIdentity));

			var agentType = agentIdentity.AgentType;

			switch (agentType) {
			case AgentType.Android:
				return new WindowsAndroidCoordinateMapper (agentIdentity, window);
			case AgentType.WPF:
				return new WpfCoordinateMapper (window);
			case AgentType.iOS:
				return new iOSCoordinateMapper (agentIdentity, window);
			default:
				throw new NotSupportedException (
					String.Format ("AgentType {0} not supported", agentType));
			}
		}

		protected static AutomationElement GetFirstChild (
			TreeWalker treeWalker,
			AutomationElement rootElement,
			ControlType controlType = null,
			Func<AutomationElement, bool> condition = null,
			bool recursive = false)
		{
			AutomationElement matchingChild = null;

			var child = treeWalker.GetFirstChild (rootElement);
			while (child != null) {
				if ((controlType == null || child.Current.ControlType == controlType)
					&& (condition == null || condition (child))) {
					matchingChild = child;
					break;
				}

				if (recursive)
					matchingChild = GetFirstChild (treeWalker, child, controlType, condition, true) ?? matchingChild;

				child = treeWalker.GetNextSibling (child);
			}

			return matchingChild;
		}
	}
}
