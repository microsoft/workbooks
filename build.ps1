#!/usr/bin/env pwsh
<#
.SYNOPSIS
Drive the build for Workbooks & Inspector.

.PARAMETER Git-Clean
Performs an exhaustive recursive clean via git. Any unstashed/committed local
changes will be removed from the tree.

.PARAMETER Build
Build the entire solution.

.PARAMETER Package
Create an installer package for the build.

.PARAMETER Test
Run all unit tests.
#>

Param (
  [Switch]
  $Help = $false,

  [Switch]
  ${Git-Clean} = $false,

  [Switch]
  $Build = $false,

  [Switch]
  $Package = $false,

  [Switch]
  $Test = $false,

  [Switch]
  $UpdatePublicApiDefinitions = $false
)

if ($Help) {
  Get-Help "$PSCommandPath"
  Exit 0
}

if (${Git-Clean}) {
  & git submodule foreach git clean -xffd
  & git clean -xffd --exclude=workbooks-proprietary
  if (Test-Path -PathType Container workbooks-proprietary) {
    Write-Host "Entering 'workbooks-proprietary'"
    Push-Location workbooks-proprietary
    & git clean -xffd
    Pop-Location
  }
  Exit 0
}

$Build = $Build -Or -Not ($Package -Or $Test -Or $UpdatePublicApiDefinitions)

$Targets = @()

if ($Build) { $Targets += "Build" }
if ($Package) { $Targets += "Package" }
if ($Test) { $Targets += "Test" }
if ($UpdatePublicApiDefinitions) { $Targets += "UpdatePublicApiDefinitions" }

$Targets = $Targets -join ","

& git submodule sync
& git submodule update --recursive --init

$errorActionPreference = "Stop"

& msbuild /target:$Targets Build.proj