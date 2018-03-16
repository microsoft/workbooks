import * as uuidv4 from 'uuid/v4'

export function randomReactKey() {
    return uuidv4()
}

let _osMac: boolean | undefined
export function osMac() {
    if (_osMac === undefined)
        _osMac = /^mac/i.test(navigator.platform)
    return _osMac
}