$globalJson = [IO.Path]::Combine($PSScriptRoot, "..", "global.json")
$targetDncVersion = (Get-Content $globalJson | ConvertFrom-Json).sdk.version

$releaseManifestUri = "https://raw.githubusercontent.com/dotnet/core/master/release-notes/releases.json"
$releaseManifest = Invoke-WebRequest $releaseManifestUri | ConvertFrom-Json
$downloadUri=$releaseManifest `
    | Where-Object { $_."version-sdk" -eq $targetDncVersion } `
    | Select-Object -First 1 `
    | ForEach-Object { "$($_."dlc-sdk")$($_."sdk-win-x64.exe")" }
$downloadFile = [IO.Path]::Combine($PSScriptRoot, [IO.Path]::GetFileName($downloadUri))
$logFile = $downloadFile + ".installer-log.txt"

if (![IO.File]::Exists($downloadFile)) {
    Write-Host "Downloading .NET Core SDK ${targetDncVersion}: ${downloadUri} ..."
    (New-Object Net.WebClient).DownloadFile($downloadUri, $downloadFile)
}

Write-Host "Installing ${downloadFile} ..."
Start-Process -Wait -FilePath $downloadFile -ArgumentList "/quiet","/log",$logFile
Get-Content $logFile | Write-Host