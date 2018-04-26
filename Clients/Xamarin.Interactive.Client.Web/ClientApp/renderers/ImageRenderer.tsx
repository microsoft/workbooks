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
    ResultRendererRepresentation,
    getRepresentationsOfType,
    getFirstRepresentationOfType
} from '../rendering'
import {
    Image,
    IImageProps,
    ImageFit
} from 'office-ui-fabric-react/lib/Image'

import './ImageRenderer.scss'

const RepresentationName = 'Image'

export default function ImageRendererFactory(result: CodeCellResult) {
    return getFirstRepresentationOfType(result, RepresentationName)
        ? new ImageRenderer
        : null
}

interface ImageData {
    $type: string
    format: string
    data: {
        $type: 'Byte[]'
        $value: string
    }
    width: number
    height: number
    scale: number
}

class ImageRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const reps = []

        for (const image of getRepresentationsOfType<ImageData>(result, RepresentationName)) {
            if (!image.format || !image.data)
                continue;

            reps.push({
                displayName: 'Image',
                component: ImageRepresentation,
                componentProps: {
                    value: 'Image',
                    image: image
                }
            })
        }

        return reps
    }
}

class ImageRepresentation extends React.Component<{ value: string, image: ImageData }> {
    render() {
        const image = this.props.image;
        const size = image.width > 0 ? { width: image.width, height: image.height} : {}
        const imageProps: IImageProps = {
            imageFit: ImageFit.contain,
            maximizeFrame: true
        }
        const src = image.format === 'Uri'
            ? atob(image.data.$value)
            : `data:${image.format};base64,${image.data.$value}`

        // FIXME: Fabric's <Image> is causing some strange clipping with at least SVG
        // return <Image src={src} {...imageProps} {...size} />
        return <img className='renderer-ImageRepresentation-container' src={src} />
    }
}