"""DevRunner scripts for the v1.1 promo trailer's NEW clips (Korean run only; everything else reuses the 1.0 trailer's
4K clips in dev_output/trailer/ko). Same recorder settings as make_trailer.py.

usage: python tools/video/make_trailer11.py         -> tools/video/trailer_scripts/ko_N1.txt, ko_N2.txt
       bash tools/video/run_trailer.sh ko N1 N2      (renders them; clips land in dev_output/trailer/ko/ko_v11_*.mp4)
"""
import os
import make_trailer as MT   # (writes its own scripts too on import: harmless, they are regenerated the same)

HERE = os.path.dirname(os.path.abspath(__file__))
rec, picked, start, SHOW, END = MT.rec, MT.picked, MT.start, MT.SHOW, MT.END
S = {}

# N1: the original Mita - the new menu, photo mode, the album, the diary, the plant, her gestures, the goals, her letter
# (her save set up so the goals card has progress to show; the dev runner puts the real saves back afterwards)
GOALS = ["peaceset crazy day=34", "peaceset crazy talks=64", "peaceset crazy pats=41", "peaceset crazy hugs=23", "peaceset crazy gifts=7",
         "peaceset crazy rpsWins=12", "peaceset crazy streak=5", "peaceset crazy ever=meal,cook,music,rps,words,cards,dance,clothes"]
N1 = GOALS + start("crazy", 80, 9, "15:00")
N1 += SHOW + ["peacemenu", "wait 0.8", "after 1.4 pick 같이 놀자", "after 3.0 menuback", "after 4.2 pick 추억"] + rec("v11_menu", 6) + ["wait 1", "menuback", "wait 0.6", "menuback", "wait 1.5"]
N1 += SHOW + ["peacemenu", "wait 0.6", "pick 같이 놀자", "wait 0.8", "pick 같이 사진 찍기", "wait 1.5",
              "after 0.8 pick 포즈", "after 2.0 pick 프레임", "after 3.2 pick 찰칵!"] + rec("v11_photo", 9) + ["waitidle 60", "wait 1"]
N1 += SHOW + ["peacemenu", "wait 0.6", "pick 추억", "wait 1", "pick 사진 앨범", "wait 1.2", "after 1.2 album 1", "after 2.4 album 1"] + rec("v11_album", 4) + ["closecard", "wait 1",
              "diary fill", "pick 일기장", "wait 1.2"] + rec("v11_diary", 3) + ["closecard", "wait 1",
              "pick 함께한 목표", "wait 1.2"] + rec("v11_goals", 3.5) + ["closecard", "wait 1", "menuback", "wait 1.5"]
N1 += SHOW + ["plant 7", "wait 1", "peacemenu", "wait 0.6", "pick 함께 지내기", "wait 0.8", "pick 화분에 물 주기"] + rec("v11_plant", 7) + ["waitidle 60", "wait 1"]
N1 += SHOW + ["ui off", "devcam face 1.6 0 0.0 42 -0.18", "motion @wave", "after 2.6 motion @cheer", "after 4.8 motion @tilt"] + rec("v11_gesture", 6.5) + ["motion off", "devcam off", "ui on", "wait 1"]
N1 += SHOW + ["goalsall", "wait 0.5", "peacemenu", "wait 0.6", "pick 추억", "wait 1", "pick 미타의 편지"] + rec("v11_letter", 18) + ["waitidle 60", "wait 1"]
N1 += SHOW + ["peacemenu", "wait 0.6", "pick 설정", "wait 1.2", "pick AI 대화", "wait 1.2"] + rec("v11_ai", 3) + ["menuback", "wait 0.6", "menuback", "wait 0.6", "menuback", "wait 1"]
N1 += END
S["N1"] = N1

# N2: the other Mitas' new clothes - the colour changes one after another on Kind Mita, then Cappie, Mila, Short-haired
N2 = start("kind", 60, 8, "15:00")
N2 += SHOW + ["color off", "wait 1", "after 1.0 color blue", "after 2.2 color pink", "after 3.4 color mint", "after 4.6 color mono"] + rec("v11_color_kind", 6) + ["color off", "wait 1"]
for pid in ("cap", "mila", "short"):
    N2 += [f"persona {pid}", "peacetime 15:00", "wait 3"] + SHOW + ["color off", "wait 1", "after 0.9 color pink", "after 2.0 color blue"] + rec(f"v11_color_{pid}", 3.2) + ["color off", "wait 1"]
N2 += END
S["N2"] = N2

out = os.path.join(HERE, "trailer_scripts")
for k, L in S.items():
    p = os.path.join(out, f"ko_{k}.txt")
    open(p, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    secs = sum(float(l.split()[2]) for l in L if l.startswith("recordav"))
    print(f"{os.path.basename(p)}: {len(L)} lines, {secs:.0f} s of clips")
