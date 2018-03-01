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
        return <pre>null</pre>
    }
}

export class NullRenderer implements ResultRenderer {
    static factory(result: CodeCellResult) {
        if (!result.valueRepresentations || result.valueRepresentations.length === 0)
            return new NullRenderer
        return null
    }

    getRepresentations(result: CodeCellResult) {
        return [ new NullRepresentation ]
    }
}