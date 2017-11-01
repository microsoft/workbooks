//
// UpdaterQueryInfo.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Client.Updater
{
    struct UpdaterQueryInfo
    {
        public string OperatingSystem { get; }
        public string ProductId { get; }

        public UpdaterQueryInfo (string operatingSystem, string productId)
        {
            OperatingSystem = operatingSystem
                ?? throw new ArgumentNullException (nameof (operatingSystem));

            ProductId = productId
                ?? throw new ArgumentNullException (nameof (productId));
        }
    }
}