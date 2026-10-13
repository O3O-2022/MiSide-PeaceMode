"""DevRunner scripts that render the showcase video's clips (1080p60 + the game's sound, see PeaceMode/AvRecorder.cs).

usage: python tools/video/make_scripts.py <lang>      (ko | en | ja | zh: the game's language for that run)
writes tools/video/scripts/<lang>_<session>.txt; clips land in BepInEx/config/video/<lang>_<clip>.mp4 (+ .raw.wav, .sync.txt).
Scenes that start one of the story's player animations (the hug, holding hands, the card table, the dance pad, the
hammer game's chair) start just before their recording: those animations don't take while a recording runs.
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
LANG = sys.argv[1] if len(sys.argv) > 1 else "ko"
AWAY = {"ko": "18분", "en": "18 minutes", "ja": "18分", "zh": "18分钟"}[LANG]
# the game's own language, set in the main menu (the game takes Steam's language at launch, whatever its prefs say)
GAMELANG = {"ko": [], "en": ["gamelang English"], "ja": ["gamelang Japanese"], "zh": ["gamelang ChineseSimplified"]}[LANG]
# (the clock back to the afternoon before each staged clip: otherwise how dark the house is depends on how long the run took)
SHOW = ["peacetime 15:00", "showcase 10.6 2.2 9.25 1.65 Kitchen", "reidle", "wait 2"]


def rec(name, sec):
    return [f"recordav {LANG}_{name} {sec}"]


def menu(name, picks, sec, pre=None, show=True, unshow=False):
    L = list(SHOW) if show else []
    if unshow: L += ["unshowcase", "wait 1", "lookat mita", "wait 1"]
    if pre: L += pre
    L.append("peacemenu")
    for p in picks:
        L.append(("choosewhen " + p[1:]) if p.startswith("#") else "pickwhen " + p)
    L += rec(name, sec) + ["waitidle 60", "wait 1"]
    return L


def start(pid, aff, tier, time, menus=False):
    L = ["name", "screen 960 540", f"peaceset {pid} affection={aff}", f"peaceset {pid} lastTier={tier}", "wait 34"] + GAMELANG
    if menus:
        L += rec("menu", 4)
        L += ["menupeace", "after 1.5 menuhover 0", "after 3 menuhover 2", "after 4.5 menuhover 3", "after 6 menuhover 6"] + rec("select", 8)
    L += [f"peace {pid}", "waitready 240", "wait 3", "calm", "freeplayer", "greet", "waitidle 90", "waitimports 400", f"peacetime {time}"]
    return L


END = ["wait 2", "screen 1920 1080 full", "wait 2", "quit"]
S = {}

# A: the original Mita - the menus, talking, touch, the games, the special moments, the fourth wall, goodbye
A = start("crazy", 80, 9, "15:00", menus=True)
A += SHOW + ["act morning"] + rec("greet_crazy", 6) + ["waitidle 30"]
A += SHOW + ["gestureall", "act talk"] + rec("talk", 8) + ["gestureall off", "waitidle 30"]
A += menu("ask", ["대화하기", "질문하기", "#0"], 11)
A += menu("pat", ["교감하기", "쓰다듬기"], 7)
A += SHOW + ["act hug"] + rec("hug", 9) + ["waitidle 40", "wait 2"]
A += SHOW + ["act special"] + rec("hold", 10) + ["waitidle 40", "wait 2"]
A += menu("compliment", ["교감하기", "칭찬하기"], 7)
A += menu("glasses", ["교감하기", "안경 써 줘"], 7)
A += SHOW + ["peacemenu", "pickwhen 교감하기", "pickwhen 안경 벗기", "wait 8", "waitidle 30"]
A += menu("clothes", ["교감하기", "옷 갈아입기", "#1"], 9)
A += menu("rps", ["같이 놀자", "가위바위보", "가위"], 12, unshow=True)
A += menu("words", ["같이 놀자", "끝말잇기", "#0", "#0", "#0"], 12, unshow=True)
A += menu("riddle", ["같이 놀자", "넌센스 퀴즈", "#0"], 12, unshow=True)
A += menu("cham", ["같이 놀자", "참참참"], 16, pre=["game chamauto"], unshow=True)
A += menu("photo", ["같이 놀자", "같이 사진 찍기"], 7)
A += menu("window", ["특별한 시간", "같이 창밖 보기"], 12, unshow=True)
A += menu("lap", ["특별한 시간", "무릎베개"], 20, unshow=True)
A += menu("song", ["특별한 시간", "노래 불러 줘"], 12, unshow=True)
A += menu("heart", ["특별한 시간", "속마음 듣기"], 10, unshow=True)
A += SHOW + ["wait 2", f"wall AltF4 {AWAY}"] + rec("altf4", 10) + ["waitidle 30"]
A += SHOW + ["unshowcase", "wait 1", "lookat mita", "wait 2", "peacemenu", "pickwhen 오늘은 여기까지", "pickwhen 메인 메뉴로"] + rec("bye", 7) + ["wait 30"] + END
S["A"] = A

# B: Kind Mita - staring, house things, favours, the diary, hide and seek, the card game, her advice, cooking
B = start("kind", 40, 7, "15:00")
B += SHOW + ["act morning"] + rec("greet_kind", 5) + ["waitidle 30"]
# (you look away first and turn to her in the recording: staring before it, she'd have had her shy moment already)
B += SHOW + ["unshowcase", "lookat 12.5 1.5 4.5", "wait 2", "after 0.7 look face"] + rec("shy", 8) + ["look off", "waitidle 30"]
B += SHOW + ["unshowcase", "wait 1", "look face", "after 1 spot oldtv"] + rec("spot", 12) + ["look off", "waitidle 60", "wait 1"]
B += SHOW + ["after 0.5 game request:fridge", "after 7 spot fridge", "after 11 peacemenu"] + rec("request", 15) + ["waitidle 60", "wait 1"]
B += SHOW + ["after 0.5 game diary"] + rec("diary", 9) + ["waitidle 60", "wait 1"]
B += SHOW + ["act special"] + rec("advice", 10) + ["waitidle 40", "wait 1"]
# (she hides by the fridge, and you walk over and find her)
B += SHOW + ["unshowcase", "wait 1", "lookat mita", "wait 1", "game hidespot 0", "peacemenu", "pickwhen 같이 놀자", "pickwhen 숨바꼭질",
             "after 12 walkto 12.4 3.6", "after 19 game found"] + rec("hide", 22) + ["waitidle 60", "wait 3"]
B += SHOW + ["peacetime 12:10", "peacemenu", "pickwhen 같이 요리하기"] + rec("cook1", 10)
# (then her at the stove - the carrot cutting ends by itself when started from a script, so it isn't filmed)
B += ["wait 6", "lookat 12.84 1.3 1.37", "wait 1"] + rec("cook2", 8) + ["peacemenu", "pickwhen 요리 그만하기", "waitidle 60", "wait 2"]
# (the card game last: its seat keeps holding you after it)
B += ["walkto @Interactive Cards >> ~90", "wait 1", "click Interactive Cards >>", "wait 1"] + rec("cards", 10) + ["cardstop", "wait 20"] + END
S["B"] = B

FOLLOW = ["freeplayer", "wait 1", "showcase 1.6 -0.9 0.9 -0.2 Main", "wait 2", "unshowcase", "wait 1", "lookat mita", "wait 1",
          "after 4 walkto 8.4 0.6", "after 12 look face", "peacemenu", "pickwhen 같이 다니자"] + rec("follow", 16) + ["look off", "game stay", "waitidle 60", "wait 2"]


# C: Sleepy Mita - her suggestion and the sofa, follow me, the weather and seasons, her coffee, bed
C = start("sleepy", 40, 7, "14:00")
# (let her go after the greeting: posed in the kitchen she can't follow you to the living room and suggest the TV there)
C += SHOW + ["act morning"] + rec("greet_sleepy", 5) + ["waitidle 30", "unshowcase", "wait 3"]
C += ["walkto 0.9 -1.4 ~90", "wait 2", "mitago 2.2 -1.1", "wait 8", "look face", "wait 1", "after 0.5 propose tv", "pickwhen 좋아", "after 7 look off"] + rec("propose", 18) + ["look off", "waitidle 90", "wait 2"]
C += ["after 0.5 lounge read"] + rec("read", 10) + ["waitidle 90", "wait 2"]
C += ["after 0.5 lounge rest"] + rec("rest", 10) + ["waitidle 90", "wait 2"]
C += ["walkto 3.0 -2.0 ~60", "lounge alone", "wait 3", "look face"] + rec("alone", 10) + ["look off", "wait 20",
      "walkto 0.95 -0.3 ~40", "look face", "wait 2", "peacemenu", "pickwhen 옆에 앉기", "after 4 look off"] + rec("join", 12) + ["look off", "waitidle 90", "wait 2"]
# (from open floor: off the sofa you'd start wedged against it)
C += FOLLOW
# (the rain and snow through the kitchen window, from a camera close to it; you stand behind it, in the kitchen, so it's drawn)
C += ["peacetime 14:00", "showcase 10.6 2.2 12.3 3.6 Kitchen", "wait 2", "lookat 14.27 1.3 4.02", "devcam 13.0 1.5 3.9 0 0 45", "devcam look 14.27 1.3 4.02",
      "weather rain", "wait 25"] + rec("rain", 5)
C += ["weather snow", "wait 25"] + rec("snow", 5) + ["devcam off", "weather clear", "wait 5"]
# (placed, not walked: walking there she tends to stand in a doorway in your way; she waits behind you)
C += ["unshowcase", "wait 1", "showcase 1.6 1.4 0.6 -0.2 Main", "wait 2", "lookat 0.5 1.3 -3.8", "peacetime 12:00", "weather season 0", "wait 3",
      "after 2 weather season 1", "after 4 weather season 2", "after 6 weather season 3"] + rec("seasons", 8) + ["weather season -1", "unshowcase", "wait 2"]
C += ["walkto 9.25 1.65 ~120", "wait 2"] + SHOW + ["act special"] + rec("coffee", 10) + ["waitidle 60", "wait 2"]
C += SHOW + ["peacetime 22:30", "wait 2", "peacemenu", "pickwhen 이제 자자", "wait 4"] + rec("bed", 14) + ["wait 40"] + END
S["C"] = C

# D: Cappie - her greeting, the nod, her guitar, the dance battle, the hammer game
D = start("cap", 40, 7, "13:00")
D += SHOW + ["act morning"] + rec("greet_cap", 5) + ["waitidle 30"]
D += SHOW + ["wait 1", "gesture nod"] + rec("nod", 4) + ["wait 2", "waitidle 20"]
D += SHOW + ["act special"] + rec("guitar", 10) + ["waitidle 40", "wait 2"]
D += ["unshowcase", "walkto 0.9 -0.4 ~90", "wait 2", "dance auto on 0.87 0", "dance start", "wait 12",
      "after 3 dance menu 1", "after 5 dance menu 0", "after 7.5 dance pick"] + rec("dance1", 14)
# (your turn starts ~80 s after her dance; you step onto the pad 2 s later - a player animation, so before the recording)
D += ["wait 10", "devcam 2.6 1.4 -1.9 -90 10 60"] + rec("dance2", 8) + ["devcam off", "wait 60", "dance state", "wait 28", "dance state"] + rec("dance3", 10)
D += ["wait 38", "dance state"] + rec("dance4", 16) + ["waitidle 120", "wait 3"]
D += ["freeplayer", "wait 2", "peacetime 14:00", "walkto 8.3 1.2 ~90", "wait 2", "hammer auto on", "hammer start", "wait 9"] + rec("hammer1", 12)
D += ["wait 6"] + rec("hammer2", 16) + ["waitidle 300", "wait 3"] + END
S["D"] = D

# E: the other three Mitas' greetings and their own things; cold affection, contempt, the top; the other languages
E = start("mila", 40, 7, "15:00")
E += SHOW + ["act morning"] + rec("greet_mila", 5) + ["waitidle 30"]
E += SHOW + ["act special"] + rec("tea", 10) + ["waitidle 60", "wait 2"]
E += ["persona short", "peacetime 15:00", "wait 3"] + SHOW + ["act morning"] + rec("greet_short", 5) + ["waitidle 30"]
E += SHOW + ["act special"] + rec("lecture", 10) + ["waitidle 60", "wait 2"]
E += ["persona chibi", "peacetime 15:00", "wait 3", "showcase 10.6 2.2 8.7 1.6 Kitchen", "reidle", "wait 2", "pitch 24", "wait 2", "act morning"] + rec("greet_chibi", 5) + ["waitidle 30"]
E += ["reidle", "wait 2", "act special", "after 5 peacechoose 0"] + rec("quiz", 10) + ["waitidle 60", "pitch 0", "wait 2"]
E += ["peaceset kind affection=-45", "peaceset kind lastTier=1", "persona kind", "peacetime 15:00", "wait 3"]
E += SHOW + ["act pat"] + rec("cold", 7) + ["waitidle 30"]
E += SHOW + ["contempt"] + rec("contempt", 4) + ["contempt off", "wait 2"]
E += ["aff 125"] + rec("top", 10) + ["waitidle 40"]
if LANG == "ko":
    for code in ("en", "ja", "zh"):
        E += [f"lang {code}"] + SHOW + ["peacemenu", "pickwhen 대화하기", "pickwhen 아무 이야기나"] + rec(f"lang_{code}", 6) + ["waitidle 40"]
    E += ["lang auto"]
E += END
S["E"] = E

# F: re-takes of a few clips on their own (come with me, the cooking - after fixes)
F = start("kind", 40, 7, "15:00")
F += FOLLOW
F += SHOW + ["peacetime 12:10", "peacemenu", "pickwhen 같이 요리하기"] + rec("cook1", 10)
F += ["wait 6", "lookat 12.84 1.3 1.37", "wait 1"] + rec("cook2", 8) + ["wait 30"] + END
S["F"] = F

out = os.path.join(HERE, "scripts")
os.makedirs(out, exist_ok=True)
for k, L in S.items():
    p = os.path.join(out, f"{LANG}_{k}.txt")
    open(p, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    secs = sum(float(l.split()[2]) for l in L if l.startswith("recordav"))
    print(f"{p}: {len(L)} lines, {secs:.0f} s of clips (~{secs * 18 / 60:.0f} min to render)")
