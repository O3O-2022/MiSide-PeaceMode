"""The downloads for players, in dev_output/release:
  MiSidePeaceMode-Setup-v<version>.exe   the installer (tools/installer)
  MiSidePeaceMode-v<version>.zip         for installing by hand: extracted into the game folder
In both: PeaceMode/bin/MiSidePeaceMode.dll (as built by deploy.ps1 or `dotnet build -c Release`) and the launcher .bat.
usage: python tools/make_release.py"""
import os, re, subprocess, sys, zipfile

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
version = re.search(r'Version = "([^"]+)"', open(os.path.join(REPO, "PeaceMode", "Plugin.cs"), encoding="utf-8").read()).group(1)
README = rf"""MiSide Peace Mode v{version}  -  unofficial fan mod (not affiliated with AIHASTO)
https://github.com/O3O-2022/MiSide-PeaceMode

The easy way / 쉬운 방법: MiSidePeaceMode-Setup-v{version}.exe (the installer, on the same download page) does all of
the below for you. 같은 내려받기 페이지의 설치 프로그램이 아래 과정을 대신 해 줍니다.

[한국어] 설치
1. BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip 을 게임 폴더(MiSideFull.exe 가 있는 곳)에 풀고,
   게임을 한 번 실행했다가 메인 메뉴에서 종료합니다. (첫 실행은 몇 분 걸립니다)
   https://github.com/BepInEx/BepInEx/releases/tag/v6.0.0-pre.2
2. 이 zip 을 같은 게임 폴더에 풉니다. (BepInEx\plugins\MiSidePeaceMode.dll, MiSide Peace Mode.bat)
3. "MiSide Peace Mode.bat" 으로 게임을 켭니다 -> 메인 메뉴 -> 평화 모드 -> 미타 선택.
   (Steam 에서 바로 켜면 Steam 오버레이 때문에 모드가 안 뜰 수 있습니다. 그럴 땐 이 배치 파일로 켜거나,
    Steam 의 MiSide 속성에서 "게임 중 Steam 오버레이 사용"을 끄세요.)
지우기: BepInEx\plugins\MiSidePeaceMode.dll 을 지웁니다. 본편 세이브는 건드리지 않습니다.

[English] Install
1. Extract BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip into the game folder (where MiSideFull.exe is),
   start the game once and quit from the main menu. (The first start takes a few minutes.)
2. Extract this zip into the same folder.
3. Start the game with "MiSide Peace Mode.bat" -> main menu -> Peaceful Mode -> pick a Mita.
   (Started from Steam, the Steam overlay can keep the mod loader from starting: use the .bat, or turn off
    "Enable the Steam Overlay while in-game" for MiSide.)
Uninstall: delete BepInEx\plugins\MiSidePeaceMode.dll. The story's saves are never touched.

For MiSide 0.93L (Steam), Windows 10/11 64-bit. Languages: Korean, English, Japanese, Simplified Chinese.
MiSide (c) AIHASTO. This mod is made by a fan and is not the official Peaceful Mode.
"""
out_dir = os.path.join(REPO, "dev_output", "release")
os.makedirs(out_dir, exist_ok=True)
out = os.path.join(out_dir, f"MiSidePeaceMode-v{version}.zip")
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    z.write(os.path.join(REPO, "PeaceMode", "bin", "MiSidePeaceMode.dll"), "BepInEx/plugins/MiSidePeaceMode.dll")
    z.write(os.path.join(REPO, "tools", "launch", "MiSide Peace Mode.bat"), "MiSide Peace Mode.bat")
    z.writestr("MiSide Peace Mode - README.txt", README.replace("\n", "\r\n").encode("utf-8-sig"))
print(out, os.path.getsize(out) // 1024, "KB")
sys.exit(subprocess.run([sys.executable, os.path.join(REPO, "tools", "installer", "build.py")]).returncode)
