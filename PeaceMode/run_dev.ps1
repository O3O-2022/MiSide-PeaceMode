param([int]$Timeout = 180, [string]$GameDir = "")
# Dev helper: put commands in <GameDir>\BepInEx\config\peacemode_dev.txt (see DevRunner.cs), then run this.
# Launches the game directly, waits for the MiSideFull process to exit.
if (-not $GameDir) { . (Join-Path (Split-Path $PSScriptRoot) "find_game.ps1"); $GameDir = Find-MiSide }
if (-not $GameDir) { "MiSide not found (pass -GameDir)"; exit 1 }
$game = Join-Path $GameDir "MiSideFull.exe"
Get-Process MiSideFull -ErrorAction SilentlyContinue | Stop-Process -Force
# steam_appid.txt keeps Steam from relaunching the game under itself: a Steam-launched process gets the overlay
# injected before UnityPlayer loads winhttp.dll, and on this PC (2026-10) doorstop then never hooks il2cpp_init,
# so BepInEx never starts. Launched directly the mod loads; the file is removed again at the end.
$appid = Join-Path $GameDir "steam_appid.txt"
$madeAppId = -not (Test-Path $appid)
if ($madeAppId) { Set-Content -Path $appid -Value "2527500" -Encoding ascii -NoNewline }
$result = "killed after timeout (saves may not have been restored: check BepInEx/config/MiSidePeaceMode*.save)"
$code = 0
try {
  Start-Process -FilePath $game -WorkingDirectory (Split-Path $game) | Out-Null
  $deadline = (Get-Date).AddSeconds($Timeout)
  # The first process may be the Steam bootstrapper, which exits before Steam starts the real game
  # (that gap can take well over 5 s on a slow PC): the game counts as started once a process has lived 20 s.
  $started = $false
  $lastSeen = Get-Date
  $done = $false
  while (-not $done -and (Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 1
    $p = Get-Process MiSideFull -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) {
      $lastSeen = Get-Date
      if (-not $started -and ((Get-Date) - $p.StartTime).TotalSeconds -ge 20) { $started = $true }
    }
    elseif ($started) { $result = "exited"; $done = $true }
    elseif (((Get-Date) - $lastSeen).TotalSeconds -gt 60) { $result = "game did not start"; $code = 1; $done = $true }
  }
  if (-not $done) {
    # at the timeout: ask the dev runner for a clean stop (it puts the saves back, then quits), kill only if it doesn't
    $stop = Join-Path $GameDir "BepInEx\config\peacemode_stop.txt"
    Set-Content -Path $stop -Value "stop" -Encoding ascii
    for ($i = 0; $i -lt 25; $i++) {
      Start-Sleep -Seconds 1
      if (-not (Get-Process MiSideFull -ErrorAction SilentlyContinue)) { $result = "stopped cleanly after timeout"; break }
    }
    Remove-Item $stop -ErrorAction SilentlyContinue
    Get-Process MiSideFull -ErrorAction SilentlyContinue | Stop-Process -Force
  }
}
finally {
  if ($madeAppId) { Remove-Item $appid -ErrorAction SilentlyContinue }
}
$result
exit $code
