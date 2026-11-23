[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $BepInExDir,

    [string] $OutputDirectory = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') {
    throw 'Run this script on Windows with references from the installed Store game.'
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Name = 'TwilightInputOverlay'
$BepInExDir = (Resolve-Path -LiteralPath $BepInExDir).Path
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$Project = Join-Path $RepoRoot "src/$Name/$Name.csproj"
$Checker = Join-Path $PSScriptRoot 'ReleaseDllCheck/ReleaseDllCheck.csproj'
[xml] $ProjectXml = Get-Content -LiteralPath $Project -Raw
$Version = $ProjectXml.SelectSingleNode("/Project/PropertyGroup[TargetFramework='net6.0']/Version").InnerText
if ($Version -cnotmatch '^0\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)-pre$') {
    throw 'The Store project version must use 0.x.x-pre.'
}
$AssetPath = Join-Path $OutputDirectory "$Name-v$Version.dll"
if (Test-Path -LiteralPath $AssetPath) {
    throw "The release DLL already exists: $AssetPath"
}

$BuildDir = Join-Path ([IO.Path]::GetTempPath()) ("$Name-release-" + [Guid]::NewGuid().ToString('N'))
try {
    & dotnet build $Project -c Release -t:Rebuild -o $BuildDir "-p:BepInExDir=$BepInExDir"
    if ($LASTEXITCODE -ne 0) { throw 'The Store Release build failed.' }

    $Dll = Join-Path $BuildDir "$Name.dll"
    & dotnet run --project $Checker -c Release -- $Dll $Name $Version "$Name.LICENSE"
    if ($LASTEXITCODE -ne 0) { throw 'Release DLL verification failed.' }

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    # Only the verified plugin DLL is published from the temporary build.
    [IO.File]::Copy($Dll, $AssetPath, $false)
    Get-FileHash -LiteralPath $AssetPath -Algorithm SHA256
}
finally {
    if (Test-Path -LiteralPath $BuildDir) {
        Remove-Item -LiteralPath $BuildDir -Recurse -Force
    }
}
