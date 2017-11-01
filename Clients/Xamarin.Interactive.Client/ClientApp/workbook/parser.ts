//
// parser.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import { xiexports } from "../xiexports"

import MarkdownIt = require("markdown-it")
type Token = MarkdownIt.Token

import { MetadataPlugin } from "./metadata"
import * as dom from "./dom"

export class MarkdownVisitor {
  _depth: number = 0
  get depth() { return this._depth }

  visitMarkdown(markdown: string, html: boolean = true) {
    const parser = MarkdownIt("commonmark", { html: html }).use(MetadataPlugin)
    this.visitTokens(parser.parse(markdown, {}))
  }

  visitTokens(tokens: Token[]) {
    for (let token of tokens)
      this.visitTokenDispatcher(token)
  }

  private visitTokenDispatcher(token: Token) {
    switch (token.type) {
      case "inline": this.visitInline(token); break
      case "text": this.visitText(token); break
      case "html_inline": this.visitHtmlInline(token); break
      case "heading_open": this.visitHeadingOpen(token); break
      case "heading_close": this.visitHeadingClose(token); break
      case "paragraph_open": this.visitParagraphOpen(token); break
      case "paragraph_close": this.visitParagraphClose(token); break
      case "bullet_list_open": this.visitBulletListOpen(token); break
      case "bullet_list_close": this.visitBulletListClose(token); break
      case "list_item_open": this.visitListItemOpen(token); break
      case "list_item_close": this.visitListItemClose(token); break
      case "fence": this.visitFence(token); break
      case "hr": this.visitHr(token); break
      case "link_open": this.visitLinkOpen(token); break
      case "link_close": this.visitLinkClose(token); break
      case "image": this.visitImage(token); break
      case "metadata_block": this.visitMetadataBlock(token); break
      default: this.visitToken(token); break
    }

    if (token.children) {
      this._depth++
      this.visitTokens(token.children)
      this._depth--
    }
  }

  visitToken(token: Token) {
    throw `unhandled token type '${token.type}'`
  }

  visitInline(token: Token) { this.visitToken(token) }
  visitText(token: Token) { this.visitToken(token) }
  visitHtmlInline(token: Token) { this.visitToken(token) }
  visitHeadingOpen(token: Token) { this.visitToken(token) }
  visitHeadingClose(token: Token) { this.visitToken(token) }
  visitParagraphOpen(token: Token) { this.visitToken(token) }
  visitParagraphClose(token: Token) { this.visitToken(token) }
  visitBulletListOpen(token: Token) { this.visitToken(token) }
  visitBulletListClose(token: Token) { this.visitToken(token) }
  visitListItemOpen(token: Token) { this.visitToken(token) }
  visitListItemClose(token: Token) { this.visitToken(token) }
  visitFence(token: Token) { this.visitToken(token) }
  visitHr(token: Token) { this.visitToken(token) }
  visitLinkOpen(token: Token) { this.visitToken(token) }
  visitLinkClose(token: Token) { this.visitToken(token) }
  visitImage(token: Token) { this.visitToken(token) }
  visitMetadataBlock(token: Token) { this.visitToken(token) }
}

export class WorkbookDocumentParser extends MarkdownVisitor {
  private currentVersion: number = 1

  private _document: dom.WorkbookDocument
  get document() { return this._document }

  visitMetadataBlock(token: Token) {
    if (this.depth > 0)
      return

    if (this.document)
      this.document.appendCell(new dom.MetadataWorkbookCell(token.content))
    else
      this._document = new dom.WorkbookDocument(WorkbookDocumentParser.parseYamlManifest(token.content))
  }

  visitFence(token: Token) {
    if (this.depth > 0)
      return

    const infos = token.info.split(" ")
    if (!this.document && infos[0] === "json")
      this._document = new dom.WorkbookDocument(WorkbookDocumentParser.parseJsonManifest(token.content))
    else if (infos[0] === "csharp")
      this.document.appendCell(new dom.CodeWorkbookCell(token.content))
    else
      this.handleMarkdownToken(token)
  }

  visitToken(token: Token) {
    if (this.depth === 0)
      this.handleMarkdownToken(token)
  }

  private handleMarkdownToken(token: Token) {
    let cell = <dom.MarkdownWorkbookCell>this.document.lastCell
    if (!(cell instanceof dom.MarkdownWorkbookCell))
      this.document.appendCell(cell = new dom.MarkdownWorkbookCell())
    cell.appendToken(token)
  }

  private static parseYamlManifest(yaml: string): dom.WorkbookManifest {
    return { version: 1 }
  }

  private static parseJsonManifest(json: string): dom.WorkbookManifest {
    return { version: 1 }
  }
}

export class WorkbookDependencyCollector extends MarkdownVisitor {
  private _dependencies: string[] = []
  get dependencies() { return this._dependencies }

  visitToken(token: Token) { }

  visitLinkOpen(token: Token) {
    this._dependencies.push(token.attrGet("href"))
  }

  visitImage(token: Token) {
    this._dependencies.push(token.attrGet("src"))
  }
}

xiexports.WorkbookDependencyCollector = () => new WorkbookDependencyCollector