//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import * as React from 'react'
import { CodeCellResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'

export default function InteractiveObjectRendererFactory(result: CodeCellResult) {
    return result.resultRepresentations &&
        result.resultRepresentations.some(r => r.$type === "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
        ? new InteractiveObjectRenderer
        : null
}

interface InteractiveObjectValue {
    $type: string
    handle: string
}

class InteractiveObjectRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.resultRepresentations)
            return reps

        for (const value of result.resultRepresentations) {
            if (value.$type !== "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
                continue

            const interactiveObject = value as InteractiveObjectValue;

            reps.push({
                displayName: 'Object Properties',
                component: InteractiveObjectRepresentation,
                componentProps: {
                    handle: interactiveObject.handle
                }
            })
        }

        return reps
    }
}

class InteractiveObjectRepresentation extends React.Component<{ handle: string }> {
    render() {
        return <code>{this.props.handle}</code>
    }
}
