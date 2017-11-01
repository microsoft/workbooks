//
// prosemirror.d.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

declare module "browserkeymap" {
  class Keymap {
    constructor(keys: { [key: string]: any })
  }
  export = Keymap;
}

declare module "prosemirror/dist/util/obj" {
  export function copyObj<T>(obj: T, base?: T): T
}

declare module "prosemirror/dist/model" {
  export class Node { }
  export class ResolvedPos { }
  export class NodeRange { }
  export class Fragment { }
  export class ReplaceError /* extends ProseMirrorError */ { }
  export class Mark { }
  export class Block extends NodeType { }
  export class Inline extends NodeType { }
  export class Text extends Inline { }
  export class MarkType { }
  export interface NodeSpec { }
  export class ContentMatch { }
  export interface ParseSpec { }
  export interface DOMOutputSpec { }

  export interface AttributeOptions {
    default?: any
    compute?: () => any
  }

  export class Attribute {
    constructor(options?: AttributeOptions)
  }

  export class NodeType {
    name: string
    schema: Schema
    attrs: Attribute
    isBlock: boolean
    isTextblock: boolean
    isInline: boolean
    isText: boolean
    isLeaf: boolean
    selectable: boolean
    draggable: boolean
    matchDOMTag: any

    create(attrs?: Object, content?: Fragment | Node | Node [], marks?: Mark[]): any
    createChecked(attrs?: Object, content?: Fragment | Node | Node [], marks?: Mark[]): Node
    createAndFill(attrs?: Object, content?: Fragment | Node | Node[], marks?: Mark[]): Node
    validContent(content: Fragment, attrs?: Object): boolean
    toDOM(node: DOMOutputSpec): void
  }

  export interface SchemaSpec {
    nodes: any,
    marks: any
  }

  export class Schema {
    constructor(spec: SchemaSpec, data?: any)
    nodeSpec: any
    markSpec: any
    data: any
    cached: Object
    nodes: { [type: string]: NodeType }
    marks: { [type: string]: MarkType }
  }
}

declare module "prosemirror/dist/schema-basic" {
  import { Schema, Block, Inline, MarkType } from "prosemirror/dist/model"

  export const schema: Schema

  export class Doc extends Block { }
  export class BlockQuote extends Block { }
  export class OrderedList extends Block { }
  export class BulletList extends Block { }
  export class ListItem extends Block { }
  export class HorizontalRule extends Block { }
  export class Heading extends Block { }
  export class CodeBlock extends Block { }
  export class Paragraph extends Block { }
  export class Image extends Inline { }
  export class HardBreak extends Inline { }
  export class EmMark extends MarkType { }
  export class StrongMark extends MarkType { }
  export class LinkMark extends MarkType { }
  export class CodeMark extends MarkType { }
}

declare module "prosemirror/dist/edit" {
  import Keymap = require("browserkeymap")
  import { Schema, ResolvedPos, NodeType, MarkType } from "prosemirror/dist/model"

  export const commands: {
    chainCommands(...commands: ((pm: ProseMirror, apply?: boolean) => boolean)[]): void
    newlineInCode(pm: ProseMirror, apply?: boolean): boolean
    splitListItem(nodeType: NodeType): (pm: ProseMirror) => boolean
    liftListItem(nodeType: NodeType): (pm: ProseMirror, apply?: boolean) => boolean
    sinkListItem(nodeType: NodeType): (pm: ProseMirror, apply?: boolean) => boolean
    toggleMark(markType: MarkType, attrs?: Object): (pm: ProseMirror, apply?: boolean) => boolean
    setBlockType(nodeType: NodeType, attrs?: Object): (pm: ProseMirror, apply?: boolean) => boolean
  }

  export class ProseMirrorOptions {
    place: HTMLElement
    schema: Schema
  }

  export class ProseMirror {
    doc: any
    selection: any
    on: any
    tr: EditorTransform
    content: HTMLElement
    wrapper: HTMLElement

    constructor(options: ProseMirrorOptions)

    setDoc(contents: any): void
    focus(): void

    addKeymap(map: any, priority?: number): void
  }

  export class EditorTransform {
    selection: Selection
    apply(options?: Object): EditorTransform
    applyAndScroll(): EditorTransform
    setSelection(selection: Selection): EditorTransform
    replaceSelection(node?: Node, inheritMarks?: boolean): EditorTransform
    deleteSelection(): EditorTransform
    typeText(text: string): EditorTransform
  }

  export class Selection {
    from: number
    to: number
    $from: ResolvedPos
    $to: ResolvedPos
    empty: boolean
    eq(other: Selection): boolean
  }

  export class TextSelection extends Selection {
    anchor: number
    head: number
    $anchor: ResolvedPos
    $head: ResolvedPos
  }

  export class NodeSelection extends Selection {
    node: Node
  }

  export class Plugin {
    get(pm: ProseMirror): any
    attach(pm: ProseMirror): any
    detach(pm: ProseMirror): any
    ensure(pm: ProseMirror): any
    config(options?: Object): Plugin
  }
}

declare module "prosemirror/dist/markdown" {
  import { Node, Schema } from "prosemirror/dist/model"
  import MarkdownIt = require("markdown-it")

  export const defaultMarkdownParser: MarkdownParser
  export const defaultMarkdownSerializer: MarkdownSerializer

  export class MarkdownParser {
    tokens: any
    constructor(schema: Schema, tokenizer: MarkdownIt.MarkdownIt, tokens: Object)
    parse(text: string): Node
  }

  export class MarkdownSerializer {
    serialize(content: Node): string
  }
}

declare module "prosemirror/dist/inputrules" {
  import { NodeType } from "prosemirror/dist/model"
  import { Plugin } from "prosemirror/dist/edit"

  export class InputRule { }

  export class InputRules {
    addRule(rule: InputRule): void
    removeRule(rule: InputRule): boolean
  }

  export const inputRules: Plugin

  export const emDash: InputRule
  export const ellipsis: InputRule
  export const openDoubleQuote: InputRule
  export const closeDoubleQuote: InputRule
  export const openSingleQuote: InputRule
  export const closeSingleQuote: InputRule
  export const smartQuotes: InputRule[]
  export const allInputRules: InputRule[]

  export function blockQuoteRule(nodeType: NodeType): InputRule
  export function orderedListRule(nodeType: NodeType): InputRule
  export function bulletListRule(nodeType: NodeType): InputRule
  export function codeBlockRule(nodeType: NodeType): InputRule
  export function headingRule(nodeType: NodeType, maxLevel: number): InputRule
}

declare module "prosemirror/dist/menu" {
  import { ProseMirror, Plugin } from "prosemirror/dist/edit"
  import { NodeType, MarkType } from "prosemirror/dist/model"

  export interface MenuElement {
    render?: (pm: ProseMirror) => Node
  }

  export interface MenuItemSpec {
    icon: Object
    label: string
    title: string
    class?: string
    css?: string
    execEvent?: string
    run?: (pm: ProseMirror) => Node
    select?: (pm: ProseMirror) => boolean
    onDeselect?: () => string
    active?: (pm: ProseMirror) => boolean
    render?: (pm: ProseMirror) => Node
  }

  export class MenuItem {
    spec: MenuItemSpec
    constructor(spec: MenuItemSpec)
  }

  export class Dropdown {
    constructor(content: MenuElement, options?: Object)
    render(pm: ProseMirror): Node
  }

  export class DropdownSubmenu {
    constructor(content: MenuElement[], options?: Object)
    render(pm: ProseMirror): Node
  }

  export function toggleMarkItem(markType: MarkType, options: Object): MenuItem
  export function insertItem(nodeType: NodeType, options: Object): MenuItem
  export function wrapItem(nodeType: NodeType, options: Object): MenuItem
  export function blockTypeItem(nodeType: NodeType, options: Object): MenuItem
  export function wrapListItem(nodeType: NodeType, options: Object): MenuItem

  export const icons: Object

  export const joinUpItem: MenuItem
  export const liftItem: MenuItem
  export const selectParentNodeItem: MenuItem

  export const tooltipMenu: Plugin
  export const menuBar: Plugin
}

declare module "prosemirror/dist/ui" {
  import { ProseMirror } from "prosemirror/dist/edit"

  export class Tooltip {
    constructor(wrapper: Node, options: string | Object)

    detach(): void
    open(node: Node, pos?: { left: number, top: number }): void
    close(): void
  }

  export class FieldPrompt {
    form: Node

    constructor(pm: ProseMirror, title: string, fields: { [key: string]: Field })

    close(): void
    open(callback?: any): void
    values(): any[]
    prompt(): { close: () => void }
    reportInvalid(dom: Node, message: string): void
  }

  export class Field {
    constructor(options: {
      value?: any,
      label: string,
      required?: boolean,
      validate?: (value: any) => string
    })

    render(pm: ProseMirror): Node
    read(dom: Node): any
    validateType(_value: any): string
  }

  export class TextField extends Field { }
  export class SelectField extends Field { }
}