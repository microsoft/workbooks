//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { CodeCellResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'

export default function ToStringRendererFactory(result: CodeCellResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$toString)
        ? new ToStringRenderer
        : null
}

interface ToStringValue {
    $type: string
    $toString: {
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
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$toString) {
                for (const format of (value as ToStringValue).$toString[0].formats) {
                    const formatKey = Object.keys(format)[0] as string
                    reps.push({
                        displayName: formatKey,
                        component: ToStringRepresentation,
                        componentProps: {
                            value: format[formatKey]
                        }
                    })
                }
            }
        }

        return reps
    }
}

class ToStringRepresentation extends React.Component<{ value: string }> {
    render() {
        console.log(this.props)
        return <code>{this.props.value}</code>
    }
}