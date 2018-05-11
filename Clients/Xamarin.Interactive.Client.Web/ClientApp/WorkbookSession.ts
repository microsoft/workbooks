//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { HubConnection } from '@aspnet/signalr'
import * as catalog from './i18n'
import { Event } from './utils/Events'
import { resolveJsonReferences } from './utils/JsonRefs'
import { CodeCellResult, CapturedOutputSegment, ICodeCellEvent, CodeCellUpdate } from './evaluation'
import { Message, StatusUIAction, StatusUIActionWithMessage, MessageKind, MessageSeverity } from './messages'

export type SdkId = string

export interface Sdk {
    id: SdkId
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
    sdk: Sdk
}

export interface LanguageDescription {
    name: string
    version?: string
}

export interface SessionDescription {
    languageDescription: LanguageDescription,
    targetPlatformIdentifier: SdkId
}

export const enum SessionEventKind {
    Uninitialized = 'Uninitialized',
    WorkbookTargetChanged = 'WorkbookTargetChanged',
    ConnectingToAgent = 'ConnectingToAgent',
    InitializingWorkspace = 'InitializingWorkspace',
    Ready = 'Ready',
    AgentFeaturesUpdated = 'AgentFeaturesUpdated',
    AgentDisconnected = 'AgentDisconnected',
    Evaluation = 'Evaluation'
}

export interface SessionEvent {
    kind: SessionEventKind
    data?: any
}

export interface PackageSource {
    source: string
}

export interface PackageDescription {
    packageId: string
    isExplicitlySelected: boolean
    identityVersion?: string
    versionRange?: string
    source?: PackageSource
}

export class WorkbookSession {
    private hubConnection = new HubConnection('/session')

    readonly sessionEvent: Event<WorkbookSession, SessionEvent>
    readonly statusUIActionEvent: Event<WorkbookSession, StatusUIActionWithMessage>
    readonly codeCellEvent: Event<WorkbookSession, ICodeCellEvent>

    private _availableWorkbookTargets: WorkbookTarget[] = []
    get availableWorkbookTargets() {
        return this._availableWorkbookTargets
    }

    constructor() {
        this.sessionEvent = new Event(<WorkbookSession>this)
        this.statusUIActionEvent = new Event(<WorkbookSession>this)
        this.codeCellEvent = new Event(<WorkbookSession>this)

        this.onSessionEventReceived = this.onSessionEventReceived.bind(this)
        this.onSessionEventsComplete = this.onSessionEventsComplete.bind(this)
    }

    async connect(): Promise<void> {
        await this.hubConnection.start()

        this._availableWorkbookTargets = <WorkbookTarget[]>await this.hubConnection.invoke(
            'GetAvailableWorkbookTargets')

        console.log('GetAvailableWorkbookTargets: %O', this.availableWorkbookTargets)

        this.hubConnection.stream('ObserveSessionEvents')
            .subscribe({
                next: <(event: {}) => void>this.onSessionEventReceived,
                complete: this.onSessionEventsComplete
            })

        if (this.availableWorkbookTargets && this.availableWorkbookTargets.length > 0)
            this.initializeSession(this.availableWorkbookTargets[0].id)
    }

    async initializeSession(description: SessionDescription | SdkId) {
        if (typeof description === 'string')
            description = {
                languageDescription: {
                    name: "csharp"
                },
                targetPlatformIdentifier: description
            }

        this.sessionEvent.dispatch({
            kind: SessionEventKind.WorkbookTargetChanged,
            data: description
        })

        await this.hubConnection.invoke('InitializeSession', description)
    }

    onSessionEventReceived(event: SessionEvent): void {
        event = resolveJsonReferences(event)

        console.log('WorkbookSession::onSessionEventReceived: %O, data: %O', event.kind, event.data)

        this.sessionEvent.dispatch(event)

        let message: StatusUIActionWithMessage = {
            action: StatusUIAction.DisplayMessage,
            message: {
                kind: MessageKind.Status,
                severity: MessageSeverity.Info,
                showSpinner: true
            }
        }

        switch (event.kind) {
            case SessionEventKind.Evaluation:
                this.codeCellEvent.dispatch(<ICodeCellEvent>event.data)
                return
            case SessionEventKind.ConnectingToAgent:
                message.message!.text = catalog.getString('Connecting to agent…')
                break
            case SessionEventKind.InitializingWorkspace:
                message.message!.text = catalog.getString('Initializing workspace…')
                break
            case SessionEventKind.Ready:
                message.action = StatusUIAction.DisplayIdle
                break
            case SessionEventKind.AgentDisconnected:
                message.message!.severity = MessageSeverity.Error
                message.message!.text = catalog.getString('Agent disconnected')
                message.message!.showSpinner = false
                break
            default:
                message.message = undefined
                break
        }

        if (message.message)
            this.statusUIActionEvent.dispatch(message)
    }

    onSessionEventsComplete(): void {
        this.disconnect()
    }

    disconnect(): Promise<void> {
        return this.hubConnection.stop()
    }

    insertCodeCell(buffer: string, relativeToCodeCellId: string | null): Promise<string> {
        return this.hubConnection.invoke('InsertCodeCell', buffer, relativeToCodeCellId, false)
    }

    updateCodeCell(codeCellId: string, buffer: string): Promise<CodeCellUpdate> {
        return this.hubConnection.invoke('UpdateCodeCell', codeCellId, buffer)
    }

    evaluate(codeCellId: string): Promise<void> {
        return this.hubConnection.invoke('Evaluate', codeCellId, false)
    }

    evaluateAll(): Promise<void> {
        return this.hubConnection.invoke('Evaluate', null, true)
    }

    getCompletions(codeCellId: string, position: monaco.Position): Promise<monaco.languages.CompletionItem[]> {
        return this.hubConnection.invoke("GetCompletions", codeCellId, position)
    }

    getHover(codeCellId: string, position: monaco.Position): Promise<monaco.languages.Hover> {
        return this.hubConnection.invoke("GetHover", codeCellId, position)
    }

    getSignatureHelp(codeCellId: string, position: monaco.Position): Promise<monaco.languages.SignatureHelp> {
        return this.hubConnection.invoke("GetSignatureHelp", codeCellId, position)
    }

    installPackages(packages: PackageDescription[]): Promise<PackageDescription[]> {
        return this.hubConnection.invoke("InstallPackages", packages)
    }

    restorePackages(packages: PackageDescription[]): Promise<PackageDescription[]> {
        return this.hubConnection.invoke("RestorePackages", packages)
    }
}