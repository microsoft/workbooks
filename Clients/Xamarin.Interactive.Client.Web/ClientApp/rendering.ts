//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { CodeCellResult } from './evaluation'
import { NullRenderer } from './renderers/NullRenderer'
import { MenuItem } from './components/DropDownMenu';

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

export interface ResultRendererRepresentationProps {
    shortDisplayName: string
    order?: number
    options?: ResultRendererRepresentationOptions
}

export interface ResultRendererRepresentationState {
    value: any | null
}

export abstract class ResultRendererRepresentation
    extends React.Component<ResultRendererRepresentationProps, ResultRendererRepresentationState>
    implements MenuItem {
    getMenuLabel() {
        return this.props.shortDisplayName
    }
}

export interface ResultRenderer {
    getRepresentations(result: CodeCellResult): ResultRendererRepresentation[]
}

export type ResultRendererFactory = (result: CodeCellResult) => ResultRenderer | null

export class ResultRendererRegistry {
    private rendererFactories: ResultRendererFactory[] = []

    register(factory: ResultRendererFactory) {
        this.rendererFactories.push(factory)
    }

    getRenderers(result: CodeCellResult): ResultRenderer[] {
        return <ResultRenderer[]>this.rendererFactories
            .map(f => f(result))
            .filter(f => f !== null)
    }
}