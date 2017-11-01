//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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