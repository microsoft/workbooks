//
// menu.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import {
  MenuItem,
  Dropdown,
  DropdownSubmenu,
  toggleMarkItem,
  insertItem,
  wrapItem,
  blockTypeItem,
  wrapListItem,
  icons,
  joinUpItem,
  liftItem,
  selectParentNodeItem,
  tooltipMenu,
  menuBar
} from "prosemirror/dist/menu"

import { ProseMirror } from "prosemirror/dist/edit"

import { FieldPrompt, TextField } from "prosemirror/dist/ui"

import { schema } from "./schema"

function iconNode(className: string) {
  let elem = document.createElement("div")
  elem.className = className
  return { dom: elem }
}

const insertImageNode = schema.nodes["image"];

export const menuItems:any = {
  insertImage: insertItem(insertImageNode, {
    label: "Image…",
    title: "Insert image",
    attrs: function (pm: ProseMirror, callback: Function, nodeType: any) {
      const node = pm.selection.node;
      const attrs = nodeType && node && node.type == nodeType && node.attrs;
      const prompt = new FieldPrompt(pm, "Insert Image", {
        src: new TextField({
          label: "Location",
          required: true,
          value: attrs && attrs.src
        }),
        title: new TextField({
          label: "Title",
          value: attrs && attrs.title
        }),
        alt: new TextField({
          label: "Description",
          value: attrs ? attrs.title : pm.doc.textBetween(
            pm.selection.from,
            pm.selection.to,
            " ")
        })
      }).open(callback)
    }
  }),

  insertHorizontalRule: insertItem(schema.nodes["horizontal_rule"], {
    label: "Horizontal Rule",
    title: "Insert horizontal rule"
  }),

  toggleStrong: toggleMarkItem(schema.marks["strong"], {
    label: "Strong",
    title: "Toggle Strong Style",
    icon: iconNode("strong_toggle")
  }),

  toggleEm: toggleMarkItem(schema.marks["em"], {
    label: "Emphasis",
    title: "Toggle Emphasis",
    icon: iconNode("em_toggle")
  }),

  toggleLink: toggleMarkItem(schema.marks["link"], {
    label: "Link…",
    title: "Add or Remove Link",
    icon: iconNode("link_set"),
    attrs: function (pm: ProseMirror, callback: Function) {
      new FieldPrompt(pm, "Create a link", {
        href: new TextField({
          label: "Link target",
          required: true
        }),
        title: new TextField({
          label: "Title"
        })
      }).open(callback)
    }
  }),

  toggleCode: toggleMarkItem(schema.marks["code"], {
    label: "Code",
    title: "Toggle Code Style",
    icon: iconNode("code_toggle")
  }),

  paragraphBlock: blockTypeItem(schema.nodes["paragraph"], {
    label: "Plain",
    title: "Change to paragraph"
  }),

  codeBlock: blockTypeItem(schema.nodes["code_block"], {
    label: "Code",
    title: "Change to code block"
  }),

  wrapBulletList: wrapListItem(schema.nodes["bullet_list"], {
    label: "Bullet List",
    title: "Wrap in bullet list",
    icon: iconNode("bullet_list_wrap")
  }),

  wrapOrderedList: wrapListItem(schema.nodes["ordered_list"], {
    label: "Ordered List",
    title: "Wrap in ordered list",
    icon: iconNode("ordered_list_wrap")
  }),

  wrapBlockquote: wrapItem(schema.nodes["blockquote"], {
    label: "Blockquote",
    title: "Wrap in block quote",
    icon: iconNode("blockquote_wrap")
  }),

  liftNode: new MenuItem({
    label: "Lift Out",
    title: "Lift out of enclosing block",
    icon: iconNode("lift"),
    run: liftItem.spec.run,
    select: liftItem.spec.select
  }),

  selectParentNode: new MenuItem({
    label: "Select Parent",
    title: "Select parent node",
    icon: iconNode("selectParentNode"),
    run: selectParentNodeItem.spec.run,
    select: selectParentNodeItem.spec.select
  })
}

for (let level = 1; level <= 6; level++)
  menuItems["heading" + level] = blockTypeItem(schema.nodes["heading"], {
    title: "Change to heading " + level,
    label: "Level " + level,
    attrs: { level: level }
  })

const insertMenu = [
  new Dropdown([
    menuItems.insertImage,
    menuItems.insertHorizontalRule
  ], { label: "Insert" })
]

const inlineMenu = [[
  menuItems.toggleStrong,
  menuItems.toggleEm,
  menuItems.toggleLink,
  menuItems.toggleCode
], insertMenu]

const typeMenu = new Dropdown([
  menuItems.paragraphBlock,
  menuItems.codeBlock,
  new DropdownSubmenu([
    menuItems.heading1,
    menuItems.heading2,
    menuItems.heading3,
    menuItems.heading4,
    menuItems.heading5,
    menuItems.heading6
  ], { label: "Heading" })
], { label: "Type…" })

const blockMenu = [[
  typeMenu,
  menuItems.wrapBulletList,
  menuItems.wrapOrderedList,
  menuItems.wrapBlockquote,
  // FIXME: need icon
  // joinUpItem,
  menuItems.liftNode,
  menuItems.selectParentNode
]]

export const menus = {
  inlineMenu: inlineMenu,
  blockMenu: blockMenu,
  fullMenu: inlineMenu.concat(blockMenu)
}