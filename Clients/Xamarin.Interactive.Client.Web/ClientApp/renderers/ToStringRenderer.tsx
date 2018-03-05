//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { RepresentedResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'
import { randomReactKey } from '../utils';

export default function ToStringRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$toString)
        ? new ToStringRenderer
        : null
}

interface ToStringValue {
    $type: string
    $toString: string | {
        culture: {
            name: string,
            lcid: number
        },
        formats: {
            [key: string]: string
        }[]
    }[]
}

class ToStringRenderer implements ResultRenderer {
    getRepresentations(result: RepresentedResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (!value.$toString)
                continue

            const toStringValue = (value as ToStringValue).$toString

            if (typeof toStringValue === 'string') {
                reps.push({
                    displayName: 'String',
                    key: randomReactKey (),
                    component: ToStringRepresentation,
                    componentProps: {
                        value: toStringValue
                    }
                })

                continue
            }

            // TODO: some way to toggle between current culture (what we're using now,
            // index 0), and invariant culture (completely ignoring it, index 1). We
            // have never exposed the invariant culture, so this is not a regression
            // over the XCB UI.
            for (const format of toStringValue[0].formats) {
                const formatKey = Object.keys(format)[0] as string
                reps.push({
                    displayName: formatKey,
                    key: randomReactKey(),
                    component: ToStringRepresentation,
                    componentProps: {
                        value: format[formatKey]
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