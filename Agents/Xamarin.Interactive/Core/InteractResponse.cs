//
// InteractResponse.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

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