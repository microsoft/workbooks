//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { CodeCellEventType, ICodeCellEvent } from "./evaluation";
import * as catalog from './i18n'
import { StatusUIAction, MessageKind, MessageSeverity } from "./messages";
import { WorkbookSession } from "./WorkbookSession";

export class WebAssemblyAgentContainer
{
    private disposed: boolean = false
    private session: WorkbookSession
    private queuedWasmMessages: any[] = []

    private _wasmFrameLoaded: boolean = false
    get wasmFrameLoaded(): boolean {
        return this._wasmFrameLoaded
    }

    readonly wasmFrame: HTMLIFrameElement
    readonly wasmMessageChannel: MessageChannel

    /**
     * Creates a new WebAssembly agent container, pre-generating the WebAssembly
     * iframe and opening the message channel. The _caller_ must actually add
     * the iframe (see {@link wasmFrame}) to the body of the page to begin the loading
     * process.
     *
     * @param session The workbook session that is controlling the WebAssembly agent
     */
    constructor(session: WorkbookSession) {
        const wasmFrame = document.createElement("iframe")
        wasmFrame.sandbox.add("allow-scripts", "allow-same-origin")
        wasmFrame.src = "/Home/Wasm"
        wasmFrame.style.display = "none";
        wasmFrame.addEventListener("load", this.executeQueuedWasmMessages.bind(this))

        const wasmMessageChannel = new MessageChannel
        wasmMessageChannel.port1.onmessage = this.dispatchWasmMessage.bind(this)

        this.wasmFrame = wasmFrame
        this.wasmMessageChannel = wasmMessageChannel
        this.session = session
    }

    /**
     * Tears down the WebAssembly agent by removing the iframe that houses it and
     * closing the message channel. Any calls to {@link pushWasmMessage} will be
     * ignored after this method is called, as there will be nothing to receive them.
     */
    dispose() {
        this.disposed = true
        this.wasmFrame.remove()
        this.wasmMessageChannel.port1.close()
    }

    /**
     * This is a callback called by the `load` event on {@link _wasmFrame}. It pushes
     * some events to the upstream session controlling us, actually sets up
     * the message channel by passing port 2 over to the iframe, and pushes any
     * queued messages over to the frame so it can execute them when it's done loading.
     *
     * @callback
     */
    private executeQueuedWasmMessages(): void {
        this._wasmFrameLoaded = true
        this.session.statusUIActionEvent.dispatch({
            action: StatusUIAction.DisplayMessage,
            message: {
                kind: MessageKind.Status,
                severity: MessageSeverity.Info,
                text: catalog.getString("Initializing WebAssembly agent…"),
                showSpinner: true
            }
        });

        if (!this.wasmFrame.contentWindow) {
            console.error("Tried to execute queued WebAssembly messages after frame load, but the frame is not there.")
            return
        }

        this.wasmFrame.contentWindow.postMessage(
            { $type: "ChannelOpenMessage" },
            location.origin,
            [ this.wasmMessageChannel.port2 ]);
        this.queuedWasmMessages.forEach(msg => this.wasmMessageChannel.port1.postMessage(msg))
    }

    /**
     * Dispatches messages sent by the WebAssembly agent.
     * @param ev The message event from the {@link MessageChannel} we opened for communications.
     */
    private dispatchWasmMessage(ev: MessageEvent): void {
        // Ignore any stragglers from the WebAssembly agent that come in after we've been torn down.
        if (this.disposed)
            return;

        const { data } = ev
        const { $type } = data

        switch ($type) {
            case CodeCellEventType.CapturedOutputSegment:
            case CodeCellEventType.Evaluation:
                this.session.codeCellEvent.dispatch(<ICodeCellEvent>data)
                if ($type === CodeCellEventType.Evaluation) {
                    const { status, codeCellId } = data
                    this.session.notifyEvaluationComplete(codeCellId, status)
                }
                break
            case "LoadingMessage":
                this.session.statusUIActionEvent.dispatch({
                    action: StatusUIAction.DisplayMessage,
                    message: {
                        kind: MessageKind.Status,
                        severity: MessageSeverity.Info,
                        text: catalog.getString("Loading WebAssembly agent…"),
                    }
                })
                break
            case "ErrorMessage":
                this.session.statusUIActionEvent.dispatch({
                    action: StatusUIAction.DisplayMessage,
                    message: {
                        kind: MessageKind.Alert,
                        severity: MessageSeverity.Error,
                        text: catalog.getString("Error loading WebAssembly agent…"),
                        detailedText: (<Error>data.error).message
                    }
                })
                break
            case "ReadyMessage":
                this.session.statusUIActionEvent.dispatch({
                    action: StatusUIAction.DisplayIdle
                })
                break
            default:
                console.warn(`Don't know what to do with WebAssembly message with type ${ev.data.$type}.`)
                break
        }
    }

    /**
     * Pushes a message to the WebAssembly agent. Can be called even before the agent is ready,
     * as it will appropriately queue messages received until the WebAssembly frame has loaded.
     * @param message The message to push to the agent.
     */
    pushWasmMessage(message: any): void {
        // Ignore any stragglers that come in to be sent to WebAssembly after we've been torn down.
        if (this.disposed)
            return

        if (!this._wasmFrameLoaded)
            this.queuedWasmMessages.push(message)
        else
            this.wasmMessageChannel.port1.postMessage(message)
    }
}