//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { StatusMessage, StatusUIAction } from '../WorkbookSession';

interface StatusBarState {
    visible: boolean;
    spin: boolean;
    text: string | null;
}

export class StatusBar extends React.Component<any, StatusBarState> {
    constructor() {
        super();
        this.state = {
            visible: false,
            spin: false,
            text: null
        };
    }

    update(action: StatusUIAction, message: StatusMessage | null) {
        switch (action) {
            case StatusUIAction.None:
            case StatusUIAction.DisplayIdle:
                this.setState({ visible: false });
                break;
            case StatusUIAction.StartSpinner:
                this.setState({
                    visible: true,
                    spin: true
                });
                break;
            case StatusUIAction.StopSpinner:
                this.setState({
                    visible: true,
                    spin: false
                });
                break;
            case StatusUIAction.DisplayMessage:
                this.setState({
                    visible: true,
                    text: message ? message.text : null
                });
                break;
        }
    }

    render() {
        if (!this.state.visible)
            return null;

        return (
            <div className='StatusBar-container' role='status'>
                <div className={this.state.spin ? 'spinner' : 'spinner hidden'}></div>
                <div className='text'>{this.state.text}</div>
            </div>
        );
    }
}