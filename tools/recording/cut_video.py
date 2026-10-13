"""Cut the recorded frames into the showcase video (video_out/MiSide_PeaceMode_cut.avi).

usage: python tools/recording/cut_video.py [frames folder]
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(REPO, "tools"))
# recorded frames (DevRunner `record`): pass the folder, or the game's BepInEx/config/frames is used
from build_media_paths import game_dir
V = sys.argv[1] if len(sys.argv) > 1 else os.path.join(game_dir(), "BepInEx", "config", "frames")
from make_video import Video

OUT = os.path.join(REPO, "video_out", "MiSide_PeaceMode_cut.avi")
os.makedirs(os.path.dirname(OUT), exist_ok=True)
F = 30


def p(name):
    return os.path.join(V, name + "_*.jpg")


def s(sec):
    return int(round(sec * F))


v = Video(OUT, fps=F, quality=78, reencode=True)
FADE = 8

# 1. main menu -> peace mode -> choosing a Mita
v.black(s(0.5))
v.clip(p("va_mainmenu"), 0, s(2.2), fade_in=FADE)
v.clip(p("va_menuopen"), 0, s(2.8))
v.clip(p("va_menu0"), 0, s(1.6))
v.clip(p("va_menu2"), 0, s(1.3))
v.clip(p("va_menu3"), 0, s(1.3))
v.clip(p("va_menu6"), 0, s(1.8), fade_out=FADE)

# 2. Crazy Mita in the kitchen: greeting, pat, hug, holding hands
v.clip(p("va_greet"), 0, s(15.5), fade_in=FADE)
v.clip(p("va_pat"), 0, s(8.5))
v.clip(p("va_hug"), 0, s(9.5))
v.clip(p("va_hold"), 0, s(13.5), fade_out=FADE)

# 3. lunch together
v.clip(p("vc_meal1"), 0, None, fade_in=FADE)
v.clip(p("vc_meal2"), 0, s(17), fade_out=FADE)

# 4. the other Mitas
v.clip(p("vb_cap_greet"), 0, s(8), fade_in=FADE)
v.clip(p("vb_cap_guitar"), 0, s(10))
v.clip(p("vb_cap_nod"), 0, None)
v.clip(p("vb_cap_shake"), 0, None, fade_out=FADE)
v.clip(p("vb_kind_advice"), 0, s(11), fade_in=FADE, fade_out=FADE)
v.clip(p("vb_mila_pat"), 0, s(7), fade_in=FADE)
v.clip(p("vb_mila_tea"), 0, s(10), fade_out=FADE)
v.clip(p("vb_sleepy_coffee"), 0, s(11), fade_in=FADE, fade_out=FADE)
v.clip(p("vc_short_lecture"), 0, s(12), fade_in=FADE, fade_out=FADE)
CHIBI = (256, 214, 1024, 646)
v.clip(p("vc_chibi_quiz"), 0, None, fade_in=FADE, crop=CHIBI)
v.clip(p("vc_chibi_answer"), 0, s(6.5), fade_out=FADE, crop=CHIBI)

# 5. night: bed, goodnight, morning
v.clip(p("vc_bed"), 0, None, fade_in=FADE, fade_out=FADE)

# 6. "오늘은 여기까지…": she waves goodbye
v.clip(p("va_bye"), 0, None, fade_in=FADE)
v.black(s(2.5))

# 7. ...and if you Alt+F4 instead
v.clip(p("va_altf4"), 0, s(21.5), fade_in=FADE, fade_out=s(0.8))
v.black(s(1.5))
v.close()
