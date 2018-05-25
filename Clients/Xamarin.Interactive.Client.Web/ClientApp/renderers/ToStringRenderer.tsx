// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { CodeCellResult } from '../evaluation'
import {
    Representation,
    getFirstRepresentationOfType,
    createContainerRepresentation,
    createComponentRepresentation
} from '../rendering'

export const ToStringRepresentationDataTypeName = 'Xamarin.Interactive.Representations.ToStringRepresentation'

export interface ToStringRepresentationData {
    $type: string
    formats: {
        name: string,
        value: string
    }[]
}

export default function createToStringRepresentation(result: CodeCellResult): Representation | null {
    const value = getFirstRepresentationOfType<ToStringRepresentationData>(
        result,
        ToStringRepresentationDataTypeName)

    if (!value)
        return null

    const rr = createContainerRepresentation(
        'ToString',
        value.formats.map(format => createComponentRepresentation(
            format.name,
            ToStringRenderer,
            { value: format.value })))

        console.log("CREATED REP: %O", rr)

        return rr
}

class ToStringRenderer extends React.Component<{ value: string }> {
    render() {
        return <code>{this.props.value}</code>
    }
}