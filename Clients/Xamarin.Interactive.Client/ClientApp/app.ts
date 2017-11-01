//
// app.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

import { ManagedObject } from "xamarin/interactive/dotnet"
import { xiexports } from "./xiexports"

function hasClass(e: HTMLElement | null, className: string) {
  if (e === null)
    return false
  if (typeof e.className !== "string")
    return false
  let classes = e.className.split(/\s+/)
  for (let i = 0; i < classes.length; i++) {
    if (classes[i] === className)
      return true
  }
  return false
}

function addClass(e: HTMLElement | null, className: string) {
  if (e === null)
    return

  if (!hasClass(e, className)) {
    if (e.className == undefined || e.className.length == 0)
      e.className = className
    else
      e.className += " " + className
  }
}

function removeClass(e: HTMLElement | null, className: string) {
  if (e === null)
    return false

  let existingClassNames = e.className
  if (typeof existingClassNames !== "string")
    return false

  existingClassNames = existingClassNames
    .split(/\s+/)
    .filter(i => i != className)
    .join(" ")

  if (e.className != existingClassNames) {
    e.className = existingClassNames
    return true
  }

  return false
}

function toggleClass(e: HTMLElement | null, className: string) {
  if (hasClass(e, className))
    removeClass(e, className)
  else
    addClass(e, className)
}

xiexports.createError = (message: string) => new Error(message)

xiexports.exceptionToggle = (e: HTMLElement) => toggleClass(e.parentElement, "expanded")

xiexports.WorkbookPageView_UpdateFooter = (footerElem: HTMLElement, focused: boolean) => {
  let elems = footerElem.querySelectorAll(".requires-editor-focus")
  for (let i = 0; i < elems.length; i++) {
    if (focused)
      addClass(<HTMLElement>elems[0], "editor-focused")
    else
      removeClass(<HTMLElement>elems[0], "editor-focused")
  }
}

xiexports.DeserializeDotNetObject = (json: string): ManagedObject => {
  // FIXME: need to handle $id/$ref
  return JSON.parse(json)
}

xiexports.scrollToElementWithId = (elementId: string) => {
  const elem = document.getElementById(elementId)
  if (elem) {
    elem.scrollIntoView({
      behavior: "smooth",
      block: "start"
    })
  }
}