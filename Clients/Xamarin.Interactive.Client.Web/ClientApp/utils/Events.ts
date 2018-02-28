//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

type EventListener<TSource extends {}, TState extends {}>
    = (source: TSource, state: TState) => 'stop' | void | undefined

export class Event<TSource, TState> {
    private source: TSource
    private listeners: EventListener<TSource, TState> [] = []

    constructor(source: TSource) {
        this.source = source
    }

    addListener(listener: EventListener<TSource, TState>): void {
        this.listeners.push(listener)
    }

    removeListener(listener: EventListener<TSource, TState>): void {
        const index = this.listeners.indexOf(listener)
        if (index >= 0)
            this.listeners.splice(index, 1)
    }

    dispatch(state: TState) {
        for (const listener of this.listeners) {
            const result = listener(this.source, state)
            if (result === 'stop')
                return
        }
    }
}