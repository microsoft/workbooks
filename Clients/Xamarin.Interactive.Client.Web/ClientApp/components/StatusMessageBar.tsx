//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { Stack } from 'immutable'
import {
    MessageBar,
    MessageBarType
} from 'office-ui-fabric-react/lib/MessageBar';

import {
    Message,
    MessageKind,
    MessageSeverity,
    StatusUIAction,
    StatusUIActionWithMessage,
    IStatusUIActionHandler,
} from '../messages'

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
        if (this.state.messageStack.size <= 0) {
            return false
        }

        const message = this.state.messageStack.peek()
        let messageBarType: MessageBarType

        switch (message.severity) {
            case MessageSeverity.Error:
                messageBarType = MessageBarType.error
                break
            default:
                messageBarType = MessageBarType.info
                break
        }

        return (
            <MessageBar messageBarType={messageBarType}>
                <span>{message.text}</span>
            </MessageBar>
        )
    }
}