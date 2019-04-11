// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { createComponentRepresentation } from '../rendering'

export default function createNullRepresentation() {
    return createComponentRepresentation('null', NullRenderer)
}

class NullRenderer extends React.Component {
    render() {
        return <code>null</code>
    }
}