/**
 * Return next block if exists given a concrete block key
 * @param {Draft.ContentState} currentContent
 * @param {string} key
 * @return {Draft.ContentBlock?}
 */
export function getNextBlockFor(currentContent: Draft.ContentState, key: string): Draft.ContentBlock | null {
    let nextBlock = null
    let nextBlockIsTarget = false
    currentContent.getBlockMap().forEach((block?: Draft.ContentBlock) => {
        if (!block)
            return true;

        if (block.getKey() === key) {
            nextBlockIsTarget = true
        } else if (nextBlockIsTarget) {
            nextBlock = block
            return false //break loop
        }
        return true //continue loop
    })

    return nextBlock
}

/**
 * Return previous block if exists given a concrete block key
 * @param {Draft.ContentState} currentContent
 * @param {string} key
 * @return {Draft.ContentBlock?}
 */
export function getPrevBlockFor(currentContent: Draft.ContentState, key: string): Draft.ContentBlock | null {
    let prevBlock = null;
    currentContent.getBlockMap().forEach((block?: Draft.ContentBlock) => {
        if (!block)
            return true;

        if (block.getKey() === key) {
            return false //break loop
        }
        prevBlock = block
        return true //continue loop
    })

    return prevBlock
}

/**
 * Return true if selection is inside a code block
 *
 * @param {Draft.EditorState} editorState
 * @return {boolean}
 */
export function hasSelectionInCodeBlock(editorState: Draft.EditorState): boolean {
    var selection = editorState.getSelection()
    var contentState = editorState.getCurrentContent()
    var startKey = selection.getStartKey()
    var currentBlock = contentState.getBlockForKey(startKey)

    return (currentBlock.getType() === 'code-block')
}


export function isBlockBackwards(editorState: Draft.EditorState, targetBlockKey: string): boolean {
    let isBackwards = false
    const currentBlock = editorState.getSelection().getAnchorKey()
    editorState.getCurrentContent().getBlockMap().forEach((block?: Draft.ContentBlock) => {
        if (!block)
            return true;
        if (block.getKey() === currentBlock) {
            isBackwards = false
            return false //break loop
        } else if (block.getKey() === targetBlockKey) {
            isBackwards = true
            return false //break loop
        }
        return true //continue loop
    })

    return isBackwards
}