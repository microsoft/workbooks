// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { CodeCellResult } from '../evaluation'
import {
    getRepresentationsOfType,
    createContainerRepresentation,
    createComponentRepresentation
} from '../rendering'

import './ImageRenderer.scss'

const RepresentationName = 'Xamarin.Interactive.Representations.Image'

interface ImageData {
    $type: string
    format: string
    data: {
        $type: 'System.Byte[]'
        $value: string
    }
    width: number
    height: number
    scale: number
}

export default function createImageRepresentation(result: CodeCellResult) {
    return createContainerRepresentation(
        'Image',
        getRepresentationsOfType<ImageData>(result, RepresentationName)
            .filter(image => image && image.format && image.data)
            .map(image => createComponentRepresentation(
                image.format,
                ImageRepresentation,
                image)))
}

class ImageRepresentation extends React.Component<ImageData> {
    render() {
        const image = this.props;
        const src = image.format === 'Uri'
            ? atob(image.data.$value)
            : `data:${image.format};base64,${image.data.$value}`
        return <img src={src}/>
    }
}