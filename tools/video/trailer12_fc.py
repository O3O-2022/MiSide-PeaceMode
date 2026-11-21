"""The re-cut promo trailer, edited in FilmCraft (the user's editor) instead of an ffmpeg filter graph.

The 1.0 / v1.1 trailer clips (dev_output/trailer/ko/ko_<clip>.mp4 + .wav) with the re-takes of the scenes the
frame-by-frame review fixed (make_trailer12.py: ko_r12_*), the main menu's theme as the music bed, Korean captions as
FilmCraft text graphics, a title card and an end card. Built headless with filmcraft-cli in phases (each phase's
results give the ids the next one needs), saved as a .fcproj, exported with filmcraft-gpu (fcgpu.py).

usage: python tools/video/trailer12_fc.py build            -> dev_output/trailer/fc/trailer12.fcproj
       python tools/video/trailer12_fc.py frames           a still from every cut (dev_output/trailer/fc/check/)
       python tools/video/trailer12_fc.py export [--draft] -> dev_output/trailer/final/MiSide_PeaceMode_v1.1_trailer_fc.mp4
"""
import json, os, subprocess, sys, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
VID = os.path.join(REPO, "dev_output", "trailer")
CLIPS = os.path.join(VID, "ko")
OUT = os.path.join(VID, "fc")
# CLEAN=1: the version for every language - no Korean captions burned in (YouTube subtitles carry them), English on the cards
CLEAN = bool(os.environ.get("TRAILER_CLEAN"))
PROJ = os.path.join(OUT, "trailer12_clean.fcproj" if CLEAN else "trailer12.fcproj")
FINAL = os.path.join(VID, "final", "MiSide_PeaceMode_v1.1_trailer_fc" + ("_clean" if CLEAN else "") + ".mp4")
CLI = r"C:\Program Files\FilmCraft\filmcraft-cli.exe"
FCGPU = os.path.expanduser(r"~\.claude\skills\filmcraft-gpu\scripts\fcgpu.py")
TPS = 254016000000          # FilmCraft ticks per second
W, H, FPS = 3840, 2160, 60
FONT = "Malgun Gothic"
VOICE_BOOST = float(os.environ.get("TRAILER_BOOST", 22))   # dB into each voice clip's limiter
BED_RATIO = float(os.environ.get("TRAILER_BED", 0.45))     # the music bed's level against her (boosted) voice
REPO_URL = "github.com/O3O-2022/MiSide-PeaceMode"

# (clip, caption key, start s in the clip, length s, clip sound gain)
STORY = []


def seg(clip, key=None, a=0.0, n=2.5, snd=1.0, who=None):
    STORY.append(dict(clip=clip, key=key, a=a, n=n, snd=snd, who=who))


seg("greet_crazy", "hook", 0.3, 3.0)
seg("select", "title", 0.3, 4.2, snd=0.0)
for who in ("kind", "sleepy", "cap", "mila", "short", "chibi"):
    seg("greet_" + who, "roster", 0.6, 1.3, who=who)
seg("v11_menu", "menu", 0.6, 4.0)
seg("talk", "touch", 3.4, 2.2); seg("r12_pat", "touch", 0.3, 2.4); seg("r12_compliment", "touch", 2.8, 2.0)
seg("r12_hug", "hug", 0.2, 3.4)
seg("r12_hands", "hands", 2.6, 3.6)
seg("r12_gift", "gift", 2.9, 2.6)
seg("v11_photo", "photo", 0.4, 5.0); seg("v11_album", "photo", 0.4, 2.6)
seg("r12_vamp_hug", "outfit", 0.4, 3.0); seg("r12_xmas_hug", "outfit", 0.4, 3.0); seg("v11_color_kind", "outfit", 0.6, 3.6, snd=0.0)
seg("rps", "games", 3.6, 2.2); seg("cards", "games", 1.5, 2.2); seg("dance1", "games", 9.5, 2.4, snd=0.0); seg("hammer2", "games", 2.4, 2.8)
seg("r12_window", "window", 2.2, 3.2); seg("r12_lap", "lap", 12.4, 3.2)
seg("r12_walk", "walk", 3.6, 2.3); seg("r12_sleepy_walk", "walk", 3.8, 2.1)
seg("cook", "day", 1.0, 2.2); seg("r12_bed", "day", 4.0, 3.0); seg("seasons", "day", 1.0, 3.6)
seg("v11_goals", "goals", 0.3, 3.0)
seg("v11_letter", "letter", 1.0, 4.4)
seg("v11_ai", "ai", 0.3, 2.6, snd=0.0)
seg("altf4", "wall", 2.6, 3.0)
seg("bye", "bye", 2.6, 3.0)
seg(None, "end", 0, 5.0)

CAPTION = {
    "hook": "공포 0%, 미타와 보내는 하루",
    "roster": "미타 7명 중 한 명과 보내는 하루",
    "menu": "새로워진 메뉴 · 마우스 · 숫자 키 · 게임패드",
    "touch": "대화 · 쓰다듬기 · 칭찬 — 몸짓으로 반응해요",
    "hug": "안아 주기",
    "hands": "손 잡기",
    "gift": "선물하기",
    "photo": "사진 모드 · 같이 찍은 사진 앨범",
    "outfit": "NEW · 뱀파이어 · 크리스마스 의상, 옷 색 바꾸기",
    "games": "가위바위보 · 카드 게임 · 댄스 배틀 · 망치 게임",
    "window": "같이 창밖 보기",
    "lap": "무릎베개",
    "walk": "같이 다니기",
    "day": "요리 · 잠자기 · 사계절",
    "goals": "NEW · 미타와 함께 이룰 목표 17가지",
    "letter": "목표를 모두 이루면, 미타의 편지",
    "ai": "NEW · Gemini · ChatGPT로 자유 대화 (선택)",
    "wall": "게임 밖 현실에도 반응하는 미타",
}
NAME = {"kind": "착한 미타", "sleepy": "잠꾸러기 미타", "cap": "캐피", "mila": "밀라", "short": "단발 미타", "chibi": "미니 미타"}


# (a test build before the re-takes exist: TRAILER_FALLBACK=1 stands the old clips in for them)
FALLBACK = {"r12_pat": "pat", "r12_compliment": "pat", "r12_hug": "hug", "r12_hands": "hold", "r12_gift": "gift",
            "r12_vamp_hug": "hug", "r12_xmas_hug": "hug", "r12_window": "window", "r12_lap": "lap", "r12_walk": "propose",
            "r12_sleepy_walk": "propose", "r12_bed": "bed"}


def clip_file(clip, ext):
    p = os.path.join(CLIPS, f"ko_{clip}{ext}")
    if os.environ.get("TRAILER_FALLBACK") and not os.path.exists(os.path.join(CLIPS, f"ko_{clip}.mp4")) and clip in FALLBACK:
        p = os.path.join(CLIPS, f"ko_{FALLBACK[clip]}{ext}")
    return p


def timeline():
    out, f = [], 0
    for s in STORY:
        n = int(round(s["n"] * FPS))
        out.append(dict(s, f0=f, frames=n))
        f += n
    return out, f


def t(sec):
    return int(round(sec * TPS))


def fr(frames):
    return frames * TPS // FPS


def run(lines, save=True, first=False):
    """run filmcraft-cli commands (a list of (id, params)); returns the results in order"""
    os.makedirs(OUT, exist_ok=True)
    script = os.path.join(OUT, "_phase.jsonl")
    with open(script, "w", encoding="utf-8") as f:
        for cid, p in lines:
            f.write(json.dumps({"id": cid, "params": p}, ensure_ascii=False) + "\n")
    args = [CLI, "--compact"]
    args += ["--save-as", PROJ] if first else ["--project", PROJ] + (["--save"] if save else [])
    r = subprocess.run(args + ["run", script], capture_output=True, text=True, encoding="utf-8", errors="replace")
    res = []
    for ln in r.stdout.splitlines():
        ln = ln.strip()
        if not ln.startswith("{"): continue
        j = json.loads(ln)
        if not j.get("ok"):
            raise SystemExit(f"FilmCraft: line {j.get('line')} {j.get('id')} failed: {j}")
        res.append(j.get("result"))
    if r.returncode != 0 or len(res) != len(lines):
        raise SystemExit(f"filmcraft-cli exit {r.returncode}, {len(res)}/{len(lines)} results\n{r.stderr[-2000:]}")
    return res


def wav_rms(path, a, n):
    with wave.open(path) as w:
        rate, ch = w.getframerate(), w.getnchannels()
        w.setpos(min(int(a * rate), w.getnframes()))
        x = np.frombuffer(w.readframes(int(n * rate)), dtype=np.int16).astype(np.float32)
    return float(np.sqrt(np.mean(x ** 2))) if len(x) else 0.0


def build():
    tl, total = timeline()
    secs = total / FPS
    missing = sorted({s["clip"] for s in tl if s["clip"] and not os.path.exists(clip_file(s["clip"], ".mp4"))})
    if missing: raise SystemExit("missing clips: " + ", ".join(missing))
    clips = sorted({s["clip"] for s in tl if s["clip"]})
    media = []
    for c in clips:
        media.append(clip_file(c, ".mp4"))
        if os.path.exists(clip_file(c, ".wav")): media.append(clip_file(c, ".wav"))
    media.append(clip_file("theme", ".wav"))
    if os.path.exists(PROJ): os.remove(PROJ)

    # phase 1: the media, the sequence (V1 picture, V2 dim, V3-V5 text; A1 her sound, A2 the music bed)
    r = run([("file.import", {"paths": media}),
             ("file.newSequence", {"name": "MiSide Peace Mode trailer", "width": W, "height": H, "fps": FPS, "video": 5, "audio": 2}),
             ("file.newColorMatte", {"color": "#1a0a16", "seconds": 10})], first=True)
    ids = dict(zip(media, r[0]["items"]))
    matte = r[2]["item"] if isinstance(r[2], dict) and "item" in r[2] else r[2]
    if isinstance(matte, dict): matte = matte.get("items", [None])[0]

    # phase 2: the cuts; her sound under each; the bed; the dims under the title and end cards
    L = []
    voiced = []
    for s in tl:
        if not s["clip"]:
            continue
        L.append(("timeline.place", {"item": ids[clip_file(s["clip"], ".mp4")], "track": "V1", "time": fr(s["f0"]),
                                     "sourceIn": t(s["a"]), "duration": fr(s["frames"]), "audio": False}))
        wav = clip_file(s["clip"], ".wav")
        if s["snd"] > 0 and wav in ids:
            L.append(("timeline.place", {"item": ids[wav], "track": "A1", "time": fr(s["f0"]),
                                         "sourceIn": t(s["a"]), "duration": fr(s["frames"]), "video": False}))
            voiced.append(wav_rms(wav, s["a"], s["n"]))
    L.append(("timeline.place", {"item": ids[clip_file("theme", ".wav")], "track": "A2", "time": 0, "sourceIn": t(1.0),
                                 "duration": fr(total), "video": False}))
    for s in tl:
        if s["key"] in ("title", "end"):
            L.append(("timeline.place", {"item": matte, "track": "V2", "time": fr(s["f0"]), "duration": fr(s["frames"]), "audio": False}))
    r = run(L)
    placed = [x["clips"][0] for x in r]

    # who is who: the placed clips in the order they were placed
    i = 0
    v1, a1 = [], []
    for s in tl:
        if not s["clip"]: continue
        v1.append((s, placed[i])); i += 1
        if s["snd"] > 0 and clip_file(s["clip"], ".wav") in ids:
            a1.append((s, placed[i])); i += 1
    bed = placed[i]; i += 1
    mattes = placed[i:]

    # phase 3: sound levels and fades, short dissolves into and out of the cards, the dims' opacity
    L = []
    rms_voice = float(np.median([v for v in voiced if v > 50])) if voiced else 2500.0
    bed_rms = wav_rms(clip_file("theme", ".wav"), 1.0, min(secs, 60))
    # (the clips were recorded quiet - her voice around -40 dBFS: each goes through a hard limiter with an input boost,
    # the music bed sits under the boosted voice)
    bed_db = VOICE_BOOST + 20 * np.log10(max(rms_voice * BED_RATIO, 1) / max(bed_rms, 1))
    L.append(("clip.audioGain", {"clips": [bed], "mode": "set", "db": round(float(bed_db), 2)}))
    L.append(("effects.apply", {"clips": [c for _, c in a1] + [bed], "effect": "hard_limiter"}))
    for s, c in a1:
        if s["snd"] != 1.0:
            L.append(("clip.audioGain", {"clips": [c], "mode": "set", "db": round(20 * np.log10(s["snd"]), 2)}))
        L.append(("effects.setParam", {"clip": c, "effect": "hard_limiter", "param": "boost", "value": VOICE_BOOST}))
        L.append(("effects.setParam", {"clip": c, "effect": "hard_limiter", "param": "max", "value": -1.5}))
        L.append(("sequence.applyAudioTransition", {"clip": c, "effect": "constant_power", "frames": 4, "edge": "in"}))
        L.append(("sequence.applyAudioTransition", {"clip": c, "effect": "constant_power", "frames": 4, "edge": "out"}))
    L.append(("effects.setParam", {"clip": bed, "effect": "hard_limiter", "param": "max", "value": -3.0}))
    L.append(("sequence.applyAudioTransition", {"clip": bed, "effect": "constant_power", "frames": 30, "edge": "in"}))
    L.append(("sequence.applyAudioTransition", {"clip": bed, "effect": "exponential_fade", "frames": 180, "edge": "out"}))
    for (s, c), m in zip([x for x in v1 if x[0]["key"] == "title"], mattes[:1]):
        L.append(("effects.setParam", {"clip": m, "effect": "opacity", "param": "opacity", "value": 72}))
        L.append(("sequence.applyVideoTransition", {"clip": m, "effect": "cross_dissolve", "frames": 10, "edge": "in"}))
        L.append(("sequence.applyVideoTransition", {"clip": m, "effect": "cross_dissolve", "frames": 12, "edge": "out"}))
    if len(mattes) > 1:
        L.append(("sequence.applyVideoTransition", {"clip": mattes[1], "effect": "cross_dissolve", "frames": 20, "edge": "in"}))
    L.append(("sequence.applyVideoTransition", {"clip": v1[0][1], "effect": "cross_dissolve", "frames": 12, "edge": "in"}))
    run(L)

    # phase 4: the texts. Captions run over every cut with the same key (one text across a run of cuts)
    texts = []   # (spec for graphics.newText, props for graphics.set, (fade in, fade out) frames)

    def text(s, f0, f1, track, props, fade=(9, 9)):
        texts.append(({"text": s, "size": props.pop("size", 112), "font": FONT, "fontStyle": props.pop("style", "Bold"),
                       "time": fr(f0), "seconds": (f1 - f0) / FPS}, props, fade))

    cap = lambda y=H - 250: {"align": "center", "fill_color": "#ffffff", "stroke": True, "stroke_width": 9,
                             "stroke_color": "#2a1830", "shadow": True, "position": [W // 2, y]}
    i = 0
    while i < len(tl):
        p = tl[i]
        j = i
        if p["key"] not in ("roster", "title", "end"):
            while j + 1 < len(tl) and tl[j + 1]["key"] == p["key"]: j += 1
        f0, f1 = p["f0"], tl[j]["f0"] + tl[j]["frames"]
        key = p["key"]
        if key in ("title", "end"):
            cy = H // 2
            text("MiSide", f0 + 8, f1, 3, dict(cap(cy - 520), size=150, style="Bold", font="Segoe UI"), (15, 15))
            text("평화 모드", f0 + 8, f1, 4, dict(cap(cy - 210), size=330, stroke_width=14, stroke_color="#7a2f8a"), (15, 15))
            text("Peace Mode  v1.1", f0 + 16, f1, 5, dict(cap(cy - 40), size=130), (15, 15))
            text(" 비공식 팬 모드  ·  UNOFFICIAL FAN MOD ", f0 + 26, f1, 3, dict(cap(cy + 170), size=92, fill_color="#281430",
                 stroke=False, shadow=False, background=True, background_color="#ffd646"), (12, 15))
            text("Not AIHASTO's official 'Peaceful Mode' — a fan mod" if CLEAN else "AIHASTO의 공식 '평화 모드'가 아닌 팬 모드입니다", f0 + 40, f1, 4, dict(cap(cy + 360), size=78), (15, 15))
            if key == "end":
                text(f"{'FREE' if CLEAN else '무료'}  ·  {REPO_URL}", f0 + 55, f1, 5, dict(cap(cy + 560), size=104, fill_color="#ffd646"), (20, 20))
                text("MiSide © AIHASTO · fan-made, not affiliated with AIHASTO", f0 + 55, f1, 3,
                     dict(cap(H - 90), size=60, style="Regular", fill_color="#e6dcf0", stroke_width=5), (20, 20))
        elif CLEAN:
            pass
        elif key == "roster":
            text(NAME[p["who"]], f0 + 2, f1 - 2, 3, cap(), (6, 6))
        elif key in CAPTION:
            # (the hug view has the game's own subtitle bar along the bottom: those captions go to the top)
            text(CAPTION[key], f0 + 6, f1 - 4, 3, cap(330) if key in ("hug", "outfit") else cap(), (10, 8))
        i = j + 1
    ro = [p for p in tl if p["key"] == "roster"]
    if ro and not CLEAN:
        text(CAPTION["roster"], ro[0]["f0"], ro[-1]["f0"] + ro[-1]["frames"], 4,
             dict(cap(330), size=80, style="Regular", fill_color="#efe4f7", stroke_width=6), (10, 10))
    # the corner mark from the title card to the end card
    t0 = next(p["f0"] + p["frames"] for p in tl if p["key"] == "title")
    t1 = next(p["f0"] for p in tl if p["key"] == "end")
    text(" 비공식 팬 모드 · Unofficial fan mod ", t0, t1, 5,
         dict(align="left", position=[70, 130], size=56, fill_color="#ffffff", stroke=False, shadow=False,
              background=True, background_color="#301828c0"), (15, 15))

    r = run([("graphics.newText", spec) for spec, _, _ in texts])
    L = []
    for (spec, props, fade), res in zip(texts, r):
        c = res["clip"]
        font = props.pop("font", None)
        if font: props["font"] = font
        L.append(("graphics.set", {"clip": c, "layer": 0, "props": props}))
        fi, fo = fade
        L.append(("sequence.applyVideoTransition", {"clip": c, "effect": "cross_dissolve", "frames": fi, "edge": "in"}))
        L.append(("sequence.applyVideoTransition", {"clip": c, "effect": "cross_dissolve", "frames": fo, "edge": "out"}))
    run(L)
    print(f"built {PROJ}: {len(v1)} cuts, {len(texts)} texts, {secs:.1f} s (music bed {bed_db:+.1f} dB)")


def frames():
    tl, _ = timeline()
    d = os.path.join(OUT, "check")
    os.makedirs(d, exist_ok=True)
    for k, s in enumerate(tl):
        mid = (s["f0"] + s["frames"] * 0.6) / FPS
        out = os.path.join(d, f"{k:02d}_{s['clip'] or s['key']}.png")
        subprocess.run([CLI, "--project", PROJ, "render", "--seconds", f"{mid:.3f}", "--out", out, "--scale", "0.25"],
                       capture_output=True)
    print("stills in", d)


def export(draft=False):
    os.makedirs(os.path.dirname(FINAL), exist_ok=True)
    out = FINAL.replace(".mp4", "_draft.mp4") if draft else FINAL
    cmd = [sys.executable, FCGPU, "export", out, "--project", PROJ]
    if draft: cmd += ["--scale", "0.5"]
    print(" ".join(cmd))
    subprocess.run(cmd, check=True)


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "build"
    if what == "build": build()
    elif what == "frames": frames()
    elif what == "export": export("--draft" in sys.argv)
