//
// RemoteMemberInfo.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Rendering
{
	sealed class RemoteMemberInfo
	{
		public long ObjectHandle { get; set; }
		public RepresentedMemberInfo MemberInfo { get; set; }
	}
}