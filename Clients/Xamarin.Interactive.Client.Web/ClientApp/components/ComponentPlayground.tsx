//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import { MockedCodeCellView } from './CodeCellView';

export class ComponentPlayground extends React.Component<RouteComponentProps<{}>> {
    public render() {
        return (
            <article style={{ margin: '1em 2em' }}>
                <h2>Code Cell</h2>
                <p>
                    <MockedCodeCellView />
                </p>
            </article>
        )
    }
}