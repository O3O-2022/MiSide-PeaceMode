param(
    [string]$GameDir = "",
    [switch]$RestoreOfficialKorean
)
# Builds the peace mode plugin and installs it into the game.
# Usage:  powershell -ExecutionPolicy Bypass -File deploy.ps1 [-GameDir "X:\...\MiSide"] [-RestoreOfficialKorean]
#   -GameDir: found by itself when left out (the folder around this repo, then every Steam library)
#   -RestoreOfficialKorean: put the official Korean text back if an older version of this project replaced it
#                           (copies <GameDir>\_KoreanBackup_Original\*.txt into Data\Languages\Korean).
$ErrorActionPreference = "Stop"
$repo = $PSScriptRoot
if (-not $GameDir) { . (Join-Path $repo "find_game.ps1"); $GameDir = Find-MiSide; if ($GameDir) { "game folder: $GameDir" } }
if (-not (Test-Path (Join-Path $GameDir "MiSideFull.exe"))) { throw "MiSide not found at '$GameDir' (pass -GameDir)" }

if ($RestoreOfficialKorean) {
    $bak = Join-Path $GameDir "_KoreanBackup_Original"
    if (-not (Test-Path $bak)) { throw "no backup at '$bak' (verify the game files in Steam instead)" }
    Copy-Item (Join-Path $bak "*.txt") (Join-Path $GameDir "Data\Languages\Korean") -Force
    "official Korean restored from $bak"
}

if (-not (Test-Path (Join-Path $GameDir "BepInEx\core\BepInEx.Unity.IL2CPP.dll"))) {
    Write-Warning "BepInEx is not installed. Extract BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip into the game folder (see README)."
    return
}
if (-not (Test-Path (Join-Path $GameDir "BepInEx\interop\Assembly-CSharp.dll"))) {
    Write-Warning "BepInEx interop assemblies are missing. Start the game once, close it, then rerun this script."
    return
}
$proj = Join-Path $repo "PeaceMode\MiSidePeaceMode.csproj"
$out = Join-Path $repo "PeaceMode\bin"
dotnet build $proj -c Release -o $out -p:GameDir="$GameDir"
if ($LASTEXITCODE -ne 0) { throw "build failed" }
Copy-Item (Join-Path $out "MiSidePeaceMode.dll") (Join-Path $GameDir "BepInEx\plugins\") -Force
# the launcher: starts the game directly (a Steam-launched process may never start BepInEx, see README)
Copy-Item (Join-Path $repo "tools\launch\MiSide Peace Mode.bat") $GameDir -Force
"peace mode plugin deployed -> $GameDir\BepInEx\plugins (launcher: $GameDir\MiSide Peace Mode.bat)"
