# Finds the MiSide folder (the one with MiSideFull.exe): next to this repo, then every Steam library, then D:\Steam.
# Dot-source it:  . (Join-Path $repo "find_game.ps1");  $GameDir = Find-MiSide
function Find-MiSide {
    $cands = @()
    # the repo may live inside the game folder
    $cands += Split-Path $PSScriptRoot
    try { $steam = (Get-ItemProperty "HKCU:\Software\Valve\Steam" -ErrorAction Stop).SteamPath } catch { $steam = $null }
    if ($steam) {
        $cands += Join-Path $steam "steamapps\common\MiSide"
        $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
        if (Test-Path $vdf) {
            foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $cands += Join-Path ($m.Groups[1].Value -replace '\\\\', '\') "steamapps\common\MiSide"
            }
        }
    }
    $cands += "D:\Steam\steamapps\common\MiSide"
    foreach ($c in $cands) { if ($c -and (Test-Path (Join-Path $c "MiSideFull.exe"))) { return $c } }
    return $null
}
