//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { CodeCellResult } from './evaluation'
import { ResultRendererFactory, ResultRenderer } from './rendering'

import { NullRenderer } from './renderers/NullRenderer'
import { TestRenderer } from './renderers/TestRenderer'

export class ResultRendererRegistry {
    private rendererFactories: ResultRendererFactory[] = []

    register(factory: ResultRendererFactory) {
        this.rendererFactories.push(factory)
    }

    getRenderers(result: CodeCellResult): ResultRenderer[] {
        return <ResultRenderer[]>this.rendererFactories
            .map(f => f(result))
            .filter(f => f !== null)
    }

    static createDefault(): ResultRendererRegistry {
        const registry = new ResultRendererRegistry
        registry.register(NullRenderer.factory)
        registry.register(TestRenderer.factory)
        return registry
    }
}