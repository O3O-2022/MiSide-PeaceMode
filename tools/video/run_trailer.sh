#!/bin/bash
# run_trailer.sh <lang> [session...]: render the trailer's clips with the game itself in that language (ko | en | ja | zh).
# One game run per session (tools/video/trailer_scripts/<lang>_<s>.txt, from make_trailer.py); the clips are moved to
# dev_output/trailer/<lang>/ and their sound brought back to speed. The game's language (its registry prefs) is set
# for the runs and put back to Korean afterwards, whatever happens.
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
GAME="$(cd "$REPO/.." && pwd)"
CFG="$GAME/BepInEx/config"
KEY='HKCU\Software\AIHASTO\MiSideFull'
VAL='Language_h3872303031'
hexname() { printf '%s' "$1" | od -An -tx1 | tr -d ' \n'; printf '00'; }
setlang() { MSYS_NO_PATHCONV=1 reg.exe add "$KEY" /v "$VAL" /t REG_BINARY /d "$(hexname "$1")" /f > /dev/null && echo "game language: $1"; }
case "$1" in
  en) NAME=English ;; ja) NAME=Japanese ;; zh) NAME=ChineseSimplified ;; ko) NAME=Korean ;;
  *) echo "unknown language $1"; exit 1 ;;
esac
L="$1"; shift
[ $# -eq 0 ] && set -- A B C D E
OUT="$REPO/dev_output/trailer/$L"
mkdir -p "$OUT"
trap 'setlang Korean; rm -f "$CFG/peacemode_dev.txt"' EXIT
python "$REPO/tools/video/make_trailer.py" "$L"
setlang "$NAME"
for s in "$@"; do
  echo "=== $L $s $(date +%T)"
  if tasklist.exe | grep -qi "MiSideFull"; then echo "!! the game is running: not starting"; exit 2; fi
  rm -rf "$CFG/video"
  cp "$REPO/tools/video/trailer_scripts/${L}_$s.txt" "$CFG/peacemode_dev.txt"
  powershell.exe -ExecutionPolicy Bypass -File "$(cygpath -w "$REPO/PeaceMode/run_dev.ps1")" -Timeout 3000 | tail -1
  grep -a "recordav done\|pickwhen timed out\|waitidle timed out\|Warning:MiSide\|Error  :MiSide" "$GAME/BepInEx/LogOutput.log" | sed 's/\[Info   :MiSide Peace Mode\] //' | cut -c1-170
  cp "$GAME/BepInEx/LogOutput.log" "$OUT/log_$s.txt"
  for f in "$CFG/video/"*.sync.txt; do
    [ -f "$f" ] || continue
    b="${f%.sync.txt}"; n="$(basename "$b")"
    mv "$b".mp4 "$b".raw.wav "$b".sync.txt "$OUT/" 2>/dev/null
    [ -f "$b.slow.txt" ] && mv "$b.slow.txt" "$OUT/"
    python "$REPO/tools/video/fix_audio.py" "$OUT/$n" && rm -f "$OUT/$n.raw.wav"
  done
  ls "$CFG" | grep -i "MiSidePeaceMode.*save" && echo "!! leftover peace save"
done
echo "=== done $(date +%T)"
