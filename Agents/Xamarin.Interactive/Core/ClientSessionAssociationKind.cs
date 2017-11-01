//
// ClientSessionAssociationKind.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Core
{
	[Serializable]
	enum ClientSessionAssociationKind
	{
		Initial,
		Reassociating,
		Dissociating
	}
}