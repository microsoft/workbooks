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

export class WorkbookEditor extends React.Component<WorkbooksEditorProps, WorkbooksEditorState> implements MonacoCellMapper {
    lastFocus?: Date;
    subscriptors: ((m: EditorMessage) => void)[];
    monacoProviderTickets: monaco.IDisposable[] = [];
    cellIdMappings: WorkbookCellIdMapping[] = [];

    constructor(props: WorkbooksEditorProps) {
        super(props);

        this.subscriptors = []
        this.lastFocus = undefined

        const editorState = EditorState.createEmpty()

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
        this.monacoProviderTickets.push(
            monaco.languages.registerHoverProvider(
                "csharp",
                new WorkbookHoverProvider(this.props.shellContext, this)))
        this.monacoProviderTickets.push(
            monaco.languages.registerSignatureHelpProvider(
                "csharp",
                new WorkbookSignatureHelpProvider(this.props.shellContext, this)))
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
            const codeCellId: string = block.getData().get("codeCellId");
            return {
                component: CodeCell,
                editable: false,
                props: {
                    shellContext: this.props.shellContext,
                    rendererRegistry: this.props.shellContext.rendererRegistry,
                    cellMapper: this,
                    codeCellId,
                    editorReadOnly: (readOnly: boolean) => this.editorReadOnly(readOnly),
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
        this.setState({
            editorState: EditorState.push(this.state.editorState, newContent, "change-block-data"),
        });
    }

    getPreviousCodeBlock(currentBlock: string) {
        const codeBlocks = this.state.editorState.getCurrentContent().getBlocksAsArray().filter((block: ContentBlock) => {
            return block.getType() === "code-block"
        });
        const currentBlockIndex = codeBlocks.findIndex((block: ContentBlock) => block.getKey() == currentBlock);
        return codeBlocks[currentBlockIndex - 1];
    }

    // TODO: This should just be a special case of createNewEmptyBlock. Clean up ASAP
    appendNewCodeCell() {
        let currentContent = this.state.editorState.getCurrentContent()

        // Empty block in between cells looks nicer
        this.createNewEmptyBlock(currentContent, false)

        currentContent = this.state.editorState.getCurrentContent()

        const newBlock = new ContentBlock({
            key: genKey(),
            type: "code-block",
            text: "",
            characterList: List()
        })

        const newBlockMap = currentContent.getBlockMap().set(newBlock.getKey(), newBlock)
        const blockArray = newBlockMap.toArray()

        const contentState = ContentState.createFromBlockArray(blockArray)

        this.onChange(EditorState.push(
            this.state.editorState,
            contentState,
            "insert-fragment"
        ))
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
        let nextSelection: SelectionState = (SelectionState.createEmpty(nextBlock.getKey())
            .set('anchorOffset', nextBlock.getText().length)
            .set('focusOffset', nextBlock.getText().length) as SelectionState)

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
            this.setState({
                editorState: EditorState.createWithContent(value.contentState)
            });
            return value.workbookMetadata;
        });
    }

    render() {
        return (
            <div className='WorkbookEditor-container' onClick={(e) => this.focus(e)}>
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