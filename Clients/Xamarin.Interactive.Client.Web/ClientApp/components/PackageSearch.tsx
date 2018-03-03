import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { WorkbookSession } from '../WorkbookSession'
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner'
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button'
import { Dialog, DialogType } from 'office-ui-fabric-react/lib/Dialog'
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox'
import { Image, ImageFit } from 'office-ui-fabric-react/lib/Image'

import './PackageSearch.scss'

interface PackageSearchProps {
    session: WorkbookSession
    getIsHidden: () => boolean
    notifyDismiss: () => void
}

interface PackageSearchState {
    query: string
    results: PackageViewModel[]
    selectedPackage?: PackageViewModel
    inProgress: boolean
    installedPackagesIds: string[]
}

interface PackageViewModel {
    id: string
    version: string
    iconUrl: string
    description: string
}

export class PackageSearch extends React.Component<PackageSearchProps, PackageSearchState> {
    constructor(props: PackageSearchProps) {
        super(props)
        this.state = {
            query: "",
            results: [],
            inProgress: false,
            installedPackagesIds: [],
        }
    }

    public render() {
        return <div>
            <Dialog
                hidden={this.props.getIsHidden()}
                dialogContentProps={{
                    type: DialogType.largeHeader,
                    title: "Add NuGet Packages",
                    subText: "NuGet is the package manager for .NET. Find the library you need from millions of popular packages.",
                    className: "packageManagerDialog",
                    showCloseButton: true
                }}
                modalProps={{
                    isBlocking: this.state.inProgress,
                    onDismiss: () => this.notifyDismiss()
                }}>

                <SearchBox
                    placeholder="Search NuGet"
                    onChange={event => this.onSearchFieldChanged(event)} />

                <div className="packageListContainer">
                    {/*
                    I wanted to use Fabric List but individual cells weren't rerendering on state changes
                    <List
                        // className="form-control"
                        // size={this.state.query.length > 0 ? 10 : 0}
                        // onChange={event => this.onSelectedPackageChanged(event)}
                        items={this.state.results}
                        onRenderCell={(item, index, isScrolling) => this._onRenderCell(item, index, isScrolling)}
                    /> */}
                    {
                        this.state.results.map(item => (
                            <div className="packageListItemContainer">
                            <div className="packageListItem">
                                <Image
                                    className="packageListItemIcon"
                                    width={ 50 }
                                    // imageFit={ImageFit.contain}
                                    src={item.iconUrl} />
                                <div className="packageListInfoContainer">
                                    <div className="packageListItemName">
                                        {item.id}
                                    </div>
                                    <div className="packageListItemVersion">
                                        {item.version}
                                    </div>
                                    <div className="packageListItemDescription">
                                        {item.description}
                                        </div>
                                    <div className="packageListActionContainer">
                                            <PrimaryButton
                                                className="packageInstallButton"
                                                text={this.isPackageInstalled(item) ? "installed" : "Install"}
                                                disabled={this.state.inProgress || this.isPackageInstalled(item)}
                                                onClick={() => this.installPackage(item)}
                                            />
                                            {
                                                this.state.inProgress && this.state.selectedPackage === item ?
                                                    <Spinner className="packageInstallSpinner" size={SpinnerSize.medium} /> :
                                                    null
                                            }
                                    </div>
                                </div>
                                </div>
                            <hr />
                            </div>
                        ))
                    }
                </div>
            </Dialog>
        </div>
    }

    isPackageInstalled(pkg: PackageViewModel): boolean {
        return this.state.installedPackagesIds.find(id => id === pkg.id) !== undefined
    }

    async onSearchFieldChanged(input: string) {
        // TODO: Cancellation or at least ignoring of results we no longer care about (as we type)
        let query = input.trim()

        let results = []
        if (query) {
            // TODO: Add supportedFramework to query? Seems like a good idea but it doesn't seem to change results
            let result = await fetch("https://api-v2v3search-0.nuget.org/query?prerelease=false&q=" + query)
            var json = await result.json()
            results = json.data
        }
        this.setState({
            query: query,
            results: results
        })
    }

    async installSelectedPackage() {
        await this.installPackage(this.state.selectedPackage)
    }

    async installPackage(pkg: PackageViewModel|undefined) {
        if (!pkg || this.state.inProgress)
            return

        this.setState({
            inProgress: true,
            selectedPackage: pkg
        })

        console.log(pkg)
        let installedPackageIds = await this.props.session.installPackage(
            pkg.id,
            pkg.version)

        this.setState({
            inProgress: false,
            installedPackagesIds: installedPackageIds,
            selectedPackage: undefined
        })
    }

    notifyDismiss() {
        this.setState({
            inProgress: false,
            selectedPackage: undefined,
            results: [],
            query: ""
        })
        this.props.notifyDismiss()
    }
}
