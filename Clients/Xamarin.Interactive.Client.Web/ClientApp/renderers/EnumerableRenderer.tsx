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
    ResultRendererRepresentation
} from '../rendering'
import { randomReactKey } from '../utils';
import { WorkbookShellContext } from '../components/WorkbookShell';
import { RepresentedObjectRepresentation, RepresentedObjectRenderer } from './RepresentedObjectRenderer';

export default function EnumerableRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === EnumerableRenderer.typeName)
        ? new EnumerableRenderer
        : null
}

interface EnumerableValue {
    $type: string
    handle: string
    slice: any[]
}

interface EnumerableProps {
    object: EnumerableValue
    context: WorkbookShellContext
    sliceProps: any
}

class EnumerableRenderer implements ResultRenderer {
    static typeName = "Xamarin.Interactive.Representations.InteractiveEnumerable"
    constructor() {
        this.interact = this.interact.bind(this)
        this.buildProps = this.buildProps.bind (this)
    }
    getRepresentations(result: RepresentedResult, context: WorkbookShellContext) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== EnumerableRenderer.typeName)
                continue

            const object = (value as EnumerableValue)

            reps.push({
                displayName: 'Enumerable',
                key: randomReactKey(),
                component: EnumerableRepresentation,
                componentProps: this.buildProps(object, context),
                interact: this.interact
            })
        }

        return reps
    }
    async interact(rep: ResultRendererRepresentation):
        Promise<ResultRendererRepresentation>
    {
        const props = rep.componentProps as EnumerableProps

        if (!props.context)
            return rep;

        if (props.object.slice)
            return rep;

        const obj = await props.context.session.interact(props.object.handle)
        return ({
            ...rep,
            componentProps: this.buildProps (obj, props.context),
            interact: undefined
        })
    }
    buildProps(object: any, context: WorkbookShellContext): EnumerableProps
    {
        let memberProps: any = {}
        const slice = object.slice || []
        Object.keys(slice).map((key, i) => {
            memberProps[i] = RepresentedObjectRenderer.buildProps(slice[i], context);
        });
        return {
            object: object,
            context: context,
            sliceProps: memberProps,
        }
    }
}

class EnumerableRepresentation extends React.Component<EnumerableProps> {
    render() {
        const slice = this.props.object.slice || []
        const sliceProps = this.props.sliceProps || []
        return (<ul key={this.props.object.handle}>
            {Object.keys(slice).map((sliceKey, i) => {
                const props = sliceProps[i]
                return <li key={i}><b>{i}</b>: <RepresentedObjectRepresentation {...props} /></li>
            })}
        </ul>)
    }
}