//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import { RouteComponentProps } from 'react-router';
import { WorkbookShell } from './WorkbookShell';

export class Home extends React.Component<RouteComponentProps<{}>, {}> {
    public render() {
        return <WorkbookShell/>
    }
}