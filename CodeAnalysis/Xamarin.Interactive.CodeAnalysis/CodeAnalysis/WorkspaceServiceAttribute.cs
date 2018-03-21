// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
    [AttributeUsage (AttributeTargets.Assembly)]
    public sealed class WorkspaceServiceAttribute : Attribute
    {
        public string LanguageName { get; }
        public Type WorkspaceServiceActivatorType { get; }

        public WorkspaceServiceAttribute (string languageName, Type workspaceServiceActivatorType)
        {
            LanguageName = languageName;
            WorkspaceServiceActivatorType = workspaceServiceActivatorType;
        }
    }
}