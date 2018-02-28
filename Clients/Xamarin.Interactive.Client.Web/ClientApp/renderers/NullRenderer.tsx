import * as React from 'react'
import { CodeCellResult } from '../evaluation';
import {
    ResultRenderer,
    ResultRendererRepresentation
} from '../rendering'

class NullRepresentation extends ResultRendererRepresentation {
    constructor() {
        super({
            shortDisplayName: 'null'
        })
    }

    render() {
        return <div>null</div>
    }
}

export class NullRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        return [ new NullRepresentation ]
    }
}