//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export function loadScript(
    path: string,
    loadedCallback: (this: HTMLElement, ev: Event) => void): void {
    const elem = document.createElement("script");
    elem.src = path;
    elem.async = true;
    elem.onload = loadedCallback;
    document.body.appendChild(elem);
}