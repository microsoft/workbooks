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
    DefaultDraftBlockRenderMap,
    convertFromHTML
} from 'draft-js'
import createMarkdownPlugin from 'draft-js-markdown-plugin'
import { List, Map, Set } from 'immutable'
import { CodeCell } from './CodeCell'
import { WorkbookSession, SessionEvent, SessionEventKind } from '../WorkbookSession'
import { MonacoCellMapper, WorkbookCompletionItemProvider, WorkbookHoverProvider, WorkbookSignatureHelpProvider } from '../utils/MonacoUtils'
import { EditorMessage, EditorMessageType, EditorKeys } from '../utils/EditorMessages'
import { getNextBlockFor, getPrevBlockFor, isBlockBackwards } from '../utils/DraftStateUtils'
import { EditorMenu, getBlockStyle, styleMap } from './Menu'
import { WorkbookShellContext } from './WorkbookShell';
import { convertToMarkdown, convertFromMarkdown } from '../utils/DraftSaveLoadUtils'
import './WorkbookEditor.scss'

interface WorkbooksEditorProps {
    shellContext: WorkbookShellContext
    content: string | undefined
}

interface WorkbooksEditorState {
    editorState: EditorState,
    plugins: any[],
    readOnly: boolean
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

type DraftBlockType = "header-one" | "header-two" | "header-three" | "header-four" | "header-five" | "header-six" | "blockquote" | "code-block" | "atomic"
| "unordered-list-item" | "ordered-list-item" | "unstyled";

export class WorkbookEditor extends React.Component<WorkbooksEditorProps, WorkbooksEditorState> implements MonacoCellMapper {
    subscriptors: ((m: EditorMessage) => void)[];
    monacoProviderTickets: monaco.IDisposable[] = [];
    cellIdMappings: WorkbookCellIdMapping[] = [];
    focusedCodeEditors: Set<string> = Set()
    editorContainer: HTMLDivElement | null = null

    constructor(props: WorkbooksEditorProps) {
        super(props);

        this.onSessionEvent = this.onSessionEvent.bind(this)

        this.subscriptors = []

        const editorState = EditorState.createEmpty()

        this.state = {
            editorState: editorState,
            plugins: [createMarkdownPlugin()],
            readOnly: true
        }

        // Monaco intellisense providers must be registered globally, not on a
        // per-editor basis. This is why the providers need a mapping from
        // Monaco model ID to Workbook cell ID.
        this.monacoProviderTickets.push(
            monaco.languages.registerCompletionItemProvider(
                "csharp",
                new WorkbookCompletionItemProvider(this.props.shellContext, this)))

        this.monacoProviderTickets.push(
            monaco.languages.registerHoverProvider(
                "csharp",
                new WorkbookHoverProvider(this.props.shellContext, this)))

        this.monacoProviderTickets.push(
            monaco.languages.registerSignatureHelpProvider(
                "csharp",
                new WorkbookSignatureHelpProvider(this.props.shellContext, this)))
    }

    private onSessionEvent(session: WorkbookSession, sessionEvent: SessionEvent) {
        this.setState({ readOnly: sessionEvent.kind !== SessionEventKind.Ready })
    }

    componentDidMount() {
        this.props.shellContext.session.sessionEvent.addListener(this.onSessionEvent)
        this.focus()
    }

    componentWillUnmount() {
        this.props.shellContext.session.sessionEvent.removeListener(this.onSessionEvent)
        for (let ticket of this.monacoProviderTickets)
            ticket.dispose()
    }

    focus(e?: React.MouseEvent<{}>) {
        (this.refs.editor as any).focus();

        // Only do this if the editor container was clicked.
        if (e && this.editorContainer && e.target === this.editorContainer) {
            // Select the last block
            let editorState = this.state.editorState;
            const currentContent = editorState.getCurrentContent();
            let lastBlock = currentContent.getBlocksAsArray().slice(-1)[0];

            if (lastBlock.getType() === "code-block") {
                lastBlock = this.createNewEmptyBlock("unstyled");
                editorState = this.insertBlockIntoState(lastBlock, "last", false);
            }

            const nextSelection = (SelectionState.createEmpty(lastBlock.getKey())
                .set('anchorOffset', lastBlock.getText().length)
                .set('focusOffset', lastBlock.getText().length) as SelectionState);

            editorState = EditorState.forceSelection(editorState, nextSelection);
            this.onChange(editorState);
        }
    }

    onChange(editorState: EditorState) {
        this.setState({ editorState })
    }

    blockRenderer(block: Draft.ContentBlock) {
        if (block.getType() === 'code-block') {
            const codeCellId: string = block.getData().get("codeCellId");
            return {
                component: CodeCell,
                editable: false,
                props: {
                    shellContext: this.props.shellContext,
                    rendererRegistry: this.props.shellContext.rendererRegistry,
                    sendEditorMessage: (message: EditorMessage) => this.sendMessage(message),
                    cellMapper: this,
                    codeCellId,
                    codeCellBlurred: (currentKey: string) => this.codeCellBlurred(currentKey),
                    codeCellFocused: (currentKey: string) => this.codeCellFocused(currentKey),
                    subscribeToEditor: (callback: () => void) => this.addMessageSubscriber(callback),
                    selectNext: (currentKey: string) => this.selectNext(currentKey),
                    selectPrevious: (currentKey: string) => this.selectPrevious(currentKey),
                    updateTextContentOfBlock: (blockKey: string, textContent: string) => this.updateTextContentOfBlock(blockKey, textContent),
                    setSelection: (anchorKey: string, offset: number) => this.setSelection(anchorKey, offset),
                    getPreviousCodeBlock: (currentBlock: string) => this.getPreviousCodeBlock(currentBlock),
                    updateBlockCodeCellId: (currentBlock: string, codeCellId: string) => this.updateBlockCodeCellId(currentBlock, codeCellId),
                    appendNewCodeCell: () => this.appendNewCodeCell(),
                }
            }
        }
        return null
    }

    // Blur event for old cell may come in after focus event for new cell, so
    // we need to actually track the collection of code cells that claim to be
    // focused. Markdown content needs to be in read-only mode whenever a code
    // cell has focus.
    codeCellBlurred(currentKey: string) {
        this.focusedCodeEditors = this.focusedCodeEditors.remove(currentKey)
        this.editorReadOnly(!this.focusedCodeEditors.isEmpty())
    }

    codeCellFocused(currentKey: string) {
        this.focusedCodeEditors = this.focusedCodeEditors.add(currentKey)
        this.editorReadOnly(!this.focusedCodeEditors.isEmpty())
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

    updateBlockCodeCellId(currentBlock: string, codeCellId: string) {
        // FIXME: In the future, we should try not to reconstruct the entire state.
        const content = this.state.editorState.getCurrentContent();
        const block = content.getBlockForKey(currentBlock);
        const newBlockData = (block.get("data") as Map<string, any>).set("codeCellId", codeCellId);
        const newBlock = block.set("data", newBlockData) as ContentBlock;
        const newBlockMap = content.getBlockMap().set(currentBlock, newBlock);
        const newContent = ContentState.createFromBlockArray(newBlockMap.toArray());
        this.onChange(EditorState.push(this.state.editorState, newContent, "change-block-data"));
    }

    getPreviousCodeBlock(currentBlock: string) {
        const codeBlocks = this.state.editorState.getCurrentContent().getBlocksAsArray().filter((block: ContentBlock) => {
            return block.getType() === "code-block"
        });
        const currentBlockIndex = codeBlocks.findIndex((block: ContentBlock) => block.getKey() == currentBlock);
        return codeBlocks[currentBlockIndex - 1];
    }

    setUpInitialState(): any {
        const newBlocks = convertFromHTML("<h1>Welcome to Workbooks!</h1>").contentBlocks.concat(
            this.createNewEmptyBlock("code-block"))

        const newContentState = ContentState.createFromBlockArray(newBlocks);
        const newEditorState = EditorState.createWithContent(newContentState);
        this.onChange(newEditorState);
    }

    appendNewCodeCell() {
        const lastBlock = this.state.editorState.getCurrentContent().getBlocksAsArray().slice(-1)[0];
        let editorState = this.state.editorState;
        if (lastBlock.getType() === "code-block")
            editorState = this.insertBlockIntoState(this.createNewEmptyBlock("unstyled"), "last", false);
        editorState = this.insertBlockIntoState(this.createNewEmptyBlock("code-block"), "last", false);
        this.onChange(editorState)
    }

    createNewEmptyBlock(blockType: DraftBlockType): ContentBlock {
        const newBlock = new ContentBlock({
            key: genKey(),
            type: blockType,
            text: "",
            characterList: List()
        });

        return newBlock;
    }

    insertBlockIntoState(block: ContentBlock, insertPosition: "first" | "last" | Number, updateState: boolean): EditorState {
        const currentContent = this.state.editorState.getCurrentContent()
        const newBlockMap = currentContent.getBlockMap().set(block.getKey(), block)
        const blockArray = newBlockMap.toArray()
        const newBlockIndex = -1

        if (typeof insertPosition === "string") {
            if (insertPosition === "first")
                blockArray.unshift(block);
            else
                blockArray.push(block);
        } else if (typeof insertPosition === "number") {
            blockArray.splice(insertPosition, 0, block);
        }

        const contentState = ContentState.createFromBlockArray(blockArray);
        const newState = EditorState.push(this.state.editorState, contentState, "insert-fragment");
        if (updateState)
            this.onChange(newState);
        return newState;
    }

    selectNext(currentKey: string): boolean {
        this.editorReadOnly(false)

        const currentContent = this.getSelectionContext().currentContent
        let nextBlock = getNextBlockFor(currentContent, currentKey)
        let editorState = this.state.editorState;

        if (!nextBlock) {
            const currentBlockType = currentContent.getBlockForKey(currentKey).getType();
            if (currentBlockType !== "code-block")
                return false;
            nextBlock = this.createNewEmptyBlock("unstyled");
            editorState = this.insertBlockIntoState(nextBlock, "last", false);
        }

        let nextSelection: SelectionState = (SelectionState.createEmpty(nextBlock.getKey())
            .set('anchorOffset', 0)
            .set('focusOffset', 0) as SelectionState)

        editorState = EditorState.forceSelection(editorState, nextSelection)
        this.onChange(editorState);

        if (nextBlock.getType() !== "code-block")
            this.focus()

        return true
    }

    selectPrevious(currentKey: string): boolean {
        this.editorReadOnly(false)

        const currentContent = this.getSelectionContext().currentContent;
        let nextBlock = getPrevBlockFor(currentContent, currentKey)
        let editorState = this.state.editorState

        if (!nextBlock) {
            const currentBlockType = currentContent.getBlockForKey(currentKey).getType();
            if (currentBlockType !== "code-block")
                return false;
            nextBlock = this.createNewEmptyBlock("unstyled");
            editorState = this.insertBlockIntoState(nextBlock, "first", false);
        }

        let nextSelection: SelectionState = (SelectionState.createEmpty(nextBlock.getKey())
            .set('anchorOffset', nextBlock.getText().length)
            .set('focusOffset', nextBlock.getText().length) as SelectionState)

        editorState = EditorState.forceSelection(editorState, nextSelection)
        this.onChange(editorState)

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
        this.setState({ readOnly })
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

    editorKeyBinding(e: React.KeyboardEvent<{}>) {
        const keyCode = e.keyCode
        if (keyCode === EditorKeys.LEFT || keyCode === EditorKeys.RIGHT)
            this.onArrow(keyCode, e)
        if (keyCode === EditorKeys.BACKSPACE) {
            const selectionContext = this.getSelectionContext();
            const targetBlock = selectionContext.currentContent.getBlockBefore(selectionContext.selectionState.getFocusKey());
            const selectionIsStartOfLine = selectionContext.selectionState.getStartOffset() === 0;

            if (targetBlock && targetBlock.getType() === "code-block" && selectionIsStartOfLine) {
                this.sendMessage({
                    type: EditorMessageType.setSelection,
                    target: targetBlock.getKey(),
                    data: {
                        isBackwards: true,
                        keyCode: e.keyCode
                    }
                })
                return null;
            } else {
                return "backspace";
            }
        }

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

    getContentToSave(): string {
        return convertToMarkdown(this.state.editorState.getCurrentContent());
    }

    loadNewContent(newContent: string): Promise<any> {
        return convertFromMarkdown(newContent, this.props.shellContext.session).then((value: {
            contentState: Draft.ContentState,
            workbookMetadata: any,
        }) => {
            this.onChange(EditorState.createWithContent(value.contentState));
            return value.workbookMetadata;
        });
    }

    render() {
        return (
            <div className='WorkbookEditor-container' ref={div => this.editorContainer = div} onClick={(e) => this.focus(e)}>
                <Editor
                    ref="editor"
                    placeholder="Author content in markdown and use ``` to insert a new code cell"
                    spellCheck={false}
                    readOnly={this.state.readOnly}
                    blockRenderMap={blockRenderMap}
                    blockRendererFn={(block: Draft.ContentBlock) => this.blockRenderer(block)}
                    blockStyleFn={getBlockStyle}
                    customStyleMap={styleMap}
                    editorState={this.state.editorState}
                    onChange={(s: EditorState) => this.onChange(s)}
                    plugins={this.state.plugins}
                    keyBindingFn={(e: React.KeyboardEvent<{}>) => this.editorKeyBinding(e)}
                    handleKeyCommand={(e: string) => this.handleKeyCommand(e)}
                    onUpArrow={(e: React.KeyboardEvent<{}>) => this.onArrow(EditorKeys.UP, e)}
                    onDownArrow={(e: React.KeyboardEvent<{}>) => this.onArrow(EditorKeys.DOWN, e)}
                />
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