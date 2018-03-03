//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { CodeCellResult } from '../evaluation';
import { ResultRenderer } from '../rendering'

export default function ToStringRendererFactory(result: CodeCellResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.hasOwnProperty('$toString'))
        ? new ToStringRenderer
        : null
}

class ToStringRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        return [{
            component: ToStringRepresentation,
            displayName: 'NUMBER1',
        }, {
            component: ToStringRepresentation,
            displayName: 'NUMBER2',
        }]
    }
}

class ToStringRepresentation extends React.Component<{}, { x: number }> {
    constructor(props: any) {
        super(props)
        this.state = { x: 0 }
        console.log('new ToStringRepresentation(%O)', props)
    }

    render() {
        return <PrimaryButton
            onClick={e => this.setState({ x: this.state.x + 1 })}>
            {this.state.x || 'click me'}
        </PrimaryButton>
    }
}