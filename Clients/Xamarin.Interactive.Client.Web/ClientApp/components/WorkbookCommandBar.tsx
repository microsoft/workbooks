import * as React from 'react';
import { assign } from 'office-ui-fabric-react/lib/Utilities';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { Toggle } from 'office-ui-fabric-react/lib/Toggle';
import { IContextualMenuItem } from 'office-ui-fabric-react/lib/ContextualMenu';

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
    {
        key: 'openWorkbook',
        name: 'Open',
        icon: 'Upload',
        onClick: () => { }
    },
    {
        key: 'saveWorkbook',
        name: 'Save',
        icon: 'Download',
        onClick: () => { }
    }
]

const farItems: IContextualMenuItem[] = [
    {
        key: 'dumpDraftState',
        icon: 'Rocket',
        onClick: () => { }
    }
]

export class WorkbookCommandBar extends React.Component<any, any> {
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