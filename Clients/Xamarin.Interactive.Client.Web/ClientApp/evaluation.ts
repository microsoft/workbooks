//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export interface PositionSpan {
    startLineNumber: number
    startColumn: number
    endLineNumber: number
    endColumn: number
    filePath?: string
}

export const enum DiagnosticSeverity {
    Hidden = 'Hidden',
    Info = 'Info',
    Warning = 'Warning',
    Error = 'Error'
}

export interface Diagnostic {
    id: string
    message: string
    severity: DiagnosticSeverity
    span: PositionSpan
}

// Events

export const enum CodeCellEventType {
    EvaluationStarted = 'CodeCellEvaluationStartedEvent',
    EvaluationFinished = 'CodeCellEvaluationFinishedEvent',
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

export const enum CodeCellEvaluationStatus {
    Success = 'Success',
    Disconnected = 'Disconnected',
    Interrupted = 'Interrupted',
    ErrorDiagnostic = 'ErrorDiagnostic',
    EvaluationException = 'EvaluationException'
}

export interface CodeCellEvaluationFinished extends ICodeCellEvent {
    status: CodeCellEvaluationStatus
    shouldStartNewCell: boolean
    diagnostics: Diagnostic[]
}

export interface CodeCellUpdate extends ICodeCellEvent {
    isSubmissionComplete: boolean
    diagnostics: Diagnostic[]
}

export interface CodeCellResult extends ICodeCellEvent  {
    resultHandling: CodeCellResultHandling
    type: string | null
    valueRepresentations: any[] | null
}

export interface CapturedOutputSegment extends ICodeCellEvent {
    fileDescriptor: number
    value: string
}