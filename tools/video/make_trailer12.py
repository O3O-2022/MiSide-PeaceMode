"""DevRunner scripts for the re-cut trailer (2026-11-16): re-takes of the scenes the frame-by-frame review fixed (hug,
hand in hand, touch reactions, the window, the lap pillow, bed, walking, the vampire / Christmas costumes). Same
recorder as make_trailer.py (4K60 + the game's sound). The edit itself is done in FilmCraft (trailer12_fc.py).

usage: python tools/video/make_trailer12.py        -> tools/video/trailer_scripts/ko_R1.txt, ko_R2.txt
       bash tools/video/run_trailer.sh ko R1 R2     (clips land in dev_output/trailer/ko/ko_r12_*.mp4)
"""
import os
import make_trailer as MT

HERE = os.path.dirname(os.path.abspath(__file__))
rec, picked, start, SHOW, END = MT.rec, MT.picked, MT.start, MT.SHOW, MT.END
S = {}


def WALK(name):
    """she walks from the living room up to you in the kitchen, seen from the front (her legs and arms move now)"""
    return SHOW + ["unshowcase", "wait 1", "mitago 2.2 -1.1", "wait 12", "look face", "wait 1", "after 0.3 mitago 8.7 1.6"] + rec(name, 9) +         ["look off", "waitidle 60", "wait 2"]

# R1: the original Mita - touch with its reactions, hug, hand in hand, a gift, the window, the lap pillow, walking with you,
# then the vampire and Christmas costumes' hugs
R1 = start("crazy", 80, 9, "15:00")
R1 += SHOW + ["act pat"] + rec("r12_pat", 6) + ["waitidle 30"]
R1 += picked("r12_compliment", ["교감하기", "칭찬하기"], 6)
R1 += SHOW + ["act hug"] + rec("r12_hug", 10) + ["waitidle 40", "wait 2"]
R1 += picked("r12_gift", ["교감하기", "선물하기", "꽃"], 9)
R1 += picked("r12_hands", ["손 잡기"], 14, unshow=True) + ["wait 10", "waitidle 60", "wait 2"]
R1 += picked("r12_window", ["특별한 시간", "같이 창밖 보기"], 12, unshow=True)
R1 += picked("r12_lap", ["특별한 시간", "무릎베개"], 18, unshow=True)
R1 += WALK("r12_walk")
for key, tag in (("HellVamp 0", "vamp"), ("Chirfns 0", "xmas")):
    R1 += [f"cloth {key}", "wait 1.5"] + SHOW + ["act hug"] + rec(f"r12_{tag}_hug", 10) + ["waitidle 40", "wait 2"]
R1 += ["cloth original 0", "wait 1"] + END
S["R1"] = R1

# R2: Sleepy Mita - walking now with her arms and legs, then bed and the morning after
R2 = start("sleepy", 60, 8, "15:00")
R2 += WALK("r12_sleepy_walk")
R2 += ["walkto 9.25 1.65 ~120", "wait 2"]
R2 += SHOW + ["peacetime 22:30", "wait 2", "peacemenu", "pickwhen 이제 자자", "wait 4"] + rec("r12_bed", 22) + ["wait 40"] + END
S["R2"] = R2

# R3: re-takes (R1's hugs were staged posed - a showcase holds her and posed she does not hug - and mitago did not move
# her): let go first and pick from the menu, as the frame-by-frame review did; the walk is her following you
FREE = ["showcase 10.6 2.2 9.25 1.65 Kitchen", "reidle", "wait 1.5", "unshowcase", "wait 0.5", "tpdist 1.6", "lookat mita", "wait 0.8"]


def HUG(name):
    return ["peacetime 15:00"] + FREE + ["peacemenu", "wait 1.3", "pick 교감하기", "wait 1.3", "pick 안아주기"] + rec(name, 10) + ["waitidle 60", "wait 2"]


def FOLLOW(name):
    return ["peacetime 15:00"] + FREE + ["peacemenu", "wait 1.3", "pick 함께 지내기", "wait 1.3", "pick 같이 다니자", "wait 1.5",
            "walkto 0.9 -0.4 ~90", "lookat mita"] + rec(name, 6) + ["game stay", "waitidle 60", "wait 2"]


R3 = start("crazy", 80, 9, "15:00")
R3 += FOLLOW("r12_walk") + ["persona sleepy", "wait 3"] + FOLLOW("r12_sleepy_walk") + END
S["R3"] = R3

# R4: the hugs. The trailer sessions' start() frees the player ("freeplayer"), and the hug carries your view through
# your own animation: freed, the view stayed where it was and she walked out of the picture. Here the player stays tied.
R4 = [l for l in start("crazy", 80, 9, "15:00") if l != "freeplayer"]
R4 += HUG("r12_hug")
for key, tag in (("HellVamp 0", "vamp"), ("Chirfns 0", "xmas")):
    R4 += [f"cloth {key}", "wait 1.5"] + HUG(f"r12_{tag}_hug")
R4 += ["cloth original 0", "wait 1"] + END
S["R4"] = R4

out = os.path.join(HERE, "trailer_scripts")
for k, L in S.items():
    p = os.path.join(out, f"ko_{k}.txt")
    open(p, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    secs = sum(float(l.split()[2]) for l in L if l.startswith("recordav"))
    print(f"{os.path.basename(p)}: {len(L)} lines, {secs:.0f} s of clips (~{secs * MT.FRAME * MT.FPS / 60:.0f} min)")
