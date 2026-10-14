param(
    [string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputRoot) { $OutputRoot = Join-Path $repositoryRoot 'artifacts/release' }
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$projectPath = Join-Path $repositoryRoot 'PhasmoEditorWinApp/PhasmoEditorWinApp.csproj'

function Invoke-DotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

Push-Location $repositoryRoot
try {
    $version = (& dotnet msbuild $projectPath -nologo -getProperty:Version).Trim()
    if ($LASTEXITCODE -ne 0 -or $version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?$') {
        throw 'Could not read a valid project version.'
    }
    $packageName = "PhasmoEditor-$version-win-x64"
    # Use a fresh staging directory to avoid stale files and running EXE locks.
    $stagingDirectory = Join-Path $OutputRoot ("staging-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null

    Invoke-DotNet -Arguments @('build', 'PhasmoEditorWinApp.sln', '-c', 'Release')
    Invoke-DotNet -Arguments @('run', '--project', 'PhasmoEditor.Tests', '-c', 'Release', '--no-build')
    Invoke-DotNet -Arguments @('publish', $projectPath, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:DebugType=None', '-p:DebugSymbols=false', '-o', $stagingDirectory)

    Copy-Item -LiteralPath README.md, CHANGELOG.md -Destination $stagingDirectory
    New-Item -ItemType Directory -Path (Join-Path $stagingDirectory 'docs') | Out-Null
    Copy-Item -LiteralPath docs/README.ru.md, docs/RELEASE_NOTES.md, docs/PUBLISHING.md -Destination (Join-Path $stagingDirectory 'docs')
    Copy-Item -LiteralPath docs/images -Destination (Join-Path $stagingDirectory 'docs') -Recurse
    $zipPath = Join-Path $OutputRoot "$packageName.zip"
    Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $zipPath -Force
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$zipPath.sha256", "$hash  $packageName.zip" + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    Write-Output "Package: $zipPath"
    Write-Output "Checksum: $zipPath.sha256"
    Write-Output "Unpacked files: $stagingDirectory"
}
finally { Pop-Location }
