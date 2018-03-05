//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { CodeCellResult } from '../evaluation'
import {
    ResultRenderer,
    ResultRendererRepresentation
} from '../rendering'

// import './VerbatimHtmlRenderer.scss'

const xirType = 'Xamarin.Interactive.Representations.VerbatimHtml'

export default function VerbatimHtmlRendererFactory(result: CodeCellResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === xirType)
        ? new VerbatimHtmlRenderer
        : null
}

interface VerbatimHtmlValue {
    $toString: string
}

class VerbatimHtmlRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type === xirType)
                reps.push({
                    displayName: 'HTML',
                    component: VerbatimHtmlRepresentation,
                    componentProps: {
                        value: value as VerbatimHtmlValue
                    }
                })
        }

        return reps
    }
}

class VerbatimHtmlRepresentation extends React.Component<{ value: VerbatimHtmlValue }> {
    render() {
        return (
            <div dangerouslySetInnerHTML={{ __html: this.props.value.$toString }} />
        )
    }
}