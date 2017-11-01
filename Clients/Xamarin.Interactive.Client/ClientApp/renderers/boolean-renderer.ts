//
// boolean-renderer.ts
//
// Author:
//   Joan Jané <jjane@plainconcepts.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { ManagedObject } from "xamarin/interactive/dotnet"
import { Renderer, RenderState, RenderTarget, RendererRepresentation, RendererRepresentationOptions } from "xamarin/interactive/rendering"

export class BooleanRenderer implements Renderer {
  private value: boolean

  get cssClass(): string {
    return "renderer-boolean"
  }

  get canExpand(): boolean {
    return false;
  }

  getRepresentations(): RendererRepresentation[] {
    return [
      {
        shortDisplayName: "Boolean"
      }
    ]
  }

  bind(renderState: RenderState) {
    this.value = renderState.source.$value
  }

  render(target: RenderTarget) {
      const elem = document.createElement ("code")
      elem.className = "csharp-keyword"
      elem.innerText = this.value.toString ()
      target.inlineTarget.appendChild (elem)
  }
}