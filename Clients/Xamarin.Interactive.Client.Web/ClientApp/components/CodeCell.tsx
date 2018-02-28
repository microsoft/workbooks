//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { WorkbookSession, Evaluation } from '../WorkbookSession'
import { MonacoCellEditor } from './MonacoCellEditor'
import { EditorMessage } from '../utils/EditorMessages'

interface CodeCellProps {
    blockProps: {
        session: WorkbookSession,
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
}

export class CodeCell extends React.Component<CodeCellProps, CodeCellState> {
    private session: WorkbookSession

    constructor(props: CodeCellProps) {
        super(props)
        this.state = {
            codeCellId: null
        }
        this.session = props.blockProps.session
    }

    async componentDidMount() {
        this.session.evaluationEvent.addListener(this.evaluationEventHandler.bind(this))

        this.setState({
            codeCellId: await this.session.insertCodeCell()
        })
    }

    async componentDidUpdate() {
        if (this.state.codeCellId)
            await this.session.updateCodeCell(
                this.state.codeCellId,
                this.props.block.text)
    }

    private async evaluate() {
        if (this.state.codeCellId)
            await this.session.evaluate(this.state.codeCellId)
    }

    private evaluationEventHandler(session: WorkbookSession, evaluation: Evaluation) {
        console.log("GOT A CALLBACK: %O -> %O", evaluation, this)
        if (evaluation.codeCellId === this.state.codeCellId) {
            console.log("CELL HANDLING: %O", evaluation)
            return 'stop'
        }
    }

    render() {
        return (
            <div className="CodeCell-container">
                <div className="CodeCell-editor-container">
                    <MonacoCellEditor
                        blockProps={this.props.blockProps}
                        block={this.props.block} />
                </div>
                <div className="CodeCell-results-container">
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