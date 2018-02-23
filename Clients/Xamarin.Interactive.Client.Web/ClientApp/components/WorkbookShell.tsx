//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { WorkbookEditor } from './WorkbookEditor';
import { HubConnection } from '@aspnet/signalr'
import { StatusBar } from './StatusBar';

export class WorkbookShell extends React.Component {
    private hubConnection = new HubConnection('/session');
    private statusBar: StatusBar | null = null;

    componentDidMount() {
        this.hubConnection.on('StatusUIAction', (action, message) => {
            if (this.statusBar)
                this.statusBar.update(action, message);
        });

        this.hubConnection
            .start()
            .then(() => this.hubConnection.invoke(
                'OpenSession',
                'xamarin-interactive:///v1?agentType=DotNetCore&sessionKind=Workbook'));
    }

    componentWillUnmount() {
        this.hubConnection.stop();
    }

    render() {
        return (
            <div>
                <WorkbookEditor content="" />
                <StatusBar ref={(statusBar : StatusBar | null) => this.statusBar = statusBar}/>
            </div>
        );
    }
}