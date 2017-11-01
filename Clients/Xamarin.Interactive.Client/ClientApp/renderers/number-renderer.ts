//
// number-renderer.ts
//
// Author:
//   Joan Jané <jjane@plainconcepts.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { ManagedObject, CulturedToStringFormats } from "xamarin/interactive/dotnet"
import { Renderer, RenderState, RenderTarget, RendererRepresentation } from "xamarin/interactive/rendering"

interface NumberRepresentation {
  label: string
  value: string
}

export class NumberRenderer implements Renderer {
  private renderState: RenderState;

  get cssClass(): string {
    return "renderer-number"
  }

  getRepresentations(): Array<RendererRepresentation> {
    const suppressDisplayNameHint = 8; //TODO: Fix this with an enum

    return this.getFormats(this.renderState.source)
      .map(format => <RendererRepresentation>{
        shortDisplayName: format.label,
        state: format.value,
        options: suppressDisplayNameHint
      });
  }

  bind(renderState: RenderState): void {
    this.renderState = renderState
  }

  render(target: RenderTarget): void {
    const elem = document.createElement("code")
    elem.className = "csharp-number"
    elem.innerText = target.representation.state;
    target.inlineTarget.appendChild(elem)
  }

  getFormats(source: ManagedObject): NumberRepresentation[] {
    if (!Array.isArray(source.$toString))
      throw new Error("source.$toString is expected to be CulturedToStringFormats[] type")

    const cultureFormatList = <CulturedToStringFormats[]>source.$toString
    const representations: NumberRepresentation[] = []

    // NOTE: only supporting the first culture group for now,
    // for feature parity with the original C# implementation
    for (const formatType of cultureFormatList[0].formats) {
      const key = Object.keys(formatType)[0]
      representations.push({
        label: key,
        value: formatType[key]
      })
    }

    return representations
  }
}