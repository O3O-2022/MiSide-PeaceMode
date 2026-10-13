"""Builds the installer for players: dev_output/release/MiSidePeaceMode-Setup-v<version>.exe
(Installer.cs, with PeaceMode/bin/MiSidePeaceMode.dll and the launcher .bat embedded).
Compiled by the C# compiler that ships with Windows (.NET Framework 4: no SDK, nothing downloaded), so the program
runs on any Windows 10/11 as it is.
usage: python tools/installer/build.py [shots]     (shots: also save a picture of the window per language)"""
import os, re, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
CSC = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
version = re.search(r'Version = "([^"]+)"', open(os.path.join(REPO, "PeaceMode", "Plugin.cs"), encoding="utf-8").read()).group(1)
work = os.path.join(REPO, "dev_output", "installer")
out_dir = os.path.join(REPO, "dev_output", "release")
os.makedirs(work, exist_ok=True)
os.makedirs(out_dir, exist_ok=True)

icon = os.path.join(HERE, "icon.ico")
if not os.path.exists(icon):   # (drawn once: a white heart on the mod's pink)
    from PIL import Image, ImageDraw
    n = 1024
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((40, 40, n - 40, n - 40), radius=220, fill=(226, 84, 172, 255))
    r, cy = 150, 420
    d.ellipse((n / 2 - 2 * r + 6, cy - r, n / 2 + 6, cy + r), fill="white")
    d.ellipse((n / 2 - 6, cy - r, n / 2 + 2 * r - 6, cy + r), fill="white")
    d.polygon([(n / 2 - 2 * r + 30, cy + 70), (n / 2 + 2 * r - 30, cy + 70), (n / 2, 790)], fill="white")
    im.save(icon, sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])

info = os.path.join(work, "AssemblyInfo.cs")
open(info, "w", encoding="utf-8").write(f'''using System.Reflection;
[assembly: AssemblyTitle("MiSide Peace Mode Setup (unofficial fan mod)")]
[assembly: AssemblyProduct("MiSide Peace Mode")]
[assembly: AssemblyDescription("Installs the MiSide Peace Mode fan mod. Not affiliated with AIHASTO.")]
[assembly: AssemblyCompany("MiSide Peace Mode (fan project)")]
[assembly: AssemblyVersion("{version}.0")]
[assembly: AssemblyFileVersion("{version}.0")]
''')
exe = os.path.join(out_dir, f"MiSidePeaceMode-Setup-v{version}.exe")
cmd = [CSC, "/nologo", "/target:winexe", "/optimize+", "/platform:anycpu", "/codepage:65001", "/out:" + exe,
       "/win32icon:" + icon, "/win32manifest:" + os.path.join(HERE, "app.manifest"),
       "/resource:" + os.path.join(REPO, "PeaceMode", "bin", "MiSidePeaceMode.dll") + ",MiSidePeaceMode.dll",
       "/resource:" + os.path.join(REPO, "tools", "launch", "MiSide Peace Mode.bat") + ",launcher.bat",
       "/r:System.Windows.Forms.dll", "/r:System.Drawing.dll", "/r:System.IO.Compression.dll", "/r:System.IO.Compression.FileSystem.dll",
       os.path.join(HERE, "Installer.cs"), info]
r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
print((r.stdout + r.stderr).strip())
if r.returncode: sys.exit(r.returncode)
print(exe, os.path.getsize(exe) // 1024, "KB")
if "shots" in sys.argv[1:]:
    subprocess.run([exe, "/shots", os.path.join(work, "shots")], check=True)
    print("shots ->", os.path.join(work, "shots"))
