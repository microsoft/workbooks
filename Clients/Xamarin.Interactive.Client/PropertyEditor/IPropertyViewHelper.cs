//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.PropertyEditor
{
    interface IPropertyViewHelper
    {
        bool IsConvertable (Type type);
        object ToLocalValue (object prop);
        object ToRemoteValue (object localValue);
    }
}