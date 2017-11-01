//
// markdown.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import MarkdownIt = require("markdown-it")

import {
  MarkdownParser,
  defaultMarkdownParser,
  defaultMarkdownSerializer
} from "prosemirror/dist/markdown"

import { copyObj } from "prosemirror/dist/util/obj"

import { schema } from "./schema"

const markdownTokens = copyObj(defaultMarkdownParser.tokens)
/*markdownTokens.html_block = {
  node: "html_block",
  attrs: tok => ({ htmlContent: tok.content })
}*/

export const markdownParser = new MarkdownParser(
  schema,
  MarkdownIt("commonmark", { html: false }),
  markdownTokens)

export const markdownSerializer = defaultMarkdownSerializer