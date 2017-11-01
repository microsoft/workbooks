//
// xiexports.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

declare var global: any

const g = typeof window === "undefined"
  ? global
  : window

export var xiexports: any = {}
g["xiexports"] = xiexports

var xamarin_interactive: any = {}
var xamarin: any = { }

function setReadOnlyProperty(
  target: any,
  property: string,
  value: any,
  enumerable: boolean = true) {
  Object.defineProperty(target, property, {
    value: value,
    writable: false,
    enumerable: enumerable,
    configurable: false
  })
}

function recursiveFreeze(o: any) {
  Object.getOwnPropertyNames(o).forEach(name => {
    var value = o[name];
    if (typeof value == "object" && value !== null)
      recursiveFreeze(value);
  });
  Object.freeze(o);
}

setReadOnlyProperty(g, "xamarin", xamarin)
setReadOnlyProperty(xamarin, "interactive", xamarin_interactive)

export function XIExportPublic(property: string, value: any) {
  setReadOnlyProperty(xamarin_interactive, property, value)
  Object.freeze(value)
}