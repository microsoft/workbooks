//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    class InteractResponse
    {
        public IInteractiveObject Result { get; set; }
    }
}