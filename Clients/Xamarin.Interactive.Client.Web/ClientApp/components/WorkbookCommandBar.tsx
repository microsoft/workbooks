import * as React from 'react';
import { assign } from 'office-ui-fabric-react/lib/Utilities';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { Toggle } from 'office-ui-fabric-react/lib/Toggle';
import { IContextualMenuItem } from 'office-ui-fabric-react/lib/ContextualMenu';

const farItems: IContextualMenuItem[] = [
    {
        key: 'dumpDraftState',
        icon: 'Rocket',
        onClick: () => { }
    }
]

export interface WorkbookCommandBarProps {
    addPackages: () => void
}

export class WorkbookCommandBar extends React.Component<WorkbookCommandBarProps, any> {

    items: IContextualMenuItem[] = [
        {
            key: 'addPackage',
            name: 'Add NuGet Package',
            icon: 'Search',
            onClick: () => this.props.addPackages()
        },
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


    constructor(props: WorkbookCommandBarProps) {
        super(props)
    }

  public render() {
    return (
        <CommandBar
            elipisisAriaLabel='More options'
            items={this.items}
            farItems={farItems}
        />
    );
  }
}