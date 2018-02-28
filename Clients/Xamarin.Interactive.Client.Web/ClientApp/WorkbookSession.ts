//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { HubConnection } from '@aspnet/signalr'
import { Event } from './utils/Events'
import { CodeCellResult } from './evaluation'

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
    evaluationEvent: Event<WorkbookSession, CodeCellResult>

    constructor(statusUIActionHandler: (action: StatusUIAction, message: StatusMessage | null) => void) {
        this.evaluationEvent = new Event(<WorkbookSession>this)
        this.hubConnection.on('StatusUIAction', statusUIActionHandler)
        this.hubConnection.on(
            'EvaluationEvent',
            (e: any) => this.evaluationEvent.dispatch(<CodeCellResult>e));
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

    provideCompletions(codeCellId: string, lineNumber: number, column: number): Promise<monaco.languages.CompletionItem[]> {
        return this.hubConnection.invoke("ProvideCompletions", codeCellId, lineNumber, column)
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