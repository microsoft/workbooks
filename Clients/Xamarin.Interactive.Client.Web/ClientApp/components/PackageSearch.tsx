import * as React from 'react'
import { RouteComponentProps } from 'react-router'
import { WorkbookSession } from '../WorkbookSession'
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner'
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button'
import { Panel, PanelType } from 'office-ui-fabric-react/lib/Panel'
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox'
import { Icon } from 'office-ui-fabric-react/lib/Icon'
import 'isomorphic-fetch'

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
    totalDownloads: number
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

    private renderListHeader() {
        return (
            <div className='PackageSearch-header'>
                <p className='PackageSearch-header-text' role='heading'>
                    Add NuGet Packages
                </p>
                <SearchBox
                    className='PackageSearch-search'
                    placeholder='Search for packages…'
                    // TODO: Figure out why setting this makes typing so finicky.
                    //       Once this works right, we can consider not clearing
                    //       query/results on dismiss. --sandy 2018-03-05
                    // value={this.state.query}
                    onChange={event => this.onSearchFieldChanged(event)} />
            </div>
        )
    }

    render() {
        return (
            <Panel
                className='PackageSearch-panel'
                type={PanelType.medium}
                isLightDismiss={true}
                isOpen={!this.props.getIsHidden()}
                onRenderHeader={() => this.renderListHeader()}
                // TODO: File bug...pretty sure this prop is backwards --sandy 2018-03-05
                isBlocking={!this.state.inProgress}
                onDismiss={() => this.notifyDismiss()}
                focusTrapZoneProps={{
                    firstFocusableSelector: 'PackageSearch-search'
                }}>
                <div className="PackageSearch-list">
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
                        this.state.results.map(item => {
                            const defaultIconUrl = 'https://nuget.org/Content/gallery/img/default-package-icon.svg'
                            let buttonLabel: string = 'Install'
                            let buttonDisabled: boolean = false
                            let spinnerVisible: boolean = false

                            if (this.isPackageInstalled(item)) {
                                buttonDisabled = true
                                buttonLabel = 'Installed'
                            } else if (this.state.inProgress) {
                                buttonDisabled = true
                                if (this.state.selectedPackage === item) {
                                    spinnerVisible = true
                                    buttonLabel = 'Installing…'
                                }
                            }

                            return (
                                <div
                                    key={item.id}
                                    className='PackageSearch-item'>
                                    <div
                                        className='PackageSearch-item-icon'
                                        style={{ backgroundImage: `url(${item.iconUrl || defaultIconUrl}` }}/>
                                    <div className='PackageSearch-item-details'>
                                        <h1>
                                            {item.id}
                                        </h1>
                                        <div className="PackageSearch-item-details-summary">
                                            <span className='PackageSearch-item-details-version'>
                                                v{item.version}
                                            </span>
                                            <span className='PackageSearch-item-details-downloads'>
                                                {item.totalDownloads.toLocaleString()} total downloads
                                            </span>
                                        </div>
                                        <div className="PackageSearch-item-details-description">
                                            {item.description}
                                        </div>
                                        <div className="PackageSearch-item-details-actions">
                                            <PrimaryButton
                                                text={buttonLabel}
                                                disabled={buttonDisabled}
                                                onClick={() => this.installPackage(item)}/>
                                            {spinnerVisible && <Spinner size={SpinnerSize.medium}/>}
                                        </div>
                                    </div>
                                </div>
                            )
                        })
                    }
                </div>
            </Panel>
        )
    }

    isPackageInstalled(pkg: PackageViewModel): boolean {
        return this.state.installedPackagesIds.find(id => id === pkg.id) !== undefined
    }

    async onSearchFieldChanged(input: string) {
        // TODO: Cancellation or at least ignoring of results we no longer care about (as we type)
        let query = input.trim()

        let results = []
        if (query) {
            const pageSize = 100 // Totally abritrary. "json" search has about 1500 results
            // TODO: Add supportedFramework to query? Seems like a good idea but it doesn't seem to change results
            let result = await fetch(`https://api-v2v3search-0.nuget.org/query?prerelease=false&q=${query}&take=${pageSize}`)
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
        let installedPackageIds = await this.props.session.installPackages([
            {
                packageId: pkg.id,
                identityVersion: pkg.version,
                isExplicitlySelected: true
            }
        ])

        this.setState({
            inProgress: false,
            installedPackagesIds: installedPackageIds
                .map(packageDescription => packageDescription.packageId),
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
