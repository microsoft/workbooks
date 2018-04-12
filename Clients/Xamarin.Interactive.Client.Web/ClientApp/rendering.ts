//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'

import { RepresentedResult } from './evaluation'
import { WorkbookShellContext } from './components/WorkbookShell';
import { extname } from 'path';

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

export interface ResultRenderer {
    getRepresentations(result: RepresentedResult, context?: WorkbookShellContext): ResultRendererRepresentation[]
}

export type ResultRendererFactory = (result: RepresentedResult) => ResultRenderer | null

export interface ResultRendererRepresentation {
    displayName: string
    key: string
    component: any
    componentProps?: {}
    order?: number
    options?: ResultRendererRepresentationOptions,
    interact?(rep: ResultRendererRepresentation): Promise<ResultRendererRepresentation>
    renderComponent?(rep: ResultRendererRepresentation): any
}

export interface RepresentationMap<K,V> {
    [K: string]: V
}

export interface RepresentedObjectState {
    representations: RepresentationMap<string, ResultRendererRepresentation>
    selectedRepresentation: string
}
