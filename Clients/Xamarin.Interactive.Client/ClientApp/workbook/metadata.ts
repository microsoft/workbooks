//
// metadata.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import MarkdownIt = require("markdown-it")

export function MetadataPlugin(md: MarkdownIt.MarkdownIt) {
  new _MetadataPlugin(md)
}

class _MetadataPlugin {
  md: MarkdownIt.MarkdownIt

  constructor(md: MarkdownIt.MarkdownIt) {
    md.block.ruler.before("hr", "metadata_block", this.parse.bind(this))
  }

  /**
    YAML metadata block (Pandoc):
      1. Delimited by three hyphens (---) at the top
      2. Delimited by three hyphens (---) or dots (...) at the bottom
      3. May occur anywhere in the document, but if not at the beginning must
         be preceded by a blank line (markdown-it handles this for us).

    Additionally, Workbooks specifies for YAML metadata blocks:
      4. Must not start with a blank line (this is to disambiguate horizontal rules)
  */
  parse(state: any, startLine: number, endLine: number, silent: boolean) {
    let nextLine = startLine
    let isMetadataBlock = false
    let haveClosingFence = false

    for (; nextLine < endLine; nextLine++) {
      const pos = state.bMarks[nextLine] + state.tShift[nextLine]
      const max = state.eMarks[nextLine]
      const len = max - pos

      // fences are exactly three characters
      if (len === 3) {
        const line = state.src.slice(pos, max)
        // (1) we have an opening fence
        if (!isMetadataBlock && line === "---")
          isMetadataBlock = true
        // (2) we have a closing fence
        else if (isMetadataBlock && (line === "---" || line === "...")) {
          haveClosingFence = true
          break
        }
      }

      if (!isMetadataBlock)
        return false

      // (4) metadata contents must not start with a blank line
      if (nextLine == startLine + 1 && len == 0)
        return false
    }

    state.line = nextLine + (haveClosingFence ? 1 : 0);

    const len = state.sCount[startLine]
    const token = state.push("metadata_block", "", 0)
    token.content = state.getLines(startLine + 1, nextLine, len, true)
    token.map = [startLine, state.line]
    return true
  }
}