@echo off
rem Starts MiSide with the peace mode plugin loaded.
rem Started from Steam, the Steam overlay attaches before the mod loader (doorstop) and BepInEx never starts
rem (2026-10 Steam client). Started directly from here it loads. steam_appid.txt keeps the game from asking
rem Steam to relaunch it; Steam itself must be running (achievements, cloud).
cd /d "%~dp0"
if not exist "MiSideFull.exe" (
    echo MiSideFull.exe not found next to this file. Put this .bat in the MiSide game folder.
    pause
    exit /b 1
)
tasklist /fi "imagename eq steam.exe" | find /i "steam.exe" >nul
if errorlevel 1 (
    echo Starting Steam...
    start "" "steam://open/main"
    timeout /t 12 /nobreak >nul
)
if not exist "steam_appid.txt" <nul set /p ="2527500" > "steam_appid.txt"
start "" "MiSideFull.exe"
