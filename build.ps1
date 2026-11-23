param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$TerrariaDir = ""
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($TerrariaDir)) {
    $candidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Terraria",
        "C:\Program Files\Steam\steamapps\common\Terraria",
        "D:\Steam\steamapps\common\Terraria",
        "D:\SteamLibrary\steamapps\common\Terraria",
        "E:\SteamLibrary\steamapps\common\Terraria"
    )
    $vdf = "C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf"
    if (Test-Path $vdf) {
        Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"' | ForEach-Object {
            $lib = $_.Matches[0].Groups[1].Value -replace '\\\\', '\'
            $candidates += (Join-Path $lib "steamapps\common\Terraria")
        }
    }
    foreach ($c in ($candidates | Select-Object -Unique)) {
        if (Test-Path (Join-Path $c "Terraria.exe")) {
            $TerrariaDir = $c
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($TerrariaDir) -or -not (Test-Path (Join-Path $TerrariaDir "Terraria.exe"))) {
    throw "Terraria.exe not found. Pass -TerrariaDir 'path\to\Terraria'."
}

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe not found. Install Visual Studio Build Tools with C++ x86 workload."
}

$vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsPath) {
    throw "Visual Studio C++ x86/x64 Build Tools not found."
}

$msbuild = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuild)) {
    throw "MSBuild.exe not found at $msbuild"
}

Write-Host "Using Terraria directory: $TerrariaDir"
Write-Host "[1/4] Building TerrariaModMenu.Bootstrapper (Win32/x86, $Configuration)"
& $msbuild (Join-Path $root "src\TerrariaModMenu.Bootstrapper\TerrariaModMenu.Bootstrapper.vcxproj") `
    /t:Build /p:Configuration=$Configuration /p:Platform=Win32 /m /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Bootstrapper build failed ($LASTEXITCODE)." }

Write-Host "`n[2/4] Building TerrariaModMenu Launcher (Win32/x86, $Configuration)"
& $msbuild (Join-Path $root "src\TerrariaModMenu.Injector\TerrariaModMenu.Injector.vcxproj") `
    /t:Build /p:Configuration=$Configuration /p:Platform=Win32 /m /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Launcher build failed ($LASTEXITCODE)." }

Write-Host "`n[3/4] Building TerrariaModMenu.Core (net48/x86, $Configuration)"
dotnet build (Join-Path $root "src\TerrariaModMenu.Core\TerrariaModMenu.Core.csproj") `
    -c $Configuration -p:Platform=x86 -p:TerrariaDir="$TerrariaDir"
if ($LASTEXITCODE -ne 0) { throw "Core build failed ($LASTEXITCODE)." }

Write-Host "`n[4/4] Staging outputs into dist\"
$distDir = Join-Path $root "dist"
$binDir  = Join-Path $distDir "bin"
New-Item -ItemType Directory -Force -Path $binDir | Out-Null

$bootDll    = Join-Path $root "src\TerrariaModMenu.Bootstrapper\bin\$Configuration\x86\TerrariaModMenu.Bootstrapper.dll"
$launcher   = Join-Path $root "src\TerrariaModMenu.Injector\bin\$Configuration\x86\TerrariaModMenu.exe"
$coreOut    = Join-Path $root "src\TerrariaModMenu.Core\bin\x86\$Configuration"
$coreDll    = Join-Path $coreOut "TerrariaModMenu.Core.dll"
$corePdb    = Join-Path $coreOut "TerrariaModMenu.Core.pdb"
$harmonyDll = Join-Path $coreOut "0Harmony.dll"

$runningTerraria = Get-Process -Name "Terraria" -ErrorAction SilentlyContinue
if ($runningTerraria) {
    Write-Warning "Terraria.exe (PID=$($runningTerraria.Id)) is running and may lock files in dist\bin\. Close Terraria if staging fails."
}

Copy-Item $launcher   $distDir -Force
Copy-Item $bootDll    $binDir  -Force
Copy-Item $coreDll    $binDir  -Force
if (Test-Path $harmonyDll) { Copy-Item $harmonyDll $binDir -Force }
if ($Configuration -eq "Debug" -and (Test-Path $corePdb)) { Copy-Item $corePdb $binDir -Force }

Write-Host "`nOutput ready in $distDir"
Get-ChildItem $distDir -Recurse -File | Format-Table FullName, Length -AutoSize
