//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'

import { Clickable } from './Clickable'

import './AbortEvaluationButton.scss'

export class AbortEvaluationButton extends Clickable<HTMLButtonElement> {
    render() {
        return (
            <button
                className='react-AbortEvaluationButton'
                type='button'
                onClick={this.props.onClick}>
                <div className='spinner'/>
            </button>
        )
    }
}