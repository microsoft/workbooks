//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { randomReactKey } from '../utils'
import { CodeCellResult } from '../evaluation'
import { ResultRenderer } from '../rendering'

export default class NullRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
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