//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.PropertyEditor
{
    interface IPropertyViewHelper
    {
        object ToLocalValue (object prop);
        object ToRemoteValue (object localValue);
    }
}