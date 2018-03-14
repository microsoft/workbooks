//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import * as matter from 'gray-matter'
import * as uuidv4 from 'uuid/v4'
import { saveAs } from 'file-saver'
import { loadTheme } from 'office-ui-fabric-react/lib/Styling';

import { osMac } from '../utils'
import { WorkbookSession, SessionEvent, SessionEventKind } from '../WorkbookSession'
import { WorkbookCommandBar } from './WorkbookCommandBar'
import { WorkbookEditor } from './WorkbookEditor'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { PackageSearch } from './PackageSearch'
import { StatusMessageBar } from './StatusMessageBar'
import { StatusUIAction, MessageKind, MessageSeverity } from '../messages'

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
    private workspaceAvailable: boolean = false

    constructor() {
        super()

        this.onDocumentKeyDown = this.onDocumentKeyDown.bind(this)
        this.onSessionEvent = this.onSessionEvent.bind(this)

        this.evaluateWorkbook = this.evaluateWorkbook.bind(this)
        this.showPackageDialog = this.showPackageDialog.bind(this)
        this.triggerFilePicker = this.triggerFilePicker.bind(this)
        this.saveWorkbook = this.saveWorkbook.bind(this)
        this.dumpDraftState = this.dumpDraftState.bind(this)

        this.shellContext = {
            session: new WorkbookSession,
            rendererRegistry: ResultRendererRegistry.createDefault()
        }

        this.state = {
            isPackageDialogHidden: true
        }
    }

    private onSessionEvent(session: WorkbookSession, sessionEvent: SessionEvent) {
        if (sessionEvent.kind === SessionEventKind.Ready) {
            this.workspaceAvailable = true
            if (this.workbookEditor)
                this.workbookEditor.setUpInitialState()
        } else {
            this.workspaceAvailable = false
        }
    }

    async componentDidMount() {
        this.shellContext.session.sessionEvent.addListener(this.onSessionEvent)

        await this.shellContext.session.connect()

        if (this.commandBar)
            this.commandBar.setWorkbookTargets(this.shellContext.session.availableWorkbookTargets)

        document.addEventListener('keydown', this.onDocumentKeyDown)

        loadTheme({})
    }

    componentWillUnmount() {
        this.shellContext.session.sessionEvent.removeListener(this.onSessionEvent)

        document.addEventListener('keydown', this.onDocumentKeyDown)

        this.shellContext.session.disconnect()

        this.commandBar = null
        this.workbookEditor = null
        this.fileButton = null
    }

    private onDocumentKeyDown(e: KeyboardEvent): void {
        if (!(osMac() ? e.metaKey : e.ctrlKey))
            return

        switch (e.key) {
            case 'g':
                e.preventDefault()
                if (this.workspaceAvailable)
                    this.showPackageDialog()
                break
            case 'o':
                e.preventDefault()
                if (this.workspaceAvailable)
                    this.triggerFilePicker()
                break
            case 's':
                e.preventDefault()
                if (this.workspaceAvailable)
                    this.saveWorkbook()
                break
        }
    }

    evaluateWorkbook() {
        this.shellContext.session.evaluateAll()
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

        const file = event.target.files[0]
        const reader = new FileReader
        reader.addEventListener('load', () => this.restorePackages (reader))
        reader.readAsText(file)
    }

    private async restorePackages(reader: FileReader) {
        if (!this.workbookEditor)
            return

        const workbookMetadata = await this.workbookEditor.loadNewContent(reader.result);
        this.workbookMetadata = workbookMetadata;

        if (!workbookMetadata.packages)
            return

        const statusEvent = this.shellContext.session.statusUIActionEvent
        statusEvent.dispatch({
            action: StatusUIAction.DisplayMessage,
            message: {
                kind: MessageKind.Status,
                severity: MessageSeverity.Info,
                text: "Restoring NuGet packages…"
            }
        });

        const packages = workbookMetadata.packages.map((manifestPackage: any) => {
            return {
                packageId: manifestPackage.id,
                versionRange: manifestPackage.version,
                isExplicitlySelected: true
            }
        })

        const restoredPackages = await this.shellContext.session.restorePackages(packages)

        if (this.packageSearchDialog) {
            this.packageSearchDialog.setState({
                installedPackagesIds: this.packageSearchDialog.state.installedPackagesIds.concat(
                    restoredPackages.map(p => p.packageId))
            })
        }

        statusEvent.dispatch({
            action: StatusUIAction.DisplayIdle
        });
    }

    saveWorkbook() {
        if (this.workbookEditor != null) {
            const contentToSave = this.workbookEditor.getContentToSave();
            this.workbookMetadata = this.workbookMetadata || {
                title: "Untitled",
                uti: "com.xamarin.workbook",
                id: uuidv4(),
                platforms: this.shellContext.session.availableWorkbookTargets.map(wt => wt.id)
            };
            const workbook = matter.stringify(contentToSave, this.workbookMetadata, {
                delims: ["---", "---\n"]
            });
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
                    evaluateWorkbook={this.evaluateWorkbook}
                    addPackages={this.showPackageDialog}
                    loadWorkbook={this.triggerFilePicker}
                    saveWorkbook={this.saveWorkbook}
                    dumpDraftState={this.dumpDraftState}
                    shellContext={this.shellContext}/>
                <PackageSearch
                    ref={component => this.packageSearchDialog = component}
                    session={this.shellContext.session}
                    notifyDismiss={() => this.hidePackageDialog()}
                    getIsHidden={() => this.state.isPackageDialogHidden}/>
                <WorkbookEditor
                    shellContext={this.shellContext}
                    ref={(editor) => this.workbookEditor = editor }
                    content=''/>
                <StatusMessageBar shellContext={this.shellContext}/>
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