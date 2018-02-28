//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { WorkbookSession } from '../WorkbookSession'
import { CodeCellResult, CodeCellResultHandling } from '../evaluation'
import { MonacoCellEditor, MonacoCellEditorProps } from './MonacoCellEditor'
import { EditorMessage } from '../utils/EditorMessages'
import { MonacoCellMapper } from './WorkbookEditor'
import { WorkbookShellContext } from './WorkbookShell'
import { ResultRendererRepresentation } from '../rendering';

interface CodeCellProps {
    blockProps: {
        shellContext: WorkbookShellContext,
        cellMapper: MonacoCellMapper,
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

interface CodeCellState {
    codeCellId: string | null
    results: CodeCellResult[]
}

export class CodeCell extends React.Component<CodeCellProps, CodeCellState> {
    private shellContext: WorkbookShellContext
    private monacoCellProps: MonacoCellEditorProps
    private monacoModelId: string

    constructor(props: CodeCellProps) {
        super(props)
        this.state = {
            codeCellId: null,
            results: []
        }
        this.shellContext = props.blockProps.shellContext
        this.monacoModelId = ""
        this.monacoCellProps = {
            block: props.block,
            blockProps: {
                editorReadOnly: props.blockProps.editorReadOnly,
                subscribeToEditor: props.blockProps.subscribeToEditor,
                selectNext: props.blockProps.selectNext,
                selectPrevious: props.blockProps.selectPrevious,
                updateTextContentOfBlock:
                    props.blockProps.updateTextContentOfBlock,
                setSelection: props.blockProps.setSelection,
                setModelId: modelId => this.monacoModelId = modelId
            }
        }
    }

    async componentDidMount() {
        this.shellContext.session.evaluationEvent.addListener(this.evaluationEventHandler.bind(this))

        var codeCellId = await this.shellContext.session.insertCodeCell()

        this.setState({
            codeCellId: codeCellId
        });

        this.props.blockProps.cellMapper.registerCellInfo(
            codeCellId, this.monacoModelId)
    }

    async componentDidUpdate() {
        if (this.state.codeCellId)
            await this.shellContext.session.updateCodeCell(
                this.state.codeCellId,
                this.props.block.text)
    }

    private async evaluate() {
        if (this.state.codeCellId)
            await this.shellContext.session.evaluate(this.state.codeCellId)
    }

    private evaluationEventHandler(session: WorkbookSession, result: CodeCellResult) {
        if (result.codeCellId === this.state.codeCellId) {
            switch (result.resultHandling) {
                case CodeCellResultHandling.Append:
                    this.setState({
                        results: this.state.results.concat(result)
                    })
                    break
                case CodeCellResultHandling.Replace:
                    this.setState({
                        results: [result]
                    })
                    break
            }
        }
    }

    render() {
        return (
            <div className="CodeCell-container">
                <div className="CodeCell-editor-container">
                    <MonacoCellEditor
                        blockProps={this.monacoCellProps.blockProps}
                        block={this.monacoCellProps.block} />
                </div>
                <div className="CodeCell-results-container">
                    {this.state.results.map(result => {
                        console.log("renderresult: %O", result)
                        this.shellContext.rendererRegistry.getRenderers(result)
                    })}
                </div>
                <div className="CodeCell-actions-container">
                    <button
                        className="button-run btn-primary btn-small"
                        type="button"
                        onClick={e => this.evaluate()}>
                        Run
                    </button>
                </div>
            </div>
        );
    }
}