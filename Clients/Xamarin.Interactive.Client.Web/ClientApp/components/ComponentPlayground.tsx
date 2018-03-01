import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import { AbortEvaluationButton } from './AbortEvaluationButton';

export class ComponentPlayground extends React.Component<RouteComponentProps<{}>> {
    public render() {
        return (
            <article>
                <h1>Component Playground for Design</h1>
            </article>
        )
    }
}