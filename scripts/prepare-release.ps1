param([string]$OutputRoot)
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
    if ($LASTEXITCODE -ne 0 -or $version -notmatch '^\d+\.\d+\.\d+$') {
        throw 'Could not read a stable project version.'
    }
    $fileName = "PhasmoEditor-$version-win-x64.exe"
    $stagingDirectory = Join-Path $OutputRoot ("staging-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null

    Invoke-DotNet -Arguments @('build', 'PhasmoEditorWinApp.sln', '-c', 'Release')
    Invoke-DotNet -Arguments @('run', '--project', 'PhasmoEditor.Tests', '-c', 'Release', '--no-build')
    Invoke-DotNet -Arguments @('publish', $projectPath, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '-p:DebugType=None', '-p:DebugSymbols=false', '-o', $stagingDirectory)

    $exePath = Join-Path $OutputRoot $fileName
    Copy-Item -LiteralPath (Join-Path $stagingDirectory 'PhasmoEditor.exe') -Destination $exePath -Force
    $hash = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$exePath.sha256", "$hash  $fileName" + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    Write-Output "Executable: $exePath"
    Write-Output "Checksum: $exePath.sha256"
}
finally { Pop-Location }
