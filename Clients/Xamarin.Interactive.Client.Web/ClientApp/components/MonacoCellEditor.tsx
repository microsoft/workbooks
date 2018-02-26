//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// <reference path="../../node_modules/monaco-editor/monaco.d.ts" />

import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { SelectionState } from 'draft-js'
import { EditorMessage, EditorMessageType, EditorKeys } from '../utils/EditorMessages'

interface MonacoCellEditorProps {
    blockProps: {
        editorReadOnly: (readOnly: boolean) => void,
        subscribeToEditor: (callback: (message: EditorMessage) => void) => void,
        selectNext: (currentKey: string) => boolean,
        selectPrevious: (currentKey: string) => boolean,
        updateTextContentOfBlock: (blockKey: string, textContent: string) => void,
        setSelection: (anchorKey: string, offset: number) => void
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
    windowResizeHandler: any;
    editor: monaco.editor.ICodeEditor;
    constructor(props: MonacoCellEditorProps) {
        super(props)
        this.state = { created: true }
    }

    componentDidMount() {
        const monacoContainer = ReactDOM.findDOMNode(this)
        this.editor = this.buildEditor(monacoContainer)
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
        this.props.blockProps.editorReadOnly(false)
        window.removeEventListener("resize", this.windowResizeHandler)
        this.editor.dispose()
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
            this.props.blockProps.editorReadOnly(false);
        })
        editor.onDidFocusEditor(() => {
            this.props.blockProps.editorReadOnly(true);
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
        this.props.blockProps.updateTextContentOfBlock(this.getKey(), this.getContent())
    }

    onEditorMessage(message: EditorMessage) {
        // ignore events not related to this instance
        if (message.target !== this.getKey())
            return

        if (message.type === EditorMessageType.setSelection) {
            const isBackwards = message.data.isBackwards
            const isUpArrow = message.data.keyCode == EditorKeys.UP
            const startColumn = !isBackwards || isBackwards && isUpArrow
            this.moveSelection(!isBackwards, startColumn)
            this.focus()
        }
    }

    onKeyDown(e: monaco.IKeyboardEvent) {
        if (e.keyCode == monaco.KeyCode.Enter) {
            e.stopPropagation()
            return
        }
        let handled = false
        if (e.keyCode == monaco.KeyCode.UpArrow || e.keyCode == monaco.KeyCode.LeftArrow)
            handled = this.handleEditorBoundaries(-1, e.keyCode == monaco.KeyCode.UpArrow)
        else if (e.keyCode == monaco.KeyCode.DownArrow || e.keyCode == monaco.KeyCode.RightArrow)
            handled = this.handleEditorBoundaries(1, e.keyCode == monaco.KeyCode.DownArrow)

        if (handled) {
            e.preventDefault()
            e.stopPropagation()
        }
    }

    focus(e?: any) {
        this.editor.focus()
        this.props.blockProps.setSelection(this.getKey(), 0)
        if (e)
            e.stopPropagation()
    }

    blur(e?: any) {
        if (this.editor.isFocused()) {
            (document.activeElement as any).blur()
        }
    }

    updateLayout() {
        let clientWidth = this.getClientWidth()
        let fontSize = this.editor.getConfiguration().fontInfo.fontSize || 0
        this.editor.layout({
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
        let pos = this.editor.getPosition()
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

        const targetColumn = firstColumn ? 1 : (this.editor.getModel() as any)._lines[lineIndex - 1].text.length + 1
        const selection = new monaco.Selection(lineIndex, targetColumn, lineIndex, targetColumn);
        this.editor.setSelection(selection)
    }

    getClientWidth() {
        return ReactDOM.findDOMNode(this).clientWidth
    }

    getEditorLines() {
        const model = this.editor.getModel();
        return model ? model.getLineCount() : 0;
    }

    getLineColumns(lineIndex: number) {
        return this.editor.getModel().getLineContent(lineIndex).length
    }

    getContent() {
        return this.editor.getModel().getValue()
    }
}