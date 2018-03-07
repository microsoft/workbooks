//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { CodeCellResult } from '../evaluation'
import {
    ResultRenderer,
    ResultRendererRepresentation
} from '../rendering'
import {
    Image,
    IImageProps,
    ImageFit
} from 'office-ui-fabric-react/lib/Image'

import './ImageRenderer.scss'
import { randomReactKey } from '../utils';

export default function ImageRendererFactory(result: CodeCellResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === "Xamarin.Interactive.Representations.Image")
        ? new ImageRenderer
        : null
}

interface ImageValue {
    $type: string
    format: string
    data: string
    width: number
    height: number
    scale: number
}

class ImageRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== "Xamarin.Interactive.Representations.Image")
                continue

            const image = (value as ImageValue)
            if (!image.format)
                continue;

            reps.push({
                displayName: 'Image',
                key: randomReactKey(),
                component: ImageRepresentation,
                componentProps: {
                    value: "Image",
                    image: image
                }
            })
        }

        return reps
    }
}

class ImageRepresentation extends React.Component<{ value: string, image: ImageValue }> {
    render() {
        const image = this.props.image;
        const size = image.width > 0 ? { width: image.width, height: image.height} : {}
        const imageProps: IImageProps = {
            imageFit: ImageFit.contain,
            maximizeFrame: true
        }
        const src = image.format === "uri" ? atob(image.data) : `data:${image.format};base64,${image.data}`

        // FIXME: Fabric's <Image> is causing some strange clipping with at least SVG
        // return <Image src={src} {...imageProps} {...size} />
        return <img className='renderer-ImageRepresentation-container' src={src} />
    }
}