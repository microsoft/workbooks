// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * Resolve object references via `$id`/`$ref` from Newtonsoft.Json when
 * ```
 * JsonSerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects
 * ```
 * @param obj The object to recursively resolve
 * @param references The dictionary of references already collected via `$id`
 * @returns An object graph with all references resolved and any `$id` removed.
 */
export function resolveJsonReferences(obj: any, references: {[key: string]: any} = {}): any {
    if (!(obj instanceof Object))
        return obj

    if (typeof obj.$ref === 'string')
        return references[obj.$ref]

    if (typeof obj.$id === 'string') {
        references[obj.$id] = obj
        delete obj.$id
    }

    for (let key in obj) {
        const original = obj[key]
        const resolved = resolveJsonReferences(original, references)
        if (original !== resolved)
            obj[key] = resolved
    }

    return obj
}