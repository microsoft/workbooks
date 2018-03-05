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

export default function NullRendererFactory(result: RepresentedResult) {
    if (!result.valueRepresentations || result.valueRepresentations.length === 0)
        return new NullRenderer
    return null
}

class NullRenderer implements ResultRenderer {
    getRepresentations(result: RepresentedResult) {
        return [{
            key: randomReactKey(),
            component: NullRepresentation,
            displayName: 'null'
        }]
    }
}

class NullRepresentation extends React.Component {
    render() {
        return <pre>null</pre>
    }
}