import * as toMarkdown from 'to-markdown'
import * as MarkdownIt from 'markdown-it'
import { stateToHTML } from 'draft-js-export-html'
import { ContentState, convertFromHTML, ContentBlock } from 'draft-js';
import * as matter from 'gray-matter';
import { WorkbookSession } from '../WorkbookSession';

export function convertToMarkdown(contentState: Draft.ContentState): string {
    const htmlContent = stateToHTML(contentState);
    const options: toMarkdown.Options = {
        converters: [
            {
                filter: 'pre',
                replacement: codeBlocksSerializer
            }
        ]
    };
    return toMarkdown(htmlContent, options);
}

function codeBlocksSerializer(content: string): string {
    let outputContent = content
    // pre blocks have only one child node <code>, when exporting, it is removed
    const startCodeTagIndex = 6
    const endCodeTagIndex = content.length - 7
    if (content.substring(0, startCodeTagIndex) === "<code>" && content.substring(endCodeTagIndex, content.length).trim() === "</code>") {
        outputContent = content.substring(startCodeTagIndex, endCodeTagIndex)
    }
    // For some reason, new lines are duplicated on code blocks
    outputContent = outputContent.replace(/(?:\n\n)/g, '\n');

    return '```csharp\n' + outputContent + '\n```\n';
}

export async function convertFromMarkdown(workbook: string, session: WorkbookSession): Promise<{
    contentState: ContentState,
    workbookMetadata: any
}> {
    const { content, data } = splitMarkdownAndMetadata(workbook);
    const md = new MarkdownIt("commonmark", {
        breaks: true,
    });
    const html = fixUpCodeElements(md.render(content));
    const { contentBlocks, entityMap } = convertFromHTML(html);

    // Load all the code cells into Roslyn. This prevents us from having to deal
    // with insanity with regards to cell ordering and async orderin later.
    let previousDocumentId: string | null = null;
    for (let index = 0; index < contentBlocks.length; index++) {
        const block = contentBlocks[index];

        if (block.getType() !== "code-block")
            continue;

        const codeCellId: string = await session.insertCodeCell(block.getText(), previousDocumentId);
        console.log(`Inserted code cell, new ID is ${codeCellId}, previous cell's ID was ${previousDocumentId}`)
        const newBlockData = (block.get("data") as Map<string, any>).set("codeCellId", codeCellId);
        const newBlock = block.merge({
            data: newBlockData,
            text: block.getText().trim()
        }) as ContentBlock;
        contentBlocks[index] = newBlock;
        previousDocumentId = codeCellId;
    }

    return {
        contentState: ContentState.createFromBlockArray(contentBlocks, entityMap),
        workbookMetadata: data
    }
}

function splitMarkdownAndMetadata(content: string): { content: string, data: {} } {
    return matter(content);
}

function fixUpCodeElements(html: string) {
    // Resulting html have code blocks with pre nodes with a single code node.
    // We don't need because draft convertFromHTML understands pre as code block, and code as inline code.
    return html
        .replace(/(<pre><code class=\"language-csharp\">)|(<pre><code>)/g, "<pre>")
        .replace(/<\/code><\/pre>/g, "</pre>")
}