//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { loadScript } from './ScriptLoader'

export const enum InitState {
    Uninitialized = 'Uninitialized',
    Initializing = 'Initializing',
    Initialized = 'Initialized',
    Error = 'Error'
}

class MonacoLoader {
    static initState: InitState = InitState.Uninitialized;

    private static changeState(newState: InitState) {
        console.log("MonacoLoader:changeState: %O → %O", MonacoLoader.initState, newState);
        MonacoLoader.initState = newState;
    }

    static initialize(initializeCallback: (initState: InitState) => void) {
        if (MonacoLoader.initState !== InitState.Uninitialized)
            return;

        MonacoLoader.changeState(InitState.Initializing);

        loadScript('/vs/loader.js', () => {
            // Keep in sync with MonacoMutingMiddleware
            var requiredFiles = [
                'vs/editor/editor.main',
                'vs/platform/keybinding/common/keybindingsRegistry',
                'vs/platform/contextkey/common/contextkey'
            ];

            (<any>window).require(requiredFiles, (em: any, keybindingsRegistry: any, contextKeyExpr: any): void => {
                // keybinding registry is not public, see https://github.com/Microsoft/monaco-editor/issues/102/
                keybindingsRegistry.KeybindingsRegistry.registerKeybindingRule({
                    id: '^acceptSelectedSuggestion',
                    primary: monaco.KeyCode.US_DOT,
                    when: contextKeyExpr.ContextKeyExpr.deserialize('editorTextFocus && suggestWidgetVisible && editorLangId == \'csharp\' && suggestionSupportsAcceptOnKey'),
                    weight: 90 // This number is not that important for us
                });

                MonacoLoader.changeState(InitState.Initialized);
                initializeCallback(MonacoLoader.initState);
            });
        });
    }
}

export function initializeMonaco(initializeCallback: (initState: InitState) => void) {
    MonacoLoader.initialize(initializeCallback);
}