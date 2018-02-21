//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { CodeCell } from './CodeCell';
import { HubConnection } from '@aspnet/signalr'

export class WorkbookShell extends React.Component {
    private hubConnection = new HubConnection('/session')

    componentDidMount() {
        this.hubConnection.on('send', data => {
            console.log('signalr: %O', data);
        });

        this.hubConnection
            .start()
            .then(() => this.hubConnection.invoke('send', 'Hello from JS'));
    }

    componentWillUnmount() {
        this.hubConnection.stop();
    }

    render() {
        return <CodeCell />
    }
}