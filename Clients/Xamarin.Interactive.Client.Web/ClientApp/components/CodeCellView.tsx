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

import { randomReactKey } from '../utils'
import { EditorMessage, EditorMessageType } from '../utils/EditorMessages'
import { CodeCellResult, CodeCellResultHandling, Diagnostic, CapturedOutputSegment } from '../evaluation'
import { ResultRendererRepresentation, ResultRendererRepresentationOptions, RepresentedObjectState, RepresentationMap } from '../rendering'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { CapturedOutputView } from './CapturedOutputView'

import './CodeCellView.scss'
import { WorkbookShellContext } from './WorkbookShell';

export interface CodeCellResultRendererState extends RepresentedObjectState {
    result: CodeCellResult
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
    capturedOutput: CapturedOutputSegment[]
    results: CodeCellResultRendererState[]
    diagnostics: Diagnostic[]
}

export abstract class CodeCellView<
    TCodeCellViewProps extends CodeCellViewProps = CodeCellViewProps,
    TCodeCellViewState extends CodeCellViewState = CodeCellViewState>
    extends React.Component<TCodeCellViewProps, TCodeCellViewState> {

    protected abstract getRendererRegistry(): ResultRendererRegistry
    protected abstract abortEvaluation(): Promise<void>
    protected abstract startEvaluation(): Promise<void>
    protected abstract renderEditor(): any

    constructor(props: TCodeCellViewProps) {
        super(props)
        this.interact = this.interact.bind(this);
    }

    protected async interact(key: string)
    {
        const state = this.state
        let index = -1
        const result = this.state.results.find((res, i) => {
            if (res.representations[key].interact) {
                index = i
                return true
            }
            return false
        })

        if (!result)
            return

        var rep = result.representations[key]
        if (rep && rep.interact) {
            var newRep = await rep.interact (rep)
            if (rep !== newRep) {
                this.state.results[index].representations[key] = newRep
                this.setState(this.state)
            }
        }
    }

    protected setStateFromResult(result: CodeCellResult, resultHandling?: CodeCellResultHandling) {
        const block = this.props as any;
        const state = this.state;
        const reps = this
            .getRendererRegistry()
            .getRenderers(result)
            .map(r => r.getRepresentations(result, block.blockProps.shellContext as WorkbookShellContext))

        const flatReps = reps.length === 0
            ? []
            : reps.reduce((a, b) => a.concat(b))

        const mapReps: RepresentationMap<string, ResultRendererRepresentation> = {}
        flatReps.map((r, i) => {
            mapReps[r.key] = r
        })

        const rendererState = {
            result: result,
            representations: mapReps,
            selectedRepresentation: flatReps[0].key
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
                if (this.state.results[0]) {
                    const oldReps = this.state.results[0].representations;
                    const oldKey = this.state.results[0].selectedRepresentation;

                    if (rendererState.representations[oldKey])
                        rendererState.selectedRepresentation = oldKey;
                    else {
                        const prevKeys = Object.keys(oldReps);
                        if (prevKeys.length == flatReps.length &&
                            Object.keys(oldReps)
                                .map((key, index) => typeof oldReps[key] === typeof flatReps[index])
                                .reduce((a, b) => a && b)) {
                            rendererState.selectedRepresentation = flatReps[prevKeys.indexOf(oldKey)].key
                        }
                    }
                }
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
                                                lineNumber: diag.span.startLineNumber,
                                                column: diag.span.startColumn
                                            }
                                        })
                                    }}>
                                    ({diag.span.startLineNumber},{diag.span.startColumn}):&nbsp;
                                        {diag.severity} {diag.id}: {diag.message}
                                </li>
                            )
                        })}
                    </ul>
                </div>
                {this.state.capturedOutput.length > 0 &&
                    <div className="CodeCell-captured-output-container">
                        <CapturedOutputView segments={this.state.capturedOutput} />
                    </div>}
                <div className="CodeCell-results-container">
                    {this.state.results.map((resultState, i) => {
                        if (Object.keys (resultState.representations).length === 0)
                            return
                        const dropdownOptions = Object.keys (resultState.representations).length > 1
                            ? Object.keys (resultState.representations).map (key => {
                                return {
                                    key: key,
                                    text: resultState.representations[key].displayName
                                }
                            })
                            : null

                        let repElem = null
                        if (resultState.selectedRepresentation) {
                            const rep = resultState.representations[resultState.selectedRepresentation]
                            rep.interact && this.interact(resultState.selectedRepresentation).then(r => console.log("updated"))

                            repElem = <rep.component key={"codeCellView:"+rep.key} {...rep.componentProps} />
                        }

                        return (
                            <div
                                key={i}
                                className="CodeCell-result">
                                <div className="CodeCell-result-renderer-container">
                                    {repElem}
                                </div>
                                {dropdownOptions && <Dropdown
                                    options={dropdownOptions}
                                    defaultSelectedKey={resultState.selectedRepresentation}
                                    onChanged={item => {
                                        this.state.results[i].selectedRepresentation = item.key as string
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