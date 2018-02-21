//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import {
  Editor,
  EditorState,
  RichUtils,
  AtomicBlockUtils,
  DraftEditorCommand,
  KeyBindingUtil,
  getDefaultKeyBinding,
  EditorBlock,
  ContentBlock,
} from 'draft-js';
import { CodeCell } from './CodeCell'

const { hasCommandModifier } = KeyBindingUtil;

const insertCodeCellCommand = 'insert-code-cell';

interface WorkbookEditorProps {
}

interface WorkbookEditorComponentState {
  editorState: EditorState;
}

export class WorkbookEditor extends React.Component<WorkbookEditorProps, WorkbookEditorComponentState> {
  constructor(props: WorkbookEditorProps) {
    super(props);
    this.state = { editorState: EditorState.createEmpty() };
  }

  handleChange(editorState: EditorState) {
    this.setState({ editorState });
  }

  handleKeyCommand(command: string, editorState: EditorState) {
    if (command === insertCodeCellCommand) {
      console.log('woooo');
      const contentState = editorState.getCurrentContent();
      const contentStateWithEntity = contentState.createEntity(
        'code-cell',
        'IMMUTABLE',
        {}
      );

      const entityKey = contentStateWithEntity.getLastCreatedEntityKey();
      const newEditorState = EditorState.set(
        editorState,
        {currentContent: contentStateWithEntity}
      );

      this.setState({
        editorState: AtomicBlockUtils.insertAtomicBlock(
          newEditorState,
          entityKey,
          ' '
        )
      });

      return 'handled';
    }

    const newState = RichUtils.handleKeyCommand(editorState, command);
    if (newState) {
      this.handleChange(newState);
      return 'handled';
    }
    return 'not-handled';
  }

  handleKeyBinding(e: React.KeyboardEvent<{}>): string | null {
    if (e.keyCode === 83 /* `S` key */ && hasCommandModifier(e)) {
      return insertCodeCellCommand;
    }
    return getDefaultKeyBinding(e);
  }

  render() {
    return (
        <div className='WorkbookEditor-Container'>
          <Editor
            editorState={this.state.editorState}
            keyBindingFn={e => this.handleKeyBinding(e)}
            blockRendererFn={b => this.blockRenderer(b)}
            handleKeyCommand={(c, e) => this.handleKeyCommand(c, e)}
            onChange={e => this.handleChange(e)}
            spellCheck={true} />
        </div>
    );
  }

  blockRenderer(block: ContentBlock): any {
    if (block.getType () === 'atomic')
      return {
        component: this.getBlockComponent,
        editable: false
      };
    return null;
  }

  getBlockComponent(props: any): any {
    return <CodeCell />
  }
}