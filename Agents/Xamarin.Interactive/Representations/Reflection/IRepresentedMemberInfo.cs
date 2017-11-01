//
// IRepresentedMemberInfo.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Representations.Reflection
{
	public interface IRepresentedMemberInfo
	{
		IRepresentedType DeclaringType { get; }
		RepresentedMemberKind MemberKind { get; }
		IRepresentedType MemberType { get; }
		string Name { get; }
		bool CanWrite { get; }
	}
}