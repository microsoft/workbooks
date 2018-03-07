//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { CodeCellResult } from '../evaluation'
import {
    ResultRenderer,
    ResultRendererRepresentation
} from '../rendering'
import { randomReactKey } from '../utils';

const xirType = 'Xamarin.Interactive.Representations.VerbatimHtml'

export default function VerbatimHtmlRendererFactory(result: CodeCellResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === xirType)
        ? new VerbatimHtmlRenderer
        : null
}

interface VerbatimHtmlValue {
    $toString: string
}

class VerbatimHtmlRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type === xirType)
                reps.push({
                    displayName: 'HTML',
                    key: randomReactKey(),
                    component: VerbatimHtmlRepresentation,
                    componentProps: {
                        value: value as VerbatimHtmlValue
                    }
                })
        }

        return reps
    }
}

class VerbatimHtmlRepresentation extends React.Component<
    { value: VerbatimHtmlValue },
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
                srcDoc={this.props.value.$toString}
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