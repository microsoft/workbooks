import * as React from 'react';
import { hasSelectionInCodeBlock } from '../utils/DraftStateUtils'

// Custom overrides for "code" style.
export const styleMap = {
    CODE: {
        backgroundColor: 'rgba(0, 0, 0, 0.05)',
        fontFamily: '"Inconsolata", "Menlo", "Consolas", monospace',
        fontSize: 16,
        padding: 2,
        borderRadius: 2
    },
};

export function getBlockStyle(block: Draft.ContentBlock): string {
    switch (block.getType()) {
        case 'blockquote': return 'RichEditor-blockquote';
        default: return "";
    }
}

interface StyleButtonProps {
    onToggle: (e: string) => void
    style: string
    iconClass: string
    active: boolean
    label: string
}

interface StyleButtonState {
}

class StyleButton extends React.Component<StyleButtonProps, StyleButtonState> {
    onToggle: (e: any) => void;
    constructor() {
        super()
        this.onToggle = (e) => {
            e.preventDefault()
            this.props.onToggle(this.props.style)
        }
    }

    render() {
        let className = (this.props.iconClass) ? this.props.iconClass : ''

        className += ' menu-button'
        className += (this.props.active) ? ' menu-button--active' : ''

        return (
            <button type="button" className={className} onMouseDown={this.onToggle} title={this.props.label}>{this.props.label}</button>
        );
    }
}

const BLOCK_TYPES = [
    { label: 'H1', style: 'header-one', iconClass: 'menu-header-1 btn btn-primary' },
    { label: 'H2', style: 'header-two', iconClass: 'menu-header-2 btn btn-primary' },
    { label: 'H3', style: 'header-three', iconClass: 'menu-header-3 btn btn-primary' },
    { label: 'H4', style: 'header-four', iconClass: 'menu-header-4 btn btn-primary' },
    { label: 'H5', style: 'header-five', iconClass: 'menu-header-5 btn btn-primary' },
    { label: 'H6', style: 'header-six', iconClass: 'menu-header-6 btn btn-primary' },
    { label: 'Blockquote', style: 'blockquote', iconClass: 'menu-blockquote btn btn-primary' },
    { label: 'Unordered list', style: 'unordered-list-item', iconClass: 'menu-unordered-list btn btn-primary' },
    { label: 'Ordered list', style: 'ordered-list-item', iconClass: 'menu-ordered-list btn btn-primary' },
    { label: 'Code block', style: 'code-block', iconClass: 'menu-code btn btn-primary' },
]

function BlockStyleControls(props: {
    editorState: Draft.EditorState
    onToggle: (type: string) => void
}) {
    const { editorState } = props;
    const selection = editorState.getSelection();
    const blockType = editorState
        .getCurrentContent()
        .getBlockForKey(selection.getStartKey())
        .getType();

    return (
        <div className="menu-controls btn-group" role="group" aria-label="Block styles">
            {BLOCK_TYPES.map((type) =>
                <StyleButton
                    key={type.label}
                    active={type.style === blockType}
                    label={type.label}
                    iconClass={type.iconClass}
                    onToggle={props.onToggle}
                    style={type.style}
                />
            )}
        </div>
    );
};

var INLINE_STYLES = [
    { label: 'Bold', style: 'BOLD', iconClass: 'menu-bold btn btn-primary' },
    { label: 'Italic', style: 'ITALIC', iconClass: 'menu-italic btn btn-primary' },
    { label: 'Underline', style: 'UNDERLINE', iconClass: 'menu-underline btn btn-primary' },
    { label: 'Monospace', style: 'CODE', iconClass: 'menu-inline-code btn btn-primary' },
];


function InlineStyleControls(props: {
    editorState: Draft.EditorState
    onToggle: (type: string) => void
    className: string
}) {
    var currentStyle = props.editorState.getCurrentInlineStyle();
    let className = `menu-controls ${props.className} btn-group`
    return (
        <div className={className} role="group" aria-label="Inline styles">
            {INLINE_STYLES.map(type =>
                <StyleButton
                    key={type.label}
                    active={currentStyle.has(type.style)}
                    label={type.label}
                    iconClass={type.iconClass}
                    onToggle={props.onToggle}
                    style={type.style}
                />
            )}
        </div>
    );
};

interface EditorMenuProps {
    editorState: Draft.EditorState
    onToggleBlock: (blockType: string) => void
    onToggleInline: (inlineType: string) => void
}

export function EditorMenu(props: EditorMenuProps) {
    const inlineMenuClassName = hasSelectionInCodeBlock(props.editorState) ?
        "menu-controls--disabled" :
        ""

    return (
        <div className="menu">
            <div className="menu-block-controls-container">
                <BlockStyleControls
                    editorState={props.editorState}
                    onToggle={props.onToggleBlock}
                />
            </div>
            <br />
            <div className="menu-inline-controls-container">
                <InlineStyleControls
                    className={inlineMenuClassName}
                    editorState={props.editorState}
                    onToggle={props.onToggleInline}
                />
            </div>
        </div>
    )
}