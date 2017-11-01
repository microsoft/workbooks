//
// rendering.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { CultureInfo, ManagedObject } from "xamarin/interactive/dotnet"
import { Renderer } from "xamarin/interactive/rendering"

import { xiexports, XIExportPublic } from "./xiexports"

import {
  NullRenderer,
  BooleanRenderer,
  NumberRenderer,
  StringRenderer,
  ColorRenderer
} from "./renderers"

export class RendererRegistry {
  private registrations: ((source: ManagedObject) => Renderer | null)[] = []

  constructor() {
    this.registerRenderer(source => {
      if (source.$type === "Xamarin.Interactive.Representations.Color")
        return new ColorRenderer
      return null
    })

    this.registerRenderer(source => {
      if (source.$type === "System.Char" || source.$type === "System.String")
        return new StringRenderer
      return null
    })

    this.registerRenderer(source => {
      if (source.$type === "System.Boolean")
        return new BooleanRenderer
      return null
    })

    this.registerRenderer(source => {
      const implementedTypes = [
        "System.SByte",
        "System.Byte",
        "System.Int16",
        "System.UInt16",
        "System.Int32",
        "System.UInt32",
        "System.Int64",
        "System.UInt64",
        "System.Single",
        "System.Double",
        "System.Decimal",
        "System.IntPtr",
        "System.UIntPtr",
        "System.nint",
        "System.nuint",
        "System.nfloat"];
      if (implementedTypes.some(t => t === source.$type))
        return new NumberRenderer
      return null
    })

    this.registerRenderer(source => {
      if (!source)
        return new NullRenderer
      return null
    })
  }

  registerRenderer(factory: (source: ManagedObject) => Renderer | null) {
    this.registrations.push(factory)
  }

  getRenderers(source: ManagedObject): Renderer[] {
    const renderers: Renderer[] = []
    for (const factory of this.registrations) {
      try {
        const renderer = factory(source)
        if (renderer)
          renderers.push(renderer)
      } catch (e) {
        console.error(`error invoking renderer factory for ${source}: ${e}`)
      }
    }
    return renderers
  }
}

XIExportPublic("RendererRegistry", xiexports.RendererRegistry = new RendererRegistry)