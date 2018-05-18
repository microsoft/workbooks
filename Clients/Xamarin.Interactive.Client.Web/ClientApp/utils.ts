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

let _isSafari: boolean | undefined
export function isSafari() {
    const _window = <any>window
    if (_isSafari === undefined)
        _isSafari = /Apple/i.test(navigator.vendor)
    return _isSafari
}