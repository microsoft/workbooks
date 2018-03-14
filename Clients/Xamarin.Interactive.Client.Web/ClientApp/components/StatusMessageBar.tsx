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

import { WorkbookSession } from '../WorkbookSession'
import { WorkbookShellContext } from './WorkbookShell'

import {
    Message,
    MessageKind,
    MessageSeverity,
    StatusUIAction,
    StatusUIActionWithMessage
} from '../messages'

import './StatusMessageBar.scss'

interface StatusMessageBarProps {
    shellContext: WorkbookShellContext
}

interface StatusMessageBarState {
    messageStack: Stack<Message>
    bounds?: {
        top: string,
        left: string,
        width: string,
        height: string
    }
}

export class StatusMessageBar extends React.Component<StatusMessageBarProps, StatusMessageBarState> {
    constructor(props: StatusMessageBarProps) {
        super(props)

        this.onStatusUIAction = this.onStatusUIAction.bind(this)
        this.layout = this.layout.bind(this)
        this.layoutAsync = this.layoutAsync.bind(this)

        this.state = {
            messageStack: Stack<Message>()
        }
    }

    componentDidMount() {
        this.props.shellContext.session.statusUIActionEvent.addListener(this.onStatusUIAction)
        window.addEventListener('resize', this.layoutAsync)
        this.layoutAsync()
    }

    componentWillUnmount() {
        this.props.shellContext.session.statusUIActionEvent.removeListener(this.onStatusUIAction)
        window.removeEventListener('resize', this.layoutAsync)
    }

    private onStatusUIAction(session: WorkbookSession, actionMessage: StatusUIActionWithMessage) {
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

    // Giant Hack! I would ideally like to just add an item to our command bar and
    // have that item's onRender render this StatusMessageBar but there is no way
    // to influence the item's actual layout. We want the layout here to fill any
    // void space between the primary+overflow items and the far/right items, and
    // to have the bar fully hidden if there is not enough space in the command bar.
    //
    // Knowing how the CommandBar does its own measure/layout, take advantage of that
    // and just position ourself in the void space after CommandBar's pass.
    //
    // Sorry not sorry. -abock, 2018-03-09.

    private layoutAsync() {
        // CommandBar will also request an animation frame to do its measure pass;
        // set a timeout to defer another loop pass to make sure we run after, and
        // then request our own animation frame.
        setTimeout(() => window.requestAnimationFrame(this.layout), 0)
    }

    private layout() {
        const commandBar = document.getElementsByClassName('ms-CommandBar')[0]
        if (!commandBar)
            return

        const primaryCommands = commandBar.getElementsByClassName('ms-CommandBar-primaryCommands')[0]
        const sideCommands = commandBar.getElementsByClassName('ms-CommandBar-sideCommands')[0]

        const leftCommands = primaryCommands.getElementsByClassName('ms-CommandBarItem')
        const leftCommand = leftCommands[leftCommands.length - 1]
        const leftBounds = leftCommands[leftCommands.length - 1].getBoundingClientRect()

        const rightCommands = sideCommands.getElementsByClassName('ms-CommandBarItem')
        const rightBounds = rightCommands[rightCommands.length - 1].getBoundingClientRect()

        this.setState({
            bounds: {
                top: leftBounds.top + 'px',
                left: leftBounds.right + 'px',
                width: (rightBounds.left - leftBounds.right) + 'px',
                height: leftBounds.height + 'px'
            }
        })
    }

    render() {
        if (!this.state.bounds)
            return false

        let message = this.state.messageStack.peek()
        const className = message ? 'visible' : 'hidden'

        if (!message)
            message = {
                text: '',
                severity: MessageSeverity.Info
            } as Message

        return (
            <div className={'StatusMessageBar-container ' + className} style={this.state.bounds}>
                <div className='StatusMessageBar-contents'>
                    <Spinner size={SpinnerSize.small}/>
                    <div className='StatusMessageBar-text'>{message.text}</div>
                </div>
            </div>
        )
    }
}