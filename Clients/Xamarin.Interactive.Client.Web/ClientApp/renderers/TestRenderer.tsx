//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'

import { CodeCellResult } from '../evaluation';
import {
    ResultRenderer,
    ResultRendererRepresentation
} from '../rendering'

class TestRepresentation extends ResultRendererRepresentation {
    static lastId: number = 0
    id: number = TestRepresentation.lastId++

    constructor(representationName: string) {
        super({
            shortDisplayName: representationName,
            value: null
        })
    }

    render() {
        return <pre>Bogus Rendering: {this.id}</pre>
    }
}

export class TestRenderer implements ResultRenderer {
    static factory(result: CodeCellResult) {
        return new TestRenderer
    }

    getRepresentations(result: CodeCellResult) {
        return [
            new TestRepresentation('Test Rep A'),
            new TestRepresentation('Test Rep B')
        ]
    }
}