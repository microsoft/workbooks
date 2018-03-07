//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import * as React from 'react'
import { CodeCellResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'
import {
    GroupedList,
    IGroup
  } from 'office-ui-fabric-react/lib/components/GroupedList/index';
  import { IColumn } from 'office-ui-fabric-react/lib/DetailsList';
  import { DetailsRow } from 'office-ui-fabric-react/lib/components/DetailsList/DetailsRow';
  import {
    FocusZone
  } from 'office-ui-fabric-react/lib/FocusZone';
  import {
    Selection,
    SelectionMode,
    SelectionZone
  } from 'office-ui-fabric-react/lib/utilities/selection/index';
import { randomReactKey } from '../utils';
import { WorkbookSession } from '../WorkbookSession';

export default function InteractiveObjectRendererFactory(result: CodeCellResult) {
    return result.valueRepresentations &&
        result.valueRepresentations.some(r => r.$type === "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
        ? new InteractiveObjectRenderer
        : null
}

interface InteractiveObjectProps {
    object: InteractiveObjectValue
    interact: (handle: string) => Promise<any>
}

interface InteractiveObjectValue {
    $type: string
    handle: string
    isExpanded: boolean | null
}

class InteractiveObjectRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult, session: WorkbookSession) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
                continue

            const interactiveObject = value as InteractiveObjectValue
            reps.push({
                displayName: 'Object Properties',
                key: randomReactKey(),
                component: InteractiveObjectRepresentation,
                componentProps: {
                    object: interactiveObject,
                    session: session,
                },
                interact: this.interact
            })
        }
        return reps
    }
    async interact(rep: ResultRendererRepresentation):
        Promise<ResultRendererRepresentation>
    {
        const props = rep.componentProps as {
            object: InteractiveObjectValue,
            session: WorkbookSession
        }

        if (!props.session)
            return rep;

        if (props.object.isExpanded)
            return rep;

        const obj = await props.session.interact(props.object.handle)
        return ({
            ...rep,
            componentProps: {
                object: obj,
                session: props.session
            },
            interact: undefined
        })
    }
}

class InteractiveObjectRepresentation extends React.Component<InteractiveObjectProps, {}> {
    constructor(props: InteractiveObjectProps) {
        super(props);
    }

    render() {
        const obj = this.props.object as any
        return (
            <ul>
                {Object.keys(obj).map(key => {
                    return <li key={key}><b>"{key}":</b> {obj[key].toString()}</li>
                }
                )}
            </ul>
        )
    }
}

