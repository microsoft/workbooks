//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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