export const EditorMessageType = {
  setSelection: 'setSelection'
}

export interface EditorMessage {
  type: string
  target: string
  data?: any
}

export const EditorKeys = {
  LEFT: 37,
  UP: 38,
  RIGHT: 39,
  DOWN: 40
}