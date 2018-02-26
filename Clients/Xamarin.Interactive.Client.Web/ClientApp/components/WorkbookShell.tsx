//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { WorkbookSession, StatusUIAction, StatusMessage } from '../WorkbookSession'
import { WorkbookEditor } from './WorkbookEditor'
import { StatusBar } from './StatusBar'

export class WorkbookShell extends React.Component {
    private session: WorkbookSession
    private statusBar: StatusBar | null = null

    constructor() {
        super()
        this.session = new WorkbookSession(this.statusUIAction)
    }

    private statusUIAction(action: StatusUIAction, message: StatusMessage | null) {
        if (this.statusBar)
            this.statusBar.update(action, message)
    }

    componentDidMount() {
        console.log("connect!")
        this.session.connect()
    }

    componentWillUnmount() {
        this.session.disconnect()
    }

    render() {
        return (
            <div>
                <WorkbookEditor
                    session={this.session}
                    content=''/>
                <StatusBar
                    ref={(statusBar: StatusBar | null) => this.statusBar = statusBar}/>
            </div>
        )
    }
}