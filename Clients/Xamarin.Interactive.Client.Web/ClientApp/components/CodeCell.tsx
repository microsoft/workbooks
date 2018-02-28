//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { WorkbookSession, CodeCellStatus } from '../WorkbookSession'
import { CodeCellResult, CodeCellResultHandling } from '../evaluation'
import { MonacoCellEditor, MonacoCellEditorProps } from './MonacoCellEditor'
import { ContentBlock } from 'draft-js';
import { EditorMessage } from '../utils/EditorMessages'
import { WorkbookShellContext } from './WorkbookShell'
import { ResultRendererRepresentation } from '../rendering';
import { MonacoCellMapper } from '../utils/MonacoUtils'
import { DropDownMenu } from './DropDownMenu';

interface CodeCellProps {
    blockProps: {
        shellContext: WorkbookShellContext,
        cellMapper: MonacoCellMapper,
        editorReadOnly: (readOnly: boolean) => void,
        subscribeToEditor: (callback: (message: EditorMessage) => void) => void,
        selectNext: (currentKey: string) => boolean,
        selectPrevious: (currentKey: string) => boolean,
        updateTextContentOfBlock: (blockKey: string, textContent: string) => void,
        setSelection: (anchorKey: string, offset: number) => void,
        getPreviousCodeBlock: (currentBlock: string) => ContentBlock,
        updateBlockCodeCellId: (currentBlock: string, codeCellId: string) => void,
    }
    block: {
        key: string
        text: string
    }
}

interface CodeCellResultRendererState {
    result: CodeCellResult
    representations: ResultRendererRepresentation[]
    selectedRepresentationIndex: number
}

interface CodeCellState {
    codeCellId: string | null
    results: CodeCellResultRendererState[]
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
                updateTextContentOfBlock: props.blockProps.updateTextContentOfBlock,
                setSelection: props.blockProps.setSelection,
                setModelId: modelId => this.monacoModelId = modelId,
                updateCodeCell: (buffer: string) => this.updateCodeCell(buffer),
                evaluate: () => this.evaluate()
            }
        }
    }

    async componentDidMount() {
        this.shellContext.session.evaluationEvent.addListener(this.evaluationEventHandler.bind(this))

        const previousBlock = this.props.blockProps.getPreviousCodeBlock(this.props.block.key);
        const previousBlockCodeCellId = previousBlock ? previousBlock.getData().get("codeCellId") : undefined;
        const codeCellId = await this.shellContext.session.insertCodeCell(previousBlockCodeCellId);
        this.setState({
            codeCellId
        });
        this.props.blockProps.updateBlockCodeCellId(this.props.block.key, codeCellId);

        this.props.blockProps.cellMapper.registerCellInfo(
            codeCellId, this.monacoModelId)
    }

    async updateCodeCell(buffer: string): Promise<CodeCellStatus> {
        if (this.state.codeCellId)
            return await this.shellContext.session.updateCodeCell(
                this.state.codeCellId,
                buffer)

        return {
            isSubmissionComplete: false,
            diagnostics: []
        }
    }

    private async evaluate() {
        if (this.state.codeCellId)
            await this.shellContext.session.evaluate(this.state.codeCellId)
    }

    private evaluationEventHandler(session: WorkbookSession, result: CodeCellResult) {
        if (result.codeCellId !== this.state.codeCellId)
            return

        console.log("evaluationEventHandler: %O", result)

        const reps = this.shellContext
            .rendererRegistry
            .getRenderers(result)
            .map(r => r.getRepresentations(result))

        const rendererState = {
            result: result,
            representations: reps.length === 0
                ? []
                : reps.reduce((a, b) => a.concat(b)),
            selectedRepresentationIndex: 0
        }

        switch (result.resultHandling) {
            case CodeCellResultHandling.Append:
                this.setState({
                    results: this.state.results.concat(rendererState)
                })
                break
            case CodeCellResultHandling.Replace:
                this.setState({
                    results: [rendererState]
                })
                break
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
                    {this.state.results.map((resultState, i) => {
                        return (
                            <div
                                key={i}
                                className="CodeCell-result">
                                <div className="CodeCell-result-renderer-container">
                                    {resultState.representations[resultState.selectedRepresentationIndex].render()}
                                </div>
                                {resultState.representations.length > 1 && <DropDownMenu
                                    items={resultState.representations}
                                    initiallySelectedIndex={resultState.selectedRepresentationIndex}
                                    selectionChanged={(i, v) => {
                                        resultState.selectedRepresentationIndex = i
                                        this.setState(this.state)
                                    }}/>}
                            </div>
                        )
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