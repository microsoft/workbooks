import * as React from 'react';
import { Fabric } from 'office-ui-fabric-react/lib/Fabric';

export interface LayoutProps {
    children?: React.ReactNode;
}

export class Layout extends React.Component<LayoutProps, {}> {
    public render() {
        return <Fabric>{this.props.children}</Fabric>;
    }
}