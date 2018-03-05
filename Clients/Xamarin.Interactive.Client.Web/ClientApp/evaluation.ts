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

export const enum CodeCellResultHandling {
    Replace = 'Replace',
    Append = 'Append'
}

export interface CodeCellResult {
    codeCellId: string
    resultHandling: CodeCellResultHandling
    type: string | null
    valueRepresentations: any[] | null
}

export interface CapturedOutputSegment {
    codeCellId: string
    value: string
}