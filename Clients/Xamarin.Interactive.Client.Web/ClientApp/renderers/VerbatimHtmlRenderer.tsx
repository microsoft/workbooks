// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { CodeCellResult } from '../evaluation'
import {
    getFirstRepresentationOfType,
    createComponentRepresentation
} from '../rendering'
import {
    ToStringRepresentationDataTypeName,
    ToStringRepresentationData
 } from './ToStringRenderer'

const RepresentationTypeName = 'Xamarin.Interactive.Representations.VerbatimHtml'

export default function createVerbatimHtmlRepresentation(result: CodeCellResult) {
    if (!getFirstRepresentationOfType(result, RepresentationTypeName))
        return null

    // VerbatimHtml on the C# side intentionally does not expose the HTML data
    // to serialization since we will always send a ToStringRepresentation. This
    // avoids sending duplicate data across the wire.
    //
    // So, grab the ToStringRepresentation and render that as HTML instead.
    const value = getFirstRepresentationOfType<ToStringRepresentationData>(
        result,
        ToStringRepresentationDataTypeName)

    if (!value)
        return null

    return createComponentRepresentation(
        'HTML',
        VerbatimHtmlRenderer,
        {
            value: value.formats[0].value
        }
    )
}

class VerbatimHtmlRenderer extends React.Component<
    { value: string },
    { width: number, height: number }> {
    constructor(props: any) {
        super(props)
        this.state = {
            width: 0,
            height: 0
        }
    }

    render() {
        return (
            <iframe
                className='renderer-VerbatimHtmlRepresentation-container'
                seamless={true}
                sandbox='allow-scripts allow-same-origin'
                srcDoc={this.props.value}
                style={{
                    border: 'none',
                    width: `${this.state.width}px`,
                    height: `${this.state.height}px`
                }}
                onLoad={e => {
                    const iframe = ReactDOM.findDOMNode(this) as any
                    const iframeDoc = iframe.contentWindow.document.documentElement
                    this.setState({
                        width: iframeDoc.scrollWidth,
                        height: iframeDoc.scrollHeight,
                    })
                }}/>
        )
    }
}