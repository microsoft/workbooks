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
    name: 'NuGet',
    icon: 'Add',
    onClick: () => { }
}

const openWorkbookItem: IContextualMenuItem = {
    key: 'openWorkbook',
    name: 'Open',
    icon: 'OpenFile',
    onClick: () => { }
}

const saveWorkbookItem: IContextualMenuItem = {
    key: 'saveWorkbook',
    name: 'Save',
    icon: 'DownloadDocument',
    onClick: () => { }
}

const items: IContextualMenuItem[] = [
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
    addPackagesItem
]

const overflowItems: IContextualMenuItem[] = [
    openWorkbookItem,
    saveWorkbookItem
]

const dumpDraftState: IContextualMenuItem = {
    key: 'dumpDraftState',
    icon: 'Rocket',
    onClick: () => { }
}

const farItems: IContextualMenuItem[] = [
    // dumpDraftState
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
                overflowItems={overflowItems}
                farItems={farItems}
            />
        );
    }
}