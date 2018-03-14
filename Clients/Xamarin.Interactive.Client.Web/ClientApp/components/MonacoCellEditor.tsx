//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// <reference path="../../node_modules/monaco-editor/monaco.d.ts" />

import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { osMac } from '../utils'
import { SelectionState } from 'draft-js'
import { EditorMessage, EditorMessageType, EditorKeys } from '../utils/EditorMessages'
import { CodeCellUpdate, DiagnosticSeverity } from '../evaluation'

import './MonacoCellEditor.scss'

export interface MonacoCellEditorProps {
    blockProps: {
        codeCellFocused: (currentKey: string) => void,
        codeCellBlurred: (currentKey: string) => void,
        subscribeToEditor: (callback: (message: EditorMessage) => void) => void,
        selectNext: (currentKey: string) => boolean,
        selectPrevious: (currentKey: string) => boolean,
        updateTextContentOfBlock: (blockKey: string, textContent: string) => void,
        setSelection: (anchorKey: string, offset: number) => void,
        setModelId: (modelId: string) => void,
        updateCodeCell(buffer: string): Promise<CodeCellUpdate | null>,
        evaluate: () => void
    }
    block: {
        key: string
        text: string
    }
}
interface MonacoCellEditorState {
    created: boolean
}

enum ViewEventType {
    ViewLinesDeleted = 8,
    ViewLinesInserted = 9
}

export class MonacoCellEditor extends React.Component<MonacoCellEditorProps, MonacoCellEditorState> {
    private windowResizeHandler: any;
    private editor?: monaco.editor.ICodeEditor;
    private lastUpdateResponse: CodeCellUpdate | null = null
    private markedTextIds: string[] = []

    constructor(props: MonacoCellEditorProps) {
        super(props)
        this.state = { created: true }
    }

    componentDidMount() {
        const monacoContainer = ReactDOM.findDOMNode(this)
        this.editor = this.buildEditor(monacoContainer)
        this.props.blockProps.setModelId(this.editor.getModel().id)
        this.props.blockProps.subscribeToEditor((message: EditorMessage) => {
            this.onEditorMessage(message)
        })

        setTimeout(() => {
            if (this.state.created) {
                this.setState({ created: false })
            }
            this.updateLayout()
            this.moveSelection(false, false)
        }, 0)
    }

    componentWillUnmount() {
        this.props.blockProps.codeCellBlurred(this.getKey())
        window.removeEventListener("resize", this.windowResizeHandler)
        this.editor!.dispose()
    }

    getKey() {
        return this.props.block.key
    }

    render() {
        return (
            <div onClick={(e) => this.focus(e)}>
            </div>
        )
    }

    buildEditor(elem: Element) {
        const targetElem = document.createElement("div")
        const editor = monaco.editor.create(targetElem, {
            value: this.props.block.text,
            language: "csharp",
            scrollBeyondLastLine: false,
            roundedSelection: false,
            overviewRulerLanes: 0, // 0 = hide overview ruler
            wordWrap: "on",
            formatOnType: true,
            lineNumbers: "on",
            lineDecorationsWidth: 8,
            contextmenu: false,
            cursorBlinking: 'phase',
            minimap: {
                enabled: false
            },
            scrollbar: {
                // must explicitly hide scrollbars so they don't interfere with mouse events
                horizontal: 'hidden',
                vertical: 'hidden',
                handleMouseWheel: false,
                useShadows: false,
            }
        })

        editor.onDidChangeModelContent((e) => {
            this.syncContent()
        })
        editor.onKeyDown(e => this.onKeyDown(e))
        editor.onDidBlurEditor(() => {
            this.props.blockProps.codeCellBlurred(this.getKey());
            this.dismissParameterHintsWindow()
        })
        editor.onDidFocusEditor(() => {
            this.props.blockProps.codeCellFocused(this.getKey());
        })

        const internalViewEventsHandler = {
            handleEvents: (e: any) => this.handleInternalViewEvents(e)
        };
        let untypedEditor: any = editor
        untypedEditor._view.eventDispatcher.addEventHandler(internalViewEventsHandler)

        this.windowResizeHandler = () => this.updateLayout()
        window.addEventListener("resize", this.windowResizeHandler)

        setTimeout(() => {
            this.updateLayout()
            this.focus()
        }, 0)

        elem.appendChild(targetElem)
        return editor
    }

    syncContent() {
        let buffer = this.getContent()
        this.props.blockProps.updateTextContentOfBlock(this.getKey(), buffer)
        this.updateCodeCellStatus(buffer) // No need to await this
    }

    async updateCodeCellStatus(buffer: string) {
        this.lastUpdateResponse = await this.props.blockProps.updateCodeCell(buffer)
        this.clearMarkedText()

        if (!this.lastUpdateResponse)
            return

        for (const diagnostic of this.lastUpdateResponse.diagnostics) {
            if (diagnostic.severity !== DiagnosticSeverity.Error)
                continue

            this.markText({
                options: {
                    inlineClassName: 'xi-diagnostic',
                    hoverMessage: diagnostic.message
                },
                range: diagnostic.span
            })
        }
    }

    markText(decoration: monaco.editor.IModelDeltaDecoration) {
        let newIds = this.editor!.getModel().deltaDecorations([], [decoration])
        this.markedTextIds.push(...newIds)
    }

    // TODO: Use this on cell reset, once we have that
    clearMarkedText() {
        this.editor!.getModel().deltaDecorations(this.markedTextIds, [])
        this.markedTextIds = []
    }

    onEditorMessage(message: EditorMessage) {
        // ignore events not related to this instance
        if (message.target !== this.getKey())
            return

        if (message.type === EditorMessageType.setSelection) {
            const isBackwards = message.data.isBackwards
            const isUpArrow = message.data.keyCode == EditorKeys.UP || message.data.keyCode == EditorKeys.BACKSPACE
            const startColumn = !isBackwards
            this.moveSelection(!isBackwards, startColumn)
            this.focus()
        }

        if (message.type == EditorMessageType.setCursor) {
            this.editor!.setPosition(message.data)
            this.focus()
        }
    }

    onKeyDown(e: monaco.IKeyboardEvent) {
        if (this.isCompletionWindowVisible())
            return
        if (e.keyCode == monaco.KeyCode.Enter) {
            if (this.onEnter(e))
                e.preventDefault()
            e.stopPropagation()
            return
        }
        let handled = false
        if ((e.keyCode == monaco.KeyCode.UpArrow && !this.isParameterHintsWindowVisible()) || e.keyCode == monaco.KeyCode.LeftArrow)
            handled = this.handleEditorBoundaries(-1, e.keyCode == monaco.KeyCode.UpArrow)
        else if ((e.keyCode == monaco.KeyCode.DownArrow && !this.isParameterHintsWindowVisible()) || e.keyCode == monaco.KeyCode.RightArrow)
            handled = this.handleEditorBoundaries(1, e.keyCode == monaco.KeyCode.DownArrow)
        else if (e.keyCode == monaco.KeyCode.Backspace && !this.isParameterHintsWindowVisible())
            handled = this.handleEditorBoundaries(-1, false);

        if (handled) {
            e.preventDefault()
            e.stopPropagation()
        }
    }

    onEnter(e: monaco.IKeyboardEvent): boolean {
        const isMod = osMac ? e.metaKey : e.ctrlKey

        // Shift+Mod+Enter: new markdown cell (do we need this now?)
        if (e.shiftKey && isMod) {
            // TODO
            return true
        }

        // Mod+Enter: evaluate
        if (isMod) {
            this.props.blockProps.evaluate()
            return true
        }

        // Shift+Enter: regular newline+indent
        if (e.shiftKey)
            return false

        if (this.lastUpdateResponse &&
            this.lastUpdateResponse.isSubmissionComplete &&
            !this.isSomethingSelected() &&
            this.isCursorAtEnd()) {
            let content = this.getContent()
            if (!content || !content.trim())
                return false
            this.props.blockProps.evaluate()
            return true
        }

        return false
    }

    isSomethingSelected() {
        let sel = this.editor!.getSelection()
        return !sel.getStartPosition().equals(sel.getEndPosition())
    }

    isCursorAtEnd() {
        let pos = this.editor!.getPosition()
        let model = this.editor!.getModel()

        return pos.lineNumber == model.getLineCount() &&
            pos.column == model.getLineMaxColumn(pos.lineNumber)
    }

    // TODO: It would be better if we had API access to SuggestController, and if
    //       SuggestController had API to check widget visibility. In the future
    //       we could add this functionality to monaco.
    //       (or if our keybindings had access to 'suggestWidgetVisible' context key)
    isCompletionWindowVisible() {
        return this.isMonacoWidgetVisible("suggest-widget")
    }

    isParameterHintsWindowVisible() {
        return this.isMonacoWidgetVisible("parameter-hints-widget")
    }

    dismissParameterHintsWindow() {
        if (this.isParameterHintsWindowVisible())
            this.editor!.setPosition({ lineNumber: 1, column: 1 })
    }

    isMonacoWidgetVisible(widgetClassName: string) {
        let node = this.editor!.getDomNode()
        if (node == null)
            return false
        let widgets = node.getElementsByClassName(widgetClassName)
        for (var i = 0; i < widgets.length; i++)
            if (widgets[i].classList.contains("visible"))
                return true
        return false
    }

    focus(e?: any) {
        this.editor!.focus()
        this.props.blockProps.setSelection(this.getKey(), 0)
        if (e)
            e.stopPropagation()
    }

    blur(e?: any) {
        if (this.editor!.isFocused()) {
            (document.activeElement as any).blur()
        }
    }

    updateLayout() {
        let clientWidth = this.getClientWidth()
        let fontSize = this.editor!.getConfiguration().fontInfo.fontSize || 0
        this.editor!.layout({
            // NOTE: Something weird happens in layout, and padding+border are added
            //       on top of the total width by monaco. So we subtract those here.
            //       Keep in sync with editor.css. Currently 0.5em left/right padding
            //       and 1px border means (1em + 2px) to subtract.
            width: clientWidth - (fontSize + 2),
            height: (this.editor as any)._view._context.viewLayout._linesLayout.getLinesTotalHeight()
        })
    }

    handleInternalViewEvents(events: any[]) {
        for (let i = 0, len = events.length; i < len; i++) {
            let type: number = events[i].type;

            // This is the best way for us to find out about lines being added and
            // removed, if you take wrapping into account.
            if (type == ViewEventType.ViewLinesInserted || type == ViewEventType.ViewLinesDeleted)
                this.updateLayout()
        }
    }

    handleEditorBoundaries(dir: 1 | -1, useLineBoundary: boolean) {
        let handled = false
        let pos = this.editor!.getPosition()
        let isBeginning = (pos.lineNumber === 1 && dir === -1)
        if (!useLineBoundary)
            isBeginning = isBeginning && pos.column === 1
        let isEnd = (pos.lineNumber === this.getEditorLines() && dir === 1)
        if (!useLineBoundary)
            isEnd = isEnd && this.getLineColumns(pos.lineNumber) + 1 === pos.column

        this.blur()
        if (isBeginning) {
            handled = this.props.blockProps.selectPrevious(this.getKey())
        } else if (isEnd) {
            handled = this.props.blockProps.selectNext(this.getKey())
        }
        if (!handled)
            this.focus()

        return handled
    }

    /**
     * Move selection to start or end of the code block
     */
    moveSelection(firstLine: boolean, firstColumn: boolean) {
        firstColumn = firstColumn === true
        let lineIndex = 1
        if (!firstLine)
            lineIndex = this.getEditorLines()

        const targetColumn = firstColumn ? 1 : (this.editor!.getModel() as any)._lines[lineIndex - 1].text.length + 1
        const selection = new monaco.Selection(lineIndex, targetColumn, lineIndex, targetColumn);
        this.editor!.setSelection(selection)
    }

    getClientWidth() {
        return ReactDOM.findDOMNode(this).clientWidth
    }

    getEditorLines() {
        const model = this.editor!.getModel();
        return model ? model.getLineCount() : 0;
    }

    getLineColumns(lineIndex: number) {
        return this.editor!.getModel().getLineContent(lineIndex).length
    }

    getContent() {
        return this.editor!.getModel().getValue()
    }
}