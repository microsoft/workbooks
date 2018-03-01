//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'

export interface ClickableProps<T> {
    onClick?: (e: React.MouseEvent<T>) => any
}

export class Clickable<T> extends React.Component<ClickableProps<T>> {
}