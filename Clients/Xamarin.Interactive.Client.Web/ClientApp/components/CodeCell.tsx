//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { WorkbookSession } from '../WorkbookSession'
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
}

export class CodeCell extends React.Component<CodeCellProps, CodeCellState> {
    constructor(props: CodeCellProps) {
        super(props)
        props.blockProps.session.insertCodeCell().then(codeCellId => console.log("ID: %O", codeCellId))
    }

    render() {
        return (
            <div className="CodeCell-container">
            <div className="CodeCell-editor-container">
                <MonacoCellEditor blockProps={this.props.blockProps} block={this.props.block} />
                </div>
                <div className="CodeCell-actions-container">
                    <button className="button-run btn-primary btn-small" type="button">
                        Run
                    </button>
                </div>
            </div>
        );
    }
}