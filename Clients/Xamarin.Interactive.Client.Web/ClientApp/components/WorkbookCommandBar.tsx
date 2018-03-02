import * as React from 'react';
import { assign } from 'office-ui-fabric-react/lib/Utilities';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { Toggle } from 'office-ui-fabric-react/lib/Toggle';
import { IContextualMenuItem } from 'office-ui-fabric-react/lib/ContextualMenu';

const openWorkbookItem: IContextualMenuItem = {
    key: 'openWorkbook',
    name: 'Open',
    icon: 'Upload',
    onClick: () => { }
}

const saveWorkbookItem: IContextualMenuItem = {
    key: 'saveWorkbook',
    name: 'Save',
    icon: 'Download',
    onClick: () => { }
}

const items: IContextualMenuItem[] = [
    {
        key: 'workbookTarget',
        name: 'Workbook Target',
        subMenuProps: {
            items: [
                {
                    key: 'workbookTarget-Console',
                    name: 'Console',
                    subMenuProps: {
                        items: [
                            {
                                key: 'workbookTarget-DotNetCore',
                                name: '.NET Core'
                            },
                            {
                                key: 'workbookTarget-Console',
                                name: '.NET Framework'
                            }
                        ]
                    }
                }
            ]
        }
    },
    openWorkbookItem,
    saveWorkbookItem
]

const dumpDraftState: IContextualMenuItem = {
    key: 'dumpDraftState',
    icon: 'Rocket',
    onClick: () => { }
}

const farItems: IContextualMenuItem[] = [
    dumpDraftState
]

interface WorkbookCommandBarProps {
    loadWorkbook: () => void
    saveWorkbook: () => void
    dumpDraftState: () => void
}

export class WorkbookCommandBar extends React.Component<WorkbookCommandBarProps, any> {
  constructor(props: WorkbookCommandBarProps) {
    super(props)
    saveWorkbookItem.onClick = props.saveWorkbook
    openWorkbookItem.onClick = props.loadWorkbook
    dumpDraftState.onClick = props.dumpDraftState
  }

  public render() {
    return (
        <CommandBar
            isSearchBoxVisible={true}
            searchPlaceholderText='Add a NuGet Package...'
            elipisisAriaLabel='More options'
            items={items}
            farItems={farItems}
        />
    );
  }
}