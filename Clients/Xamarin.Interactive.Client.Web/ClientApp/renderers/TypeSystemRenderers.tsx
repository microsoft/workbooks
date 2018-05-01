//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react'
import { CodeCellResult } from '../evaluation';
import {
    ResultRenderer,
    ResultRendererRepresentation,
    getFirstRepresentationOfType
} from '../rendering'
import { randomReactKey } from '../utils'

function typeName(name: string): string {
    return `Xamarin.Interactive.Representations.Reflection.${name}`
}

type LanguageName = 'csharp'

const TypeNodeDataTypeName = typeName('TypeNode')

interface Type {
    typeName: TypeSpec
}

interface TypeName {
    namespace?: string
    name: string
    typeArgumentCount?: number
}

type Modifier = number | 'Pointer' | 'ByRef' | 'BoundArray'

interface TypeSpec {
    name: TypeName
    assemblyName: string
    modifiers?: Modifier[]
    nestedNames?: TypeName[]
    typeArguments?: TypeSpec[]
}

export default function TypeSpecRendererFactory(result: CodeCellResult) {
    return getFirstRepresentationOfType(result, TypeNodeDataTypeName)
        ? new TypeSpecRenderer
        : null
}

class TypeSpecRenderer implements ResultRenderer {
    getRepresentations(result: CodeCellResult) {
        const value = getFirstRepresentationOfType<Type>(
            result,
            TypeNodeDataTypeName)

        if (value)
            return [{
                key: randomReactKey(),
                displayName: 'Type',
                component: TypeRepresentation,
                componentProps: { value }
            }]

        return []
    }
}

interface TypeNameRepresentationProps {
    languageName: LanguageName
    typeName: TypeName
    typeArguments?: TypeSpec[]
    isNestedName?: boolean
}

class TypeNameRepresentation extends React.PureComponent<TypeNameRepresentationProps> {
    render(): JSX.Element {
        const typeArguments = (this.props.typeArguments && this.props.typeArguments.length > 0
            ? this.props.typeArguments
            : new Array(this.props.typeName.typeArgumentCount || 0).fill(null))
            .map((typeArgument, i) => {
                return (
                    <span key={i}>
                        { i > 0 && ', '}
                        { typeArgument && <TypeSpecRepresentation
                            languageName={this.props.languageName}
                            typeSpec={typeArgument}/> }
                    </span>
                )
            })

        let typeArgumentContainer: {
            name?: JSX.Element
            open?: string,
            close?: string
        }

        if (this.props.languageName === 'csharp' &&
            !this.props.isNestedName &&
            this.props.typeName.namespace === 'System' &&
            this.props.typeName.name === 'ValueTuple')
            typeArgumentContainer = {
                open: '(',
                close: ')'
            }
        else
            typeArgumentContainer = {
                name: this.renderTypeName(),
                open: '<',
                close: '>'
            }

        return (
            <span className='typename'>
                {this.props.isNestedName && '+'}
                {typeArgumentContainer.name}
                {typeArguments.length > 0 && (
                    <span>
                        {typeArgumentContainer.open}
                        {typeArguments}
                        {typeArgumentContainer.close}
                    </span>
                )}
            </span>
        )
    }

    private renderTypeName(): JSX.Element {
        let keyword = typeNameToLanguageKeyword(this.props.languageName, this.props.typeName)
        if (keyword)
            return <span className='typename-keyword mtk6'>{keyword}</span>

        return (
            <span className='typename-name'>
                {this.props.typeName.namespace &&
                    <span className='typename-namespace mtk3'>{this.props.typeName.namespace}.</span>}

                {this.props.typeName.name}
            </span>
        )
    }
}

interface TypeSpecRepresentationProps {
    languageName: LanguageName
    typeSpec: TypeSpec
}

class TypeSpecRepresentation extends React.PureComponent<TypeSpecRepresentationProps> {
    render() {
        const ts = this.props.typeSpec
        let typeArgumentOffset = 0

        const allNames = [ts.name].concat(ts.nestedNames || []).map((name, i) => {
            let typeArgumentsForName: TypeSpec[] = []
            if (ts.typeArguments && ts.typeArguments.length && name.typeArgumentCount) {
                typeArgumentsForName = ts.typeArguments.slice(
                    typeArgumentOffset,
                    typeArgumentOffset + name.typeArgumentCount)
                typeArgumentOffset += name.typeArgumentCount
            }

            return <TypeNameRepresentation
                key={i}
                languageName={this.props.languageName}
                typeName={name}
                typeArguments={typeArgumentsForName}
                isNestedName={i > 0}/>
        })

        let modifiers = ''

        if (ts.modifiers) {
            for (const modifier of ts.modifiers) {
                switch (modifier) {
                    case 'Pointer':
                        modifiers += '*'
                        break
                    case 'ByRef':
                        modifiers += '&'
                        break
                    case 'BoundArray':
                        modifiers += '[*]'
                        break
                    default:
                        modifiers += `[${','.repeat(modifier - 1)}]`
                        break
                }
            }
        }

        return <span>{allNames}{modifiers}</span>
    }
}

class TypeRepresentation extends React.Component<{ value: Type }> {
    render() {
        return (
            <code>
                <TypeSpecRepresentation
                    languageName={'csharp'}
                    typeSpec={this.props.value.typeName}/>
            </code>
        )
    }
}

function typeNameToLanguageKeyword(language: LanguageName, typeName: TypeName): string | null {
    if (language !== 'csharp')
        return null

    if (!typeName || typeName.namespace !== 'System')
        return null

    switch (typeName.name) {
        case 'Void':
            return 'void'
        case 'SByte':
            return 'sbyte'
        case 'Byte':
            return 'byte'
        case 'Double':
            return 'double'
        case 'Decimal':
            return 'decimal'
        case 'Char':
            return 'char'
        case 'String':
            return 'string'
        case 'Object':
            return 'object'
        case 'Boolean':
            return 'bool'
        case 'Int16':
            return 'short'
        case 'UInt16':
            return 'ushort'
        case 'Int32':
            return 'int'
        case 'UInt32':
            return 'uint'
        case 'Int64':
            return 'long'
        case 'UInt64':
            return 'ulong'
        case 'Single':
            return 'float'
        case 'nint':
            return 'nint'
        case 'nuint':
            return 'nuint'
        case 'nfloat':
            return 'nfloat'
    }

    return null
}