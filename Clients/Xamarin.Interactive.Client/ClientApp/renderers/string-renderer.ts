//
// string-renderer.ts
//
// Author:
//   Joan Jané <jjane@plainconcepts.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { ManagedObject } from "xamarin/interactive/dotnet"
import { Renderer, RenderState, RenderTarget, RendererRepresentation, RendererRepresentationOptions } from "xamarin/interactive/rendering"

export class StringRenderer implements Renderer {
  private readonly cSharpRepresentation = "C#";
  private charKind: boolean = false;
  private value: string | null;

  get cssClass(): string {
    return "renderer-string"
  }

  getRepresentations(): RendererRepresentation[] {
    // The use of RendererRepresentationOptions.SuppressDisplayNameHint does not compile for some reason
    const suppressDisplayNameHint = 8;
    return [
      {
        shortDisplayName: this.cSharpRepresentation,
        options: suppressDisplayNameHint
      },
      {
        shortDisplayName: "Plain Text",
        options: suppressDisplayNameHint
      }
    ]
  }

  bind(renderState: RenderState) {
    this.charKind = renderState.source.$type === "System.Char";
    this.value = <string>renderState.source.$toString;
  }

  render(target: RenderTarget) {
    if (target.representation.shortDisplayName === this.cSharpRepresentation) {
      this.renderCSharp(target)
    } else {
      this.renderRaw(target)
    }
  }

  renderCSharp(target: RenderTarget) {
    const quote = this.charKind ? `'` : `"`;
    const elem = document.createElement("code")
    if (this.value === null) {
      elem.className = "csharp-keyword"
      elem.innerText = "null"
    } else {
      elem.className = this.charKind ? "csharp-char" : "csharp-string"
      elem.innerText = `${quote}${this.formatValue(this.value)}${quote}`
    }
    target.inlineTarget.appendChild(elem)
  }

  renderRaw(target: RenderTarget) {
    target.inlineTarget.innerText = this.value || ""
  }

  convertChar(ch: string) {
    const charCode = ch.charCodeAt(0)
    switch (charCode) {
      case 92: //'\\'
        return "\\\\";
      case 0: //'\0'
        return "\\0";
      case 7: //'\a'
        return "\\a";
      case 8: //'\b'
        return "\\b";
      case 12: //'\f'
        return "\\f";
      case 10: //'\n'
        return "\\n";
      case 13: //'\r'
        return "\\r";
      case 9: //'\t'
        return "\\t";
      case 11: //'\v'
        return "\\v";
      default:
        //print control and surrogate characters
        if (charCode < 0x020 || (charCode >= 0xD800 && charCode <= 0xDFFF) ||
          // print all uncommon white spaces as numbers
          (/\s/.test(ch) && ch !== ' '))
          return "\\u" + ("000" + charCode.toString(16)).slice(-4);

        return ch;
    }
  }

  formatValue(value: string): string {
    if (this.charKind)
      return this.convertChar (value);

    let formattedValue = "";
    for (let i = 0; i < value.length; i++) {
      formattedValue += this.convertChar (value.charAt (i));
    }
    return formattedValue;
  }
}