//
// Author:
//   Larry Ewing <lewing@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
import * as React from 'react'
import { CodeCellResult } from '../evaluation';
import { ResultRenderer, ResultRendererRepresentation } from '../rendering'
import { WorkbookShellContext, WorkbookShell } from '../components/WorkbookShell';
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
    getRepresentations(result: CodeCellResult) {
        const reps: ResultRendererRepresentation[] = []

        if (!result.valueRepresentations)
            return reps

        for (const value of result.valueRepresentations) {
            if (value.$type !== "Xamarin.Interactive.Representations.ReflectionInteractiveObject")
                continue

            const interactiveObject = value as InteractiveObjectValue
            reps.push({
                displayName: 'Object Properties',
                component: InteractiveObjectRepresentation,
                componentProps: {
                    object: interactiveObject,
                    interact: result.interact,
                }
            })
        }
        return reps
    }
}

class InteractiveObjectRepresentation extends React.Component<InteractiveObjectProps, InteractiveObjectValue> {
    constructor(props: InteractiveObjectProps) {
        super(props);
        this.updateObject = this.updateObject.bind(this);
        const state = { ...props.object }
        this.state = state
    }

    updateObject(updated: any) {
        this.setState(updated)
    }

    render() {
        if (!this.state.isExpanded)
            this.props.interact(this.props.object.handle).then(this.updateObject);

        if (!this.state) {
            return <code>{this.props.object.handle}</code>
        }

        const obj = this.state as any
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

