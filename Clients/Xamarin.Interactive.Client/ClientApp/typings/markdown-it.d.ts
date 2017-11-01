//
// markdown-it.d.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

declare module "markdown-it" {
  var MarkdownIt: {
    (presetName?: string, options?: MarkdownIt.Options): MarkdownIt.MarkdownIt
    new (presetName?: string, options?: MarkdownIt.Options): MarkdownIt.MarkdownIt
  }

  namespace MarkdownIt {
    export interface Options {
      html?: boolean
      xhtmlOut?: boolean
      breaks?: boolean
      langPrefix?: string
      linkify?: boolean
      typographer?: boolean
      quotes?: string | string[]
      highlight?: (str: string, lang: string) => string
    }

    export interface MarkdownIt {
      block: ParserBlock

      parse(src: string, env: any): Token[]
      use(plugin: (md: MarkdownIt, options: any) => any): MarkdownIt
    }

    export interface Token {
      attrs: string[][]
      block: boolean
      children: Token[]
      content: string
      hidden: boolean
      info: string
      level: number
      map: number[]
      markup: string
      meta: any
      nesting: number
      tag: string
      type: string

      attrGet(name: string): string
      attrSet(name: string, value: string): void
      attrIndex(attrName: string): number
      attrJoin(name: string, value: string): void
      attrPush(attr: string[]): void
    }

    export interface ParserBlock {
      ruler: Ruler
    }

    export interface Rule {
      (state: any): void;
    }

    export interface Ruler {
      before(beforeName: string, ruleName: string, rule: Function, options?: any): void
      after(afterName: string, ruleName: string, rule: Function, options?: any): void
    }
  }

  export = MarkdownIt
}