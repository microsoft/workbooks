//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { List, Record } from 'immutable'
import { CodeCellResult } from './evaluation'
import { randomReactKey } from './utils'

export const enum ResultRendererRepresentationOptions {
    None = 0,

    /**
    * The representation will always be provided the expanded render
    * targets and will not be collapsible at all.
    */
    ForceExpand = 1,

    /**
     * The representation is collapsible, but will be expanded by default.
     */
    ExpandedByDefault = 2,

    /**
     * The representation is collapsible and will be collapsed by default if
     * it is the only or initially selected renderer, and otherwise expanded
     * automatically when selected from the menu.
     */
    ExpandedFromMenu = 4,

    /**
     * The display name of the representation will be suppressed in the
     * representation button label and shown only in the button's menu,
     * but only if all other representations also have the hint.
     */
    SuppressDisplayNameHint = 8
}

export interface Representation {
    readonly key: React.Key
    readonly displayName: string
    readonly order?: number
    readonly component?: any
    readonly componentProps?: {}
    readonly children?: List<Representation>
}

export function createComponentRepresentation(
    displayName: string,
    component: any,
    componentProps?: {},
    order?: number) {
    return {
        key: randomReactKey(),
        displayName,
        component,
        componentProps,
        order
    }
}

export function createContainerRepresentation(
    displayName: string,
    children: Representation[],
    order?: number) {
    return {
        key: randomReactKey(),
        displayName,
        children: children && List<Representation>(children),
        order
    }
}

export function getRepresentationsOfType<T = {}>(result: CodeCellResult, typeName: string): T[] {
    const reps = []
    if (result && result.resultRepresentations) {
        for (const representation of result.resultRepresentations) {
            if (representation && representation.$type === typeName)
                reps.push(representation)
        }
    }
    return reps
}

export function getFirstRepresentationOfType<T = {}>(result: CodeCellResult, typeName: string): T | null {
    const reps = getRepresentationsOfType<T>(result, typeName)
    return reps && reps.length > 0
        ? reps[0]
        : null
}

export type RepresentationFactory = (result: CodeCellResult) => Representation | null

export class RepresentationRegistry {
    private factories: List<RepresentationFactory> = List()

    constructor(...factories: RepresentationFactory[]) {
        this.register(...factories)
    }

    register(...factories: RepresentationFactory[]) {
        this.factories = this.factories.push(...factories)
    }

    getRepresentations(result: CodeCellResult): Representation | null {
        let representations = List<Representation>()

        if (result.isNullResult)
            representations = representations.push(createNullRepresentation())
        else
            this.factories.forEach(factory => {
                const representation = factory && factory(result)
                if (representation && (representation.component ||
                        (representation.children && representation.children.size > 0)))
                    representations = representations.push(representation)
            })

        if (representations.size == 0)
            return null

        return {
            key: randomReactKey(),
            displayName: '_root',
            children: representations
        }
    }
}

import createNullRepresentation from './renderers/NullRenderer'
import createToStringRepresentation from './renderers/ToStringRenderer'
import createImageRepresentation from './renderers/ImageRenderer'
import createVerbatimHtmlRepresentation from './renderers/VerbatimHtmlRenderer'
import createCalenderRepresentation from './renderers/CalendarRenderer'
import createTypeRepresentation from './renderers/TypeSystemRenderers'

export function createDefaultRegistry() {
    return new RepresentationRegistry(
        // More exciting and specific renderers should be first
        createCalenderRepresentation,
        createImageRepresentation,
        createVerbatimHtmlRepresentation,
        createTypeRepresentation,
        createToStringRepresentation)
}

export function createDesignRegistry() {
    return createDefaultRegistry()
}