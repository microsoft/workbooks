//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { IContextualMenuItem } from 'office-ui-fabric-react/lib/ContextualMenu';

import { WorkbookTarget, DotNetSdk } from '../WorkbookSession'

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

interface WorkbookCommandBarState {
    items: IContextualMenuItem[]
}

export class WorkbookCommandBar extends React.Component<WorkbookCommandBarProps, WorkbookCommandBarState> {
    constructor(props: WorkbookCommandBarProps) {
        super(props)

        this.state = {
            items: [
                addPackagesItem
            ]
        }

        addPackagesItem.onClick = props.addPackages
        saveWorkbookItem.onClick = props.saveWorkbook
        openWorkbookItem.onClick = props.loadWorkbook
        dumpDraftState.onClick = props.dumpDraftState
    }

    setWorkbookTargets(targets: WorkbookTarget[]) {
        let targetItems: IContextualMenuItem[] = []
        for (const target of targets)
            targetItems.push({
                key: target.id,
                name: `${target.flavor} (${(target.sdk as any).Name})`
            })

        this.setState({
            items: [
                {
                    key: 'workbookTarget',
                    name: 'Workbook Target',
                    subMenuProps: {
                        items: targetItems
                    }
                },
                addPackagesItem
            ]
        })
    }

    render() {
        return (
            <CommandBar
                elipisisAriaLabel='More options'
                items={this.state.items}
                overflowItems={overflowItems}
                farItems={farItems}
            />
        );
    }
}