//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export interface Range {
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
    range: Range
}

// Events

export const enum CodeCellEventType {
    EvaluationStarted = 'Xamarin.Interactive.CodeAnalysis.Events.CodeCellEvaluationStartedEvent',
    EvaluationFinished = 'Xamarin.Interactive.CodeAnalysis.Events.CodeCellEvaluationFinishedEvent',
    Evaluation = 'Xamarin.Interactive.CodeAnalysis.Evaluating.Evaluation',
    CapturedOutputSegment = 'Xamarin.Interactive.CodeAnalysis.CapturedOutputSegment',
    Compilation = 'Xamarin.Interactive.CodeAnalysis.Compilation',
    TargetCompilationConfiguration = 'Xamarin.Interactive.CodeAnalysis.TargetCompilationConfiguration'
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
    status: CodeCellEvaluationStatus
    resultHandling: CodeCellResultHandling
    resultType: string | null
    resultRepresentations: any[]
    isNullResult: boolean
    evaluationDuration: string
    cultureLCID: number
    uiCultureLCID: number
}

export interface CapturedOutputSegment extends ICodeCellEvent {
    fileDescriptor: number
    value: string
}