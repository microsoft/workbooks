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
    ResultRendererRepresentation,
    getFirstRepresentationOfType
} from '../rendering'

const RepresentationName = 'ToStringRepresentation'

export default function ToStringRendererFactory(result: CodeCellResult) {
    return getFirstRepresentationOfType(result, RepresentationName)
        ? new ToStringRenderer
        : null
}

export interface ToStringRepresentationData {
    $type: string
    formats: {
        name: string,
        value: string
    }[]
}

class ToStringRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []
        const value = getFirstRepresentationOfType<ToStringRepresentationData>(result, RepresentationName)

        if (value) {
            // TODO: some way to toggle between current culture (what we're using now,
            // index 0), and invariant culture (completely ignoring it, index 1). We
            // have never exposed the invariant culture, so this is not a regression
            // over the XCB UI.
            for (const format of value.formats) {
                reps.push({
                    displayName: format.name,
                    component: ToStringRepresentation,
                    componentProps: {
                        value: format.value
                    }
                })
            }
        }

        return reps
    }
}

class ToStringRepresentation extends React.Component<{ value: string }> {
    render() {
        return <code>{this.props.value}</code>
    }
}