//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { IContextualMenuItem } from 'office-ui-fabric-react/lib/ContextualMenu';

const addPackagesItem: IContextualMenuItem = {
    key: 'addPackage',
    name: 'Add NuGet Package',
    icon: 'Search',
    onClick: () => { }
}

const openWorkbookItem: IContextualMenuItem = {
    key: 'openWorkbook',
    name: 'Open',
    icon: 'Upload',
    onClick: () => { }
}

const saveWorkbookItem: IContextualMenuItem = {
    key: 'saveWorkbook',
    name: 'Save',
    icon: 'Download',
    onClick: () => { }
}

const items: IContextualMenuItem[] = [
    addPackagesItem,
    {
        key: 'workbookTarget',
        name: 'Workbook Target',
        subMenuProps: {
            items: [
                {
                    key: 'workbookTarget-Console',
                    name: 'Console',
                    subMenuProps: {
                        items: [
                            {
                                key: 'workbookTarget-DotNetCore',
                                name: '.NET Core'
                            },
                            {
                                key: 'workbookTarget-Console',
                                name: '.NET Framework'
                            }
                        ]
                    }
                }
            ]
        }
    },
    openWorkbookItem,
    saveWorkbookItem
]

const dumpDraftState: IContextualMenuItem = {
    key: 'dumpDraftState',
    icon: 'Rocket',
    onClick: () => { }
}

const farItems: IContextualMenuItem[] = [
    dumpDraftState
]

interface WorkbookCommandBarProps {
    addPackages: () => void
    loadWorkbook: () => void
    saveWorkbook: () => void
    dumpDraftState: () => void
}

export class WorkbookCommandBar extends React.Component<WorkbookCommandBarProps, any> {
    constructor(props: WorkbookCommandBarProps) {
        super(props)

        addPackagesItem.onClick = props.addPackages
        saveWorkbookItem.onClick = props.saveWorkbook
        openWorkbookItem.onClick = props.loadWorkbook
        dumpDraftState.onClick = props.dumpDraftState
    }

    public render() {
        return (
            <CommandBar
                elipisisAriaLabel='More options'
                items={items}
                farItems={farItems}
            />
        );
    }
}