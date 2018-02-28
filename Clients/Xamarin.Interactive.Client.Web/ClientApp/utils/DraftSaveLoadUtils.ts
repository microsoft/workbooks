import * as toMarkdown from 'to-markdown'
import * as MarkdownIt from 'markdown-it'
import { stateToHTML } from 'draft-js-export-html'
import { ContentState, convertFromHTML } from 'draft-js';
import * as matter from 'gray-matter';

export function convertToMarkdown(contentState: Draft.ContentState): string {
    const htmlContent = stateToHTML(contentState);
    const options = {
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

    return '```csharp\n' + outputContent + '\n```';
}

export function convertFromMarkdown(workbook: string): ContentState {
    const { content, data } = splitMarkdownAndMetadata(workbook);
    const html = fixUpCodeElements(new MarkdownIt("commonmark").render(content));
    const result = convertFromHTML(html);
    return ContentState.createFromBlockArray(result.contentBlocks, result.entityMap);
}

function splitMarkdownAndMetadata(content: string): { content: string, data: {} } {
    return matter(content);
}

function fixUpCodeElements(html: string) {
    // Resulting html have code blocks with pre nodes with a single code node.
    // We don't need because draft convertFromHTML understands pre as code block, and code as inline code.
    return html
        .replace(/(<pre><code class=\"csharp language-csharp\">)|(<pre><code>)/g, "<pre>")
        .replace(/<\/code><\/pre>/g, "</pre>")
}