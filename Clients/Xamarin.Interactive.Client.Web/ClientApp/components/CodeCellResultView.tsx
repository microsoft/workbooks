//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';

import {
    CodeCellResult,
    CodeCellResultHandling
} from '../evaluation'

interface CodeCellResultViewProps {
    result: CodeCellResult
}

interface CodeCellResultViewState {
}

export class CodeCellResultView extends React.Component<CodeCellResultViewProps, CodeCellResultViewState> {
    render() {
        return (
            <div/>
        )
    }
}