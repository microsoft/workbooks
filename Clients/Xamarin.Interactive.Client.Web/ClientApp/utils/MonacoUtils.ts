import { WorkbookShellContext } from '../components/WorkbookShell'

export interface MonacoCellMapper {
    registerCellInfo(codeCellId: string, monacoModelId: string): void
    getCodeCellId(monacoModelId: string): string|null
}

export class WorkbookCompletionItemProvider implements monaco.languages.CompletionItemProvider {
    triggerCharacters = ['.']
    private shellContext: WorkbookShellContext
    private mapper: MonacoCellMapper

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

        return await this.shellContext.session.getCompletions(
            codeCellId,
            position)
    }
}

export class WorkbookHoverProvider implements monaco.languages.HoverProvider {
    private shellContext: WorkbookShellContext
    private mapper: MonacoCellMapper

    constructor(shellContext: WorkbookShellContext, mapper: MonacoCellMapper) {
        this.shellContext = shellContext
        this.mapper = mapper
    }

    async provideHover(
        model: monaco.editor.IReadOnlyModel,
        position: monaco.Position,
        token: monaco.CancellationToken) {
        // TODO: Investigate best way to consume Monaco CancellationTokens
        let modelId = (model as monaco.editor.IModel).id // TODO: Replace with URI usage to avoid cast?

        let codeCellId = this.mapper.getCodeCellId(modelId)

        if (codeCellId == null)
            return <monaco.languages.Hover>{}

        return await this.shellContext.session.getHover(
            codeCellId,
            position)
    }
}

export class WorkbookSignatureHelpProvider implements monaco.languages.SignatureHelpProvider {
    signatureHelpTriggerCharacters = ['(', ',']

    private shellContext: WorkbookShellContext
    private mapper: MonacoCellMapper

    constructor(shellContext: WorkbookShellContext, mapper: MonacoCellMapper) {
        this.shellContext = shellContext
        this.mapper = mapper
    }

    async provideSignatureHelp(
        model: monaco.editor.IReadOnlyModel,
        position: monaco.Position,
        token: monaco.CancellationToken) {
        // TODO: Investigate best way to consume Monaco CancellationTokens
        let modelId = (model as monaco.editor.IModel).id // TODO: Replace with URI usage to avoid cast?

        let codeCellId = this.mapper.getCodeCellId(modelId)

        if (codeCellId == null)
            return <monaco.languages.SignatureHelp>{}

        return await this.shellContext.session.getSignatureHelp(
            codeCellId,
            position)
    }
}