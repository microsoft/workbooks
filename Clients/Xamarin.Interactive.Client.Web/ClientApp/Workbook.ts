//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as matter from "gray-matter"
import * as uuidv4 from "uuid/v4"
import { WorkbookTarget, WorkbookSession, PackageDescription } from "./WorkbookSession"
import { Map, List } from "immutable"
import { loadAsync, JSZipObject } from "jszip"

const defaultUti: string = "com.xamarin.workbook"
const manifestPlatformToWorkbookTargetMap: Map<string, string> = Map({
    "ios": "ios-xamarinios",
    "macnet45": "mac-xamarinmac-full",
    "macmobile": "mac-xamarinmac-modern",
    "android": "android-xamarinandroid",
    "wpf": "wpf",
    "console": "console",
    "dotnetcore": "console-netcore"
});

function getWorkbookTargetIdFromPlatform(platform: string): string {
    return manifestPlatformToWorkbookTargetMap.get(platform.toLowerCase())
}

function getPlatformFromWorkbookTargetId(workbookTargetId: string): string {
    return manifestPlatformToWorkbookTargetMap.findKey(val => val === workbookTargetId)
}

function splitMarkdownAndMetadata(workbookContent: string): { content: string, data: {} } {
    return matter(workbookContent)
}

export async function loadWorkbookFromString(workbookSession: WorkbookSession, fileName: string, workbookContent: string): Promise<Workbook> {
    const { content, data } = splitMarkdownAndMetadata(workbookContent)
    return new Workbook(workbookSession, fileName, content, data)
}

export async function loadWorkbookFromUrl(workbookSession: WorkbookSession, url: string): Promise<Workbook> {
    var response = await fetch(url)

    if (!response.ok)
        throw new Error("Couldn't load the workbook")

    var content = await response.text()
    return await loadWorkbookFromString (workbookSession, url, content)
}

export async function loadWorkbookFromGist(workbookSession: WorkbookSession, gistUrl: string) : Promise<Workbook> {
    // TODO: Specify revision to load, specify root workbook file.
    const gistId = gistUrl.split('/').slice(-1)
    const gistFetchUrl = `/api/gist/${gistId}`
    const gistZipResponse = await fetch(gistFetchUrl)

    if (!gistZipResponse.ok)
        throw new Error("Couldn't download Gist from GitHub.");

    var gistZip = await gistZipResponse.blob();
    const workbookPackage = new File([ gistZip ], `${gistId}.zip`, {
        type: "application/zip"
    });
    return await loadWorkbookFromWorkbookPackage(workbookSession, workbookPackage);
}

export async function loadWorkbookFromWorkbookPackage(workbookSession: WorkbookSession, workbookPackage: File): Promise<Workbook> {
    const loadedZip = await loadAsync(workbookPackage)
    const workbookFiles = loadedZip.filter((path, file) => path.endsWith(".workbook"));

    if (workbookFiles.length === 0)
        throw new Error("No workbooks in workbook package.");

    // Grab index.workbook or the first workbook file if there isn't an index.
    const workbookFile = workbookFiles.find(wf => wf.name === "index.workbook") || workbookFiles[0];
    const workbookFileContents = await workbookFile.async("text")

    // TODO: Pass down _all_ of the files so that we can implement #r and #load.
    return await loadWorkbookFromString(workbookSession, workbookPackage.name, workbookFileContents)
}

export interface WorkbookManifest {
    packages: List<PackageDescription>
    platforms: List<WorkbookTarget>
    title?: string
    id?: string
    uti: string
    rest: {}
}

export class Workbook {
    private readonly _rawMetadata: any
    private readonly _markdownContent: string
    private readonly _manifest: WorkbookManifest
    private readonly _fileName?: string

    get rawMetadata(): any {
        return this._rawMetadata
    }

    get markdownContent(): string {
        return this._markdownContent
    }

    get manifest(): WorkbookManifest {
        return this._manifest
    }

    get fileName(): string | undefined {
        return this._fileName
    }

    constructor(workbookSession: WorkbookSession, fileName: string, markdownContent: string, metadata: any) {
        this._fileName = fileName;
        this._markdownContent = markdownContent.trim()
        this._rawMetadata = metadata
        this._manifest = this.parseManifest(this._rawMetadata, workbookSession)
    }

    getManifestToSave(): any {
        const saveableManifest: any = Object.assign({
            id: this.manifest.id,
            uti: this.manifest.uti,
            title: this.manifest.title,
        }, this.manifest.rest)
        saveableManifest.packages = this.manifest.packages.map(pd => {
            return !pd ? undefined : { id: pd.packageId, version: pd.identityVersion }
        }).filter((_: any) => _).toArray()
        saveableManifest.platforms = this.manifest.platforms.map(wt => {
            return !wt ? undefined : getPlatformFromWorkbookTargetId(wt.id)
        }).filter((_: any) => _).toArray()
        return saveableManifest
    }

    /**
     * Parses raw metadata into the bits that we actually currently use, while preserving any
     * metadata that Workbooks doesn't know about.
     * @param rawMetadata The raw metadata coming from reading the YAML manifest out of a workbook.
     * @param workbookSession The current workbook session.
     */
    parseManifest(rawMetadata: any, workbookSession: WorkbookSession): WorkbookManifest {
        let checkUti = true;
        const finalMetadata: any = { uti: defaultUti }

        // Make a copy of the raw metadata (if there is any) so that we can preserve metadata Workbooks
        // doesn't know about, without duplicating any of it. We'll remove properties we actually consume
        // from the `rawMetadata` object (all of the deletes below) so that we can store it as the
        // preserved metadata.
        if (!rawMetadata) {
            rawMetadata = {}
            checkUti = false
        } else
            rawMetadata = Object.assign({}, rawMetadata)

        const hasMatchingUti = (!rawMetadata.uti || rawMetadata.uti.toLowerCase() === defaultUti);
        if (checkUti && !hasMatchingUti)
            throw new Error("Invalid manifest")
        else
            delete rawMetadata.uti

        let id = rawMetadata.id;
        if (id)
            delete rawMetadata.id
        else
            id = uuidv4();
        finalMetadata.id = id;

        if (rawMetadata.title) {
            finalMetadata.title = rawMetadata.title
            delete rawMetadata.title
        } else if (this.fileName) {
            finalMetadata.title = this.fileName.substring(0, this.fileName.lastIndexOf('.'))
            // If the filename came from a .zip file, we'd have a duplicated extension.
            if (finalMetadata.title.endsWith(".workbook"))
                finalMetadata.title = finalMetadata.title.substring(0, finalMetadata.title.lastIndexOf('.'))
        }

        if (rawMetadata.packages) {
            finalMetadata.packages = List((rawMetadata.packages as any[]).map(val => (<PackageDescription>{
                packageId: val.id,
                identityVersion: val.version
            })))
            delete rawMetadata.packages
        } else {
            finalMetadata.packages = List()
        }

        const platform: string = rawMetadata.platform;
        const platforms: string[] = rawMetadata.platforms || [];
        // Map the list of platforms in the workbook to the actual workbook targets. Filter
        // out any undefineds (the trailing `filter(_ => _)` call).
        finalMetadata.platforms = List(platforms.concat(platform).map(workbookPlatform => {
            if (!workbookPlatform)
                return undefined

            const matchingTarget = workbookSession.availableWorkbookTargets.find(value => {
                return value.id === getWorkbookTargetIdFromPlatform(workbookPlatform)
            })
            if (!matchingTarget)
                console.warn(`Could not find matching target for workbook platform ${workbookPlatform}.`)
            return matchingTarget;
        }).filter(_ => _))
        delete rawMetadata.platform;
        delete rawMetadata.platforms;

        finalMetadata.rest = rawMetadata;

        return <WorkbookManifest>finalMetadata;
    }
}

