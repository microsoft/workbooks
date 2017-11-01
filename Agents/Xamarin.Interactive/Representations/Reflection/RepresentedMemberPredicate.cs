//
// RepresentedMemberPredicate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Representations.Reflection
{
	delegate bool RepresentedMemberPredicate (RepresentedMemberInfo representedMemberInfo, object obj);
}