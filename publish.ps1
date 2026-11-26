param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Message = "Release $Version"
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must look like 1.2.3' }
$versionFile = Join-Path $root 'src\Version.cs'
$text = Get-Content $versionFile -Raw
$text = $text -replace 'Text = "[^"]+"', "Text = `"$Version`""
Set-Content $versionFile $text -Encoding utf8 -NoNewline

& (Join-Path $root 'build.ps1')

$latest = Join-Path $root 'latest'
New-Item -ItemType Directory -Force $latest | Out-Null
Copy-Item (Join-Path $root 'obj\Pebble.exe') (Join-Path $latest 'Stillpoint.exe') -Force
Copy-Item (Join-Path $root 'obj\Pebble.exe') $latest -Force
Set-Content (Join-Path $latest 'version.txt') $Version -Encoding ascii -NoNewline

$built = (Get-Item (Join-Path $latest 'Pebble.exe')).VersionInfo.FileVersion
if ($built -ne "$Version.0") { throw "Built exe reports $built, expected $Version.0" }

Push-Location $root
$ErrorActionPreference = 'Continue'
try {
    git add -- src tools fonts app.manifest build.ps1 publish.ps1 README.txt README.md .gitignore latest/Stillpoint.exe latest/Pebble.exe latest/version.txt
    if ($LASTEXITCODE -ne 0) { throw 'git add failed' }
    git commit -m $Message
    if ($LASTEXITCODE -ne 0) { throw 'git commit failed' }
    git push origin main
    if ($LASTEXITCODE -ne 0) { throw 'git push failed' }
}
finally {
    Pop-Location
}
Write-Host "Published $Version"
