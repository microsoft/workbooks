//
// IInteractiveObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Representations
{
	interface IInteractiveObject : IRepresentationObject
	{
		long RepresentedObjectHandle { get; }
		long Handle { get; set; }
		void Initialize ();
		void Reset ();
		IInteractiveObject Interact (object message);
	}
}