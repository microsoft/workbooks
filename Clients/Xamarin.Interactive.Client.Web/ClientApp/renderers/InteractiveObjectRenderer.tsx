//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import * as React from 'react'
import { RepresentedResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'
import { randomReactKey } from '../utils';
import { WorkbookShellContext } from '../components/WorkbookShell';
import {RepresentedObjectRenderer, RepresentedObjectRepresentation } from './RepresentedObjectRenderer';

export default function InteractiveObjectRendererFactory(result: RepresentedResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
        ? new InteractiveObjectRenderer
        : null
}

interface InteractiveObjectProps {
    object: InteractiveObjectValue
    context: WorkbookShellContext
    memberProps: any
}

interface InteractiveObjectValue {
    $type: string
    handle: string
    isExpanded: boolean | null
}

class InteractiveObjectRenderer implements ResultRenderer {
    public static readonly typeName = "Xamarin.Interactive.Representations.ReflectionInteractiveObject"

    constructor() {
        this.interact = this.interact.bind(this)
        this.buildProps = this.buildProps.bind(this)
    }

    getRepresentations(result: RepresentedResult, context: WorkbookShellContext) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== InteractiveObjectRenderer.typeName)
                continue

            const interactiveObject = value as InteractiveObjectValue
            reps.push({
                displayName: 'Object Properties',
                key: randomReactKey(),
                component: InteractiveObjectRepresentation,
                componentProps: this.buildProps (value, context),
                interact: this.interact
            })
        }
        return reps
    }

    async interact(rep: ResultRendererRepresentation):
        Promise<ResultRendererRepresentation>
    {
        const props = rep.componentProps as InteractiveObjectProps

        if (!props.context)
            return rep;

        if (props.object.isExpanded)
            return rep;

        const obj = await props.context.session.interact(props.object.handle)
        return ({
            ...rep,
            componentProps: this.buildProps(obj, props.context),
            interact: undefined
        })
    }

    buildProps(object: any, context: WorkbookShellContext): InteractiveObjectProps
    {
        let memberProps: any = {}
        Object.keys(object).map(key => {
            memberProps[key] = RepresentedObjectRenderer.buildProps(object[key], context);
        });
        return {
            object: object,
            context: context,
            memberProps: memberProps
        }
    }
}

class InteractiveObjectRepresentation extends React.Component<InteractiveObjectProps, {}> {
    constructor(props: InteractiveObjectProps) {
        super(props);
    }

    render() {
        const obj = this.props.object as any
        return (
            <ul key={this.props.object.handle}>
                {Object.keys(obj).map(key => {
                    var member = obj[key]
                    const memberProps = this.props.memberProps[key];
                    const ro = member.$type === RepresentedObjectRenderer.typeName

                    if (ro)
                        return <li key={key}><b>"{key}":</b> <RepresentedObjectRepresentation {...memberProps} /></li>
                    else
                        return <li key={key}><b>"{key}":</b> {member.toString()}</li>
                })}
            </ul>
        )
    }
}