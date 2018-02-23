//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { MonacoCellEditor } from './MonacoCellEditor'

export class CodeCell extends React.Component {
    render() {
        return (
            <div className="CodeCell-container">
                <div className="CodeCell-editor-container">
                    <MonacoCellEditor />
                </div>
                <div className="CodeCell-actions-container">
                    <button className="button-run btn-primary btn-small" type="button">
                        Run
                    </button>
                </div>
            </div>
        );
    }
}