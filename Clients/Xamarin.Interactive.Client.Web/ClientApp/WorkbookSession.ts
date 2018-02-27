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

export interface CodeCellStatus
{
    isSubmissionComplete: boolean
    diagnostics: monaco.editor.IModelDecoration[]
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

    insertCodeCell(relativeToCodeCellId?: string): Promise<string> {
        return this.hubConnection.invoke('InsertCodeCell', '', relativeToCodeCellId || null, false)
    }

    updateCodeCell(codeCellId: string, buffer: string): Promise<CodeCellStatus> {
        return this.hubConnection.invoke('UpdateCodeCell', codeCellId, buffer)
    }

    evaluate(codeCellId: string, evaluateAll: boolean = false) {
        return this.hubConnection.invoke('Evaluate', codeCellId, evaluateAll)
    }

    provideCompletions(codeCellId: string, lineNumber: number, column: number): Promise<monaco.languages.CompletionItem[]> {
        return this.hubConnection.invoke("ProvideCompletions", codeCellId, lineNumber, column)
    }

    provideHover(codeCellId: string, lineNumber: number, column: number): Promise<monaco.languages.Hover> {
        return this.hubConnection.invoke("ProvideHover", codeCellId, lineNumber, column)
    }

    provideSignatureHelp(codeCellId: string, lineNumber: number, column: number): Promise<monaco.languages.SignatureHelp> {
        return this.hubConnection.invoke("ProvideSignatureHelp", codeCellId, lineNumber, column)
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