// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import * as ReactDOM from 'react-dom'
import { randomReactKey, classNames } from '../utils';

import { TransitionGroup } from './TransitionGroup';

import './Dropdown.scss'

export interface DropdownOption {
    key: React.Key
    text: string
}

export interface DropdownProps {
    className?: string
    options: DropdownOption[]
    defaultSelectedKey?: React.Key
    onChanged?: (selectedKey: React.Key) => void
}

interface DropdownState {
    selectedKey: React.Key
    menuState: 'hidden' | 'visible'
}

export class Dropdown extends React.PureComponent<DropdownProps, DropdownState> {
    private buttonRef?: HTMLButtonElement
    private menuRef?: HTMLUListElement

    constructor(props: DropdownProps) {
        super(props)

        this.state = {
            selectedKey: props.defaultSelectedKey || props.options [0].key,
            menuState: 'hidden'
        }

        this.onDocumentMouseDown = this.onDocumentMouseDown.bind(this)
    }

    private onDocumentMouseDown(e: MouseEvent) {
        let hitTest = false

        for (const elem of [this.menuRef, this.buttonRef]) {
            if (elem) {
                const rect = elem.getBoundingClientRect()
                if (e.pageX >= rect.left &&
                    e.pageX <= rect.right &&
                    e.pageY >= rect.top &&
                    e.pageY <= rect.bottom) {
                    hitTest = true
                    break
                }
            }
        }

        if (!hitTest)
            this.hideMenu()
    }

    private toggleMenu() {
        if (this.state.menuState === 'visible')
            this.hideMenu()
        else
            this.showMenu()
    }

    private showMenu() {
        this.setState({ menuState: 'visible' })
        document.addEventListener('mousedown', this.onDocumentMouseDown)
    }

    private hideMenu() {
        this.setState({ menuState: 'hidden' })
        document.removeEventListener('mousedown', this.onDocumentMouseDown)
    }

    private selectItem(key: React.Key) {
        this.setState({ selectedKey: key })
        this.hideMenu()

        if (this.props.onChanged)
            this.props.onChanged(key)
    }

    render() {
        const { menuState, selectedKey } = this.state

        const selectedOption = this.props.options.find(o => o.key === selectedKey)
        if (!selectedOption)
            return false

        const menuId = randomReactKey()

        return (
            <div className={classNames(
                'Dropdown',
                { 'DropdownActive': menuState === 'visible' },
                this.props.className)}>
                <button
                    className='DropdownButton'
                    role='combobox'
                    aria-haspopup='true'
                    aria-expanded={menuState === 'visible'}
                    aria-owns={menuId}
                    ref={node => this.buttonRef = node as HTMLButtonElement}
                    onClick={e => this.toggleMenu()}>
                    <div className='DropdownButtonLabel'>{selectedOption.text}</div>
                    <div className='DropdownColumnRight DropdownButtonExpander'>
                        <svg width="0.8em" height="0.8em" viewBox="0 0 16 16">
                            <g strokeWidth="1" fill="none">
                                <polyline id="Path" points="2 6 7.80645161 11.5 14 6"/>
                            </g>
                        </svg>
                    </div>
                </button>
                <TransitionGroup visible={menuState === 'visible'}>
                    <ul
                        id={menuId}
                        role='listbox'
                        ref={node => this.menuRef = node as HTMLMenuElement}>
                        {this.props.options.map((option, i) => {
                            const isSelected = option.key === selectedOption.key
                            return <li
                                key={i}
                                className={isSelected ? 'DropdownSelectedItem' : ''}
                                role='option'
                                aria-selected={isSelected}
                                onClick={e => this.selectItem(option.key)}>
                                <div>{option.text}</div>
                                <div className='DropdownColumnRight'></div>
                            </li>
                        })}
                    </ul>
                </TransitionGroup>
            </div>
        )
    }
}