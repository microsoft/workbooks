//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { HubConnection } from '@aspnet/signalr'

export const enum StatusUIAction {
    None,
    DisplayIdle,
    DisplayMessage,
    StartSpinner,
    StopSpinner
}

export interface StatusMessage {
    text: string | null
}

export class WorkbookSession {
    private hubConnection = new HubConnection('/session')

    constructor(statusUIActionHandler: (action: StatusUIAction, message: StatusMessage | null) => void) {
        this.hubConnection.on('StatusUIAction', statusUIActionHandler)
        this.hubConnection.on('EvaluationEvent', (e: any) => {
            console.log('got result: %O', e);
        });
    }

    onCodeCellEvent() {
    }

    insertCodeCell(): Promise<string> {
        return this.hubConnection.invoke('InsertCodeCell', '', null, false)
    }

    updateCodeCell(codeCellId: string, buffer: string): Promise<void> {
        return this.hubConnection.invoke('UpdateCodeCell', codeCellId, buffer)
    }

    evaluate(codeCellId: string, evaluateAll: boolean = false) {
        return this.hubConnection.invoke('Evaluate', codeCellId, evaluateAll)
    }

    connect() {
        this.hubConnection
            .start()
            .then(() => this.hubConnection.invoke(
                'OpenSession',
                'xamarin-interactive:///v1?agentType=DotNetCore&sessionKind=Workbook'))
    }

    disconnect() {
        this.hubConnection.stop()
    }
}