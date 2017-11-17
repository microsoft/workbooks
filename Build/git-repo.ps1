#!/usr/bin/env pwsh
#
# Author:
#   Aaron Bockover <abock@microsoft.com>
#
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Param(
  [Parameter(Mandatory = $true, Position = 1)]
  [string]$DependenciesJsonPath,
  [switch]$Update = $false,
  [string]$GitHubToken
)

function CloneOrUpdate {
  Param(
    [Parameter(ValueFromPipeline)]$Dependency
  )

  $gitUrl = $Dependency.Name
  $checkoutPath = $Dependency.Path
  $gitRef = $Dependency.Version

  if (-Not ($gitUrl -And $checkoutPath -And $gitRef)) {
    Write-Error "Incomplete dependency object in ${DependenciesJsonPath}"
    Write-Output $Dependency
    Exit 1
  }

  $alreadyCloned = Test-Path -PathType Container $checkoutPath
  if ($alreadyCloned -And -Not $Update) {
    Write-Host $checkoutPath already exists and -Update was not specified
    Return
  }

  # if HEAD points to a tag, assume the other repo has a matching tag;
  # if it does not, then the checkout/update will fail
  $tagRef = $(& git tag --points-at HEAD | Select-Object -First 1)
  if ($tagRef) {
    $gitRef = $tagRef
  }

  Write-Host "Cloning or updating:"
  Write-Host "  -> URL:  $gitUrl"
  Write-Host "  -> Ref:  $gitRef"
  Write-Host "  -> Path: $checkoutPath"
  Write-Host

  if (-Not $alreadyCloned) {
    & git clone $gitUrl $checkoutPath
  }

  Push-Location $checkoutPath
  try {
    if (-Not (& git checkout $gitRef)) {
      Write-Error "Unable to check out $gitRef"
      Exit 1
    }
    & git submodule sync
    & git submodule update --recursive --init
  } finally {
    Pop-Location
  }
}

if (-Not (Test-Path -PathType Leaf $DependenciesJsonPath)) {
  Write-Error "$DependenciesJsonPath does not exist"
  Exit 1
}

$DependenciesJsonDirectory = (Get-ChildItem $DependenciesJsonPath).DirectoryName

(Get-Content -Raw $DependenciesJsonPath | ConvertFrom-Json) |
  Where-Object { $_.Kind -Eq "GitRepo" } |
  Foreach-Object {
    $_.Path = [IO.Path]::Combine($DependenciesJsonDirectory, $_.Path)
    if ($GitHubToken) {
      $uri = New-Object -TypeName System.UriBuilder -ArgumentList $_.Name
      if ($uri.Host -Eq "github.com") {
        $uri.UserName = $GitHubToken
        $_.Name = $uri.ToString()
      }
    }
    Return $_
  } | CloneOrUpdate