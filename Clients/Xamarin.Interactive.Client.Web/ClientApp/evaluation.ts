import { CodeCellView } from "./components/CodeCellView";

//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export const enum DiagnosticSeverity {
    Hidden = 'Hidden',
    Info = 'Info',
    Warning = 'Warning',
    Error = 'Error'
}

export interface LinePosition {
    line: number
    character: number
}

export interface LinePositionSpan {
    start: LinePosition
    end: LinePosition
}

export interface FileLinePositionSpan {
    path: string
    hasMappedPath: boolean
    span: LinePositionSpan
}

export interface Diagnostic {
    id: string
    message: string
    severity: DiagnosticSeverity
    span: FileLinePositionSpan
}

export interface CodeCellState {
    id: string
    agentTerminatedWhileEvaluating: boolean
    evaluationCount: number
    isResultAnExpression: boolean
    diagnostics: Diagnostic[]
}

export interface EvaluationResult {
    success: boolean
    shouldStartNewCell: boolean
    codeCellStates: CodeCellState[]
}

// Events

export const enum CodeCellEventType {
    EvaluationStarted = 'CodeCellEvaluationStartedEvent',
    Result = 'CodeCellResultEvent',
    CapturedOutputSegment = 'CapturedOutputSegment'
}

export interface ICodeCellEvent {
    $type: CodeCellEventType
    codeCellId: string
}

export const enum CodeCellResultHandling {
    Replace = 'Replace',
    Append = 'Append'
}

export interface CodeCellResult extends ICodeCellEvent  {
    codeCellId: string
    resultHandling: CodeCellResultHandling
    type: string | null
    valueRepresentations: any[] | null
    interact: ((handle: string) => Promise<any>) | undefined
}

export interface CapturedOutputSegment extends ICodeCellEvent {
    codeCellId: string
    fileDescriptor: number
    value: string
}