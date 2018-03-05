//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { HubConnection } from '@aspnet/signalr'
import { Event } from './utils/Events'
import { CodeCellResult, EvaluationResult, CapturedOutputSegment } from './evaluation'
import { Message, StatusUIAction, StatusUIActionHandler, StatusUIActionWithMessage } from './messages'

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

export const enum ClientSessionEventKind {
    /**
     * Will be raised once per subscription indicating that the session is available for use.
    */
    SessionAvailable = 'SessionAvailable',

    /**
     * Will be raised when the session title has changed, such as when a workbook is saved.
     */
    SessionTitleUpdated = 'SessionTitleUpdated',

    /**
     * Can be raised any number of times per subscription, indicating that a connection
     * to the agent associated with the session has been made and that ClientSession.Agent.Api
     * is usable.
     */
    AgentConnected = 'AgentConnected',

    /**
     * Can raised any number of times per subscription, indicating that agent-side features
     * may have changed, such as new available view hierarchies.
     */
    AgentFeaturesUpdated = 'AgentFeaturesUpdated',

    /**
     * Can be raised any number of times per subscription, indicating that a connection
     * to the agent associated with the session has been lost and that ClientSession.Agent.Api
     * is not available.
     */
    AgentDisconnected = 'AgentDisconnected',

    /**
     * Can be raised any number of times per subscription, indicating that the compilation
     * workspace is ready for use. Will always be invoked after <see cref="AgentConnected"/>.
     */
    CompilationWorkspaceAvailable = 'CompilationWorkspaceAvailable'
}

export interface ClientSessionEvent {
    kind: ClientSessionEventKind
}

export class WorkbookSession {
    private hubConnection = new HubConnection('/session')

    readonly clientSessionEvent: Event<WorkbookSession, ClientSessionEvent>
    readonly statusUIActionEvent: Event<WorkbookSession, StatusUIActionWithMessage>
    readonly evaluationEvent: Event<WorkbookSession, CodeCellResult>
    readonly capturedOutputSegmentEvent: Event<WorkbookSession, CapturedOutputSegment>

    private _availableWorkbookTargets: WorkbookTarget[] = []
    get availableWorkbookTargets() {
        return this._availableWorkbookTargets
    }

    constructor() {
        this.clientSessionEvent = new Event(<WorkbookSession>this)
        this.statusUIActionEvent = new Event(<WorkbookSession>this)
        this.evaluationEvent = new Event(<WorkbookSession>this)
        this.capturedOutputSegmentEvent = new Event(<WorkbookSession>this)

        this.hubConnection.on(
            'ClientSessionEvent',
            (e: ClientSessionEvent) => {
                this.clientSessionEvent.dispatch(e)
                console.debug('Hub: ClientSessionEvent: %O', e)
            })

        this.hubConnection.on(
            'StatusUIAction',
            (action: StatusUIAction, message: Message) => {
                console.debug('Hub: StatusUIAction: action: %O, message: %O', action, message)
                this.statusUIActionEvent.dispatch({
                    action: action,
                    message: message
                })
            })

        this.hubConnection.on(
            'EvaluationEvent',
            (codeCellResult: CodeCellResult) => {
                this.evaluationEvent.dispatch(codeCellResult)
                console.debug('Hub: EvaluationEvent: %O', codeCellResult)
            })

        this.hubConnection.on(
            'CapturedOutputSegmentEvent',
            (capturedOutputSegment: CapturedOutputSegment) => {
                this.capturedOutputSegmentEvent.dispatch(capturedOutputSegment)
                console.debug('Hub: CapturedOutputSegmentEvent: %O', capturedOutputSegment)
            })
    }

    async connect(): Promise<void> {
        await this.hubConnection.start()

        this._availableWorkbookTargets = <WorkbookTarget[]>await this.hubConnection.invoke(
            'GetAvailableWorkbookTargets')

        console.log('GetAvailableWorkbookTargets: %O', this.availableWorkbookTargets)

        await this.hubConnection.invoke(
            'OpenSession',
            'xamarin-interactive:///v1?agentType=Console&sessionKind=Workbook')
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

    evaluate(codeCellId: string, evaluateAll: boolean = false): Promise<EvaluationResult> {
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