//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import * as ReactDOM from 'react-dom'
import * as Immutable from 'immutable'

import { ProgressIndicator } from 'office-ui-fabric-react/lib/ProgressIndicator';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';

import { randomReactKey } from '../utils'
import { Dropdown } from './Dropdown'
import { ActionButton } from './ActionButton'
import { EditorMessage, EditorMessageType } from '../utils/EditorMessages'
import { CodeCellResult, CodeCellResultHandling, Diagnostic, CapturedOutputSegment } from '../evaluation'
import { Representation, RepresentationRegistry } from '../rendering'
import createCapturedOutputRepresentation from '../renderers/CapturedOutputRenderer'
import { RepresentationSelector } from './RepresentationSelector'

import './CodeCellView.scss'

export const enum CodeCellViewStatus {
    Unbound,
    Ready,
    Evaluating,
    Aborting
}

interface RepresentationViewProps {
    rootRepresentation: Representation
}

interface RepresentationViewState {
    selectedRepresentation?: Representation
}

class RepresentationView extends React.Component<RepresentationViewProps, RepresentationViewState> {
    constructor(props: RepresentationViewProps) {
        super(props)
        this.state = {}
    }

    shouldComponentUpdate(nextProps: RepresentationViewProps, nextState: RepresentationViewState) {
        function getKey(representation: Representation | undefined) {
            if (representation)
                return representation.key
            return undefined
        }

        return getKey(this.state.selectedRepresentation) !== getKey(nextState.selectedRepresentation)
    }

    render() {
        return (
            <div className='CodeCell-representation'>
                <RepresentationSelector
                    key={`representation-selector:${this.props.rootRepresentation.key}`}
                    rootRepresentation={this.props.rootRepresentation}
                    onRenderRepresentation={selectedRepresentation => this.setState({ selectedRepresentation })}/>
                {this.state.selectedRepresentation && this.state.selectedRepresentation.component &&
                    <div className={`CodeCell-representation-renderer-container CodeCell-representation-${this.state.selectedRepresentation.component.name}`}>
                        <this.state.selectedRepresentation.component
                            key={`render-component:${this.state.selectedRepresentation.key}`}
                            {... this.state.selectedRepresentation.componentProps}/>
                    </div>
                }
            </div>
        )
    }
}

export interface CodeCellViewProps {
    representationRegistry: RepresentationRegistry
}

export interface CodeCellViewState {
    status: CodeCellViewStatus
    capturedOutput: CapturedOutputSegment[]
    results: JSX.Element[]
    diagnostics: Diagnostic[]
}

export abstract class CodeCellView<
    TCodeCellViewProps extends CodeCellViewProps = CodeCellViewProps,
    TCodeCellViewState extends CodeCellViewState = CodeCellViewState>
    extends React.Component<TCodeCellViewProps, TCodeCellViewState> {

    protected abstract getRepresentationRegistry(): RepresentationRegistry
    protected abstract startEvaluation(): Promise<void>
    protected abstract abortEvaluation(): Promise<void>
    protected abstract renderEditor(): any

    protected setStateFromResult(result: CodeCellResult, resultHandling?: CodeCellResultHandling) {
        const rootRepresentation = this
            .getRepresentationRegistry()
            .getRepresentations(result)

        const resultView = rootRepresentation && <RepresentationView
            key={rootRepresentation.key}
            rootRepresentation={rootRepresentation}/>

        switch (resultHandling || result.resultHandling) {
            case CodeCellResultHandling.Append:
                if (resultView)
                    this.setState({ results: this.state.results.concat(resultView) })
                break
            case CodeCellResultHandling.Replace:
            default:
                this.setState({ results: resultView ? [resultView] : [] })
                break
        }
    }

    private renderActions() {
        switch (this.state.status) {
            case CodeCellViewStatus.Evaluating:
            case CodeCellViewStatus.Aborting:
                return <ActionButton
                    iconName="CodeCell-Running"
                    title="Cancel Cell Evaluation"
                    onClick={() => this.abortEvaluation()}/>
            case CodeCellViewStatus.Ready:
                return <ActionButton
                    iconName="CodeCell-Run"
                    title="Evaluate Cell"
                    onClick={() => this.startEvaluation()}/>
        }

        return false
    }

    render() {
        return (
            <div className="CodeCell-container">
                <div className="CodeCell-editor-container">
                    {this.renderEditor()}
                    <div className="CodeCell-actions-container">
                        {this.renderActions()}
                    </div>
                </div>
                <div className="CodeCell-diagnostics-container">
                    <ul>
                        {this.state.diagnostics.map((diag, i) => {
                            return (
                                <li
                                    key={randomReactKey()}
                                    className={"CodeCell-diagnostic-" + diag.severity}
                                    onClick={(e) => {
                                        e.stopPropagation()
                                        this.sendEditorMessage({
                                            target: this.getBlockKey(),
                                            type: EditorMessageType.setCursor,
                                            data: {
                                                lineNumber: diag.range.startLineNumber,
                                                column: diag.range.startColumn
                                            }
                                        })
                                    }}>
                                    ({diag.range.startLineNumber},{diag.range.startColumn}):&nbsp;
                                        {diag.severity} {diag.id}: {diag.message}
                                </li>
                            )
                        })}
                    </ul>
                </div>
                {this.state.capturedOutput.length > 0 &&
                    <div className="CodeCell-captured-output-container">
                        <RepresentationView
                            rootRepresentation={{
                                key: `CapturedOutput:${this.state.capturedOutput.length}`,
                                displayName: '_root',
                                children: Immutable.List(
                                    [createCapturedOutputRepresentation(this.state.capturedOutput)])
                            }}/>
                    </div>}
                <div className="CodeCell-results-container">
                    {this.state.results}
                </div>
            </div>
        );
    }

    protected sendEditorMessage(message: EditorMessage) {
        // no-op here
    }

    protected getBlockKey(): string {
        // should be overridden
        return ""
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
            capturedOutput: [],
            results: [],
            diagnostics: []
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

    protected getRepresentationRegistry(): RepresentationRegistry {
        return this.props.representationRegistry
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