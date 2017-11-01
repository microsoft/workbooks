//
// editor.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import Keymap = require("browserkeymap")
import { Schema, Block, Fragment, Mark, Attribute } from "prosemirror/dist/model"
import { ProseMirror, commands } from "prosemirror/dist/edit"
import { schema as schemaBasic, Image, LinkMark } from "prosemirror/dist/schema-basic"

import { tooltipMenu, menuBar } from "prosemirror/dist/menu"

import { schema } from "../prosemirror/schema"
import { markdownParser, markdownSerializer } from "../prosemirror/markdown"
import { menus, menuItems } from "../prosemirror/menu"

import { xiexports } from "../xiexports"

import { WorkbookMutationObserver } from "./mutation-observer"

import {
  InputRule,
  blockQuoteRule,
  bulletListRule,
  orderedListRule,
  codeBlockRule,
  headingRule,
  inputRules,
  allInputRules
} from "prosemirror/dist/inputrules"

xiexports.WorkbookEditor = (options: WorkbookEditorOptions) => new WorkbookEditor(options)

class WorkbookEditorOptions {
  placeElem: HTMLElement
  onCursorUpDown: (isUp: boolean, isMod: boolean) => boolean
  onModEnter: () => void
  onFocus: () => void
  onChange: () => void
}

class WorkbookEditor {
  private options: WorkbookEditorOptions
  pm: ProseMirror
  inputRules: InputRule[]

  constructor(options: WorkbookEditorOptions) {
    this.options = options
    this.pm = new ProseMirror({
      place: options.placeElem,
      schema: schema
    })

    this.pm.addKeymap(this.buildKeymap())

    this.inputRules = allInputRules.concat(this.buildInputRules())
    let rules = inputRules.ensure(this.pm)
    this.inputRules.forEach(rule => {
      rules.addRule(rule)
    })

    if (options.onFocus)
      this.pm.on.focus.add(options.onFocus)

    if (options.onChange)
      this.pm.on.change.add(options.onChange)

    WorkbookMutationObserver.Instance.observeMutations(this.pm.content)
  }

  dispose() {
    WorkbookMutationObserver.Instance.cancelObservation(this.pm.content)
  }

  setMenuStyle(type: "bar" | "tooltip") {
    switch (type) {
      case "bar":
        tooltipMenu.detach(this.pm)
        menuBar.config({
          float: true,
          content: menus.fullMenu
        }).attach(this.pm)
        break
      case "tooltip":
        menuBar.detach(this.pm)
        tooltipMenu.config({
          selectedBlockMenu: true,
          inlineContent: menus.inlineMenu,
          blockContent: menus.blockMenu
        }).attach(this.pm)
        break
      default:
        throw `unsupported menu style '${type}'`
    }
  }

  set content(content: string) {
    this.pm.setDoc(markdownParser.parse(content))
  }

  get content(): string {
    return markdownSerializer.serialize(this.pm.doc)
  }

  focus() {
    return this.pm.focus()
  }

  getMenuItems() {
    return menuItems
  }

  shouldFocusPreviousEditor() {
    const selection = this.pm.selection
    if (!selection.empty)
      return false

    const selectedNode = selection.$from.parent

    // Keep checking firstChild until we either find thes selected node, or hit
    // the bottom. If we find the parent, we should focus the previous editor.
    for (let firstChild = this.pm.doc.firstChild; firstChild != null; firstChild = firstChild.firstChild) {
      if (firstChild == selectedNode)
        return selection.$from.nodeBefore == null
    }

    return false
  }

  shouldFocusNextEditor() {
    const selection = this.pm.selection
    if (!selection.empty)
      return false

    const selectedNode = selection.$from.parent;

    // Same as above, but last child.
    for (let lastChild = this.pm.doc.lastChild; lastChild != null; lastChild = lastChild.lastChild) {
      if (lastChild == selectedNode)
        return selection.$from.nodeAfter == null
    }

    return false;
  }

  private buildInputRules() {
    return [
      blockQuoteRule(schema.nodes["blockquote"]),
      bulletListRule(schema.nodes["bullet_list"]),
      orderedListRule(schema.nodes["ordered_list"]),
      codeBlockRule(schema.nodes["code_block"]),
      headingRule(schema.nodes["heading"], 6),
    ]
  }

  private buildKeymap() {
    const listItemNode = schema.nodes["list_item"]
    const hardBreakNode = schema.nodes["hard_break"]
    const hardBreakCommand = commands.chainCommands(commands.newlineInCode, pm => {
      pm.tr.replaceSelection(hardBreakNode.create()).applyAndScroll()
      return true
    })

    const keys: any = {
      "Mod-B": commands.toggleMark(schema.marks["strong"]),
      "Mod-I": commands.toggleMark(schema.marks["em"]),
      "Mod-`": commands.toggleMark(schema.marks["code"]),

      "Enter": commands.splitListItem(listItemNode),
      "Mod-[": commands.liftListItem(listItemNode),
      "Mod-]": commands.sinkListItem(listItemNode),

      "Shift-Enter": hardBreakCommand,
      "Shift-Mod-Enter": (pm: any) => pm.input.dispatchKey("Enter", null),

      "Mod-Enter": this.options.onModEnter
    }

    if (this.options.onCursorUpDown instanceof Function) {
      keys["Up"] = (pm: ProseMirror) => this.options.onCursorUpDown(true, false)
      keys["Down"] = (pm: ProseMirror) => this.options.onCursorUpDown(false, false)
      keys["Mod-Up"] = (pm: ProseMirror) => this.options.onCursorUpDown(true, true)
      keys["Mod-Down"] = (pm: ProseMirror) => this.options.onCursorUpDown(false, true)
    }

    const headingNode = schema.nodes["heading"]
    for (let level = 1; level <= 6; level++)
      keys["Mod-" + level] = commands.setBlockType(
        headingNode, { level: level })

    return new Keymap(keys)
  }
}