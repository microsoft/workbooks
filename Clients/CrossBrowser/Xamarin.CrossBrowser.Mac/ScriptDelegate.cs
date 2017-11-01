//
// ScriptDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

namespace Xamarin.CrossBrowser
{
    public delegate void ScriptAction (dynamic thisObject, dynamic [] arguments);
    public delegate dynamic ScriptFunc (dynamic thisObject, dynamic [] arguments);
}