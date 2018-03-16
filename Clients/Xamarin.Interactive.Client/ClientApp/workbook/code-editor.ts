//
// codeEditor.ts
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

import { xiexports } from "../xiexports"

xiexports.monaco = {
  loaded: false,
}

var onloadCallbacks: Array<() => void> = []

// Pass in a callback to be notified once monaco is loaded
xiexports.monaco.onload = (f: () => void) => {
  if (xiexports.monaco.loaded)
    f()
  else
    onloadCallbacks.push(f)
}

// Called by monaco to announce that it is done loading
xiexports.monaco.init = () => {
  xiexports.monaco.loaded = true
  while (onloadCallbacks.length > 0) {
    var callback = onloadCallbacks.shift()
    if (callback != null)
      callback()
  }
}

xiexports.monaco.WorkbookCodeEditor = (options: WorkbookCodeEditorOptions) => new WorkbookCodeEditor(options)

xiexports.monaco.RegisterWorkbookCompletionItemProvider = (lang: string, onCompletionListRequested: IProvide<monaco.languages.CompletionItem[]>) =>
  monaco.languages.registerCompletionItemProvider(
    lang,
    new WorkbookCompletionItemProvider(onCompletionListRequested))

xiexports.monaco.RegisterWorkbookSignatureHelpProvider = (lang: string, onSignatureHelpRequested: IProvide<monaco.languages.SignatureHelp>) =>
  monaco.languages.registerSignatureHelpProvider(
    lang,
    new WorkbookSignatureHelpProvider(onSignatureHelpRequested))

xiexports.monaco.RegisterWorkbookHoverProvider = (lang: string, onHoverRequested: IProvide<monaco.languages.Hover>) =>
  monaco.languages.registerHoverProvider(
    lang,
    new WorkbookHoverProvider(onHoverRequested))


xiexports.monaco.Promise = (init: (complete: monaco.TValueCallback<any>, error: (err: any) => void) => void) =>
  new monaco.Promise(init)

interface IProvide<T> {
  (model: monaco.editor.IReadOnlyModel, position: monaco.Position, token: monaco.CancellationToken): monaco.Thenable<T>;
}

enum AbstractCursorPosition {
  DocumentStart = 0,
  DocumentEnd = 1,
  FirstLineEnd = 2,
  Up,
  Down,
}

enum ViewEventType {
  ViewLinesDeleted = 8,
  ViewLinesInserted = 9
}

class WorkbookCompletionItemProvider implements monaco.languages.CompletionItemProvider {
  private provideImpl: IProvide<monaco.languages.CompletionItem[]>
  triggerCharacters = ['.']

  constructor(onCompletionListRequested: IProvide<monaco.languages.CompletionItem[]>) {
    this.provideImpl = onCompletionListRequested
  }

  provideCompletionItems(
    model: monaco.editor.IReadOnlyModel,
    position: monaco.Position,
    token: monaco.CancellationToken) {
      return this.provideImpl(model, position, token)
  }
}

class WorkbookSignatureHelpProvider implements monaco.languages.SignatureHelpProvider {
  private provideImpl: IProvide<monaco.languages.SignatureHelp>
  signatureHelpTriggerCharacters = ['(', ',']

  constructor(onSignatureHelpRequested: IProvide<monaco.languages.SignatureHelp>) {
    this.provideImpl = onSignatureHelpRequested
  }

  provideSignatureHelp(
    model: monaco.editor.IReadOnlyModel,
    position: monaco.Position,
    token: monaco.CancellationToken) {
      return this.provideImpl(model, position, token)
  }
}

class WorkbookHoverProvider implements monaco.languages.HoverProvider {
  private provideImpl: IProvide<monaco.languages.Hover>

  constructor(onHoverRequested: IProvide<monaco.languages.Hover>) {
    this.provideImpl = onHoverRequested
  }

  provideHover(
    model: monaco.editor.IReadOnlyModel,
    position: monaco.Position,
    token: monaco.CancellationToken) {
      return this.provideImpl(model, position, token)
  }
}

interface WorkbookCodeEditorChangeEvent {
  text: string
  triggerChar: string|null
  newCursorPosition: monaco.IPosition
  isDelete: boolean
  isUndoing: boolean
  isRedoing: boolean
}

interface WorkbookCodeEditorOptions {
  placeElem: HTMLElement
  readOnly: boolean
  fontSize: number
  showLineNumbers: boolean
  onCursorUpDown?: (isUp: boolean) => boolean
  onEnter?: (isShift: boolean, isMeta: boolean, isCtrl: boolean) => boolean
  onFocus?: () => void
  onChange?: (event: WorkbookCodeEditorChangeEvent) => void
  theme?: string,
  wrapLongLines: boolean,
}

class WorkbookCodeEditor {
  private options: WorkbookCodeEditorOptions
  private monacoEditorOptions: monaco.editor.IEditorConstructionOptions
  private markedTextIds: string[] = []
  private windowResizeHandler: EventListener
  private internalViewEventsHandler: any
  private debouncedUpdateLayout: () => void
  mEditor: monaco.editor.IStandaloneCodeEditor

  constructor(options: WorkbookCodeEditorOptions) {
    this.options = options
    this.monacoEditorOptions = {
      language: "csharp",
      readOnly: options.readOnly,
      scrollBeyondLastLine: false,
      roundedSelection: false,
      fontSize: options.fontSize,
      overviewRulerLanes: 0, // 0 = hide overview ruler
      formatOnType: true,
      wordWrap: options.wrapLongLines ? "on" : "off",
      renderIndentGuides: false,
      contextmenu: false,
      cursorBlinking: 'phase',
      minimap: {
        enabled: false,
      },
      scrollbar: {
        // must explicitly hide scrollbars so they don't interfere with mouse events
        horizontal: options.wrapLongLines ? "hidden" : "auto",
        vertical: 'hidden',
        handleMouseWheel: false,
        useShadows: false,
      },
    }

    this.updateLineNumbers()

    this.mEditor = monaco.editor.create(
      options.placeElem, this.monacoEditorOptions)

    monaco.editor.setTheme(options.theme || "vs")
    this.mEditor.onKeyDown(e => this.onKeyDown(e))

    if (this.options.onChange)
      this.mEditor.onDidChangeModelContent(e => this.onChange(e))

    if (this.options.onFocus)
      this.mEditor.onDidFocusEditor(() => {
        if (this.options.onFocus != null)
          this.options.onFocus()
      })

    this.debouncedUpdateLayout = this.debounce(() => this.updateLayout(), 250)
    this.updateLayout()

    this.windowResizeHandler = _ => this.updateLayout()
    window.addEventListener("resize", this.windowResizeHandler)

    let untypedEditor: any = this.mEditor
    this.internalViewEventsHandler = {
      handleEvents: (e: any[]) => this.handleInternalViewEvents(e),
    }
    untypedEditor._view.eventDispatcher.addEventHandler(this.internalViewEventsHandler)
  }

  // See ViewEventHandler
  private handleInternalViewEvents(events: any[]) {
    for (let i = 0, len = events.length; i < len; i++) {
      let type: number = events[i].type;

      // This is the best way for us to find out about lines being added and
      // removed, if you take wrapping into account.
      if (type == ViewEventType.ViewLinesInserted || type == ViewEventType.ViewLinesDeleted)
        this.updateLayout()
    }
  }

  private onKeyDown(e: monaco.IKeyboardEvent) {
    if (this.isCompletionWindowVisible())
      return

    let cancel = false

    if (e.keyCode == monaco.KeyCode.Enter && this.options.onEnter)
      cancel = this.options.onEnter(e.shiftKey, e.metaKey, e.ctrlKey)
    else if (e.keyCode == monaco.KeyCode.UpArrow && !e.shiftKey && !e.metaKey && this.options.onCursorUpDown && !this.isParameterHintsWindowVisible())
      cancel = this.options.onCursorUpDown(true)
    else if (e.keyCode == monaco.KeyCode.DownArrow && !e.shiftKey && !e.metaKey  && this.options.onCursorUpDown && !this.isParameterHintsWindowVisible())
      cancel = this.options.onCursorUpDown(false)
    else if (e.metaKey && (
      ((e.keyCode == monaco.KeyCode.US_CLOSE_SQUARE_BRACKET || e.keyCode == monaco.KeyCode.US_OPEN_SQUARE_BRACKET) && e.shiftKey) ||
      e.keyCode == monaco.KeyCode.KEY_G))
      e.stopPropagation()

    if (cancel) {
      e.preventDefault()
      e.stopPropagation()
    }
  }

  private updateLayout() {
    let clientWidth = this.options.placeElem.parentElement && this.options.placeElem.parentElement.clientWidth || 0
    let fontSize = this.monacoEditorOptions.fontSize || 0

    this.mEditor.layout({
      // NOTE: Something weird happens in layout, and padding+border are added
      //       on top of the total width by monaco. So we subtract those here.
      //       Keep in sync with editor.css. Currently 0.5em left/right padding
      //       and 1px border means (1em + 2px) to subtract.
      width: clientWidth - (fontSize + 2),
      height: (<any>this.mEditor)._view._context.viewLayout._linesLayout.getLinesTotalHeight()
    })
  }

  private onChange(e: monaco.editor.IModelContentChangedEvent) {
    for (const change of e.changes) {
      let changedText = change.text == null ? "" : change.text
      let isDelete = changedText.length == 0

      let newCursorPosition = {
        lineNumber: change.range.startLineNumber,
        column: change.range.startColumn,
      }

      for (let ch of changedText) {
        if (ch == e.eol) {
          newCursorPosition.lineNumber++
          newCursorPosition.column = 1
        } else
          newCursorPosition.column++
      }

      if (this.options.onChange != null)
        this.options.onChange({
          isUndoing: e.isUndoing,
          isRedoing: e.isRedoing,
          isDelete: isDelete,
          newCursorPosition: newCursorPosition,
          triggerChar: isDelete ? null : change.text[change.text.length - 1],
          text: changedText,
        })
    }
  }

  isSomethingSelected() {
    let sel = this.mEditor.getSelection()
    return !sel.getStartPosition().equals(sel.getEndPosition())
  }

  getLastLineIndex() {
    return this.mEditor.getModel().getLineCount() - 1
  }

  isCursorAtEnd() {
    let pos = this.mEditor.getPosition()
    let model = this.mEditor.getModel()

    return pos.lineNumber == model.getLineCount() &&
      pos.column == model.getLineMaxColumn(pos.lineNumber)
  }

  isReadOnly() {
    return this.monacoEditorOptions.readOnly
  }

  setReadOnly(readOnly: boolean) {
    this.monacoEditorOptions.readOnly = readOnly
    this.mEditor.updateOptions(this.monacoEditorOptions)
  }

  setFontSize(fontSize: number) {
    this.monacoEditorOptions.fontSize = fontSize
    this.updateOptions()
  }

  setTheme(themeName: string) {
    monaco.editor.setTheme(themeName)
  }

  setWordWrap(wrapLongLines: boolean) {
    this.monacoEditorOptions.wordWrap = wrapLongLines ? "on" : "off"
    if (this.monacoEditorOptions.scrollbar)
      this.monacoEditorOptions.scrollbar.horizontal = wrapLongLines ? "hidden" : "auto"
    this.updateOptions()
  }

  setShowLineNumbers(showLineNumbers: boolean) {
    this.options.showLineNumbers = showLineNumbers
    this.updateLineNumbers()
    this.updateOptions()
  }

  private updateLineNumbers() {
    this.monacoEditorOptions.lineNumbersMinChars = 1
    if (this.options.showLineNumbers) {
      this.monacoEditorOptions.lineNumbers = 'on'
      this.monacoEditorOptions.lineDecorationsWidth = 8
    } else {
      this.monacoEditorOptions.lineNumbers = 'off'
      this.monacoEditorOptions.lineDecorationsWidth = 0
    }
  }

  private updateOptions() {
    this.mEditor.updateOptions(this.monacoEditorOptions)
    this.debouncedUpdateLayout()
  }

  private debounce (action: () => void, delay: number) {
    var debounceTimeout: number
    return () => {
      clearTimeout(debounceTimeout)
      debounceTimeout = setTimeout(action, delay)
    }
  }

  focus() {
    this.updateLayout()
    this.mEditor.focus()
  }

  getModelId() {
    return this.mEditor.getModel().id
  }

  getText() {
    return this.mEditor.getValue({
      preserveBOM: true,
      lineEnding: "\n"
    })
  }

  setText(value: string) {
    this.mEditor.setValue(value)
    this.debouncedUpdateLayout()
  }

  // TODO: It would be better if we had API access to SuggestController, and if
  //       SuggestController had API to check widget visibility. In the future
  //       we could add this functionality to monaco.
  //       (or if our keybindings had access to 'suggestWidgetVisible' context key)
  isCompletionWindowVisible() {
    return this.isMonacoWidgetVisible("suggest-widget")
  }

  isParameterHintsWindowVisible() {
    return this.isMonacoWidgetVisible("parameter-hints-widget")
  }

  dismissParameterHintsWindow() {
    if (this.isParameterHintsWindowVisible())
      this.mEditor.setPosition({lineNumber:1, column:1})
  }

  isMonacoWidgetVisible(widgetClassName: string) {
    let node = this.mEditor.getDomNode()
    if (node == null)
      return false
    let widgets = node.getElementsByClassName(widgetClassName)
    for (var i = 0; i < widgets.length; i++)
      if (widgets[i].classList.contains("visible"))
        return true
    return false
  }

  showCompletionWindow() {
    this.mEditor.trigger("", "editor.action.triggerSuggest", null)
  }

  setCursorPosition(position: AbstractCursorPosition) {
    switch (position) {
      case AbstractCursorPosition.DocumentStart:

        this.mEditor.trigger("", 'cursorTop', null)
        break
      case AbstractCursorPosition.DocumentEnd:
        this.mEditor.trigger("", 'cursorBottom', null)
        break
      case AbstractCursorPosition.FirstLineEnd:
        this.mEditor.setPosition(new monaco.Position(
          1, this.mEditor.getModel().getLineContent(1).length + 1))
        break
      case AbstractCursorPosition.Up:
        this.mEditor.trigger("", 'cursorUp', null)
        break
      case AbstractCursorPosition.Down:
        this.mEditor.trigger("", 'cursorDown', null)
        break
    }
  }

  markText(range: monaco.IRange, className?: string, title?: string) {
    let newIds = this.mEditor.getModel().deltaDecorations([], [{
      range: range,
      options: {
        inlineClassName: className,
        hoverMessage: title,
      },
    }])
    this.markedTextIds.push(...newIds)
  }

  clearMarkedText() {
    this.mEditor.getModel().deltaDecorations(this.markedTextIds, [])
    this.markedTextIds = []
  }

  dispose() {
    window.removeEventListener("resize", this.windowResizeHandler)

    let untypedEditor: any = this.mEditor
    untypedEditor._view.eventDispatcher.removeEventHandler(this.internalViewEventsHandler)

    this.mEditor.dispose()
  }
}
