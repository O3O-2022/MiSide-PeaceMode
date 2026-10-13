"""DevRunner scripts that render the trailer's clips (4K60 + the game's sound, see PeaceMode/AvRecorder.cs).

usage: python tools/video/make_trailer.py <lang>      (ko | en | ja | zh: the game's language for that run)
writes tools/video/trailer_scripts/<lang>_<session>.txt; clips land in BepInEx/config/video/<lang>_<clip>.mp4
(+ .raw.wav, .sync.txt) and run_trailer.sh moves them to dev_output/trailer/<lang>/.

The same staging as make_scripts.py (the long showcase video), cut down to what the trailer shows, with the newer
things added (gifts, music at the recorder, the battery warning, Kind Mita's memory of the story). Differences:
4K, 0.05 real seconds a frame (this PC renders a 4K frame in ~0.03 s), the house music off (the trailer has its own
music bed: the main menu's theme, recorded once in the Korean run as `theme`), a pause after the imports before the first
staging (staged right away, something of the free mode's start took her back to the stove).
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
LANG = sys.argv[1] if len(sys.argv) > 1 else "ko"
AWAY = {"ko": "18분", "en": "18 minutes", "ja": "18分", "zh": "18分钟"}[LANG]
GAMELANG = {"ko": [], "en": ["gamelang English"], "ja": ["gamelang Japanese"], "zh": ["gamelang ChineseSimplified"]}[LANG]
W, H, FPS = 3840, 2160, 60
FRAME = 0.15   # (0.05 is enough for this PC left alone - a 4K frame takes ~0.03 s - but with anything else using the GPU frames ran late and their sound
               #  was cut: a browser, and with another game running even 0.1 was far too little - frames took 0.2-0.3 s)
SHOW = ["peacetime 15:00", "showcase 10.6 2.2 9.25 1.65 Kitchen", "reidle", "wait 2"]
BED_SECONDS = 125


def rec(name, sec):
    return [f"recordav {LANG}_{name} {sec} {FPS} {W} {H}"]


def picked(name, picks, sec, pre=None, show=True, unshow=False, last_when=None):
    """a menu action picked before the recording starts (the trailer shows what happens, not the menu)"""
    L = list(SHOW) if show else []
    if unshow: L += ["unshowcase", "wait 1", "lookat mita", "wait 1"]
    if pre: L += pre
    L.append("peacemenu")
    for p in picks:
        L += ["wait 1.3", "pick " + p]
    if last_when: L.append("pickwhen " + last_when)   # (a choice that comes up during the recording)
    L += rec(name, sec) + ["waitidle 60", "wait 1"]
    return L


def start(pid, aff, tier, time, menus=False, greet=True):
    L = ["name", "screen 960 540", f"avframe {FRAME}", f"peaceset {pid} affection={aff}", f"peaceset {pid} lastTier={tier}", "wait 34"] + GAMELANG
    if menus:
        if LANG == "ko":
            L += rec("theme", BED_SECONDS)   # the menu's music, the whole trailer long (not "bed": that is Sleepy Mita's bedtime clip, which overwrote it once)
        L += ["menupeace", "after 1.2 menuhover 0", "after 2.4 menuhover 2", "after 3.6 menuhover 3", "after 4.8 menuhover 6"] + rec("select", 7)
    L += [f"peace {pid}", "waitready 240", "wait 3", "calm", "musicoff", "freeplayer"]
    if greet: L += ["greet", "waitidle 90"]
    L += ["waitimports 400", "wait 5", f"peacetime {time}"]
    return L


END = ["wait 2", "screen native", "wait 2", "quit"]
S = {}

# A: the original Mita - the select page, talking, touch, a gift, games, the special moments, the fourth wall, goodbye
A = start("crazy", 80, 9, "15:00", menus=True)
A += SHOW + ["act morning"] + rec("greet_crazy", 5) + ["waitidle 30"]
A += SHOW + ["peacemenu", "wait 0.6"] + rec("choices", 3) + ["choiceescape", "wait 2"]
A += SHOW + ["gestureall", "act talk"] + rec("talk", 6) + ["gestureall off", "waitidle 40"]
A += SHOW + ["act pat"] + rec("pat", 6) + ["waitidle 30"]
A += SHOW + ["act hug"] + rec("hug", 8) + ["waitidle 40", "wait 2"]
A += SHOW + ["act special"] + rec("hold", 8) + ["waitidle 40", "wait 2"]
A += picked("gift", ["교감하기", "선물하기", "꽃"], 10)
A += SHOW + ["act photo"] + rec("photo", 7) + ["waitidle 40", "wait 1"]
A += picked("rps", ["같이 놀자", "가위바위보"], 11, unshow=True, last_when="가위")
# (the head-turning game goes on for three rounds after its clip: let it end before anything else is staged)
A += picked("cham", ["같이 놀자", "참참참"], 8, pre=["game chamauto"], unshow=True) + ["wait 30", "waitidle 60", "wait 2"]
A += picked("window", ["특별한 시간", "같이 창밖 보기"], 10, unshow=True)
A += picked("music", ["특별한 시간", "같이 음악 듣기"], 10, unshow=True)
A += picked("lap", ["특별한 시간", "무릎베개"], 17, unshow=True)
A += SHOW + ["wait 2", f"wall AltF4 {AWAY}"] + rec("altf4", 9) + ["waitidle 40"]
A += SHOW + ["wall BatteryLow"] + rec("battery", 7) + ["waitidle 40"]
A += SHOW + ["unshowcase", "wait 1", "lookat mita", "wait 2", "peacemenu", "pickwhen 오늘은 여기까지", "pickwhen 메인 메뉴로"] + rec("bye", 7) + ["wait 30"] + END
S["A"] = A

# the cooking, from low over the counter in front of her (from where you stand she is a small figure with her back to
# you, and from the side her hair hides her face: found with test shots, dev_output/camtest*.jpg)
COOK = SHOW + ["peacetime 12:10", "peacemenu", "wait 1.3", "pick 같이 요리하기", "wait 16", "lookat 12.84 1.3 1.37",
               "devcam 13.5 1.1 1.9 0 0 52", "devcam look 12.92 1.36 1.28", "wait 1"] + rec("cook", 6) + ["devcam off"]
COOK += ["peacemenu", "pickwhen 요리 그만하기", "waitidle 60", "wait 2"]
# the card game, from across the table (seated, your own view looks over her head and the seat does not let it turn;
# her head is at 7.54 0.83 -5.99). Last in a session: its seat keeps holding you after it
CARDS = ["walkto @Interactive Cards >> ~90", "wait 1", "click Interactive Cards >>", "wait 3.5",
         "devcam 7.54 1.05 -4.75 0 0 52", "devcam look 7.54 0.7 -5.99", "wait 0.5"] + rec("cards", 8) + ["devcam off", "cardstop", "wait 20"]

# B: Kind Mita - her memory of the story, cooking, cold and at the top, the card game
B = start("kind", 40, 7, "15:00")
B += SHOW + ["act morning"] + rec("greet_kind", 5) + ["waitidle 30"]
B += SHOW + ["story play 3"] + rec("story", 10) + ["waitidle 40"]
B += COOK
B += ["aff -45", "wait 1"] + SHOW + ["act pat"] + rec("cold", 6) + ["waitidle 30"]
B += SHOW + ["contempt"] + rec("contempt", 3) + ["contempt off", "wait 2"]
# (her affection first, then her own talk back on: the other way round she remarks on the bottom she was just at)
B += SHOW + ["aff 125", "wait 0.5", "calm off"] + rec("top", 9) + ["calm", "waitidle 40"]
B += CARDS + END
S["B"] = B

# C: Sleepy Mita - her suggestion and the sofa, the weather and seasons, bed
C = start("sleepy", 40, 7, "14:00")
C += SHOW + ["act morning"] + rec("greet_sleepy", 5) + ["waitidle 30", "unshowcase", "wait 3"]
C += ["walkto 0.9 -1.4 ~90", "wait 2", "mitago 2.2 -1.1", "wait 8", "look face", "wait 1", "after 0.5 propose tv", "pickwhen 좋아", "after 7 look off"] + rec("propose", 17) + ["look off", "waitidle 90", "wait 2"]
C += ["peacetime 14:00", "showcase 10.6 2.2 12.3 3.6 Kitchen", "wait 2", "lookat 14.27 1.3 4.02", "devcam 13.0 1.5 3.9 0 0 45", "devcam look 14.27 1.3 4.02",
      "weather rain", "wait 25"] + rec("rain", 4)
C += ["weather snow", "wait 25"] + rec("snow", 4) + ["devcam off", "weather clear", "wait 5"]
C += ["unshowcase", "wait 1", "showcase 1.6 1.4 0.6 -0.2 Main", "wait 2", "lookat 0.5 1.3 -3.8", "peacetime 12:00", "weather season 0", "wait 3",
      "after 2 weather season 1", "after 4 weather season 2", "after 6 weather season 3"] + rec("seasons", 8) + ["weather season -1", "unshowcase", "wait 2"]
C += ["walkto 9.25 1.65 ~120", "wait 2"]
C += SHOW + ["peacetime 22:30", "wait 2", "peacemenu", "pickwhen 이제 자자", "wait 4"] + rec("bed", 14) + ["wait 40"] + END
S["C"] = C

# D: Cappie - her greeting, the dance battle, the hammer game (the old script's waits, in game time: the dance has its own clock)
D = start("cap", 40, 7, "13:00")
D += SHOW + ["act morning"] + rec("greet_cap", 5) + ["waitidle 30"]
D += ["unshowcase", "walkto 0.9 -0.4 ~90", "wait 2", "dance auto on 0.87 0", "dance start", "wait 12",
      "after 3 dance menu 1", "after 5 dance menu 0", "after 7.5 dance pick"] + rec("dance1", 14)
D += ["wait 10", "devcam 2.6 1.4 -1.9 -90 10 60"] + rec("dance2", 8) + ["devcam off", "wait 60", "dance state", "wait 28", "dance state"] + rec("dance3", 10)
D += ["wait 38", "dance state", "wait 16", "waitidle 120", "wait 3"]
D += ["freeplayer", "wait 2", "peacetime 14:00", "walkto 8.3 1.2 ~90", "wait 2", "hammer auto on", "hammer start", "wait 9", "wait 12", "wait 6"] + rec("hammer", 14)
D += ["waitidle 300", "wait 3"] + END
S["D"] = D

# E: Mila, the mod's other languages (the Korean run only), Short-haired Mita, Chibi Mita
E = start("mila", 40, 7, "15:00")
E += SHOW + ["act morning"] + rec("greet_mila", 5) + ["waitidle 30"]
if LANG == "ko":
    for code in ("en", "ja", "zh"):
        E += [f"lang {code}"] + SHOW + ["act talk"] + rec(f"lang_{code}", 5) + ["waitidle 40"]
    E += ["lang auto"]
E += ["persona short", "peacetime 15:00", "wait 3"] + SHOW + ["act morning"] + rec("greet_short", 5) + ["waitidle 30"]
E += ["persona chibi", "peacetime 15:00", "wait 3", "showcase 10.6 2.2 8.7 1.6 Kitchen", "reidle", "wait 2", "pitch 24", "wait 2", "act morning"] + rec("greet_chibi", 5) + ["waitidle 30", "pitch 0", "wait 2"]
E += END
S["E"] = E

# F: a re-take of the photo on its own (in the first Korean run it came after the head-turning game, still going on)
F = start("crazy", 80, 9, "15:00")
F += SHOW + ["act photo"] + rec("photo", 7) + ["waitidle 40", "wait 1"] + END
S["F"] = F

# M: the menu's music on its own (sound is all that matters: a small picture in real time)
S["M"] = ["name", "screen 960 540", "avframe 0.034", "wait 34", f"recordav {LANG}_theme {BED_SECONDS} 30 640 360"] + END

# G: a re-take of the cooking and the card game (new cameras)
S["G"] = start("kind", 40, 7, "15:00") + COOK + CARDS + END
# H: Cappie's hammer game on its own, its rounds (not the rules she explains first: ~50 s) from three cameras - seated,
# your own view looks past her at the ceiling. Button at 8.74 0.83 3.09, her seat 8.87 0 4.35, yours 9.30 0 2.03
CAP2 = start("cap", 40, 7, "13:00")
CAP2 += ["freeplayer", "wait 2", "peacetime 14:00", "walkto 8.3 1.2 ~90", "wait 2", "hammer auto on", "hammer start", "wait 47"]
CAP2 += ["devcam 7.9 1.4 3.3 0 0 50", "devcam look 8.9 1.0 3.9"] + rec("hammer", 16)
CAP2 += ["devcam 8.5 1.35 2.2 0 0 50", "devcam look 9.05 1.15 4.6"] + rec("hammer2", 14)
CAP2 += ["devcam 8.3 1.5 6.2 180 15 60"] + rec("hammer3", 14)
CAP2 += ["devcam off", "waitidle 300", "wait 3"] + END
S["H"] = CAP2

out = os.path.join(HERE, "trailer_scripts")
os.makedirs(out, exist_ok=True)
total = 0
for k, L in S.items():
    p = os.path.join(out, f"{LANG}_{k}.txt")
    open(p, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    secs = sum(float(l.split()[2]) for l in L if l.startswith("recordav") and "_theme " not in l)
    total += secs
    print(f"{os.path.basename(p)}: {len(L)} lines, {secs:.0f} s of clips (~{secs * FRAME * FPS / 60:.0f} min to render)")
print(f"{LANG}: {total:.0f} s of clips in all (~{total * FRAME * FPS / 60:.0f} min to render, plus the runs' own time)")
