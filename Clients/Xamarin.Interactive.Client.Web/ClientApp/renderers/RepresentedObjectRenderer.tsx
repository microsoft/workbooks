//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { RepresentedResult } from '../evaluation'
import {
    ResultRenderer,
    ResultRendererRepresentation,
    ResultRendererRepresentationOptions,
    RepresentedObjectState,
    RepresentationMap
} from '../rendering'
import { randomReactKey } from '../utils';
import { WorkbookShellContext, WorkbookShell } from '../components/WorkbookShell';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export default function RepresentedObjectRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === RepresentedObjectRenderer.typeName)
        ? new RepresentedObjectRenderer
        : null
}

interface RepresentedObjectValue {
    $type: string
    representedType: string
    representations: any []
}

interface RepresentedObjectProps {
    object: RepresentedObjectValue
    context: WorkbookShellContext
    state: RepresentedObjectState
}

export class RepresentedObjectRenderer implements ResultRenderer {
    public static readonly typeName = "Xamarin.Interactive.Representations.RepresentedObject"

    constructor() {
        this.buildProps = this.buildProps.bind(this)
    }

    getRepresentations(result: RepresentedResult, context: WorkbookShellContext) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== RepresentedObjectRenderer.typeName)
                continue

            const object = value as RepresentedObjectValue
            reps.push({
                displayName: 'RepresentedObject',
                key: randomReactKey(),
                component: RepresentedObjectRepresentation,
                componentProps: RepresentedObjectRenderer.buildProps(object, context),
            })
        }
        return reps
    }

    buildProps(object: any, context: WorkbookShellContext): RepresentedObjectProps {
        return RepresentedObjectRenderer.buildProps(object, context);
    }

    public static buildProps(object: any, context: WorkbookShellContext): RepresentedObjectProps {
        const result = {
            valueRepresentations: object.representations,
            type: object.$type
        }
        const reps = context.rendererRegistry
            .getRenderers(result)
            .map(r => r.getRepresentations(result, context))

        const flatReps = reps.length === 0
            ? []
            : reps.reduce((a, b) => a.concat(b))

        const mapReps: RepresentationMap<string, ResultRendererRepresentation> = {}
        flatReps.map((r, i) => {
            mapReps[r.key] = r
        })

        return {
            object: object,
            context: context,
            state: {
                representations: mapReps,
                selectedRepresentation: flatReps[0].key
            },
        }
    }
}

export class RepresentedObjectRepresentation extends React.Component<RepresentedObjectProps, RepresentedObjectState> {
    constructor(props: RepresentedObjectProps) {
        super(props)
        this.state = props.state
        this.interact = this.interact.bind (this)
    }
    protected async interact(key: string)
    {
        const state = this.state
        let index = -1

        var rep = this.state.representations[key]
        if (rep && rep.interact) {
            var newRep = await rep.interact (rep)
            if (rep !== newRep) {
                this.state.representations[key] = newRep
                this.setState({
                    selectedRepresentation: key
                })
            }
        }
    }
    render() {
        const dropdownOptions = Object.keys(this.state.representations).length > 1
            ? Object.keys(this.state.representations).map(key => {
                return {
                    key: key,
                    text: this.state.representations[key].displayName
                }
            })
            : null

        let repElem = null
        if (this.state.selectedRepresentation) {
            const rep = this.state.representations[this.state.selectedRepresentation]
            rep.interact && this.interact(this.state.selectedRepresentation).then(r => console.log("updated"))

            repElem = <rep.component key={"RepresentedObject:"+rep.key} {...rep.componentProps} />
        }

        return (
            <div
                className="CodeCell-result">
                <div className="CodeCell-result-renderer-container">
                    {repElem}
                </div>
                {dropdownOptions && <Dropdown

                    options={dropdownOptions}
                    defaultSelectedKey={this.state.selectedRepresentation}
                    onChanged={item => {
                        this.setState({ selectedRepresentation: item.key as string })
                    }} />}
            </div>
        )
    }
}