//
// css-monitor.ts
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

import { xiexports } from "./xiexports"

class CssVisitor {
  visitDocument(document: HTMLDocument) {
    this.visitStyleSheetList(document.styleSheets)
  }

  visitStyleSheetList(styleSheetList: StyleSheetList) {
    for (let i = 0; i < styleSheetList.length; i++)
      this.visitStyleSheet(styleSheetList[i])
  }

  visitStyleSheet(styleSheet: StyleSheet) {
    if (styleSheet instanceof CSSStyleSheet)
      this.visitCSSStyleSheet(styleSheet)
  }

  visitCSSStyleSheet(styleSheet: CSSStyleSheet) {
    this.visitCSSRuleList(styleSheet.cssRules)
  }

  visitCSSRuleList(cssRuleList: CSSRuleList) {
    for (let i = 0; i < cssRuleList.length; i++)
      this.visitCSSRule(cssRuleList[i])
  }

  visitCSSRule(cssRule: CSSRule) {
    if (cssRule instanceof CSSImportRule)
      this.visitCSSImportRule(cssRule)
  }

  visitCSSImportRule(cssImportRule: CSSImportRule) {
    if (cssImportRule.styleSheet)
      this.visitStyleSheet(cssImportRule.styleSheet)
  }
}

class RecursiveStyleSheetCollector extends CssVisitor {
  private _styleSheets: StyleSheet[] = []
  get styleSheets() {
    return this._styleSheets
  }

  visitStyleSheet(styleSheet: StyleSheet) {
    this._styleSheets.push(styleSheet)
    super.visitStyleSheet(styleSheet)
  }
}

interface MonitoredStyleSheet {
  ownerLinkElement: HTMLLinkElement
  styleSheet: StyleSheet
}

interface MonitoredStyleSheetSet {
  [href: string]: MonitoredStyleSheet
}

export class CssMonitor {
  private urlParser = document.createElement("a")

  private getUrlPath(url: string) {
    this.urlParser.href = url
    return this.urlParser.pathname
  }

  getStyleSheets(): MonitoredStyleSheetSet {
    const collector = new RecursiveStyleSheetCollector()
    collector.visitDocument(document)

    const sheets: MonitoredStyleSheetSet = {}

    for (let sheet of collector.styleSheets) {
      let node = sheet.ownerNode
      let currentSheet = sheet
      while (!node && currentSheet) {
        node = currentSheet.ownerNode
        currentSheet = currentSheet.parentStyleSheet
      }

      sheets[this.getUrlPath(sheet.href)] = {
        ownerLinkElement: <HTMLLinkElement>node,
        styleSheet: sheet
      }
    }

    return sheets
  }

  notifyFileEvent(path: string) {
    const sheets = this.getStyleSheets()
    const reloadedPaths: string[] = []

    for (let sheetPath in sheets) {
      if (sheetPath !== path && sheetPath !== "/serve-source" + path)
        continue

      this.reloadLinkElement(sheets[sheetPath].ownerLinkElement)
      reloadedPaths.push(path)
    }

    if (reloadedPaths.length)
      this.inidcatePathsReloaded(reloadedPaths)
  }

  private reloadLinkElement(linkElement: HTMLLinkElement) {
    let href = this.getUrlPath(linkElement.href)
    if (href.indexOf("/serve-source/") !== 0)
      href = "/serve-source" + href
    linkElement.href = href
  }

  private notifyElem: HTMLUListElement
  private notifyElemHideTimeout: number

  private inidcatePathsReloaded(reloadedPaths: string[]) {
    const showClassName = "css-monitor-notify-reload visible"
    const hideClassName = "css-monitor-notify-reload"
    const pathElems: HTMLElement[] = []

    if (!this.notifyElem) {
      this.notifyElem = document.createElement("ul")
      this.notifyElem.className = hideClassName
    }

    for (let path of reloadedPaths) {
      const pathElem = document.createElement("li")
      if (path[0] === '/')
        path = path.substr(1)
      pathElem.innerText = path
      this.notifyElem.appendChild(pathElem)
      pathElems.push(pathElem)
    }

    if (!this.notifyElem.parentElement) {
      document.body.appendChild(this.notifyElem)
      setTimeout(() => this.notifyElem.className = showClassName, 100)
    } else
      this.notifyElem.className = showClassName

    const cssTransitionDuration = 500
    const visibilityDuration = 4000

    if (this.notifyElemHideTimeout)
      clearTimeout(this.notifyElemHideTimeout)

    // first timer simply hides the container, but
    // may be cancelled by subsequent additions
    this.notifyElemHideTimeout = setTimeout(
      () => this.notifyElem.className = hideClassName,
      visibilityDuration)

    // second timer has the same delay plus the duration
    // of the actual hide transition (keep in sync with CSS)
    // in order to keep the elements around while transitioning
    setTimeout(() => {
      for (const elem of pathElems)
        elem.remove()
    }, visibilityDuration + visibilityDuration)
  }
}

xiexports.cssMonitor = new CssMonitor()