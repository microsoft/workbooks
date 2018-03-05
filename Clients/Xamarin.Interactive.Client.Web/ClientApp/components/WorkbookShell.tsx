//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import * as matter from 'gray-matter';
import { saveAs } from 'file-saver'

import { WorkbookSession, ClientSessionEvent, ClientSessionEventKind } from '../WorkbookSession'
import { WorkbookCommandBar } from './WorkbookCommandBar'
import { WorkbookEditor } from './WorkbookEditor'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { PackageSearch } from './PackageSearch';
import { StatusMessageBar } from './StatusMessageBar';
import { StatusUIActionWithMessage, StatusUIAction, MessageKind, MessageSeverity } from '../messages'

import './WorkbookShell.scss'

export interface WorkbookShellContext {
    session: WorkbookSession
    rendererRegistry: ResultRendererRegistry
}

interface WorkbookShellState {
    isPackageDialogHidden: boolean
}

export class WorkbookShell extends React.Component<any, WorkbookShellState> {
    private shellContext: WorkbookShellContext
    private commandBar: WorkbookCommandBar | null = null
    private workbookEditor: WorkbookEditor | null = null
    private fileButton: HTMLInputElement | null = null
    private packageSearchDialog: PackageSearch | null = null
    private workbookMetadata: any

    private initialStatusMessageBarActionMessages: StatusUIActionWithMessage[] = []
    private statusMessageBarComponent: StatusMessageBar | null = null

    constructor() {
        super()

        this.shellContext = {
            session: new WorkbookSession,
            rendererRegistry: ResultRendererRegistry.createDefault()
        }

        this.state = {
            isPackageDialogHidden: true
        }
    }

    private onStatusUIAction(session: WorkbookSession, actionMessage: StatusUIActionWithMessage) {
        if (this.statusMessageBarComponent) {
            this.initialStatusMessageBarActionMessages = []
            this.statusMessageBarComponent.onStatusUIAction(actionMessage)
        } else {
            this.initialStatusMessageBarActionMessages.push(actionMessage)
        }
    }

    private onClientSessionEvent(session: WorkbookSession, clientSessionEvent: ClientSessionEvent) {
        if (clientSessionEvent.kind === ClientSessionEventKind.CompilationWorkspaceAvailable) {
            if (this.workbookEditor)
                this.workbookEditor.appendNewCodeCell()
        }
    }

    async componentDidMount() {
        this.shellContext.session.statusUIActionEvent.addListener(this.onStatusUIAction.bind(this))
        this.shellContext.session.clientSessionEvent.addListener(this.onClientSessionEvent.bind(this))

        await this.shellContext.session.connect()

        if (this.commandBar)
            this.commandBar.setWorkbookTargets(this.shellContext.session.availableWorkbookTargets)
    }

    componentWillUnmount() {
        this.shellContext.session.statusUIActionEvent.removeListener(this.onStatusUIAction)
        this.shellContext.session.clientSessionEvent.removeListener(this.onClientSessionEvent)

        this.shellContext.session.disconnect()

        this.commandBar = null
        this.workbookEditor = null
        this.fileButton = null
        this.statusMessageBarComponent = null
    }

    showPackageDialog() {
        this.setState({ isPackageDialogHidden: false })
    }

    hidePackageDialog() {
        this.setState({ isPackageDialogHidden: true })
    }

    triggerFilePicker() {
        if (this.fileButton == null)
            return;
        this.fileButton.click();
    }

    loadWorkbook(event: React.ChangeEvent<HTMLInputElement>) {
        if (event.target.files == null) {
            alert("No files.");
            return;
        }

        const file = event.target.files[0];
        const reader = new FileReader();
        reader.addEventListener("load", async () => {
            if (this.workbookEditor != null) {
                const workbookMetadata = await this.workbookEditor.loadNewContent(reader.result);
                this.workbookMetadata = workbookMetadata;
                if (workbookMetadata.packages) {
                    this.onStatusUIAction(this.shellContext.session, {
                        action: StatusUIAction.DisplayMessage,
                        message: {
                            id: 1001,
                            kind: MessageKind.Status,
                            severity: MessageSeverity.Info,
                            text: "Installing NuGet packages",
                            showSpinner: true,
                            detailedText: null
                        }
                    });
                    for (const nuget of workbookMetadata.packages) {
                        const { id, version } = nuget;
                        this.onStatusUIAction(this.shellContext.session, {
                            action: StatusUIAction.DisplayMessage,
                            message: {
                                id: 1002,
                                kind: MessageKind.Status,
                                severity: MessageSeverity.Info,
                                text: `Installing ${id} v${version}`,
                                detailedText: null,
                                showSpinner: true
                            }
                        });
                        await this.shellContext.session.installPackage(id, version);
                        this.onStatusUIAction(this.shellContext.session, {
                            action: StatusUIAction.DisplayMessage,
                            message: {
                                id: 1003,
                                kind: MessageKind.Status,
                                severity: MessageSeverity.Info,
                                text: `Installed ${id} v${version}`,
                                detailedText: null,
                                showSpinner: true
                            }
                        });
                    }
                    this.onStatusUIAction(this.shellContext.session, {
                        action: StatusUIAction.DisplayMessage,
                        message: {
                            id: 1004,
                            kind: MessageKind.Status,
                            severity: MessageSeverity.Info,
                            text: "Installed NuGet packages",
                            showSpinner: false,
                            detailedText: null
                        }
                    });
                    if (this.packageSearchDialog) {
                        this.packageSearchDialog.setState({
                            installedPackagesIds: this.packageSearchDialog.state.installedPackagesIds.concat(
                                workbookMetadata.packages.map((p: any) => p.id))
                        })
                    }
                    setTimeout(() => {
                        this.onStatusUIAction(this.shellContext.session, {
                            action: StatusUIAction.DisplayIdle
                        });
                    }, 2000);
                }
            }
        });
        reader.readAsText(file);
    }

    generateUuid() {
        return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
            (+c ^ (crypto.getRandomValues(new Uint8Array(1)) as Uint8Array)[0] & 15 >> +c / 4).toString(16)
        )
    }

    saveWorkbook() {
        if (this.workbookEditor != null) {
            const contentToSave = this.workbookEditor.getContentToSave();
            this.workbookMetadata = this.workbookMetadata || {
                title: "Untitled",
                uti: "com.xamarin.workbook",
                id: this.generateUuid(),
                platforms: this.shellContext.session.availableWorkbookTargets.map(wt => wt.id)
            };
            const workbook = matter.stringify(contentToSave, this.workbookMetadata);
            var blob = new Blob([workbook], { type: "text/markdown;charset=utf-8" })
            saveAs(blob, `${this.workbookMetadata.title}.workbook`);
        }
    }

    dumpDraftState() {
        if (this.workbookEditor != null) {
            this.workbookEditor.logContent();
        }
    }

    render() {
        return (
            <div className='WorkbookShell-container'>
                <WorkbookCommandBar
                    ref={component => this.commandBar = component}
                    addPackages={this.showPackageDialog.bind(this)}
                    loadWorkbook={this.triggerFilePicker.bind(this)}
                    saveWorkbook={this.saveWorkbook.bind(this)}
                    dumpDraftState={this.dumpDraftState.bind(this)}
                    shellContext={this.shellContext}
                />
                <StatusMessageBar
                    ref={component => this.statusMessageBarComponent = component}
                    initialActionMessages={this.initialStatusMessageBarActionMessages} />
                <PackageSearch
                    ref={component => this.packageSearchDialog = component}
                    session={this.shellContext.session}
                    notifyDismiss={() => this.hidePackageDialog()}
                    getIsHidden={() => this.state.isPackageDialogHidden}
                />
                <WorkbookEditor
                    shellContext={this.shellContext}
                    ref={(editor) => this.workbookEditor = editor }
                    content=''/>
                <div style={{ display: "none" }}>
                    <input
                        type="file"
                        ref={(input) => { this.fileButton = input; }}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => this.loadWorkbook(e)} />
                </div>
            </div>
        )
    }
}