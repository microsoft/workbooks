//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export const enum StatusUIAction {
    None = 'None',
    DisplayIdle = 'DisplayIdle',
    DisplayMessage = 'DisplayMessage',
    StartSpinner = 'StartSpinner',
    StopSpinner = 'StopSpinner'
}

export const enum MessageKind {
    Status = 'Status',
    Alert = 'Alert'
}

export const enum MessageSeverity {
    Info = 'Info',
    Error = 'Error'
}

export interface Message {
    id?: number
    kind: MessageKind
    severity: MessageSeverity
    text?: string
    detailedText?: string
    showSpinner?: boolean
}

export interface StatusUIActionWithMessage {
    action: StatusUIAction
    message?: Message
}