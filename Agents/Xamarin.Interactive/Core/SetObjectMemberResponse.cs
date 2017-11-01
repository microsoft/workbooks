//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class SetObjectMemberResponse
    {
        public bool Success { get; set; }
        public InteractiveObject UpdatedValue { get; set; }
    }
}