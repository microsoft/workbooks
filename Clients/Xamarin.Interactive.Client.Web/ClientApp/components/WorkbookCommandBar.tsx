//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { IContextualMenuItem } from 'office-ui-fabric-react/lib/ContextualMenu';

import {
    WorkbookTarget,
    DotNetSdk,
    WorkbookSession,
    SessionEvent,
    SessionEventKind
} from '../WorkbookSession'

import { WorkbookShellContext } from './WorkbookShell';

interface WorkbookCommandBarProps {
    shellContext: WorkbookShellContext
    evaluateWorkbook: () => void
    addPackages: () => void
    loadWorkbook: () => void
    saveWorkbook: () => void
    dumpDraftState: () => void
}

interface WorkbookCommandBarState {
    canOpenWorkbook: boolean
    workbookTargetItems: IContextualMenuItem[]
}

export class WorkbookCommandBar extends React.Component<WorkbookCommandBarProps, WorkbookCommandBarState> {
    constructor(props: WorkbookCommandBarProps) {
        super(props)

        this.onSessionEvent = this.onSessionEvent.bind(this)

        this.state = {
            canOpenWorkbook: false,
            workbookTargetItems: []
        }
    }

    private onSessionEvent(session: WorkbookSession, sessionEvent: SessionEvent) {
        this.setState({ canOpenWorkbook: sessionEvent.kind === SessionEventKind.Ready })
    }

    componentDidMount() {
        this.props.shellContext.session.sessionEvent.addListener(this.onSessionEvent)
    }

    componentWillUnmount() {
        this.props.shellContext.session.sessionEvent.removeListener(this.onSessionEvent)
    }

    setWorkbookTargets(targets: WorkbookTarget[]) {
        let workbookTargetItems: IContextualMenuItem[] = []
        for (const target of targets)
            workbookTargetItems.push({
                key: target.id,
                name: `${target.flavor} (${(target.sdk as any).Name})`
            })
        this.setState({ workbookTargetItems })
     }

    render() {
        const commandBarProps = {
            items: [
                {
                    key: 'workbookTarget',
                    name: 'Mono: .NET Framework',
                    icon: 'CSharpLanguage',
                    subMenuProps: {
                        items: this.state.workbookTargetItems
                    }
                },
                {
                    key: 'evaluateWorkbook',
                    name: 'Run All',
                    icon: 'Play',
                    onClick: this.props.evaluateWorkbook
                },
                {
                    key: 'addPackage',
                    name: 'NuGet',
                    icon: 'Add',
                    onClick: this.props.addPackages
                }
            ],
            overflowItems: [
                {
                    key: 'openWorkbook',
                    name: 'Open',
                    icon: 'OpenFile',
                    disabled: !this.state.canOpenWorkbook,
                    onClick: this.props.loadWorkbook
                },
                {
                    key: 'saveWorkbook',
                    name: 'Save',
                    icon: 'DownloadDocument',
                    onClick: this.props.saveWorkbook
                }
            ],
            farItems: [
                {
                    key: 'dumpDraftState',
                    icon: 'Rocket',
                    onClick: this.props.dumpDraftState
                }
            ]
        }

        return (
            <CommandBar
                elipisisAriaLabel='More options'
                {... commandBarProps}
            />
        );
    }
}