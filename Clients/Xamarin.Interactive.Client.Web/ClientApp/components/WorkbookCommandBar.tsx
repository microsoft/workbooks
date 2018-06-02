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
    Sdk,
    WorkbookSession,
    SessionEvent,
    SessionEventKind,
    SdkId,
    SessionDescription
} from '../WorkbookSession'

import { WorkbookShellContext } from './WorkbookShell';

interface WorkbookCommandBarProps {
    shellContext: WorkbookShellContext
    evaluateWorkbook: () => void
    addPackages: () => void
    loadWorkbook: () => void
    saveWorkbook: () => void
    dumpDraftState: () => void
    loadGist: () => void
}

interface WorkbookCommandBarState {
    canOpenWorkbook: boolean
    workbookTargetItem: IContextualMenuItem
}

const noSelectedWorkbookTargetItem = {
    key: 'selectWorkbookTarget',
    name: 'Select Target Platform',
    icon: 'CSharpLanguage',
}

export class WorkbookCommandBar extends React.Component<WorkbookCommandBarProps, WorkbookCommandBarState> {
    constructor(props: WorkbookCommandBarProps) {
        super(props)

        this.onSessionEvent = this.onSessionEvent.bind(this)

        this.state = {
            canOpenWorkbook: false,
            workbookTargetItem: noSelectedWorkbookTargetItem
        }
    }

    private onSessionEvent(session: WorkbookSession, sessionEvent: SessionEvent) {
        this.setState({ canOpenWorkbook: sessionEvent.kind === SessionEventKind.Ready })

        switch (sessionEvent.kind) {
            case SessionEventKind.Ready:
                this.setState({canOpenWorkbook: true})
                break;
            case SessionEventKind.WorkbookTargetChanged:
                this.selectWorkbookTarget(sessionEvent.data)
                break;
        }
    }

    componentDidMount() {
        this.props.shellContext.session.sessionEvent.addListener(this.onSessionEvent)
    }

    componentWillUnmount() {
        this.props.shellContext.session.sessionEvent.removeListener(this.onSessionEvent)
    }

    private selectWorkbookTarget(selectedTarget: SessionDescription) {
        function createWorkbookTargetItem(target: WorkbookTarget): IContextualMenuItem {
            return {
                key: target.id,
                name: target.sdk.name + (target.sdk.version ? ` ${target.sdk.version}` : '')
            }
        }

        let workbookTargetItems: IContextualMenuItem[] = []
        let selectedWorkbookTargetItem: IContextualMenuItem | null = null

        for (const target of this.props.shellContext.session.availableWorkbookTargets) {
            let item = createWorkbookTargetItem(target)

            if (target.id === selectedTarget.targetPlatformIdentifier) {
                selectedWorkbookTargetItem = createWorkbookTargetItem(target)
                selectedWorkbookTargetItem.key += '-selected'
                selectedWorkbookTargetItem.icon = 'CSharpLanguage'

                item.icon = 'CheckMark'
            } else {
                item.onClick = (ev, clickedItem) => {
                    if (clickedItem)
                        this.props.shellContext.session.initializeSession(clickedItem.key)
                }
            }

            workbookTargetItems.push(item)
        }

        if (!selectedWorkbookTargetItem)
            selectedWorkbookTargetItem = noSelectedWorkbookTargetItem

        selectedWorkbookTargetItem.subMenuProps = {
            items: workbookTargetItems
        }

        this.setState({
            workbookTargetItem: selectedWorkbookTargetItem
        })
    }

    render() {
        const commandBarProps = {
            items: [
                this.state.workbookTargetItem,
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
                    key: 'openGist',
                    name: 'Open Gist',
                    icon: 'OpenSource',
                    disabled: !this.state.canOpenWorkbook,
                    onClick: this.props.loadGist
                },
                {
                    key: 'saveWorkbook',
                    name: 'Save',
                    icon: 'DownloadDocument',
                    onClick: this.props.saveWorkbook
                }
            ],
            // farItems: [
            //     {
            //         key: 'dumpDraftState',
            //         icon: 'Rocket',
            //         onClick: this.props.dumpDraftState
            //     }
            // ]
        }

        return (
            <CommandBar
                elipisisAriaLabel='More options'
                {... commandBarProps}
            />
        );
    }
}