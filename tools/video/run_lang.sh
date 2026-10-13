#!/bin/bash
# run_lang.sh <lang> [session...]: render a video base with the game itself in that language (en | ja | zh | ko).
# Sets the game's language (its registry prefs) for the runs, then puts it back to Korean, whatever happens.
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
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
trap 'setlang Korean' EXIT
python "$REPO/tools/video/make_scripts.py" "$L"
setlang "$NAME"
bash "$REPO/tools/video/run_sessions.sh" "$L" "$@"
