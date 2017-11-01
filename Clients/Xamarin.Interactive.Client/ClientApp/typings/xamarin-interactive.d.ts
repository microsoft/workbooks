//
// xamarin-interactive.d.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

declare module "xamarin/interactive/dotnet" {
  /**
   * A .NET CultureInfo.
   * See https://msdn.microsoft.com/en-us/library/system.globalization.cultureinfo.aspx .
   */
  export interface CultureInfo {
    name: string
    lcid: number
  }

  /**
   * A serialized .NET object.
   */
  export interface ManagedObject {
    /**
     * The value of `GetType ().ToString ()` for the underlying .NET object.
     * Includes namepspace but no assembly qualification.
     */
    $type: string
    $toString?: string | CulturedToStringFormats[]
    $value?: any
  }

  /**
   * Represents various ToString formats for a given culture.
   */
  export interface CulturedToStringFormats {
    culture: CultureInfo
    formats: {
      [formatSpecifier: string]: string
    }[]
  }
}

declare module "xamarin/interactive/rendering" {
  import { ManagedObject, CultureInfo } from "xamarin/interactive/dotnet"

  export enum RendererRepresentationOptions {
    None = 0,

    /**
     * The representation will always be provided the expanded render
     * targets and will not be collapsible at all.
     */
    ForceExpand = 1,

    /**
     * The representation is collapsible, but will be expanded by default.
     */
    ExpandedByDefault = 2,

    /**
     * The representation is collapsible and will be collapsed by default if
     * it is the only or initially selected renderer, and otherwise expanded
     * automatically when selected from the menu.
     */
    ExpandedFromMenu = 4,

    /**
     * The display name of the representation will be suppressed in the
     * representation button label and shown only in the button's menu,
     * but only if all other representations also have the hint.
     */
    SuppressDisplayNameHint = 8
  }

  /**
   * One (of perhaps many) representation(s) for a given renderer.
   */
  export interface RendererRepresentation {
    /**
     * The name to show in the representation drop-down menu in the client.
     */
    shortDisplayName: string
    /**
     * An optional piece of state for use by the renderer.
     */
    state?: any
    /**
     * Optional. Defaults to RendererRepresentationOptions.None.
     */
    options?: RendererRepresentationOptions
    /**
     * Optional numerical value to determine sort order of renderers in the
     * client's drop-down menu. Defaults to 0.
     */
    order?: number
  }

  /**
   * Provides access to the HTML targets during Renderer.render.
   */
  export interface RenderTarget {
    /**
     * The selected representation to render.
     */
    representation: RendererRepresentation
    /**
     * The container element to modify for the 'collapsed' rendering.
     */
    inlineTarget: HTMLElement
    /**
     * The container element to modify for the 'expanded' rendering.
     */
    expandedTarget: HTMLElement
    /**
     * Returns true if the representation is currently expanded.
     */
    isExpanded: boolean
  }

  /**
   * Provides access to the serialized object being rendered, and other state.
   */
  export interface RenderState {
    /**
     * The state of the parent object, if there is one. Renderers might choose
     * to layout differently depending on whether or not they are being displayed
     * as part of a member row in an interactive object table, for example.
     */
    parentState: RenderState
    /**
     * The serialized .NET source object.
     */
    source: ManagedObject
    /**
     * Optional CultureInfo.
     */
    cultureInfo?: CultureInfo
  }

  /**
   * Provides custom rendering(s) for ManagedObjects.
   */
  export interface Renderer {
    /**
     * The CSS class added to both the inline rendering target and the expanded
     * rendering target.
     */
    cssClass: string
    /**
     * The representation(s) provided by this Renderer.
     */
    getRepresentations(): RendererRepresentation[]
    /**
     * Called once when an object is ready for rendering (though some other
     * renderer may currently be selected). Useful for doing one-time work.
     */
    bind(renderState: RenderState): void
    /**
     * Called when it is time to render the serialized object into the HTML target(s).
     */
    render(target: RenderTarget): void

    /**
     * Optional; set to false to prevent this renderer's representations from
     * showing up in the client's drop-down menu.
     */
    isEnabled?: boolean
    /**
     * Optional; set to true to enable showing the expanded rendering target.
     */
    canExpand?: boolean
    /**
     * Optional. Notifies renderer when a collapse occurs, in case extra work is
     * needed beyond the work done during render.
     */
    collapse?(): void
    /**
     * Optional. Notifies renderer when an expand occurs, in case extra work is
     * needed beyond the work done during render.
     */
    expand?(): void
  }

  /**
   * Accessible via xamarin.interactive.RendererRegistry, this is used to
   * register renderers.
   */
  export class RendererRegistry {
    /**
     * Register a renderer factory method. Typically this method will check the
     * type of the source object, and return an appropriate Renderer if it is
     * a type the caller wants to handle.
     */
    registerRenderer(factory: (source: ManagedObject) => Renderer): void
  }
}