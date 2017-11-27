//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive
{
    sealed class InteractiveInstallationPaths
    {
        public string WorkbooksClientInstallPath { get; }
        public string InspectorClientInstallPath { get; }
        public string AgentsInstallPath { get; }
        public string ToolsInstallPath { get; }

        public InteractiveInstallationPaths (
            string agentsInstallPath,
            string workbooksClientInstallPath = null,
            string inspectorClientInstallPath = null,
            string toolsInstallPath = null)
        {
            AgentsInstallPath = agentsInstallPath
                ?? throw new ArgumentNullException (nameof (agentsInstallPath));

            WorkbooksClientInstallPath = workbooksClientInstallPath;

            InspectorClientInstallPath = inspectorClientInstallPath;

            ToolsInstallPath = toolsInstallPath;
        }
    }
}
