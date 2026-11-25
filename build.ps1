$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Find-Csc {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
        if ($found) { return $found }
    }
    $found = Get-ChildItem "${env:ProgramFiles(x86)}\Microsoft Visual Studio", "$env:ProgramFiles\Microsoft Visual Studio" -Recurse -Filter csc.exe -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like '*\Roslyn\csc.exe' } | Select-Object -First 1
    if ($found) { return $found.FullName }
    throw 'C# compiler not found. Install Visual Studio Build Tools with the ".NET desktop build tools" workload.'
}

$csc = Find-Csc
$obj = Join-Path $root 'obj'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $obj, $dist | Out-Null

$common = @(
    '-nologo', '-noconfig', '-langversion:latest', '-warnaserror+', '-optimize+',
    '-r:System.dll', '-r:System.Core.dll', '-r:System.Drawing.dll', '-r:System.Windows.Forms.dll'
)

& $csc @common -target:exe "-out:$obj\IconGen.exe" "$root\src\Icons.cs" "$root\tools\IconGen.cs"
if ($LASTEXITCODE -ne 0) { throw 'IconGen build failed' }
& "$obj\IconGen.exe" "$obj\app.ico"
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed' }

$sources = Get-ChildItem "$root\src\*.cs" | ForEach-Object { $_.FullName }
& $csc @common -target:winexe -platform:anycpu "-win32manifest:$root\app.manifest" "-win32icon:$obj\app.ico" "-out:$dist\Stillpoint.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

Copy-Item "$root\README.txt" $dist -Force
Write-Host "Built $dist\Stillpoint.exe"

