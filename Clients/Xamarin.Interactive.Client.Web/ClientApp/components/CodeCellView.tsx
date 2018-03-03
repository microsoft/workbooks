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
import {
    Dropdown,
    IDropdown,
    DropdownMenuItemType,
    IDropdownOption
} from 'office-ui-fabric-react/lib/Dropdown';

import { CodeCellResult, CodeCellResultHandling } from '../evaluation'
import { ResultRendererRepresentation } from '../rendering'
import { ResultRendererRegistry } from '../ResultRendererRegistry'

import './CodeCellView.scss'

export interface CodeCellResultRendererState {
    result: CodeCellResult
    representations: ResultRendererRepresentation[]
    selectedRepresentation: ResultRendererRepresentation | null
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

        const flatReps = reps.length === 0
            ? []
            : reps.reduce((a, b) => a.concat(b))

        const rendererState = {
            result: result,
            representations: flatReps,
            selectedRepresentation: flatReps.length > 0 ? flatReps [0] : null
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

                        const dropdownOptions = resultState.representations.length > 1
                            ? resultState.representations.map((item, index) => {
                                return {
                                    key: index,
                                    text: item.displayName
                                }
                            })
                            : null

                        let resultElement: any = undefined

                        if (resultState.selectedRepresentation) {
                            resultElement = React.createElement(
                                resultState.selectedRepresentation.component,
                                resultState.selectedRepresentation);

                            console.log(
                                '%O: React.createElement(%O, %O)',
                                resultState.selectedRepresentation.displayName,
                                resultState.selectedRepresentation.component,
                                resultState.selectedRepresentation)
                        }

                        return (
                            <div
                                key={i}
                                className="CodeCell-result">
                                <div className="CodeCell-result-renderer-container">
                                    {resultElement}
                                </div>
                                {dropdownOptions && <Dropdown
                                    options={dropdownOptions}
                                    defaultSelectedKey={dropdownOptions[0].key}
                                    onChanged={item => {
                                        resultState.selectedRepresentation = resultState.representations[item.key as number]
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
    resultHandling?: CodeCellResultHandling
}

export class MockedCodeCellView extends CodeCellView<MockedCodeCellProps> {
    constructor(props: MockedCodeCellProps) {
        super(props)

        this.state = {
            status: CodeCellViewStatus.Ready,
            results: []
        }
    }

    setStateFromPendingResult() {
        const result = this.props.results.shift()
        if (result)
            this.setStateFromResult(result, this.props.resultHandling)
    }

    componentDidMount() {
        this.setStateFromPendingResult()
    }

    componentDidUpdate() {
        this.setStateFromPendingResult()
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