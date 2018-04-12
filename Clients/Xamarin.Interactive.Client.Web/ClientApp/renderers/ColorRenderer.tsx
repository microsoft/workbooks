//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { RepresentedResult } from '../evaluation'
import {
    ResultRenderer,
    ResultRendererRepresentation
} from '../rendering'
import {
    ColorPicker
} from 'office-ui-fabric-react/lib/ColorPicker';

import { randomReactKey } from '../utils';

export default function ColorRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === "Xamarin.Interactive.Representations.Color")
        ? new ColorRenderer
        : null
}

interface ColorValue {
    $type: string
    $id: string
    Colorspace: number
    Red: number
    Blue: number
    Green: number
    Alpha: number
}

class ColorRenderer implements ResultRenderer {
    public static readonly typeName = "Xamarin.Interactive.Representations.Color"

    getRepresentations(result: RepresentedResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== ColorRenderer.typeName)
                continue

            const color = (value as ColorValue)
            if (!color.$type)
                continue

            reps.push({
                displayName: 'Color (compact)',
                key: randomReactKey(),
                component: ColorRepresentation,
                componentProps: {
                    value: "Color (compact)",
                    color: color,
                    compact: true
                }
            })
            reps.push({
                displayName: 'Color',
                key: randomReactKey(),
                component: ColorRepresentation,
                componentProps: {
                    value: "Color",
                    color: color
                }
            })
        }

        return reps
    }
}

class ColorRepresentation extends React.Component<{ value: string, color: ColorValue, compact?: boolean }> {
    render() {
        const format = (component: number) => Math.trunc (Math.max (0, Math.min (component * 255, 255)))

        const color = this.props.color
        const stringColor = `rgba(${format(color.Red)},${format(color.Green)},${format(color.Blue)},${color.Alpha})`
        if (this.props.compact)
            return <div style={{ backgroundColor: stringColor, width: "2em", height: "2em", float: "left" }} className='renderer-ColorRepresentation-compact'/>
        return <ColorPicker color={stringColor} />
    }
}