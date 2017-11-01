//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Rendering
{
    sealed class RemoteMemberInfo
    {
        public long ObjectHandle { get; set; }
        public RepresentedMemberInfo MemberInfo { get; set; }
    }
}