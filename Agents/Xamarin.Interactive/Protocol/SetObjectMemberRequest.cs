// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class SetObjectMemberRequest : MainThreadRequest<SetObjectMemberResponse>
    {
        public long ObjectHandle { get; }
        public RepresentedMemberInfo MemberInfo { get; }
        public object Value { get; }
        public bool ReturnUpdatedValue { get; }

        [JsonConstructor]
        public SetObjectMemberRequest (
            long objectHandle,
            RepresentedMemberInfo memberInfo,
            object value,
            bool returnUpdatedValue)
        {
            ObjectHandle = objectHandle;
            MemberInfo = memberInfo;
            Value = value;
            ReturnUpdatedValue = returnUpdatedValue;
        }

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
            return Task.FromResult (new SetObjectMemberResponse (success, updatedValue));
        }
    }
}