//
// mutation-observer.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

import { xiexports } from "../xiexports"

export interface WorkbookHeader {
  level: number,
  oldId: string,
  newId: string,
  text: string
}

interface MutationObserverRegistration {
  target: HTMLElement
  observer: MutationObserver
}

export class WorkbookMutationObserver {
  static readonly Instance: WorkbookMutationObserver = new WorkbookMutationObserver()

  private readonly blockquotePrefixClasses: { [prefix: string]: string } = {
    "💩": "level-big-problem",
    "🚫": "level-problem",
    "⚠️": "level-warning",
    "ℹ️": "level-note",
    "[!NOTE]": "level-note"
  }

  private mutationObservers: MutationObserverRegistration[] = []
  private modelChangeObservers: ((headers: WorkbookHeader[]) => void)[] = []

  observeModelChanges(callback: (headers: WorkbookHeader[]) => void) {
    this.modelChangeObservers.push(callback)
  }

  observeMutations(target: HTMLElement) {
    const observer = new MutationObserver(this.observeHandler.bind(this))
    observer.observe(target, {
      subtree: true,
      childList: true,
      characterData: true
    })
    this.mutationObservers.push({
      target: target,
      observer: observer
    })
  }

  cancelObservation(target: HTMLElement) {
    const i = this.findMutationObserverRegistration(target)
    if (i >= 0) {
      this.mutationObservers[i].observer.disconnect()
      this.mutationObservers.splice(i, 1)
      this.notifyModelChanged()
    }
  }

  private findMutationObserverRegistration(target: HTMLElement): number {
    for (let i = 0; i < this.mutationObservers.length; i++) {
      if (this.mutationObservers[i].target === target)
        return i
    }
    return -1
  }

  private observeHandler(records: MutationRecord[], observer: MutationObserver) {
    for (let record of records) {
      this.handleNode(record.target)
      this.handleNodes(record.addedNodes)
      this.handleNodes(record.removedNodes)
    }
  }

  private handleNodes(nodes: NodeList) {
    for (let i = 0, n = nodes.length; i < n; i++)
      this.handleNode(nodes[i])
  }

  private handleNode(node: Node) {
    if (node instanceof HTMLElement) {
      switch (node.tagName) {
        case "H1":
        case "H2":
        case "H3":
          this.notifyModelChanged()
          break
        default:
          let elem: HTMLElement | null = node
          while (elem && elem.tagName && elem.tagName !== "BLOCKQUOTE")
            elem = elem.parentElement

          if (!elem)
            break

          const text = elem.innerText
          let cssClassToSet: string | null = null

          for (let prefix in this.blockquotePrefixClasses) {
            const cssClass = this.blockquotePrefixClasses[prefix]
            if (text && text.length > 0 && text.indexOf(prefix) === 0)
              cssClassToSet = cssClass
            else if (cssClass !== cssClassToSet)
              elem.classList.remove(cssClass)
          }

          if (cssClassToSet)
            elem.classList.add(cssClassToSet)

          break
      }
    }
  }

  private idFromText(text: string) {
    return text
      .toLowerCase()
      .replace(/[^\w\- ]/g, "")
      .replace(/ /g, '-')
  }

  private notifyModelChanged() {
    let headers: WorkbookHeader[] = []
    let elems = document.querySelectorAll("h1, h2, h3, h4, h5, h6")

    for (let i = 0, n = elems.length; i < n; i++) {
      const elem = <HTMLElement>elems[i]

      if (!elem.parentElement || this.findMutationObserverRegistration(elem.parentElement) < 0) {
        continue
      }

      const text = elem.innerText.trim()
      const baseId = this.idFromText(text)

      let suffix = 1
      let currentId = baseId
      let existingElem: HTMLElement | null = null

      while(true) {
        existingElem = document.getElementById(currentId)
        if (!existingElem || existingElem === elem)
          break
        currentId = baseId + "-" + suffix++
      }

      const oldId = elem.id
      elem.id = currentId

      if (this.modelChangeObservers.length === 0) {
        continue
      }

      let level = 0

      switch (elem.tagName) {
        case "H1": level = 1; break
        case "H2": level = 2; break
        case "H3": level = 3; break
      }

      if (level > 0) {
        headers.push({
          level: level,
          oldId: oldId,
          newId: currentId,
          text: text
        })
      }
    }

    for (let callback of this.modelChangeObservers) {
      callback(headers)
    }
  }
}

xiexports.WorkbookMutationObserver = WorkbookMutationObserver.Instance