#!/bin/bash
# run_inspect.sh [session ...]: one game run per inspection session (tools/inspect/scripts/<s>.txt), its frames moved to
# dev_output/inspect/<s>/frames and analysed into dev_output/inspect/<s>/report (inspect_frames.py).
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
GAME="$(cd "$REPO/.." && pwd)"
CFG="$GAME/BepInEx/config"
python "$REPO/tools/inspect/make_inspect.py" > /dev/null
[ $# -eq 0 ] && set -- A B C D M_kind M_cap M_mila M_sleepy M_short M_chibi
trap 'rm -f "$CFG/peacemode_dev.txt"' EXIT
for s in "$@"; do
  echo "=== $s $(date +%T)"
  if tasklist.exe | grep -qi "MiSideFull"; then echo "!! the game is running: not starting"; exit 2; fi
  # (a session's frames take several GB: a full disk once broke the run half way - the saves could not be put back)
  FREE=$(df -BG --output=avail "$REPO" | tail -1 | tr -dc 0-9)
  if [ "${FREE:-0}" -lt 8 ]; then echo "!! only ${FREE} GB free: not starting $s"; exit 3; fi
  rm -rf "$CFG/frames"
  cp "$REPO/tools/inspect/scripts/$s.txt" "$CFG/peacemode_dev.txt"
  powershell.exe -ExecutionPolicy Bypass -File "$(cygpath -w "$REPO/PeaceMode/run_dev.ps1")" -Timeout 3000 | tail -1
  OUT="$REPO/dev_output/inspect/$s"
  rm -rf "$OUT"; mkdir -p "$OUT"
  cp "$GAME/BepInEx/LogOutput.log" "$OUT/log.txt"
  grep -a "pick: no\|pickat .*no such\|timed out\|Exception\|Error  :MiSide\|recidle .*settled" "$OUT/log.txt" | sed 's/\[Info   :MiSide Peace Mode\] //' | cut -c1-160
  [ -d "$CFG/frames" ] && mv "$CFG/frames" "$OUT/frames"
  python "$REPO/tools/inspect/inspect_frames.py" "$OUT/frames" "$OUT/report" | tail -40
done
echo "=== done $(date +%T)"
