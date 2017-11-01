//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.CrossBrowser
{
    public delegate void ScriptAction (dynamic thisObject, dynamic [] arguments);
    public delegate dynamic ScriptFunc (dynamic thisObject, dynamic [] arguments);
}