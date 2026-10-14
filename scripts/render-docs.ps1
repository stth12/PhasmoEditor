$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    New-Item -ItemType Directory -Path docs/images -Force | Out-Null
    & dotnet run --project PhasmoEditor.Tests -c Release -- docs/images/interface-en.png 1120 880 en docs
    if ($LASTEXITCODE -ne 0) { throw 'English screenshot rendering failed.' }
    & dotnet run --project PhasmoEditor.Tests -c Release --no-build -- docs/images/interface-ru.png 1120 880 ru docs
    if ($LASTEXITCODE -ne 0) { throw 'Russian screenshot rendering failed.' }
}
finally { Pop-Location }
