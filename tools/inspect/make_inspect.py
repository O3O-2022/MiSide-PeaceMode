"""DevRunner scripts that record every feature frame by frame for inspection (recidle: 30 fps game time, 960x540,
until she is idle again or a menu waits). Frames land in BepInEx/config/frames/<name>_NNNN.jpg; then
tools/inspect/inspect_frames.py flags the frames that jump and makes contact sheets.

usage: python tools/inspect/make_inspect.py      -> tools/inspect/scripts/<session>.txt
       bash tools/inspect/run_inspect.sh [session ...]
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
# she is put in the kitchen, then let go (a showcase holds her posed, and posed she doesn't hug, hold hands...), and
# you stand in front of her looking at her - as in play
SHOW = ["showcase 10.6 2.2 9.25 1.65 Kitchen", "reidle", "wait 1.5", "unshowcase", "wait 0.5", "tpdist 1.6", "lookat mita", "wait 0.6"]
END = ["wait 2", "quit"]


def start(pid, time="15:00", aff=90):
    return ["wait 22", f"peace {pid}", "waitready 240", "waitimports 300", "wait 5", "calm", f"aff {aff}", f"peacetime {time}", "wait 1"]


def act(name, picks, rec_max=40, then=None, show=True, pre=None):
    """open her menu, pick the way down, record until she is idle again; `then`: (command, record?) steps after"""
    L = list(SHOW) if show else []
    if pre: L += pre
    L += ["peacemenu", "wait 0.8"]
    for i, p in enumerate(picks):
        L += [p if p.startswith("pickat") else "pick " + p]
        if i < len(picks) - 1: L += ["wait 0.8"]
    L += [f"recidle {name} {rec_max}"]
    for n, step in enumerate(then or []):
        L += [step, "wait 0.3", f"recidle {name}_{n + 1} {rec_max}"]
    L += ["waitidle 90", "wait 1"]
    return L


S = {}

# A: the original Mita - talk, touch, gifts, the small games, photo, the special times, hand in hand
A = start("crazy")
A += act("c_talk", ["대화하기", "아무 이야기나"])
A += act("c_question", ["대화하기", "질문하기", "pickat 0"])
A += act("c_pat", ["교감하기", "쓰다듬기"])
A += act("c_hug", ["교감하기", "안아주기"])
A += act("c_compliment", ["교감하기", "칭찬하기"])
A += act("c_glasses", ["교감하기", "안경 써 줘"])
A += act("c_glasses_off", ["교감하기", "안경 벗기"])
A += act("c_gift", ["교감하기", "선물하기", "pickat 0"])
A += act("c_rps", ["같이 놀자", "가위바위보"], then=["pickat 0", "pickat 0", "pickat 0", "pickat 1"])
A += act("c_cham", ["같이 놀자", "참참참"], pre=["game chamauto"], rec_max=60)
A += act("c_words", ["같이 놀자", "끝말잇기"], then=["pickat 0", "pickat 0", "pickat 0", "pickat 0"])
A += act("c_riddle", ["같이 놀자", "넌센스 퀴즈"], then=["pickat 0", "pickat 0", "pickat 0", "pickat 0"])
A += act("c_photo", ["같이 놀자", "같이 사진 찍기", "pickat 3"])
A += act("c_window", ["특별한 시간", "같이 창밖 보기"], rec_max=60)
A += act("c_music", ["특별한 시간", "같이 음악 듣기"], rec_max=60)
A += act("c_lap", ["특별한 시간", "무릎베개"], rec_max=60)
A += act("c_song", ["특별한 시간", "노래 불러 줘"], rec_max=60)
A += act("c_heart", ["특별한 시간", "속마음 듣기"], rec_max=60)
A += act("c_hands", ["손 잡기"], rec_max=90)
A += act("c_hide", ["같이 놀자", "숨바꼭질"], then=["game found"], rec_max=30)
A += END
S["A"] = A

# B: the day - cooking and the meal, the plant, following you, the card game, TV game, night talk, bed
B = start("crazy", "12:00")
B += act("c_meal", ["같이 밥 먹기"], rec_max=90)
B += ["peacetime 18:00", "wait 2"]   # (a meal had together is that meal: cooking is the next one's)
B += act("c_cook", ["같이 요리하기"], rec_max=40, then=["pickat 0"])
B += ["peacemenu", "wait 0.8", "pick 요리 그만하기", "waitidle 90", "wait 1", "peacetime 15:00", "wait 1"]
B += act("c_plant", ["함께 지내기", "화분에 물 주기"], pre=["plant 3"])
B += act("c_follow", ["함께 지내기", "같이 다니자"], then=["walkto 0.9 -0.4 ~90", "lookat mita", "game stay"], rec_max=30)
B += act("c_tv", ["같이 놀자", "원작 미니게임", "TV 게임"], rec_max=40)
B += ["wait 3", "menuback", "waitidle 60", "wait 2"]
B += ["peacetime 23:00", "wait 2"]
B += act("c_night", ["대화하기", "밤 이야기"], rec_max=60)
B += act("c_sleep", ["함께 지내기", "이제 자자"], rec_max=60)
B += END
S["B"] = B

# C: the costumes in motion - school uniform, vampire, Christmas: touch, hug, walking, sitting at the table, lap
C = start("crazy", "12:00")
for key, tag in (("HellVamp 0", "vamp"), ("Chirfns 0", "xmas"), ("FIIdClSchool 1", "school")):
    C += [f"cloth {key}", "wait 1"]
    C += act(f"o_{tag}_pat", ["교감하기", "쓰다듬기"])
    C += act(f"o_{tag}_hug", ["교감하기", "안아주기"])
    C += act(f"o_{tag}_follow", ["함께 지내기", "같이 다니자"], then=["walkto 0.9 -0.4 ~90", "lookat mita", "game stay"], rec_max=30)
    C += act(f"o_{tag}_lap", ["특별한 시간", "무릎베개"], rec_max=60)
C += ["cloth original 0", "wait 1"]
C += act("o_menu", ["교감하기", "옷 갈아입기", "pickat 0"], rec_max=20)
C += END
S["C"] = C

# D: Cappie's games
D = start("cap", "13:00")
D += ["unshowcase", "walkto 0.9 -0.4 ~90", "wait 2", "dance auto on 0.87 0", "dance start", "after 6 dance pick", "after 10 dance pick", "recidle k_dance 180", "waitidle 200", "wait 3"]
D += ["freeplayer", "peacetime 14:00", "walkto 8.3 1.2 ~90", "wait 2", "hammer auto on", "hammer start", "recidle k_hammer 200", "waitidle 300", "wait 3"]
D += END
S["D"] = D

# E..I: the other Mitas - what each of them has
for pid in ("kind", "cap", "mila", "sleepy", "short", "chibi"):
    L = start(pid, "12:00")
    p = pid[:2]
    L += act(f"{p}_talk", ["대화하기", "아무 이야기나"])
    L += act(f"{p}_pat", ["교감하기", "쓰다듬기"])
    L += act(f"{p}_hug", ["교감하기", "안아주기"])
    L += act(f"{p}_compliment", ["교감하기", "칭찬하기"])
    L += act(f"{p}_rps", ["같이 놀자", "가위바위보"], then=["pickat 0", "pickat 1"])
    L += act(f"{p}_photo", ["같이 놀자", "같이 사진 찍기", "pickat 3"])
    if pid != "chibi": L += act(f"{p}_clothes", ["교감하기", "옷 갈아입기", "pickat 0"], rec_max=20)
    L += act(f"{p}_meal", ["같이 밥 먹기"], rec_max=90, pre=["peacetime 12:05", "wait 1"])
    L += act(f"{p}_follow", ["함께 지내기", "같이 다니자"], then=["walkto 0.9 -0.4 ~90", "lookat mita", "game stay"], rec_max=30)
    L += ["peacetime 23:00", "wait 2"]
    L += act(f"{p}_sleep", ["함께 지내기", "이제 자자"], rec_max=60)
    L += END
    S["M_" + pid] = L

out = os.path.join(HERE, "scripts")
os.makedirs(out, exist_ok=True)
for k, L in S.items():
    open(os.path.join(out, k + ".txt"), "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    print(k, len(L), "lines,", sum(1 for l in L if l.startswith("recidle")), "recordings")
