//
// MemberReferenceRequestArgs.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Rendering
{
	sealed class MemberReferenceRequestArgs : EventArgs
	{
		public InteractiveObject InteractiveObject { get; }
		public RepresentedType InteractiveObjectType { get; }
		public string MemberName { get; }

		public MemberReferenceRequestArgs (
			InteractiveObject interactiveObject,
			RepresentedType interactiveObjectType,
			string memberName = null)
		{
			if (interactiveObject == null)
				throw new ArgumentNullException (nameof(interactiveObject));
			if (interactiveObjectType == null)
				throw new ArgumentNullException (nameof(interactiveObjectType));

			InteractiveObject = interactiveObject;
			InteractiveObjectType = interactiveObjectType;
			MemberName = memberName;
		}
	}
}