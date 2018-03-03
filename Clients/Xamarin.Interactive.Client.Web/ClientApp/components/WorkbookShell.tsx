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
import './WorkbookShell.scss'

export interface WorkbookShellContext {
    session: WorkbookSession
    rendererRegistry: ResultRendererRegistry
}

export class WorkbookShell extends React.Component {
    private shellContext: WorkbookShellContext
    private statusBar: StatusBar | null = null

    constructor() {
        super()
        this.shellContext = {
            session: new WorkbookSession(this.statusUIAction),
            rendererRegistry: ResultRendererRegistry.createDefault()
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

    render() {
        return (
            <div className='WorkbookShell-container'>
                <WorkbookCommandBar />
                <WorkbookEditor
                    shellContext={this.shellContext}
                    content=''/>
                <StatusBar
                    ref={(statusBar: StatusBar | null) => this.statusBar = statusBar} />
                {/* <div style={{ display: "none" }}>
                    <input
                        type="file"
                        ref={(input) => { this.fileButton = input; }}
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => this.loadMarkdown(e)} />
                </div> */}
            </div>
        )
    }
}