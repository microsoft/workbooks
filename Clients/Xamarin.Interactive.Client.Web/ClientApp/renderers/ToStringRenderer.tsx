//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { randomReactKey } from '../utils'
import { CodeCellResult } from '../evaluation'
import {
    ResultRenderer,
    ResultRendererRepresentation,
    getFirstRepresentationOfType
} from '../rendering'

export const ToStringRepresentationDataTypeName = 'Xamarin.Interactive.Representations.ToStringRepresentation'

export interface ToStringRepresentationData {
    $type: string
    formats: {
        name: string,
        value: string
    }[]
}

export default function ToStringRendererFactory(result: CodeCellResult) {
    return getFirstRepresentationOfType(result, ToStringRepresentationDataTypeName)
        ? new ToStringRenderer
        : null
}

class ToStringRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []
        const value = getFirstRepresentationOfType<ToStringRepresentationData>(
            result,
            ToStringRepresentationDataTypeName)

        if (value) {
            // TODO: some way to toggle between current culture (what we're using now,
            // index 0), and invariant culture (completely ignoring it, index 1). We
            // have never exposed the invariant culture, so this is not a regression
            // over the XCB UI.
            for (const format of value.formats) {
                reps.push({
                    key: randomReactKey(),
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