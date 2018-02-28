import * as React from 'react'
import Editor from 'draft-js-plugins-editor'
import {
    EditorState,
    SelectionState,
    ContentState,
    RichUtils,
    getDefaultKeyBinding,
    KeyBindingUtil,
    Modifier,
    genKey,
    ContentBlock,
    DefaultDraftBlockRenderMap
} from 'draft-js'
import createMarkdownPlugin from 'draft-js-markdown-plugin'
import { List, Map } from 'immutable'
import { CodeCell } from './CodeCell'
import { WorkbookSession } from '../WorkbookSession'
import { EditorMessage, EditorMessageType, EditorKeys } from '../utils/EditorMessages'
import { getNextBlockFor, getPrevBlockFor, isBlockBackwards } from '../utils/DraftStateUtils'
import { EditorMenu, getBlockStyle, styleMap } from './Menu'
import { WorkbookShellContext } from './WorkbookShell';
/*import { convertFromMarkdown } from '../utils/draftImportUtils'
import { convertToMarkdown } from '../utils/draftExportUtils'*/

interface WorkbooksEditorProps {
    shellContext: WorkbookShellContext
    content: string | undefined
}

interface WorkbooksEditorState {
    editorState: EditorState,
    plugins: any[],
    readOnly: boolean
}

export interface MonacoCellMapper {
    registerCellInfo(codeCellId: string, monacoModelId: string): void
    getCodeCellId(monacoModelId: string): string|null
}

interface WorkbookCellIdMapping {
    codeCellId: string
    monacoModelId: string
}

const blockRenderMap = DefaultDraftBlockRenderMap.merge(Map({
    'code-block': {
        element: 'div'
    }
}));

// TODO: Should this move? Annoying that these must be registered globally
class WorkbookCompletionItemProvider implements monaco.languages.CompletionItemProvider {
    triggerCharacters = ['.']
    shellContext: WorkbookShellContext
    mapper: MonacoCellMapper

    constructor(shellContext: WorkbookShellContext, mapper: MonacoCellMapper) {
        this.shellContext = shellContext
        this.mapper = mapper
    }

    async provideCompletionItems(
        model: monaco.editor.IReadOnlyModel,
        position: monaco.Position,
        token: monaco.CancellationToken) {
        // TODO: Investigate best way to consume Monaco CancellationTokens
        let items: monaco.languages.CompletionItem[] = []
        let modelId = (model as monaco.editor.IModel).id // TODO: Replace with URI usage to avoid cast?

        let codeCellId = this.mapper.getCodeCellId(modelId)

        if (codeCellId == null)
            return items

        items = await this.shellContext.session.provideCompletions(codeCellId, position.lineNumber, position.column)

        // TODO: See if we can fix this on the server side. See comments on MonacoCompletionItem
        for (let item of items) {
            if (item.insertText == null)
                item.insertText = undefined
            if (item.detail == null)
                item.detail = undefined
        }

        return items
    }
}

export class WorkbookEditor extends React.Component<WorkbooksEditorProps, WorkbooksEditorState> implements MonacoCellMapper {
    lastFocus?: Date;
    subscriptors: ((m: EditorMessage) => void)[];
    monacoProviderTickets: monaco.IDisposable[] = [];
    cellIdMappings: WorkbookCellIdMapping[] = [];

    constructor(props: WorkbooksEditorProps) {
        super(props);

        this.subscriptors = []
        this.lastFocus = undefined

        let editorState = EditorState.createEmpty()

        this.state = {
            editorState: editorState,
            plugins: [createMarkdownPlugin()],
            readOnly: false
        }

        // Monaco intellisense providers must be registered globally, not on a
        // per-editor basis. This is why the providers need a mapping from
        // Monaco model ID to Workbook cell ID.
        this.monacoProviderTickets.push(
            monaco.languages.registerCompletionItemProvider(
                "csharp",
                new WorkbookCompletionItemProvider(this.props.shellContext, this)))
    }

    componentDidMount() {
        this.focus()
    }

    componentWillUnmount() {
        for (let ticket of this.monacoProviderTickets)
            ticket.dispose()
    }

    focus(e?: any) {
        const focusThresholdToBlur = this.lastFocus && (+Date.now() - +this.lastFocus) > 1000
        if (focusThresholdToBlur)
            (document.activeElement as any).blur();

        this.lastFocus = new Date();
        (this.refs.editor as any).focus();
    }

    onChange(editorState: EditorState) {
        this.setState({ editorState })
    }

    blockRenderer(block: Draft.ContentBlock) {
        if (block.getType() === 'code-block') {
            return {
                component: CodeCell,
                editable: false,
                props: {
                    shellContext: this.props.shellContext,
                    cellMapper: this,
                    editorReadOnly: (readOnly: boolean) => this.editorReadOnly(readOnly),
                    subscribeToEditor: (callback: () => void) => this.addMessageSubscriber(callback),
                    selectNext: (currentKey: string) => this.selectNext(currentKey),
                    selectPrevious: (currentKey: string) => this.selectPrevious(currentKey),
                    updateTextContentOfBlock: (blockKey: string, textContent: string) => this.updateTextContentOfBlock(blockKey, textContent),
                    setSelection: (anchorKey: string, offset: number) => this.setSelection(anchorKey, offset),
                }
            }
        }
        return null
    }

    registerCellInfo(codeCellId: string, monacoModelId: string) {
        this.cellIdMappings.push({
            codeCellId: codeCellId,
            monacoModelId: monacoModelId
        })
    }

    getCodeCellId(monacoModelId: string) {
        for (let mapping of this.cellIdMappings)
            if (mapping.monacoModelId == monacoModelId)
                return mapping.codeCellId

        return null
    }

    createNewEmptyBlock(currentContent: ContentState, insertBefore: boolean): ContentBlock {
        // If this is a code block, we should insert a new block immediately after
        const newBlock = new ContentBlock({
            key: genKey(),
            type: "unstyled",
            text: "",
            characterList: List()
        });

        const newBlockMap = currentContent.getBlockMap().set(newBlock.getKey(), newBlock)
        const blockArray = newBlockMap.toArray();

        // If we should insert the block before the current content, splice it into the 0th
        // position in the block array.
        if (insertBefore) {
            const newBlockIndex = blockArray.findIndex((cb: ContentBlock, idx: number) => {
                return cb.getKey() === newBlock.getKey();
            });
            blockArray.splice(newBlockIndex, 1);
            blockArray.splice(0, 0, newBlock);
        }

        const contentState = ContentState.createFromBlockArray(blockArray, undefined);

        this.setState({
            editorState: EditorState.push(this.state.editorState, contentState, "insert-fragment"),
        });
        return newBlock;
    }

    selectNext(currentKey: string): boolean {
        this.editorReadOnly(false)

        const currentContent = this.getSelectionContext().currentContent;
        let nextBlock = getNextBlockFor(currentContent, currentKey)
        if (!nextBlock) {
            const currentBlockType = currentContent.getBlockForKey(currentKey).getType();
            if (currentBlockType !== "code-block")
                return false;
            nextBlock = this.createNewEmptyBlock(currentContent, false);
        }

        var nextSelection = SelectionState.createEmpty(nextBlock.getKey())
        nextSelection
            .set('anchorOffset', 0)
            .set('focusOffset', 0)

        var editorState = EditorState.forceSelection(this.state.editorState, nextSelection)
        this.setState({ editorState })
        if (nextBlock.getType() !== "code-block")
            this.focus()

        return true
    }

    selectPrevious(currentKey: string): boolean {
        this.editorReadOnly(false)

        const currentContent = this.getSelectionContext().currentContent;
        let nextBlock = getPrevBlockFor(currentContent, currentKey)
        if (!nextBlock) {
            const currentBlockType = currentContent.getBlockForKey(currentKey).getType();
            if (currentBlockType !== "code-block")
                return false;
            nextBlock = this.createNewEmptyBlock(currentContent, true);
        }
        var nextSelection = SelectionState.createEmpty(nextBlock.getKey())
        nextSelection
            .set('anchorOffset', 0)
            .set('focusOffset', 0)

        var editorState = EditorState.forceSelection(this.state.editorState, nextSelection)
        this.setState({ editorState })
        if (nextBlock.getType() !== "code-block")
            this.focus()

        return true
    }

    /**
     * Set readonly editor to fix bad behaviours moving focus between code blocks and text
     * https://draftjs.org/docs/advanced-topics-block-components.html#recommendations-and-other-notes
     * @param {boolean} readOnly
     */
    editorReadOnly(readOnly: boolean) {
        this.setState({ readOnly: readOnly })
    }

    updateTextContentOfBlock(blockKey: string, textContent: string) {
        // Create a selection of the hole block and replace it with new text
        const content = this.state.editorState.getCurrentContent()
        const end = content.getBlockForKey(blockKey).getText().length
        const selection = SelectionState.createEmpty(blockKey)
            .set("anchorOffset", 0)
            .set("focusKey", blockKey)
            .set("focusOffset", end)
        const newContent = Modifier.replaceText(content, selection as SelectionState, textContent)

        // apply changes
        const newState = EditorState.push(
            this.state.editorState,
            newContent,
            "insert-characters"
        )
        this.onChange(newState)

    }

    setSelection(anchorKey: string, offset: number) {
        offset = offset | 0
        const selection = SelectionState.createEmpty(anchorKey)
            .set("anchorOffset", 0)

        this.onChange(EditorState.forceSelection(this.state.editorState, selection as SelectionState))
    }

    editorKeyBinding(e?: any) {
        const keyCode = e.keyCode
        if (keyCode === EditorKeys.LEFT || keyCode === EditorKeys.RIGHT)
            this.onArrow(keyCode, e)

        return getDefaultKeyBinding(e)
    }

    /**
     * Add a subscriber to custom messages/events of editor.
     * @param {fn} callback
     */
    addMessageSubscriber(callback: () => void) {
        this.subscriptors.push(callback)
    }

    /**
     * Send a message to subscribers
     * @param {EditorMessage} message
     */
    sendMessage(message: EditorMessage) {
        this.subscriptors.forEach(function (callback) {
            callback(message)
        })
    }

    getSelectionContext(): {
        selectionState: Draft.SelectionState,
        currentContent: Draft.ContentState,
    } {
        var selectionState = this.state.editorState.getSelection()
        var anchorKey = selectionState.getAnchorKey()
        var currentContent = this.state.editorState.getCurrentContent()

        return {
            selectionState,
            currentContent
        }
    }

    /**
     * Handle heyboard arrow navigation
     * @param {KeyboardArrows|number} dir Arrow keycode
     * @param {Event} e Event
     */
    onArrow(dir: number, e?: React.KeyboardEvent<{}>) {
        var selectionContext = this.getSelectionContext()
        var targetBlock = null
        const selection = window.getSelection();
        switch (dir) {
            case EditorKeys.UP:
                targetBlock = selectionContext.currentContent.getBlockBefore(selectionContext.selectionState.getFocusKey())
                break
            case EditorKeys.DOWN:
                targetBlock = selectionContext.currentContent.getBlockAfter(selectionContext.selectionState.getFocusKey())
                break
            case EditorKeys.RIGHT:
                const selectionIsEndOfLine = selection && selection.anchorNode && selection.anchorNode.textContent
                    && selection.anchorOffset === selection.anchorNode.textContent.length
                if (selectionIsEndOfLine)
                    targetBlock = selectionContext.currentContent.getBlockAfter(selectionContext.selectionState.getFocusKey())

                break
            case EditorKeys.LEFT:
                const selectionIsStartOfLine = selectionContext.selectionState.getStartOffset() === 0
                if (selectionIsStartOfLine)
                    targetBlock = selectionContext.currentContent.getBlockBefore(selectionContext.selectionState.getFocusKey())

                break
            default:
                break
        }

        if (!targetBlock) {
            return
        }

        const isBackwards = isBlockBackwards(this.state.editorState, targetBlock.getKey())
        this.sendMessage({
            type: EditorMessageType.setSelection,
            target: targetBlock.getKey(),
            data: {
                isBackwards: isBackwards,
                keyCode: e ? e.keyCode : undefined
            }
        })
    }

    handleKeyCommand(command: string): "handled" | "not-handled" {
        const newState = RichUtils.handleKeyCommand(this.state.editorState, command)
        if (newState) {
            this.onChange(newState)
            return "handled"
        }
        return "not-handled"
    }

    /**
     * Change the type of block on current selection
     * @param {string} blockType
     */
    toggleBlockType(blockType: string) {
        this.onChange(
            RichUtils.toggleBlockType(
                this.state.editorState,
                blockType
            )
        )
    }

    /**
     * Change the type of inline block on current selection
     * @param {*} blockType
     */
    toggleInlineStyle(inlineStyle: string) {
        this.onChange(
            RichUtils.toggleInlineStyle(
                this.state.editorState,
                inlineStyle
            )
        )
    }

    render() {
        let className = 'xi-editor'
        var contentState = this.state.editorState.getCurrentContent()
        if (!contentState.hasText()) {
            if (contentState.getBlockMap().first().getType() !== 'unstyled') {
                className += ' xi-editor--hidePlaceholder'
            }
        }

        return (
            <div className="xi-editor-container">
                <EditorMenu
                    editorState={this.state.editorState}
                    onToggleBlock={(type: string) => this.toggleBlockType(type)}
                    onToggleInline={(type: string) => this.toggleInlineStyle(type)}
                />
                <br />
                <button type="button" className="btn btn-primary" onClick={() => this.logContent()}>Log Draft blocks to console</button>
                <br /><br />
                <div className={className} onClick={(e) => this.focus(e)}>
                    <Editor
                        ref="editor"
                        placeholder=""
                        spellCheck={false}
                        readOnly={this.state.readOnly}
                        blockRenderMap={blockRenderMap}
                        blockRendererFn={(block: Draft.ContentBlock) => this.blockRenderer(block)}
                        blockStyleFn={getBlockStyle}
                        customStyleMap={styleMap}
                        editorState={this.state.editorState}
                        onChange={(s: EditorState) => this.onChange(s)}
                        plugins={this.state.plugins}
                        keyBindingFn={(e: any) => this.editorKeyBinding(e)}
                        handleKeyCommand={(e: string) => this.handleKeyCommand(e)}
                        onUpArrow={(e: React.KeyboardEvent<{}>) => this.onArrow(EditorKeys.UP, e)}
                        onDownArrow={(e: React.KeyboardEvent<{}>) => this.onArrow(EditorKeys.DOWN, e)}
                    />
                </div>
            </div>
        )
    }

    logContent() {
        console.log("______editor content______")
        this.state.editorState.getCurrentContent().getBlockMap().forEach((e: any) => console.log(`${e.key}(${e.type}): "${e.text}"`))
        console.log("______editor selection______")
        console.log(
            `${this.state.editorState.getSelection().getAnchorKey()}[${this.state.editorState.getSelection().getAnchorOffset()}]-` +
            `${this.state.editorState.getSelection().getFocusKey()}[${this.state.editorState.getSelection().getFocusOffset()}]`
        )
    }
}