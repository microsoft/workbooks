//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { RepresentedResult } from '../evaluation';
import { ResultRenderer } from '../rendering'
import { randomReactKey } from '../utils';

export default function TestRendererFactory(result: RepresentedResult) {
    if (!result.valueRepresentations || result.valueRepresentations.length === 0)
        return new TestRenderer
    return null
}

class TestRenderer implements ResultRenderer {
    getRepresentations(result: RepresentedResult) {
        return [
            {
                key: randomReactKey(),
                component: TestRepresentation,
                componentProps: {
                    id: 1
                },
                displayName: 'Test Representation 1'
            },
            {
                key: randomReactKey(),
                component: TestRepresentation,
                componentProps: {
                    id: 2
                },
                displayName: 'Test Representation 2'
            }
        ]
    }
}

class TestRepresentation extends React.Component<{ id: number }> {
    render() {
        return <pre>Test Rendering: {this.props.id}</pre>
    }
}