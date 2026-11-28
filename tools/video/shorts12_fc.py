"""v1.1 YouTube Shorts (1080x1920, 60 fps), edited in FilmCraft from the 4K trailer clips (dev_output/trailer/ko).

Each 4K clip is scaled to fill the height (88.9 %) and slid so she is in the middle of the tall frame; a scene whose
point is wider than that (the goals card) is shrunk to fit the width over a blurred, filled copy of itself. Korean
title on top with English under it, a Korean + English caption per scene in the upper-lower third (clear of the
Shorts player's own buttons on the right and its text at the bottom), an end card pointing to the full video. Sound:
her clips through a hard limiter, the menu theme under them (as trailer12_fc.py).

usage: python tools/video/shorts12_fc.py build [1 2 3] | frames [n] | export [n]
       -> dev_output/shorts12/short<n>.fcproj, dev_output/shorts12/short<n>.mp4
"""
import json, os, subprocess, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import trailer12_fc as T

REPO = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(REPO, "dev_output", "shorts12")
W, H, FPS = 1080, 1920, 60
SRC_W, SRC_H = 3840, 2160
FILL = 100.0 * H / SRC_H            # 88.9 %: the clip's height fills the frame
FIT = 100.0 * W / SRC_W * 1.0       # 28.1 %: the whole width fits
FONT = T.FONT
VOICE_BOOST, BED_RATIO = 22.0, 0.45

# shot: (clip, start s, length s, x of what matters in the clip 0..1, caption key, fit?)
SHORTS = {
    1: dict(title=("새 의상 미타와 포옹", "A hug from Vampire & Christmas Mita"),
            shots=[("r12_vamp_hug", 0.0, 4.4, 0.50, "vamp", False),
                   ("r12_xmas_hug", 0.0, 4.4, 0.50, "xmas", False),
                   ("r12_hug", 0.2, 3.6, 0.50, "orig", False)]),
    2: dict(title=("미타와 함께 이룰 목표 17가지", "17 goals to reach with Mita"),
            shots=[("v11_goals", 0.3, 3.6, 0.50, "goals", True),
                   ("v11_letter", 0.4, 4.6, 0.48, "letter", False),
                   ("v11_letter", 7.4, 4.0, 0.48, "letter2", False)]),
    3: dict(title=("공포 0%, 미타와 보내는 하루", "0% horror — a day with Mita"),
            shots=[("r12_hands", 5.9, 1.6, 0.50, "hands", False),
                   ("r12_window", 2.2, 3.0, 0.64, "window", False),
                   ("r12_lap", 12.4, 3.0, 0.50, "lap", False),
                   ("r12_walk", 3.6, 2.3, 0.47, "walk", False),
                   ("r12_bed", 4.0, 3.0, 0.50, "bed", False)]),
}
CAPS = {
    "vamp": ("뱀파이어 의상", "Vampire outfit"), "xmas": ("크리스마스 의상", "Christmas outfit"),
    "orig": ("원래 옷도 물론", "and her own clothes, of course"),
    "goals": ("같이 이룰 목표 17가지", "17 goals to reach together"),
    "letter": ("다 이루면…", "Reach them all…"), "letter2": ("미타가 편지를 써 줘요", "and Mita writes you a letter"),
    "hands": ("손 잡기", "Hold hands"), "window": ("같이 창밖 보기", "Look outside together"),
    "lap": ("무릎베개", "Lap pillow"), "walk": ("같이 다니기", "She walks with you"), "bed": ("잘 자요", "Good night"),
}
END = 2.6   # the end card, seconds


def proj(n): return os.path.join(OUT, f"short{n}.fcproj")


def run(path, lines, first=False):
    os.makedirs(OUT, exist_ok=True)
    script = os.path.join(OUT, "_phase.jsonl")
    with open(script, "w", encoding="utf-8") as f:
        for cid, p in lines:
            f.write(json.dumps({"id": cid, "params": p}, ensure_ascii=False) + "\n")
    args = [T.CLI, "--compact"] + (["--save-as", path] if first else ["--project", path, "--save"])
    r = subprocess.run(args + ["run", script], capture_output=True, text=True, encoding="utf-8", errors="replace")
    res = []
    for ln in r.stdout.splitlines():
        if ln.strip().startswith("{"):
            j = json.loads(ln)
            if not j.get("ok"): raise SystemExit(f"FilmCraft line {j.get('line')} {j.get('id')}: {j}")
            res.append(j.get("result"))
    if r.returncode != 0 or len(res) != len(lines):
        raise SystemExit(f"filmcraft-cli exit {r.returncode}, {len(res)}/{len(lines)}\n{r.stderr[-1500:]}")
    return res


def timeline(n):
    out, f = [], 0
    for clip, a, sec, x, key, fit in SHORTS[n]["shots"]:
        fr = int(round(sec * FPS))
        out.append(dict(clip=clip, a=a, n=sec, x=x, key=key, fit=fit, f0=f, frames=fr))
        f += fr
    return out, f, f + int(END * FPS)


def build(n):
    t, fr = T.t, T.fr
    tl, shots_end, total = timeline(n)
    clips = sorted({s["clip"] for s in tl})
    media = [T.clip_file(c, ".mp4") for c in clips] + [T.clip_file(c, ".wav") for c in clips if os.path.exists(T.clip_file(c, ".wav"))]
    media.append(T.clip_file("theme", ".wav"))
    p = proj(n)
    if os.path.exists(p): os.remove(p)
    r = run(p, [("file.import", {"paths": media}),
                ("file.newSequence", {"name": f"Short {n}", "width": W, "height": H, "fps": FPS, "video": 4, "audio": 2}),
                ("file.newColorMatte", {"color": "#1a0a16", "seconds": END + 1})], first=True)
    ids = dict(zip(media, r[0]["items"]))
    matte = r[2]["item"] if isinstance(r[2], dict) else r[2]

    # the pictures (a fitted scene: a blurred filled copy on V1, the fitted one on V2), her sound, the bed, the end dim
    L, kinds, voiced = [], [], []
    for s in tl:
        v = ids[T.clip_file(s["clip"], ".mp4")]
        L.append(("timeline.place", {"item": v, "track": "V1", "time": fr(s["f0"]), "sourceIn": t(s["a"]), "duration": fr(s["frames"]), "audio": False}))
        kinds.append(("v1", s))
        if s["fit"]:
            L.append(("timeline.place", {"item": v, "track": "V2", "time": fr(s["f0"]), "sourceIn": t(s["a"]), "duration": fr(s["frames"]), "audio": False}))
            kinds.append(("fit", s))
        wav = T.clip_file(s["clip"], ".wav")
        if wav in ids:
            L.append(("timeline.place", {"item": ids[wav], "track": "A1", "time": fr(s["f0"]), "sourceIn": t(s["a"]), "duration": fr(s["frames"]), "video": False}))
            kinds.append(("a1", s)); voiced.append(T.wav_rms(wav, s["a"], s["n"]))
    L.append(("timeline.place", {"item": ids[T.clip_file("theme", ".wav")], "track": "A2", "time": 0, "sourceIn": t(1.0), "duration": fr(total), "video": False}))
    kinds.append(("bed", None))
    L.append(("timeline.place", {"item": matte, "track": "V1", "time": fr(shots_end), "duration": fr(total - shots_end), "audio": False}))
    kinds.append(("end", None))
    placed = [x["clips"][0] for x in run(p, L)]

    # framing, blur, sound
    L = []
    a1 = []
    rms_voice = float(np.median([v for v in voiced if v > 50])) if voiced else 2500.0
    bed_rms = T.wav_rms(T.clip_file("theme", ".wav"), 1.0, 30)
    for (kind, s), c in zip(kinds, placed):
        if kind == "v1":
            L.append(("effects.setParam", {"clip": c, "effect": "motion", "param": "scale", "value": round(FILL, 3)}))
            L.append(("effects.setParam", {"clip": c, "effect": "motion", "param": "position", "value": [round(W / 2 + (0.5 - s["x"]) * SRC_W * FILL / 100, 1), H / 2]}))
            if s["fit"]:
                L.append(("effects.apply", {"clips": [c], "effect": "gaussian_blur"}))
        elif kind == "fit":
            L.append(("effects.setParam", {"clip": c, "effect": "motion", "param": "scale", "value": round(FIT * 1.0, 3)}))
            L.append(("effects.setParam", {"clip": c, "effect": "motion", "param": "position", "value": [W / 2, H / 2]}))
        elif kind == "a1":
            a1.append(c)
        elif kind == "bed":
            bed = c
    fitted_v1 = [c for (k, s), c in zip(kinds, placed) if k == "v1" and s["fit"]]
    for c in fitted_v1:
        L.append(("effects.setParam", {"clip": c, "effect": "gaussian_blur", "param": "blurriness", "value": 120}))
        L.append(("effects.setParam", {"clip": c, "effect": "gaussian_blur", "param": "repeat_edge", "value": True}))
    bed_db = VOICE_BOOST + 20 * np.log10(max(rms_voice * BED_RATIO, 1) / max(bed_rms, 1))
    L.append(("clip.audioGain", {"clips": [bed], "mode": "set", "db": round(float(bed_db), 2)}))
    L.append(("effects.apply", {"clips": a1 + [bed], "effect": "hard_limiter"}))
    for c in a1:
        L.append(("effects.setParam", {"clip": c, "effect": "hard_limiter", "param": "boost", "value": VOICE_BOOST}))
        L.append(("effects.setParam", {"clip": c, "effect": "hard_limiter", "param": "max", "value": -1.5}))
        L.append(("sequence.applyAudioTransition", {"clip": c, "effect": "constant_power", "frames": 4, "edge": "in"}))
        L.append(("sequence.applyAudioTransition", {"clip": c, "effect": "constant_power", "frames": 4, "edge": "out"}))
    L.append(("effects.setParam", {"clip": bed, "effect": "hard_limiter", "param": "max", "value": -3.0}))
    L.append(("sequence.applyAudioTransition", {"clip": bed, "effect": "constant_power", "frames": 20, "edge": "in"}))
    L.append(("sequence.applyAudioTransition", {"clip": bed, "effect": "exponential_fade", "frames": 90, "edge": "out"}))
    L.append(("sequence.applyVideoTransition", {"clip": placed[kinds.index(("end", None))], "effect": "cross_dissolve", "frames": 12, "edge": "in"}))
    run(p, L)

    # the words: title on top for the whole short, captions per scene, the end card
    texts = []
    cap = lambda y, size, color="#ffffff", sw=8: {"align": "center", "fill_color": color, "stroke": True, "stroke_width": sw,
                                                  "stroke_color": "#2a1830", "shadow": True, "position": [W // 2 - 20, y], "size": size}

    def text(s, f0, f1, props, fade=(8, 8)):
        texts.append(({"text": s, "size": props["size"], "font": FONT, "fontStyle": props.pop("style", "Bold"),
                       "time": fr(f0), "seconds": (f1 - f0) / FPS}, props, fade))

    ko, en = SHORTS[n]["title"]
    text(ko, 0, shots_end, cap(250, 76 if len(ko) <= 12 else 60), (10, 8))
    text(en, 6, shots_end, cap(325, 44, "#ffe9f6", 6), (10, 8))
    for s in tl:
        k, e = CAPS[s["key"]]
        text(k, s["f0"] + 5, s["f0"] + s["frames"] - 3, cap(1320, 84), (8, 6))
        text(e, s["f0"] + 8, s["f0"] + s["frames"] - 3, cap(1405, 46, "#ffe9f6", 6), (8, 6))
    e0 = shots_end + 6
    text("MiSide 평화 모드 v1.1", e0, total, cap(760, 80), (10, 10))
    text("Peaceful Mode · unofficial fan mod", e0 + 4, total, cap(840, 44, "#ffe9f6", 6), (10, 10))
    text("▶ 전체 영상 · Full video", e0 + 10, total, cap(1010, 64, "#ffd646"), (10, 10))
    text("무료 · Free — GitHub", e0 + 14, total, cap(1100, 52), (10, 10))
    r = run(p, [("graphics.newText", spec) for spec, _, _ in texts])
    L = []
    for (spec, props, (fi, fo)), res in zip(texts, r):
        c = res["clip"]
        L.append(("graphics.set", {"clip": c, "layer": 0, "props": props}))
        L.append(("sequence.applyVideoTransition", {"clip": c, "effect": "cross_dissolve", "frames": fi, "edge": "in"}))
        L.append(("sequence.applyVideoTransition", {"clip": c, "effect": "cross_dissolve", "frames": fo, "edge": "out"}))
    run(p, L)
    print(f"short {n}: {len(tl)} shots, {total / FPS:.1f} s -> {p}")


def frames(n):
    tl, shots_end, total = timeline(n)
    d = os.path.join(OUT, f"check{n}")
    os.makedirs(d, exist_ok=True)
    times = [(s["f0"] + s["frames"] * 0.55) / FPS for s in tl] + [(shots_end + total) / 2 / FPS]
    for k, sec in enumerate(times):
        subprocess.run([T.CLI, "--project", proj(n), "render", "--seconds", f"{sec:.3f}", "--out", os.path.join(d, f"{k:02d}.png"), "--scale", "0.5"], capture_output=True)
    from PIL import Image
    ims = [Image.open(os.path.join(d, f"{k:02d}.png")) for k in range(len(times))]
    sheet = Image.new("RGB", (540 * len(ims), 960))
    for k, im in enumerate(ims): sheet.paste(im.resize((540, 960)), (540 * k, 0))
    sheet.save(os.path.join(OUT, f"sheet{n}.jpg"), quality=85)
    print(os.path.join(OUT, f"sheet{n}.jpg"))


def export(n):
    out = os.path.join(OUT, f"short{n}.mp4")
    subprocess.run([sys.executable, T.FCGPU, "export", out, "--project", proj(n)], check=True)


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "build"
    ns = [int(a) for a in sys.argv[2:]] or [1, 2, 3]
    for n in ns:
        {"build": build, "frames": frames, "export": export}[what](n)
