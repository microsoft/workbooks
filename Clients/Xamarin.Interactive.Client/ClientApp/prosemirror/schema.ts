//
// schema.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import { Schema, Block, Inline, Attribute } from "prosemirror/dist/model"
import { schema as schemaBasic } from "prosemirror/dist/schema-basic"

class HtmlBlock extends Block {
  get attrs() {
    return { htmlContent: new Attribute }
  }

  get matchDOMTag() {
    return "div[xi-html]"
  }

  get draggable() {
    return true
  }

  toDOM(node: any) {
    const elem = document.createElement("div")
    elem.setAttribute("xi-html", "")
    elem.innerHTML = node.attrs.htmlContent
    return elem
  }
}

export const schema = new Schema({
  nodes: schemaBasic.nodeSpec.append({
    html_block: { type: HtmlBlock, group: "block" }
  }),
  marks: schemaBasic.markSpec
})