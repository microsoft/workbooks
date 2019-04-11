import * as uuidv4 from 'uuid/v4'

export function randomReactKey() {
    return uuidv4()
}

interface ClassNamePredicates {
    [className: string]: boolean | (() => boolean)
}

/**
 * Flatten an array of class names into a well formed string
 * suitable for React's `className` property.
 *
 * @param names an array of CSS class names. Array elements may be string
 * or an object whose property names map to a boolean or a predicate
 * function to indicate whether the class name should be included.
 *
 * @example
 * classNames(
 *   'AlwaysInclude',
 *   {
 *     'SometimesInclude: false,
 *     'AlwaysInclude': true,
 *     'MaybeInclude': () => Math.random() >= 0.5
 *   },
 *   null,
 *   undefined) => ['AlwaysInclude', 'MaybeInclude'?]
 */
export function classNames(...names: (ClassNamePredicates | string | undefined | null)[]): string {
    const selectedNames: string[] = []

    function selectName(name: string) {
        if (selectedNames.indexOf(name) < 0)
            selectedNames.push(name)
    }

    for (const name of names) {
        if (!name)
            continue

        if (typeof name === 'string') {
            selectName(name)
            continue
        }

        for (const key in name) {
            if (name.hasOwnProperty(key)) {
                let predicate = name[key]
                if (predicate instanceof Function)
                    predicate = predicate()
                if (predicate)
                    selectName(key)
            }
        }
    }

    return selectedNames.join(' ')
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

export function debounce(action: () => void, delay: number = 0): () => void {
    var debounceTimeout: number
    return () => {
        clearTimeout(debounceTimeout)
        debounceTimeout = window.setTimeout(
            () => window.requestAnimationFrame(action),
            delay)
    }
}