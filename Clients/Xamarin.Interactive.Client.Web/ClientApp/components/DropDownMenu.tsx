//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';

export interface MenuItem {
    getMenuLabel(): string
}

interface DropDownMenuProps {
    items: MenuItem[]
    noSelectionLabel?: string
    initiallySelectedIndex?: number
    selectionChanged?: (index: number, item: MenuItem) => void
}

interface DropDownMenuState {
    selectedItemIndex: number
}

export class DropDownMenu extends React.Component<DropDownMenuProps, DropDownMenuState> {
    constructor(props: DropDownMenuProps) {
        super(props)
        this.state = {
            selectedItemIndex: this.props.initiallySelectedIndex === undefined
                ? -1
                : this.props.initiallySelectedIndex
        }
    }

    selectItem(index: number) {
        this.setState({
            selectedItemIndex: index
        })

        if (this.props.selectionChanged)
            this.props.selectionChanged(index, this.props.items[index])
    }

    get selectedItemIndex(): number {
        return this.state.selectedItemIndex
    }

    render() {
        let buttonLabel = this.props.noSelectionLabel || 'Select Item'
        if (this.state.selectedItemIndex >= 0)
            buttonLabel = this.props.items[this.state.selectedItemIndex].getMenuLabel()

        return (
            <div className='dropdown'>
                <button
                    className='btn btn-default dropdown-toggle'
                    type='button'
                    data-toggle='dropdown'>
                    {buttonLabel}&nbsp;
                    <span className='caret'></span>
                </button>
                <ul className='dropdown-menu'>
                    {this.props.items.map((item, i) =>
                        <li key={i.toString()}>
                            <a
                                className='dropdown-item'
                                href='#'
                                onClick={e => this.selectItem(i)}>
                                {item.getMenuLabel()}
                            </a>
                        </li>
                    )}
                </ul>
            </div>
        )
    }
}