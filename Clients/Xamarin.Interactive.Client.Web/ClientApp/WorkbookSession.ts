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

export interface CodeCellUpdateResponse {
    isSubmissionComplete: boolean
    diagnostics: monaco.editor.IModelDecoration[]
}

export interface DotNetSdk {
    name: string
    profile: string
    targetFramework: string
    version: string
}

export interface WorkbookTarget {
    id: string
    flavor: string
    icon: string
    optionalFeatures: string[]
    sdk: DotNetSdk
}

export class WorkbookSession {
    private hubConnection = new HubConnection('/session')

    private _availableWorkbookTargets: WorkbookTarget[] = []
    get availableWorkbookTargets() {
        return this._availableWorkbookTargets
    }

    private _evaluationEvent: Event<WorkbookSession, CodeCellResult>
    get evaluationEvent() {
        return this._evaluationEvent
    }

    constructor(statusUIActionHandler: (action: StatusUIAction, message: StatusMessage | null) => void) {
        this._evaluationEvent = new Event(<WorkbookSession>this)
        this.hubConnection.on('StatusUIAction', statusUIActionHandler)
        this.hubConnection.on(
            'EvaluationEvent',
            (e: any) => this.evaluationEvent.dispatch(<CodeCellResult>e));
    }

    async connect(): Promise<void> {
        await this.hubConnection.start()

        this._availableWorkbookTargets = <WorkbookTarget[]>await this.hubConnection.invoke(
            'GetAvailableWorkbookTargets')

        console.log('GetAvailableWorkbookTargets: %O', this.availableWorkbookTargets)

        await this.hubConnection.invoke(
            'OpenSession',
            'xamarin-interactive:///v1?agentType=DotNetCore&sessionKind=Workbook')
    }

    disconnect(): Promise<void> {
        return this.hubConnection.stop()
    }

    insertCodeCell(buffer: string, relativeToCodeCellId: string | null): Promise<string> {
        return this.hubConnection.invoke('InsertCodeCell', buffer, relativeToCodeCellId, false)
    }

    updateCodeCell(codeCellId: string, buffer: string): Promise<CodeCellUpdateResponse> {
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

    installPackage(packageId: string, version: string): Promise<string[]> {
        return this.hubConnection.invoke("InstallPackage", packageId, version)
    }
}