//
// dom.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import MarkdownIt = require("markdown-it")

import { xiexports } from "../xiexports"
import { WorkbookDocumentParser } from "./parser"

export interface WorkbookCell { }

export class MetadataWorkbookCell implements WorkbookCell {
  constructor(public yaml: string) { }
}

export class CodeWorkbookCell implements WorkbookCell {
  constructor(public code: string) { }
}

export class MarkdownWorkbookCell implements WorkbookCell {
  private _tokens: MarkdownIt.Token[] = []
  get tokens() { return this._tokens }

  appendToken(token: MarkdownIt.Token) {
    this._tokens.push(token)
  }
}

export interface WorkbookManifest {
  version: number
}

export class WorkbookDocument {
  private _manifest: WorkbookManifest
  get manifest() { return this._manifest }

  private _cells: WorkbookCell[] = []
  get cells() { return this._cells }

  constructor(manifest: WorkbookManifest) {
    this._manifest = manifest
  }

  get lastCell() {
    const len = this.cells.length
    return len > 0 ? this.cells[len - 1] : undefined
  }

  appendCell(cell: WorkbookCell) {
    this._cells.push(cell)
  }

  static read(markdown: string) {
    const visitor = new WorkbookDocumentParser
    visitor.visitMarkdown(markdown)
    return visitor.document
  }
}

xiexports.WorkbookDocument = WorkbookDocument