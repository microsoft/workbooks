//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class SetObjectMemberRequest : MainThreadRequest<SetObjectMemberResponse>
    {
        public long ObjectHandle { get; set; }
        public RepresentedMemberInfo MemberInfo { get; set; }
        public object Value { get; set; }
        public bool ReturnUpdatedValue { get; set; }

        protected override Task<SetObjectMemberResponse> HandleAsync (Agent agent)
        {
            InteractiveObject updatedValue;
            var success = agent.TrySetObjectMember (
                ObjectHandle,
                MemberInfo,
                Value,
                ReturnUpdatedValue,
                out updatedValue);
            updatedValue?.Interact (new InteractiveObject.ReadAllMembersInteractMessage ());
            return Task.FromResult (new SetObjectMemberResponse {
                Success = success,
                UpdatedValue = updatedValue,
            });
        }
    }
}