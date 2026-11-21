"""The v1.1 promo trailer (Korean, captions burned in): the 1.0 trailer's 4K clips for the basics + the new v1.1 clips
(make_trailer11.py: new menu, gestures, photo mode, album, diary, plant, the other Mitas' clothes, goals, her letter,
the AI settings), cut over the main menu's theme. Same machinery as trailer.py (sound mix, 4K x264, ASS burn-in).

usage: python tools/video/trailer11.py audio | video | sheet | all
       -> dev_output/trailer/final/MiSide_PeaceMode_v1.1_trailer.mp4
"""
import os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import trailer as T

T.OUT_NAME = "MiSide_PeaceMode_v1.1_trailer"
T.STORY.clear()
seg = T.seg

# (clip, caption, start s, length s, name, own sound)
seg("greet_crazy", "hook", 0.3, 3.0)
seg("select", "title", 0.3, 4.2, snd=0.0)
for who in ("kind", "sleepy", "cap", "mila", "short", "chibi"):
    seg("greet_" + who, "roster", 0.6, 1.3, who)
seg("v11_menu", "menu", 0.6, 4.6)
seg("talk", "touch", 3.4, 2.2); seg("pat", "touch", 0.3, 2.2); seg("hug", "touch", 0.2, 2.6)
seg("v11_photo", "photo", 0.4, 7.0)
seg("v11_album", "album", 0.4, 3.2)
seg("v11_diary", "diary", 0.3, 2.4)
seg("v11_color_kind", "clothes", 0.6, 5.0, snd=0.0)
for pid in ("cap", "mila", "short"):   # (the first change came late: the recolour took a few seconds of real time)
    seg("v11_color_" + pid, "clothes", 1.0, 2.2, snd=0.0)
seg("rps", "games", 3.6, 2.4); seg("cards", "games", 1.5, 2.4); seg("dance1", "games", 9.5, 2.6, snd=0.0); seg("hammer2", "games", 2.4, 3.0)
seg("cook", "day", 1.0, 2.4); seg("lap", "day", 11.5, 2.8); seg("bed", "day", 7.0, 2.8); seg("seasons", "day", 1.0, 4.4)
seg("v11_goals", "goals", 0.3, 3.2)
seg("v11_letter", "letter", 1.0, 6.0)
seg("v11_ai", "ai", 0.3, 2.8, snd=0.0)
seg("altf4", "wall", 2.6, 3.2)
seg("bye", "bye", 2.6, 3.2)
seg(None, "end", 0, 5.0)

CAPTION = {
    "roster": "미타 7명 중 한 명과 보내는 하루",
    "menu": "새로워진 메뉴 · 마우스 · 숫자 키 · 게임패드",
    "touch": "대화 · 쓰다듬기 · 안아 주기",
    "gesture": "대사에 맞춰 움직이는 몸짓",
    "photo": "사진 모드 — 포즈 · 표정 · 프레임",
    "album": "같이 찍은 사진 앨범",
    "diary": "미타가 쓰는 일기장",
    "plant": "하루 한 번, 같이 키우는 화분",
    "clothes": "NEW · 다른 미타들도 옷 갈아입기",
    "games": "가위바위보 · 카드 게임 · 댄스 배틀 · 망치 게임",
    "day": "요리 · 무릎베개 · 잠자기 · 사계절",
    "goals": "NEW · 미타와 함께 이룰 목표 17가지",
    "letter": "목표를 모두 이루면, 미타의 편지",
    "ai": "NEW · Gemini · ChatGPT로 자유 대화 (선택)",
    "wall": "게임 밖 현실에도 반응하는 미타",
}
NAME = {"kind": "착한 미타", "sleepy": "잠꾸러기 미타", "cap": "캐피", "mila": "밀라", "short": "단발 미타", "chibi": "미니 미타"}


def make_ass():
    A, W, H, K, FPS = T.A, T.W, T.H, T.K, T.FPS
    tl = [dict(s, f0=f0, frames=fr) for s, f0, fr in T.timeline()]
    ev = []

    def add(t0, t1, style, text, layer=1, fade=(180, 180)):
        ev.append(f"Dialogue: {layer},{A.ts(t0)},{A.ts(t1)},{style},,0,0,0,,{{\\fad({fade[0]},{fade[1]})}}{text}")

    cx, cy = W // 2, H // 2
    fn = T.fn
    badge = f" 비공식 팬 모드  ·  {fn('lat', 'UNOFFICIAL FAN MOD')} "
    i = 0
    while i < len(tl):
        p = tl[i]
        t0 = p["f0"] / FPS
        j = i
        if p["key"] not in ("roster", "title", "end"):
            while j + 1 < len(tl) and tl[j + 1]["key"] == p["key"]: j += 1
        t1 = (tl[j]["f0"] + tl[j]["frames"]) / FPS
        key = p["key"]
        if key in ("title", "end"):
            add(t0, t1, "Box", f"{{\\an7\\pos(0,0)\\1c&H1A0A16&\\1a&H{'48' if key == 'title' else '00'}&\\bord0\\shad0\\p1}}m 0 0 l {W} 0 {W} {H} 0 {H}{{\\p0}}", layer=0, fade=(150, 250))
            add(t0 + 0.15, t1, "Title2", f"{{\\pos({cx},{cy - int(300 * K)})}}MiSide", fade=(300, 300))
            add(t0 + 0.15, t1, "Title", f"{{\\pos({cx},{cy - int(170 * K)})}}평화 모드", fade=(300, 300))
            add(t0 + 0.3, t1, "Title2", f"{{\\pos({cx},{cy - int(40 * K)})}}Peace Mode  v1.1", fade=(300, 300))
            add(t0 + 0.5, t1, "Badge", f"{{\\pos({cx},{cy + int(90 * K)})}}{badge}", fade=(250, 300))
            add(t0 + 0.8, t1, "Sub", f"{{\\pos({cx},{cy + int(200 * K)})}}AIHASTO의 공식 '평화 모드'가 아닌 팬 모드입니다", fade=(300, 300))
            if key == "end":
                add(t0 + 1.0, t1, "Sub", f"{{\\pos({cx},{cy + int(300 * K)})\\fs{int(54 * K)}\\1c&H46D6FF&}}무료 · {fn('lat', T.REPO_URL.replace('https://', ''))}", fade=(400, 400))
                add(t0 + 1.0, t1, "Small", fn("lat", "MiSide © AIHASTO · fan-made, not affiliated with AIHASTO"), fade=(400, 400))
        elif key == "hook":
            add(t0 + 0.3, t1 - 0.1, "Cap", f"{{\\pos({cx},{H - int(150 * K)})}}공포 0%, 미타와 보내는 하루", layer=3, fade=(300, 200))
        elif key == "roster":
            add(t0 + 0.05, t1 - 0.05, "Cap", f"{{\\pos({cx},{H - int(150 * K)})}}{NAME[p['who']]}", layer=3, fade=(120, 120))
        elif key in CAPTION:
            text = CAPTION[key]
            if text.startswith("NEW · "):
                text = f"{{\\1c&H46D6FF&}}NEW{{\\r}}  " + text[6:]
            add(t0 + 0.15, t1 - 0.1, "Cap", f"{{\\pos({cx},{H - int(150 * K)})}}{text}", layer=3, fade=(200, 200))
        i = j + 1
    # the roster's line above the names
    r = [p for p in tl if p["key"] == "roster"]
    if r:
        add(r[0]["f0"] / FPS, (r[-1]["f0"] + r[-1]["frames"]) / FPS, "Small", f"{{\\pos({cx},{H - int(240 * K)})\\an5}}{CAPTION['roster']}", layer=3, fade=(200, 200))
    # the corner mark from the title card on
    first, last = tl[2]["f0"] / FPS, tl[-1]["f0"] / FPS
    add(first, last, "Mark", f" 비공식 팬 모드 · {fn('lat', 'Unofficial fan mod')} ", layer=2, fade=(300, 300))
    p = os.path.join(T.BUILD, "trailer11.ass")
    os.makedirs(T.BUILD, exist_ok=True)
    k = K
    cap = (f"Style: Cap,{T.FN['ko']},{int(58 * k)},&H00FFFFFF,&H00FFFFFF,&H00301828,&H96000000,1,0,0,0,100,100,0,0,1,"
           f"{int(5 * k)},{int(3 * k)},5,{int(80 * k)},{int(80 * k)},0,1\n")
    # (the style goes in [V4+ Styles], before [Events] and its own Format line: after it, the style line was misread)
    head = T.ass_header().replace("\n[Events]", cap + "\n[Events]", 1)
    open(p, "w", encoding="utf-8-sig").write(head + "\n".join(ev) + "\n")
    return p


T.make_ass = make_ass

if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    tl = T.timeline()
    print(f"{len(tl)} cuts, {sum(f for _, _, f in tl) / T.FPS:.1f} s")
    missing = sorted({s['clip'] for s in T.STORY if s['clip'] and not os.path.exists(T.clip_file(s['clip'], '.mp4'))})
    if missing: print("missing clips:", ", ".join(missing))
    if what in ("audio", "all"): T.build_audio()
    if what in ("video", "all"): T.render()
    if what == "sheet": T.sheet()
