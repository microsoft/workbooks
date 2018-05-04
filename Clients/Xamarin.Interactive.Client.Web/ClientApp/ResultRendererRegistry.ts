//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { CodeCellResult } from './evaluation'
import { ResultRendererFactory, ResultRenderer } from './rendering'

import NullRenderer from './renderers/NullRenderer'
import CalendarRendererFactory from './renderers/CalendarRenderer'
import ToStringRendererFactory from './renderers/ToStringRenderer'
import ImageRendererFactory from './renderers/ImageRenderer'
import VerbatimHtmlRendererFactory from './renderers/VerbatimHtmlRenderer'
import TestRendererFactory from './renderers/TestRenderer'
import InteractiveObjectRendererFactory from './renderers/InteractiveObjectRenderer'
import TypeSpecRendererFactory from './renderers/TypeSystemRenderers';

export class ResultRendererRegistry {
    private rendererFactories: ResultRendererFactory[] = []

    register(factory: ResultRendererFactory) {
        this.rendererFactories.push(factory)
    }

    getRenderers(result: CodeCellResult): ResultRenderer[] {
        if (result.isNullResult)
            return [new NullRenderer];
        else if (result.resultRepresentations && result.resultRepresentations.length > 0)
            return <ResultRenderer[]>this.rendererFactories
                .map(f => f(result))
                .filter(f => f !== null)
        else
            return []
    }

    static createDefault(): ResultRendererRegistry {
        const registry = new ResultRendererRegistry
        // More exciting and specific renderers should be first
        registry.register(CalendarRendererFactory)
        registry.register(ImageRendererFactory)
        registry.register(VerbatimHtmlRendererFactory)
        registry.register(TypeSpecRendererFactory)

        // These are 'catch all' and should always be last
        // registry.register(InteractiveObjectRendererFactory)
        registry.register(ToStringRendererFactory)
        return registry
    }

    static createForDesign(): ResultRendererRegistry {
        const registry = ResultRendererRegistry.createDefault()
        registry.register(TestRendererFactory)
        return registry
    }
}