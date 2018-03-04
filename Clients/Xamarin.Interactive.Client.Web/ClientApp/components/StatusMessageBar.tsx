//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { Stack } from 'immutable'

import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner'

import {
    Message,
    MessageKind,
    MessageSeverity,
    StatusUIAction,
    StatusUIActionWithMessage,
    IStatusUIActionHandler,
} from '../messages'

import './StatusMessageBar.scss'

interface StatusMessageBarProps {
    initialActionMessages: StatusUIActionWithMessage[]
}

interface StatusMessageBarState {
    messageStack: Stack<Message>
}

export class StatusMessageBar
    extends React.Component<StatusMessageBarProps, StatusMessageBarState>
    implements IStatusUIActionHandler {

    constructor(props: StatusMessageBarProps) {
        super(props)
        this.state = {
            messageStack: Stack<Message>()
        }
    }

    onStatusUIAction(actionMessage: StatusUIActionWithMessage): void {
        switch (actionMessage.action) {
            case StatusUIAction.None:
            case StatusUIAction.DisplayIdle:
                this.setState({ messageStack: this.state.messageStack.clear() })
                break
            case StatusUIAction.DisplayMessage:
                if (actionMessage.message)
                    this.setState({ messageStack: this.state.messageStack.push(actionMessage.message) })
                break
            case StatusUIAction.StartSpinner:
                break
            case StatusUIAction.StopSpinner:
                break
        }
    }

    render() {
        let message = this.state.messageStack.peek()
        const className = message ? 'visible' : 'hidden'

        if (!message)
            message = {
                text: '',
                severity: MessageSeverity.Info
            } as Message

        return (
            <div className={'StatusMessageBar-container ' + className}>
                <div className='StatusMessageBar-contents'>
                    <Spinner size={SpinnerSize.small}/>
                    <div>{message.text}</div>
                </div>
            </div>
        )
    }
}