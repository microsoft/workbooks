//
// color.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { ManagedObject } from "xamarin/interactive/dotnet"
import { Renderer, RenderState, RenderTarget, RendererRepresentation  } from "xamarin/interactive/rendering"

interface Color extends ManagedObject {
  Red: number
  Green: number
  Blue: number
  Alpha: number
}

export class ColorRenderer implements Renderer {
  private color: Color
  private r: number
  private g: number
  private b: number
  private a: number

  get cssClass(): string {
    return "renderer-color"
  }

  getRepresentations() {
    return [
      { shortDisplayName: "ARGB Hex", state: this.renderArgbHex },
      { shortDisplayName: "RGBA CSS", state: this.renderRgbaCss }
    ]
  }

  bind(renderState: RenderState) {
    const byte = (b: number) => Math.max(Math.min(255, Math.floor(b * 255)), 0)
    this.color = <Color>renderState.source
    this.r = byte(this.color.Red)
    this.g = byte(this.color.Green)
    this.b = byte(this.color.Blue)
    this.a = byte(this.color.Alpha)
  }

  render(target: RenderTarget) {
      target.inlineTarget.appendChild(this.renderColor(target.representation.state))
  }

  private renderColor(innerRenderer: () => HTMLElement): HTMLElement {
    const colorElem = document.createElement("div")
    colorElem.style.backgroundColor = `rgba(${this.r},${this.g},${this.b},${this.color.Alpha})`

    const colorShellElem = document.createElement("div")
    colorShellElem.appendChild(colorElem)

    const captionElem = document.createElement("figcaption")
    captionElem.appendChild(innerRenderer.call(this))

    const figureElem = document.createElement("figure")
    figureElem.appendChild(colorShellElem)
    figureElem.appendChild(captionElem)

    return figureElem
  }

  private renderArgbHex(): HTMLElement {
    const toHex = (n: number) => {
      const hex = n.toString(16)
      return hex.length < 2 ? `0${hex}` : hex
    }

    const elem = document.createElement("code")
    elem.className = this.color.Alpha < 1 ? "argb-hex" : "rgb-hex"
    elem.innerHTML = `#${this.color.Alpha < 0 ? toHex(this.a) : ""}` +
      `${toHex(this.r)}${toHex(this.g)}${toHex(this.b)}`
    return elem
  }

  private renderRgbaCss(): HTMLElement {
    const comma = "<span class=\"comma\">,</span>"
    const elem = document.createElement("code")
    const rgb = `${this.r}${comma}${this.g}${comma}${this.b}`

    if (this.color.Alpha < 1) {
      elem.className = "rgba-css css-function"
      elem.innerHTML = `${rgb}${comma}${this.color.Alpha}`
    } else {
      elem.className = "rgb-css css-function"
      elem.innerHTML = rgb
    }

    return elem
  }
}