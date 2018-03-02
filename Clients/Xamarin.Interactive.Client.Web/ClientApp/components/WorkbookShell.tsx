//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { WorkbookSession, StatusUIAction, StatusMessage } from '../WorkbookSession'
import { WorkbookCommandBar } from './WorkbookCommandBar'
import { WorkbookEditor } from './WorkbookEditor'
import { StatusBar } from './StatusBar'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { PackageSearch } from './PackageSearch';
import './WorkbookShell.scss'
import { saveAs } from 'file-saver'
import * as matter from 'gray-matter';

export interface WorkbookShellContext {
    session: WorkbookSession
    rendererRegistry: ResultRendererRegistry
}

interface WorkbookShellState {
    isPackageDialogHidden: boolean
}

export class WorkbookShell extends React.Component<any, WorkbookShellState> {
    private shellContext: WorkbookShellContext
    private statusBar: StatusBar | null = null
    private workbookEditor: WorkbookEditor | null = null
    private fileButton: HTMLInputElement | null = null
    private workbookMetadata: any

    constructor() {
        super()
        this.shellContext = {
            session: new WorkbookSession(this.statusUIAction),
            rendererRegistry: ResultRendererRegistry.createDefault()
        }
        this.state = {
            isPackageDialogHidden: true
        }
    }

    private statusUIAction(action: StatusUIAction, message: StatusMessage | null) {
        if (this.statusBar)
            this.statusBar.update(action, message)
    }

    componentDidMount() {
        this.shellContext.session.connect()
    }

    componentWillUnmount() {
        this.shellContext.session.disconnect()
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
        reader.addEventListener("load", () => {
            if (this.workbookEditor != null)
                this.workbookEditor.loadNewContent(reader.result).then(workbookMetadata => {
                    this.workbookMetadata = workbookMetadata;
                });
        });
        reader.readAsText(file);
    }

    saveWorkbook() {
        if (this.workbookEditor != null) {
            const contentToSave = this.workbookEditor.getContentToSave();
            const workbook = matter.stringify(contentToSave, this.workbookMetadata);
            var blob = new Blob([workbook], { type: "text/markdown;charset=utf-8" })
            const title = (this.workbookMetadata && this.workbookMetadata.title)
                ? this.workbookMetadata.title
                : `workbook-${new Date().toISOString().replace(/[:\.]/g, '-')}`
            saveAs(blob, `${title}.workbook`);
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
                    addPackages={this.showPackageDialog.bind(this)}
                    loadWorkbook={this.triggerFilePicker.bind(this)}
                    saveWorkbook={this.saveWorkbook.bind(this)}
                    dumpDraftState={this.dumpDraftState.bind(this)}
                />
                <PackageSearch
                    session={this.shellContext.session}
                    notifyDismiss={() => this.hidePackageDialog()}
                    getIsHidden={() => this.state.isPackageDialogHidden}
                />
                <WorkbookEditor
                    shellContext={this.shellContext}
                    ref={(editor) => this.workbookEditor = editor }
                    content=''/>
                <StatusBar
                    ref={(statusBar: StatusBar | null) => this.statusBar = statusBar} />
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