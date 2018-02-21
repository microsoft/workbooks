//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// <reference path="../../node_modules/monaco-editor/monaco.d.ts" />

import * as React from 'react';
import * as ReactDOM from 'react-dom';

interface MonacoCellEditorProps {
}

interface MonacoCellEditorState {
    monaco?: monaco.editor.IStandaloneCodeEditor | null;
    monacoEditorOptions?: monaco.editor.IEditorConstructionOptions | null;
}

export class MonacoCellEditor extends React.Component<MonacoCellEditorProps, MonacoCellEditorState> {
    containerElement?: HTMLElement | null = null;

    constructor(props: MonacoCellEditorProps) {
        super(props);
        this.state = {
            monaco: null
        };
    }

    componentDidMount() {
        if (!this.containerElement) {
            console.warn("MonacoCellEditor:componentDidMount was called but no containerElement is bound");
            return;
        }

        const editorOptions: monaco.editor.IEditorConstructionOptions = {
        };

        this.setState({
            monacoEditorOptions: editorOptions,
            monaco: (window as any).monaco.editor.create(
                this.containerElement,
                editorOptions)
        });
    }

    componentWillUnmount() {
        if (this.state.monaco)
            this.state.monaco.dispose();
    }

    updateContainerElement(containerElement: HTMLElement | null) {
        this.containerElement = containerElement;
    }

    render() {
        return (
            <div
                ref={e => this.updateContainerElement(e)}
                className="MonacoCellEditor-container" />
        );
    }
}