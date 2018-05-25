//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { RouteComponentProps } from 'react-router';

import { CodeCellResult, CodeCellEvaluationStatus, CodeCellResultHandling, CodeCellEventType } from '../evaluation'
import { createDefaultRegistry, createDesignRegistry } from '../rendering'
import { MockedCodeCellView } from './CodeCellView';
import { TestRepresentationSelector } from './RepresentationSelector';

export class ComponentPlayground extends React.Component<RouteComponentProps<{}>> {
    private readonly nullResult: CodeCellResult = {
        $type: CodeCellEventType.Evaluation,
        codeCellId: '85cd037b-4cb6-4489-a854-912959b60a6b/3fb9e8a3-2c29-429d-b417-e2678761b57e',
        status: CodeCellEvaluationStatus.Success,
        isNullResult: false,
        evaluationDuration: '',
        cultureLCID: 1033,
        uiCultureLCID: 1033,
        resultHandling: CodeCellResultHandling.Replace,
        resultType: null,
        resultRepresentations: []
    }

    private readonly numberResult: CodeCellResult = {
        $type: CodeCellEventType.Evaluation,
        codeCellId: '65a6fd4c-696f-4b2f-9d0d-3bf452e69f5f/eab2b254-9047-4670-bdb1-5e24aefa4843',
        status: CodeCellEvaluationStatus.Success,
        isNullResult: false,
        evaluationDuration: '',
        cultureLCID: 1033,
        uiCultureLCID: 1033,
        resultHandling: CodeCellResultHandling.Replace,
        resultType: 'System.Double',
        resultRepresentations: [
            25000000000.32,
            {
                $type: 'System.Double',
                $toString: [
                    {
                        culture: {
                            name: 'en-US',
                            lcid: 1033
                        },
                        formats: [
                            { '{value}': '25000000000.32' },
                            { '{value:N}': '25,000,000,000.32' },
                            { '{value:C}': '$25,000,000,000.32' }
                        ]
                    },
                    {
                        culture: {
                            name: '',
                            lcid: 127
                        },
                        formats: [
                            { '{value}': '25000000000.32' },
                            { '{value:N}': '25,000,000,000.32' },
                            { '{value:C}': '¤25,000,000,000.32' }
                        ]
                    }
                ]
            }
        ]
    }

    private readonly dateTimeResult: CodeCellResult = {
        $type: CodeCellEventType.Evaluation,
        codeCellId: 'dca76582-6c22-4c64-9893-2270a67552ce/e9234d19-89a4-4e43-a7cd-780f3fd04541',
        status: CodeCellEvaluationStatus.Success,
        isNullResult: false,
        evaluationDuration: '',
        cultureLCID: 1033,
        uiCultureLCID: 1033,
        resultHandling: CodeCellResultHandling.Replace,
        resultType: 'System.DateTime',
        resultRepresentations: [
            '2018-03-03T23:22:00.102405',
            {
                $type: 'System.DateTime',
                $toString: '03/03/2018 23:22:00'
            }
        ]
    }

    private readonly typeResult: CodeCellResult = {
        $type: CodeCellEventType.Evaluation,
        codeCellId: '12cc4902-1783-4f74-850e-e63474d6b2af/0eba8723-2689-45c2-970f-c5200c8e582f',
        status: CodeCellEvaluationStatus.Success,
        resultHandling: CodeCellResultHandling.Replace,
        resultType: 'System.RuntimeType',
        isNullResult: false,
        resultRepresentations: [
            {
                $type: 'Xamarin.Interactive.Representations.Reflection.TypeNode',
                typeName: {
                    $type: 'Xamarin.Interactive.Representations.Reflection.TypeSpec',
                    name: {
                        $type: 'Xamarin.Interactive.Representations.Reflection.TypeSpec+TypeName',
                        namespace: 'System',
                        name: 'Int32'
                    },
                    assemblyName: 'System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e'
                }
            },
            {
                $type: 'Xamarin.Interactive.Representations.ToStringRepresentation',
                formats: [
                    {
                    $type: 'Xamarin.Interactive.Representations.ToStringRepresentation+Format',
                    name: '{0}',
                    value: 'System.Int32'
                    }
                ]
            },
            {
                $type: 'Xamarin.Interactive.Representations.ReflectionInteractiveObject',
                hasMembers: true,
                toStringRepresentation: 'System.Int32',
                handle: 2,
                representedObjectHandle: 1,
                representedType: 'System.RuntimeType'
            }
        ],
        evaluationDuration: '00:00:00.0040860',
        cultureLCID: 1033,
        uiCultureLCID: 1033
    }

    public render() {
        return (
            <article style={{ margin: '1em 2em' }}>
                {/* <h2>Code Cell</h2>
                <MockedCodeCellView
                    representationRegistry={createDefaultRegistry()}
                    results={[]} />

                <h2>Code Cell with Number Result</h2>
                <MockedCodeCellView
                    representationRegistry={createDefaultRegistry()}
                    results={[this.numberResult]} />

                <h2>Code Cell with DateTime Result</h2>
                <MockedCodeCellView
                    representationRegistry={createDesignRegistry()}
                    results={[this.dateTimeResult]}/>

                <h2>Code Cell with Type Result</h2>
                <MockedCodeCellView
                    representationRegistry={createDesignRegistry()}
                    results={[this.typeResult]}/>

                <h2>Code Cell with Multiple Results</h2>
                <MockedCodeCellView
                    representationRegistry={createDesignRegistry()}
                    results={[this.nullResult, this.nullResult]}
                    resultHandling={CodeCellResultHandling.Append}/> */}

                <TestRepresentationSelector/>
            </article>
        )
    }
}