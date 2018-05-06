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
import { WorkbookSession, SessionEvent, SessionEventKind, SdkId } from '../WorkbookSession'
import { WorkbookCommandBar } from './WorkbookCommandBar'
import { WorkbookEditor } from './WorkbookEditor'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { PackageSearch } from './PackageSearch'
import { StatusMessageBar } from './StatusMessageBar'
import { StatusUIAction, MessageKind, MessageSeverity } from '../messages'

import './WorkbookShell.scss'
import { loadWorkbookFromString, loadWorkbookFromWorkbookPackage, loadWorkbookFromGist, Workbook } from '../Workbook';

export interface WorkbookShellContext {
    session: WorkbookSession
    rendererRegistry: ResultRendererRegistry
}

interface WorkbookShellState {
    isPackageDialogHidden: boolean
}

export class WorkbookShell extends React.Component<any, WorkbookShellState> {
    private readonly commandBar: React.RefObject<WorkbookCommandBar> = React.createRef<WorkbookCommandBar>()
    private readonly workbookEditor: React.RefObject<WorkbookEditor> = React.createRef<WorkbookEditor>()
    private readonly fileButton: React.RefObject<HTMLInputElement> = React.createRef<HTMLInputElement>()
    private readonly packageSearchDialog: React.RefObject<PackageSearch> = React.createRef<PackageSearch>()

    private shellContext: WorkbookShellContext
    private workbook: Workbook | null = null
    private workspaceAvailable: boolean = false

    constructor(props: any) {
        super(props)

        this.onDocumentKeyDown = this.onDocumentKeyDown.bind(this)
        this.onSessionEvent = this.onSessionEvent.bind(this)

        this.evaluateWorkbook = this.evaluateWorkbook.bind(this)
        this.showPackageDialog = this.showPackageDialog.bind(this)
        this.triggerFilePicker = this.triggerFilePicker.bind(this)
        this.saveWorkbook = this.saveWorkbook.bind(this)
        this.dumpDraftState = this.dumpDraftState.bind(this)
        this.triggerGistPicker = this.triggerGistPicker.bind(this)

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
            if (this.workbookEditor.current)
                this.workbookEditor.current.setUpInitialState()
        } else {
            this.workspaceAvailable = false
        }
    }

    async componentDidMount() {
        this.shellContext.session.sessionEvent.addListener(this.onSessionEvent)

        await this.shellContext.session.connect()

        document.addEventListener('keydown', this.onDocumentKeyDown)

        loadTheme({})
    }

    componentWillUnmount() {
        this.shellContext.session.sessionEvent.removeListener(this.onSessionEvent)

        document.addEventListener('keydown', this.onDocumentKeyDown)

        this.shellContext.session.disconnect()
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
        if (this.fileButton.current)
            this.fileButton.current.click();
    }

    async triggerGistPicker() {
        if (!this.workbookEditor.current)
            return;

        // TODO: Real UI.
        const gistUrl = prompt("Enter the URL of the Gist you want to load from?");
        if (!gistUrl)
            return;

        this.workbook = await loadWorkbookFromGist(this.shellContext.session, gistUrl);
        await this.workbookEditor.current.loadNewContent(this.workbook.markdownContent)
        await this.restoreNuGetPackages()
    }

    workbookFileChosen(event: React.ChangeEvent<HTMLInputElement>) {
        if (event.target.files == null) {
            alert("No files.");
            return;
        }

        const file = event.target.files[0]
        const reader = new FileReader
        reader.addEventListener('load', () => this.loadWorkbook(file, reader))
        reader.readAsArrayBuffer(file)
    }

    sniffHeader(file: ArrayBuffer): Boolean {
        if (file.byteLength < 4)
            return false;

        const magic = new Uint32Array(file.slice(0, 4))[0]

        switch (magic) {
            case 0x04034B50:
            case 0x06054B50:
            case 0x08074B50:
                return true;
            default:
                return false;
        }
    }

    async loadWorkbook(file: File, reader: FileReader) {
        if (!this.workbookEditor.current)
            return

        if (file.type === "application/zip" || this.sniffHeader(reader.result)) {
            this.workbook = await loadWorkbookFromWorkbookPackage(this.shellContext.session, file)
        } else {
            const workbookString = new TextDecoder("utf-8").decode(reader.result)
            this.workbook = await loadWorkbookFromString(this.shellContext.session, file.name, workbookString)
        }

        await this.workbookEditor.current.loadNewContent(this.workbook.markdownContent)
        await this.restoreNuGetPackages()
    }

    async restoreNuGetPackages() {
        if (!this.workbook || !this.workbook.manifest.packages || this.workbook.manifest.packages.count() == 0)
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

        const restoredPackages = await this.shellContext.session.restorePackages(this.workbook.manifest.packages.toArray())
        if (this.packageSearchDialog.current) {
            this.packageSearchDialog.current.setState({
                installedPackagesIds: this.packageSearchDialog.current.state.installedPackagesIds.concat(
                    restoredPackages.map(p => p.packageId))
            })
        }

        statusEvent.dispatch({
            action: StatusUIAction.DisplayIdle
        });
    }

    saveWorkbook() {
        if (this.workbookEditor.current != null && this.workbook != null) {
            const contentToSave = this.workbookEditor.current.getContentToSave()
            const saveableManifest = this.workbook.getManifestToSave()
            const workbook = matter.stringify(contentToSave, saveableManifest, {
                delims: ["---", "---\n"]
            })
            var blob = new Blob([workbook], { type: "text/markdown;charset=utf-8" })
            saveAs(blob, `${this.workbook.manifest.title}.workbook`)
        }
    }

    dumpDraftState() {
        if (this.workbookEditor.current != null) {
            this.workbookEditor.current.logContent();
        }
    }

    render() {
        return (
            <div className='WorkbookShell-container'>
                <WorkbookCommandBar
                    ref={this.commandBar}
                    evaluateWorkbook={this.evaluateWorkbook}
                    addPackages={this.showPackageDialog}
                    loadWorkbook={this.triggerFilePicker}
                    saveWorkbook={this.saveWorkbook}
                    dumpDraftState={this.dumpDraftState}
                    shellContext={this.shellContext}
                    loadGist={this.triggerGistPicker}/>
                <PackageSearch
                    ref={this.packageSearchDialog}
                    session={this.shellContext.session}
                    notifyDismiss={() => this.hidePackageDialog()}
                    getIsHidden={() => this.state.isPackageDialogHidden}/>
                <WorkbookEditor
                    shellContext={this.shellContext}
                    ref={this.workbookEditor}
                    content=''/>
                <StatusMessageBar shellContext={this.shellContext}/>
                <div style={{ display: "none" }}>
                    <input
                        type="file"
                        ref={this.fileButton}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => this.workbookFileChosen(e)} />
                </div>
            </div>
        )
    }
}