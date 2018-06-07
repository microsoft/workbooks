// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { randomReactKey, classNames } from '../utils'
import { Representation } from '../rendering'
import { Dropdown } from './Dropdown';
import { TransitionGroup, transitionHeight } from './TransitionGroup'

import './RepresentationSelector.scss'
import { List } from 'immutable';

function findPathToRepresentation(
    representation: Representation | null,
    predicate: (representation: Representation) => boolean): Representation[] | null {
    if (!representation)
        return null

    if (predicate(representation))
        return [representation]

    if (!representation.children)
        return null

    for (let i = 0; i < representation.children.size; i++) {
        const path = findPathToRepresentation(
            representation.children.get(i),
            predicate)
        if (path) {
            path.unshift(representation)
            return path
        }
    }

    return null
}

function findPathToRepresentationByKey(
    representation: Representation,
    key: React.Key): Representation[] | null {
    return findPathToRepresentation(
        representation,
        representation => representation.key === key)
}

interface RepresentationSelectorProps {
    rootRepresentation: Representation
    onRenderRepresentation?: (representation: Representation) => void
}

interface RepresentationSelectorState {
    selectedPath: Representation[] | null
}

export class RepresentationSelector extends React.PureComponent<
    RepresentationSelectorProps, RepresentationSelectorState> {
    constructor(props: RepresentationSelectorProps) {
        super(props)

        this.onSelectedKeyChanged = this.onSelectedKeyChanged.bind(this)

        this.state = {
            selectedPath: this.selectKey(null)
        }

       setTimeout(() => this.raiseOnRepresentationSelected(), 0)
    }

    private selectKey(selectedKey: React.Key | null): Representation[] | null {
        // try to find a representation by key if we have one,
        // falling back to the first one that is renderable if
        // not or the key is not found
        return (selectedKey &&
            findPathToRepresentationByKey(
                this.props.rootRepresentation,
                selectedKey)) ||
            findPathToRepresentation(
                this.props.rootRepresentation,
                representation => !!representation.component)
    }

    private raiseOnRepresentationSelected() {
        const { selectedPath } = this.state
        if (selectedPath && selectedPath.length > 0 && this.props.onRenderRepresentation)
            this.props.onRenderRepresentation(selectedPath[selectedPath.length - 1])
    }

    private onSelectedKeyChanged(selectedKey: React.Key) {
        this.setState({ selectedPath: this.selectKey(selectedKey) })
    }

    componentDidUpdate() {
        this.raiseOnRepresentationSelected()
    }

    private renderDropdowns(
        dropdowns: JSX.Element[],
        selectedPath: Representation[],
        representation: Representation) {
        if (!representation || !representation.children || representation.children.size === 0)
            return

        const options = representation.children.map(representation => {
            return {
                key: representation!.key,
                text: representation!.displayName
            }
        }).toArray()

        const inSelectionPath = selectedPath[0].key === representation.key

        if (options.length > 1)
            dropdowns.push(<Dropdown
                className={classNames({'SelectedRepresentation': inSelectionPath})}
                ref={elem => transitionHeight(elem, inSelectionPath)}
                key={dropdowns.length}
                options={options}
                defaultSelectedKey={inSelectionPath && selectedPath.length > 1 ? selectedPath[1].key : undefined}
                onChanged={this.onSelectedKeyChanged}/>)

        representation.children.forEach(child => {
            this.renderDropdowns(
                dropdowns,
                selectedPath.slice(1),
                child!)
        })
    }

    render() {
        if (!this.state.selectedPath)
            return false

        const dropdowns: JSX.Element[] = []
        this.renderDropdowns(
            dropdowns,
            this.state.selectedPath,
            this.props.rootRepresentation)

        return (
            <div className='RepresentationSelector'>
                {dropdowns}
            </div>
        )
    }
}

function randomInteger(min: number, max: number) {
    return Math.floor(Math.random() * (max - min + 1)) + min
}

export class TestRepresentationSelector extends React.PureComponent {
    private rootRepresentation: Representation = this.createRepresentation('', 0)!

    private createRepresentation(displayName: string, depth: number): Representation | null {
        if (depth > 4)
            return null

        const representation: Representation = {
            key: randomReactKey(),
            displayName: displayName,
            children: List<Representation>(
                new Array(randomInteger(depth < 1 ? 1 : 0, 5))
                    .fill(undefined)
                    .map((_, i) => this.createRepresentation(
                        `${displayName && `${displayName}.`}${i}`,
                        depth + 1))
                    .filter(representation => representation))
        }

        if (!representation.children || representation.children.size === 0)
            (representation as any).component = <div>Hi</div>

        return representation
    }

    render() {
        return (
            <RepresentationSelector rootRepresentation={this.rootRepresentation}/>
        )
    }
}