//
// null-renderer.ts
//
// Author:
//   Joan Jané <jjane@plainconcepts.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { ManagedObject } from "xamarin/interactive/dotnet"
import { Renderer, RenderState, RenderTarget, RendererRepresentation } from "xamarin/interactive/rendering"

export class NullRenderer implements Renderer {
  get cssClass(): string {
    return "renderer-null"
  }

  get canExpand(): boolean {
    return false
  }

  getRepresentations(): RendererRepresentation[] {
    return [
      {
        shortDisplayName: "Null"
      }
    ]
  }

  bind(renderState: RenderState) {
  }

  render(target: RenderTarget) {
    const elem = document.createElement("code")
    elem.className = "csharp-keyword"
    elem.innerText = "null"
    target.inlineTarget.appendChild(elem)
  }
}