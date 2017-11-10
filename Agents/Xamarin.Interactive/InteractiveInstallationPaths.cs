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
        public string WorkbookAppsInstallPath { get; }
        public string ToolsInstallPath { get; }

        public InteractiveInstallationPaths (
            string workbooksClientInstallPath,
            string inspectorClientInstallPath,
            string agentsInstallPath,
            string workbookAppsInstallPath,
            string toolsInstallPath)
        {
            WorkbooksClientInstallPath = workbooksClientInstallPath
                ?? throw new ArgumentNullException (nameof (workbooksClientInstallPath));

            InspectorClientInstallPath = inspectorClientInstallPath
                ?? throw new ArgumentNullException (nameof (inspectorClientInstallPath));

            AgentsInstallPath = agentsInstallPath
                ?? throw new ArgumentNullException (nameof (agentsInstallPath));

            WorkbookAppsInstallPath = workbookAppsInstallPath
                ?? throw new ArgumentNullException (nameof (workbookAppsInstallPath));

            ToolsInstallPath = toolsInstallPath
                ?? throw new ArgumentNullException (nameof (toolsInstallPath));
        }
    }
}
