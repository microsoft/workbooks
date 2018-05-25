// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { classNames } from '../utils';

import './TransitionGroup.scss'

export interface TransitionProps {
    visible: boolean
}

export class TransitionGroup extends React.PureComponent<TransitionProps> {
    constructor(props: TransitionProps) {
        super(props)
        this.state = {}
    }
    render() {
        return (
            <div ref={elem => transitionHeight(elem, this.props.visible, true)}>
                {this.props.children}
            </div>
        )
    }
}

export function transitionHeight(
    elem: HTMLElement | React.ReactInstance | null,
    visible: boolean,
    applyMaxHeight?: boolean) {
    if (!elem)
        return undefined

    if (!(elem instanceof HTMLElement))
        elem = ReactDOM.findDOMNode(elem) as HTMLElement

    if (!(elem instanceof HTMLElement))
        return undefined

    if (applyMaxHeight)
        elem.style.maxHeight = elem.scrollHeight + 'px'

    elem.classList.add('TransitionGroup')

    if (visible)
        elem.classList.remove('TransitionGroupHidden')
    else
        elem.classList.add('TransitionGroupHidden')
}