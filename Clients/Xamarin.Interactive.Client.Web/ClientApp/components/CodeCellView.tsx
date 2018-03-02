//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import { ActionButton } from 'office-ui-fabric-react/lib/Button';
import { ProgressIndicator } from 'office-ui-fabric-react/lib/ProgressIndicator';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';

import { CodeCellResult, CodeCellResultHandling } from '../evaluation'
import { ResultRendererRepresentation } from '../rendering'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { DropDownMenu } from './DropDownMenu'

import './CodeCellView.scss'

export interface CodeCellResultRendererState {
    result: CodeCellResult
    representations: ResultRendererRepresentation[]
    selectedRepresentationIndex: number
}

export const enum CodeCellViewStatus {
    Unbound,
    Ready,
    Evaluating,
    Aborting
}

export interface CodeCellViewProps {
    rendererRegistry: ResultRendererRegistry
}

export interface CodeCellViewState {
    status: CodeCellViewStatus
    results: CodeCellResultRendererState[]
}

export abstract class CodeCellView<
    TCodeCellViewProps extends CodeCellViewProps = CodeCellViewProps,
    TCodeCellViewState extends CodeCellViewState = CodeCellViewState>
    extends React.Component<TCodeCellViewProps, TCodeCellViewState> {

    protected abstract getRendererRegistry(): ResultRendererRegistry
    protected abstract abortEvaluation(): Promise<void>
    protected abstract startEvaluation(): Promise<void>
    protected abstract renderEditor(): any

    protected setStateFromResult(result: CodeCellResult, resultHandling?: CodeCellResultHandling) {
        const reps = this
            .getRendererRegistry()
            .getRenderers(result)
            .map(r => r.getRepresentations(result))

        const rendererState = {
            result: result,
            representations: reps.length === 0
                ? []
                : reps.reduce((a, b) => a.concat(b)),
            selectedRepresentationIndex: 0
        }

        if (!resultHandling)
            resultHandling = result.resultHandling

        switch (resultHandling) {
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

    private renderActions() {
        switch (this.state.status) {
            case CodeCellViewStatus.Unbound:
                return null
            case CodeCellViewStatus.Evaluating:
                return (
                    <div className='actions'>
                        <ActionButton
                            className='CancelButton'
                            iconProps={{ iconName: 'Cancel' }}
                            onClick={e => this.abortEvaluation()}>
                            Cancel
                        </ActionButton>
                        <ProgressIndicator />
                     </div>
                )
            case CodeCellViewStatus.Aborting:
                return <div>Aborting...</div>
            case CodeCellViewStatus.Ready:
                return (
                    <ActionButton
                        iconProps={{ iconName: 'Play' }}
                        onClick={e => this.startEvaluation()}>
                        Run
                    </ActionButton>
                )
        }
        return <div/>
    }

    render() {
        return (
            <div className="CodeCell-container">
                <div className="CodeCell-editor-container">
                    {this.renderEditor()}
                </div>
                <div className="CodeCell-results-container">
                    {this.state.results.map((resultState, i) => {
                        if (resultState.representations.length === 0)
                            return

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
                    {this.renderActions()}
                </div>
            </div>
        );
    }
}

export interface MockedCodeCellProps extends CodeCellViewProps {
    results: CodeCellResult[]
}

export class MockedCodeCellView extends CodeCellView<MockedCodeCellProps> {
    constructor(props: MockedCodeCellProps) {
        super(props)

        this.state = {
            status: CodeCellViewStatus.Ready,
            results: []
        }
    }

    componentDidMount() {
        for (const result of this.props.results)
            this.setStateFromResult(result)
    }

    protected getRendererRegistry(): ResultRendererRegistry {
        return this.props.rendererRegistry
    }

    protected async startEvaluation(): Promise<void> {
        this.setState({status: CodeCellViewStatus.Evaluating})
    }

    protected async abortEvaluation(): Promise<void> {
        this.setState({status: CodeCellViewStatus.Ready})
    }

    protected renderEditor() {
        return (
            <div style={{
                color: '#999',
                backgroundColor: '#FAFAFA',
                padding: '5px',
                fontFamily: 'monospace'
            }}>
                <div>// hello</div>
                <div>var x = 2 + 2</div>
            </div>
        )
    }
}