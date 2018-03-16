export const EditorMessageType = {
    setSelection: 'setSelection',
    setCursor: 'setCursor'
}

export interface EditorMessage {
  type: string
  target: string
  data?: any
}

export const EditorKeys = {
  BACKSPACE: 8,
  LEFT: 37,
  UP: 38,
  RIGHT: 39,
  DOWN: 40
}