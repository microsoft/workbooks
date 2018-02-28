export const enum CodeCellResultHandling {
    Replace = 'Replace',
    Append = 'Append'
}

export interface CodeCellResult {
    codeCellId: string
    resultHandling: CodeCellResultHandling
    type: string | null
    valueRepresentations: any[] | null
}