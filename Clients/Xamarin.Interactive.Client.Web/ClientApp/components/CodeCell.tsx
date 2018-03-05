//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { WorkbookSession, CodeCellUpdateResponse } from '../WorkbookSession'
import { CodeCellResult, CodeCellResultHandling } from '../evaluation'
import { MonacoCellEditor, MonacoCellEditorProps } from './MonacoCellEditor'
import { ContentBlock } from 'draft-js';
import { EditorMessage } from '../utils/EditorMessages'
import { WorkbookShellContext } from './WorkbookShell'
import { ResultRendererRepresentation } from '../rendering';
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { MonacoCellMapper } from '../utils/MonacoUtils'

import {
    CodeCellViewStatus,
    CodeCellView,
    CodeCellViewProps,
    CodeCellViewState,
} from './CodeCellView';

interface CodeCellProps extends CodeCellViewProps {
    blockProps: {
        shellContext: WorkbookShellContext
        rendererRegistry: ResultRendererRegistry
        cellMapper: MonacoCellMapper
        codeCellId: string
        codeCellFocused: (codeCellId: string) => void
        codeCellBlurred: (codeCellId: string) => void
        subscribeToEditor: (callback: (message: EditorMessage) => void) => void
        selectNext: (currentKey: string) => boolean
        selectPrevious: (currentKey: string) => boolean
        updateTextContentOfBlock: (blockKey: string, textContent: string) => void
        setSelection: (anchorKey: string, offset: number) => void
        getPreviousCodeBlock: (currentBlock: string) => ContentBlock
        updateBlockCodeCellId: (currentBlock: string, codeCellId: string) => void
        appendNewCodeCell: () => void // TODO: Eventually need support for inserting arbitrarily
    }
    block: {
        key: string
        text: string
    }
}

interface CodeCellState extends CodeCellViewState {
    codeCellId: string | null
}

export class CodeCell extends CodeCellView<CodeCellProps, CodeCellState> {
    private shellContext: WorkbookShellContext
    private monacoCellProps: MonacoCellEditorProps
    private monacoModelId: string

    constructor(props: CodeCellProps) {
        super(props)

        this.state = {
            codeCellId: this.props.blockProps.codeCellId,
            results: [],
            status: CodeCellViewStatus.Unbound,
            diagnostics: []
        }

        this.shellContext = props.blockProps.shellContext

        this.monacoModelId = ''
        this.monacoCellProps = {
            block: props.block,
            blockProps: {
                codeCellBlurred: props.blockProps.codeCellBlurred,
                codeCellFocused: props.blockProps.codeCellFocused,
                subscribeToEditor: props.blockProps.subscribeToEditor,
                selectNext: props.blockProps.selectNext,
                selectPrevious: props.blockProps.selectPrevious,
                updateTextContentOfBlock: props.blockProps.updateTextContentOfBlock,
                setSelection: props.blockProps.setSelection,
                setModelId: modelId => this.monacoModelId = modelId,
                updateCodeCell: (buffer: string) => this.updateCodeCell(buffer),
                evaluate: () => this.startEvaluation()
            }
        }
    }

    protected getRendererRegistry(): ResultRendererRegistry {
        return this.props.blockProps.rendererRegistry
    }

    async componentDidMount() {
        this.shellContext
            .session
            .evaluationEvent
            .addListener(this.evaluationEventHandler.bind(this))

        let codeCellId: string;

        if (!this.state.codeCellId) {
            const previousBlock = this.props
                .blockProps
                .getPreviousCodeBlock(this.props.block.key);

            const previousBlockCodeCellId = previousBlock
                ? previousBlock.getData().get('codeCellId')
                : undefined;

            codeCellId = await this.shellContext
                .session
                .insertCodeCell(this.props.block.text, previousBlockCodeCellId);

            this.setState({
                status: CodeCellViewStatus.Ready,
                codeCellId: codeCellId,
            });
            this.props.blockProps.updateBlockCodeCellId(this.props.block.key, codeCellId);
        } else {
            codeCellId = this.state.codeCellId;
            this.setState({
                status: CodeCellViewStatus.Ready
            })
        }

        this.props.blockProps.cellMapper.registerCellInfo(codeCellId, this.monacoModelId)
    }

    async updateCodeCell(buffer: string): Promise<CodeCellUpdateResponse> {
        if (this.state.codeCellId)
            return await this.shellContext.session.updateCodeCell(
                this.state.codeCellId,
                buffer)

        return {
            isSubmissionComplete: false,
            diagnostics: []
        }
    }

    protected async startEvaluation(): Promise<void> {
        if (this.state.codeCellId && this.state.status === CodeCellViewStatus.Ready) {
            this.setState({ status: CodeCellViewStatus.Evaluating })

            // TODO: I can't help but feel that this should be handled at a
            //       higher level. The result has state info for potentially all
            //       cells.
            const result = await this.shellContext.session.evaluate(this.state.codeCellId)

            if (result.shouldStartNewCell)
                this.props.blockProps.appendNewCodeCell()

            let codeCellState = result.codeCellStates.filter(s => s.id == this.state.codeCellId)[0]

            // TODO: What about diags/etc for other cell states?
            this.setState({
                status: CodeCellViewStatus.Ready,
                diagnostics: codeCellState.diagnostics
            })
        }
    }

    protected async abortEvaluation(): Promise<void> {
    }

    private evaluationEventHandler(session: WorkbookSession, result: CodeCellResult) {
        if (result.codeCellId !== this.state.codeCellId)
            return

        console.log("evaluationEventHandler: %O", result)

        this.setStateFromResult(result)
    }

    protected renderEditor() {
        return <MonacoCellEditor
            blockProps={this.monacoCellProps.blockProps}
            block={this.monacoCellProps.block} />
    }
}