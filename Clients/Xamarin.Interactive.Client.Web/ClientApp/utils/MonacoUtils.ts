import { WorkbookShellContext } from '../components/WorkbookShell'

export interface MonacoCellMapper {
    registerCellInfo(codeCellId: string, monacoModelId: string): void
    getCodeCellId(monacoModelId: string): string|null
}

export class WorkbookCompletionItemProvider implements monaco.languages.CompletionItemProvider {
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