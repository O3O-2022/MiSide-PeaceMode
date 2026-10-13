"""Where the game (and so the recorded frames) is: MISIDE_DIR, the folder around this repo, or D:/Steam/steamapps/common/MiSide."""
import os
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def game_dir():
    for d in (os.environ.get("MISIDE_DIR"), os.path.dirname(REPO), r"D:\Steam\steamapps\common\MiSide"):
        if d and os.path.exists(os.path.join(d, "MiSideFull.exe")):
            return d
    return os.path.dirname(REPO)
