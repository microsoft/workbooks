//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { RouteComponentProps } from 'react-router';

import { CodeCellResult, CodeCellResultHandling } from '../evaluation'
import { ResultRendererRegistry } from '../ResultRendererRegistry'
import { MockedCodeCellView } from './CodeCellView';

export class ComponentPlayground extends React.Component<RouteComponentProps<{}>> {
    private readonly nullResult: CodeCellResult = {
        codeCellId: 'ccid-1',
        resultHandling: CodeCellResultHandling.Replace,
        type: null,
        valueRepresentations: null
    }

    public render() {
        return (
            <article style={{ margin: '1em 2em' }}>
                <h2>Code Cell</h2>
                <MockedCodeCellView
                    rendererRegistry={ResultRendererRegistry.createDefault()}
                    results={[]} />

                <h2>Code Cell with Results</h2>
                <MockedCodeCellView
                    rendererRegistry={ResultRendererRegistry.createDefault()}
                    results={[this.nullResult, this.nullResult]} />
            </article>
        )
    }
}