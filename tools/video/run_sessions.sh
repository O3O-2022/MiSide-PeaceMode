#!/bin/bash
# run_sessions.sh <lang> <session...>: render the video clips of those sessions (tools/video/scripts/<lang>_<s>.txt)
# one game run each, then move the clips to dev_output/video/<lang>/ and bring their sound back to speed.
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
GAME="$(cd "$REPO/.." && pwd)"
CFG="$GAME/BepInEx/config"
LANG_CODE="$1"; shift
OUT="$REPO/dev_output/video/$LANG_CODE"
mkdir -p "$OUT"
for s in "$@"; do
  echo "=== $LANG_CODE $s $(date +%T)"
  rm -rf "$CFG/video"
  cp "$REPO/tools/video/scripts/${LANG_CODE}_$s.txt" "$CFG/peacemode_dev.txt"
  powershell.exe -ExecutionPolicy Bypass -File "$(cygpath -w "$REPO/PeaceMode/run_dev.ps1")" -Timeout 14400 | tail -2
  grep -a "recordav done\|record:\|pickwhen timed out" "$GAME/BepInEx/LogOutput.log" | cut -c1-200
  cp "$GAME/BepInEx/LogOutput.log" "$OUT/log_$s.txt"
  for f in "$CFG/video/"*.sync.txt; do
    [ -f "$f" ] || continue
    b="${f%.sync.txt}"; n="$(basename "$b")"
    mv "$b".mp4 "$b".raw.wav "$b".sync.txt "$OUT/" 2>/dev/null
    [ -f "$b.slow.txt" ] && mv "$b.slow.txt" "$OUT/"
    python "$REPO/tools/video/fix_audio.py" "$OUT/$n"
  done
  ls "$CFG" | grep -i "MiSidePeaceMode.*save" && echo "!! leftover peace save"
done
rm -f "$CFG/peacemode_dev.txt"
echo "=== done $(date +%T)"
