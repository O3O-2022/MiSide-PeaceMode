"""The showcase video, put together per language: the rendered clips in story order, trimmed, with their sound evened
out, the captions burned in, and a thumbnail and the YouTube title/description beside it.

usage: python tools/video/assemble.py audio <base>            the base's sound track + timeline (once per base)
       python tools/video/assemble.py video <lang> [...]       the video (needs its base's sound track)
       python tools/video/assemble.py thumb <lang> [...]       the thumbnail
       python tools/video/assemble.py meta                     titles and descriptions of every language
       python tools/video/assemble.py all <lang> [...]         audio (if missing), video, thumbnail
       (lang: all = every language)

Bases are the game rendered in one language (dev_output/video/<base>/<base>_<clip>.mp4 + .wav, see make_scripts.py):
ko, en, ja, zh. Every other language uses the English base (Traditional Chinese the Simplified one), with its own
captions. Output: dev_output/video/final/<lang>.mp4, <lang>.jpg, <lang>.json.

The frames go from the clips' decoders straight into one encoder (no intermediate video: one generation of loss).
"""
import json, os, subprocess, sys, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
VID = os.environ.get("PEACE_VID") or os.path.join(REPO, "dev_output", "video")
BUILD = os.path.join(VID, "build")
FINAL = os.path.join(VID, "final")
FONTS = os.path.join(REPO, "dev_output", "fonts")
sys.path.insert(0, HERE)
from i18n_a import T as _A
from i18n_b import T as _B
from i18n_c import T as _C

T = {**_A, **_B, **_C}
G = json.load(open(os.path.join(REPO, "lang", "glossary.json"), encoding="utf-8"))
LANGS = [k for k in G if not k.startswith("_")]
W, H, FPS, RATE = 1920, 1080, 60, 48000
BASE_OF = {"ko": "ko", "en": "en", "ja": "ja", "zh": "zh", "zht": "zh"}
FONT = {"ko": "Malgun Gothic", "ja": "Yu Gothic", "zh": "Microsoft YaHei", "zht": "Microsoft JhengHei", "th": "Leelawadee UI"}
RTL = {"ar", "arz", "fa"}
FONT_FILES = ["malgun.ttf", "malgunbd.ttf", "YuGothB.ttc", "YuGothM.ttc", "msyh.ttc", "msyhbd.ttc", "msjh.ttc", "msjhbd.ttc",
              "LeelawUI.ttf", "LeelaUIb.ttf", "segoeui.ttf", "segoeuib.ttf", "seguisym.ttf"]


def ffbin(name="ffmpeg"):
    root = os.path.join(REPO, "dev_output", "ffmpeg")
    for d in os.listdir(root):
        p = os.path.join(root, d, "bin", name + ".exe")
        if os.path.exists(p):
            return p
    return name


FF = ffbin()


# ---------------------------------------------------------------- the story
# (clip, caption key, start s, length s (None: to the clip's end), whose name to show)
STORY = []


def seg(clip, key=None, a=0.0, n=None, who=None):
    STORY.append(dict(clip=clip, key=key, a=a, n=n, who=who))


# the start: the menu, the seven of them
seg("menu", "title", 0, 4)
seg("select", "select", 0, 8)
for who in ("crazy", "kind", "sleepy", "cap", "mila", "short", "chibi"):
    seg("greet_" + who, "roster", 0.3, 4.2, who)
# talking and touch
seg("talk", "talk", 0, 8)
seg("ask", "ask", 0, 10.5)
seg("pat", "pat", 0, 7)
seg("hug", "hug", 0, 6.5)          # (after the embrace the view is over her shoulder: the kitchen)
seg("hold", "hold", 0, 7.5)
seg("compliment", "compliment", 0, 7)
seg("glasses", "glasses", 0, 7)
seg("clothes", "clothes", 0, 9)
# games
seg("rps", "rps", 0, 11)
seg("words", "words", 0, 10)
seg("riddle", "riddle", 0, 10)
seg("cham", "cham", 0, 14)
seg("hide", "hide")
seg("photo", "photo", 0, 7)
seg("cards", "cards", 0, 10)
# Cappie's games
seg("dance1", "dance", 1.5, 12.5)
seg("dance2", "dance", 0, 8)
seg("dance3", "dance", 0, 10)
seg("dance4", "dance")
seg("hammer1", "hammer")
seg("hammer2", "hammer")
# going around together, the special times
seg("follow", "follow", 0, 14)
seg("window", "window", 0, 12)
seg("lap", "lap", 0, 18)
seg("song", "song", 0, 12)
seg("heart", "heart", 0, 10)
# the house and her
seg("spot", "spot", 0, 10)
seg("request", "request")
seg("propose", "propose")
seg("diary", "diary")
seg("read", "sofa", 0, 8); seg("rest", "sofa", 0, 7)
seg("alone", "sofa_alone", 0, 7); seg("join", "sofa_alone", 0, 10)
seg("cook1", "cook"); seg("cook2", "cook")
seg("bed", "bed", 0, 12)
seg("rain", "rain"); seg("snow", "rain")
seg("seasons", "seasons")
for clip, who in (("advice", "kind"), ("coffee", "sleepy"), ("guitar", "cap"), ("tea", "mila"), ("lecture", "short"), ("quiz", "chibi")):
    seg(clip, "specials", 1.0, 7.0, who)
# how she takes you
seg("shy", "shy"); seg("nod", "nod")
seg("top", "aff_top")
seg("cold", "cold"); seg("contempt", "cold")
seg("altf4", "altf4")
for c in ("lang_en", "lang_ja", "lang_zh"):
    seg(c, "languages", 0.5, 4.5)
seg("bye", "bye")
seg(None, "end", 0, 4.5)


def base_of(lang):
    return BASE_OF.get(lang, "en")


def clip_file(base, clip, ext):
    p = os.path.join(VID, base, f"{base}_{clip}{ext}")
    if not os.path.exists(p) and clip.startswith("lang_"):   # (only the Korean run switches the mod's language)
        p = os.path.join(VID, "ko", f"ko_{clip}{ext}")
    return p


def clip_seconds(base, clip):
    with wave.open(clip_file(base, clip, ".wav")) as w:
        return w.getnframes() / w.getframerate()


def story_for(base):
    """the story for one base: outside Korean the title is over the character select, the game's own menu not being in
    that language (its language is set after it's drawn, see DevRunner gamelang)"""
    if base == "ko":
        return STORY
    out = []
    for s in STORY:
        if s["clip"] == "menu":
            s = dict(s, clip="select", a=0, n=4)
        elif s["clip"] == "select":
            s = dict(s, a=4, n=4)
        out.append(s)
    return out


def timeline(base):
    """each part's frames: [(part, first frame, frame count)]"""
    out, f = [], 0
    for s in story_for(base):
        n = s["n"]
        if n is None:
            n = clip_seconds(base, s["clip"]) - s["a"]
        frames = int(round(n * FPS))
        out.append((s, f, frames))
        f += frames
    return out


# ---------------------------------------------------------------- sound
def read_wav(path):
    with wave.open(path) as w:
        ch, rate = w.getnchannels(), w.getframerate()
        a = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).reshape(-1, ch).astype(np.float32)
    if ch == 1:
        a = np.repeat(a, 2, axis=1)
    assert rate == RATE, f"{path}: {rate} Hz"
    return a[:, :2]


def build_audio(base):
    d = os.path.join(BUILD, base)
    os.makedirs(d, exist_ok=True)
    parts = []
    tl = timeline(base)
    for i, (s, f0, frames) in enumerate(tl):
        n = frames * RATE // FPS
        if s["clip"] is None:
            parts.append(np.zeros((n, 2), np.float32)); continue
        a = read_wav(clip_file(base, s["clip"], ".wav"))
        start = int(round(s["a"] * RATE))
        x = a[start:start + n]
        if len(x) < n:
            x = np.concatenate([x, np.zeros((n - len(x), 2), np.float32)])
        x = x.copy()
        # (short fades at each cut: the music of one moment doesn't slam into the next; the first and last parts longer)
        fin = int(RATE * (0.6 if i == 0 else 0.12))
        fout = int(RATE * (1.2 if s["key"] == "bye" else 0.2))
        x[:fin] *= np.linspace(0, 1, fin, dtype=np.float32)[:, None]
        x[-fout:] *= np.linspace(1, 0, fout, dtype=np.float32)[:, None]
        parts.append(x)
    raw = os.path.join(d, "audio_raw.wav")
    with wave.open(raw, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(np.clip(np.concatenate(parts), -32768, 32767).astype(np.int16).tobytes())
    # loudness: two passes of EBU R128 (-14 LUFS, as YouTube plays it), linear so the game's own dynamics stay
    r = subprocess.run([FF, "-hide_banner", "-i", raw, "-af", "loudnorm=I=-14:TP=-1.5:LRA=11:print_format=json", "-f", "null", "-"],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    js = r.stderr[r.stderr.rindex("{"):r.stderr.rindex("}") + 1]
    m = json.loads(js)
    af = (f"loudnorm=I=-14:TP=-1.5:LRA=11:measured_I={m['input_i']}:measured_TP={m['input_tp']}:measured_LRA={m['input_lra']}"
          f":measured_thresh={m['input_thresh']}:offset={m['target_offset']}:linear=true,aresample={RATE}")
    total = sum(fr for _, _, fr in tl) / FPS
    subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y", "-i", raw, "-af", af + f",apad,atrim=0:{total:.6f}",
                    "-ar", str(RATE), "-ac", "2", "-c:a", "pcm_s16le", os.path.join(d, "audio.wav")], check=True)
    json.dump([dict(clip=s["clip"], key=s["key"], a=s["a"], who=s["who"], f0=f0, frames=fr) for s, f0, fr in tl],
              open(os.path.join(d, "timeline.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=0)
    print(f"{base}: sound {total:.1f} s, measured {m['input_i']} LUFS -> -14")


# ---------------------------------------------------------------- captions
def ts(sec):
    sec = max(0.0, sec)
    h = int(sec // 3600); m = int(sec % 3600 // 60); s = sec % 60
    return f"{h}:{m:02d}:{s:05.2f}"


def text_of(lang, key):
    g = G[lang]
    t = T[lang][key].format(pm=g["peace_mode"], cap=g["cap"])
    if key == "cook":   # (the video shows her at the stove, not the carrot cutting the caption's second half is about)
        t = t.split(" — ")[0]
    return t


def languages_line():
    # (each language's name in a font that has it)
    return ("{\\fnMalgun Gothic}한국어{\\r} · English · {\\fnYu Gothic}日本語{\\r} · {\\fnMicrosoft YaHei}中文{\\r}")


def ass_header(lang, w=W, h=H):
    f = FONT.get(lang, "Segoe UI")
    k = w / 1920
    return f"""[Script Info]
ScriptType: v4.00+
PlayResX: {w}
PlayResY: {h}
WrapStyle: 0
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Cap,{f},{int(46 * k)},&H00FFFFFF,&H00FFFFFF,&H5A2A1422,&H5A2A1422,1,0,0,0,100,100,0,0,3,{int(16 * k)},0,2,{int(200 * k)},{int(200 * k)},{int(56 * k)},1
Style: Who,{f},{int(44 * k)},&H00C88CFF,&H00FFFFFF,&H00301828,&H80000000,1,0,0,0,100,100,0,0,1,{int(4 * k)},{int(2 * k)},7,{int(64 * k)},{int(64 * k)},{int(48 * k)},1
Style: Title,{f},{int(120 * k)},&H00FFFFFF,&H00FFFFFF,&H00A0409A,&H64000000,1,0,0,0,100,100,0,0,1,{int(7 * k)},{int(4 * k)},5,{int(80 * k)},{int(80 * k)},0,1
Style: Sub,{f},{int(56 * k)},&H00FFFFFF,&H00FFFFFF,&H00301828,&H64000000,1,0,0,0,100,100,0,0,1,{int(4 * k)},{int(2 * k)},5,{int(80 * k)},{int(80 * k)},0,1
Style: Small,{f},{int(34 * k)},&H00E6DCF0,&H00FFFFFF,&H00301828,&H64000000,0,0,0,0,100,100,0,0,1,{int(3 * k)},0,2,{int(80 * k)},{int(80 * k)},{int(40 * k)},1
Style: Box,Arial,10,&H00000000,&H00000000,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,0,0,0,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""


def make_ass(lang):
    base = base_of(lang)
    tl = json.load(open(os.path.join(BUILD, base, "timeline.json"), encoding="utf-8"))
    ev = []

    def add(t0, t1, style, text, layer=1, fade=(250, 250)):
        ev.append(f"Dialogue: {layer},{ts(t0)},{ts(t1)},{style},,0,0,0,,{{\\fad({fade[0]},{fade[1]})}}{text}")

    # consecutive parts with the same caption share one
    i = 0
    while i < len(tl):
        p = tl[i]
        j = i
        while j + 1 < len(tl) and tl[j + 1]["key"] == p["key"]:
            j += 1
        t0 = p["f0"] / FPS
        t1 = (tl[j]["f0"] + tl[j]["frames"]) / FPS
        key = p["key"]
        if key == "title":
            add(t0, t1, "Box", f"{{\\an7\\pos(0,0)\\1c&H1A0A16&\\1a&H70&\\bord0\\shad0\\p1}}m 0 0 l {W} 0 {W} {H} 0 {H}{{\\p0}}", layer=0, fade=(0, 400))
            add(t0 + 0.3, t1, "Title", f"{{\\pos({W // 2},{H // 2 - 50})}}{text_of(lang, 'title')}", fade=(500, 400))
            add(t0 + 0.8, t1, "Sub", f"{{\\pos({W // 2},{H // 2 + 70})}}{text_of(lang, 'subtitle')}", fade=(500, 400))
        elif key == "end":
            add(t0 + 0.2, t1, "Title", f"{{\\pos({W // 2},{H // 2 - 60})}}{text_of(lang, 'title')}", fade=(600, 600))
            add(t0 + 0.6, t1, "Sub", f"{{\\pos({W // 2},{H // 2 + 60})}}{text_of(lang, 'subtitle')}", fade=(600, 600))
            add(t0 + 0.6, t1, "Small", text_of(lang, "fan"), fade=(600, 600))
        else:
            text = languages_line() if key == "languages" else text_of(lang, key)
            add(t0 + 0.25, t1 - 0.1, "Cap", text)
        # whose moment it is
        for k in range(i, j + 1):
            q = tl[k]
            if q.get("who"):
                a = q["f0"] / FPS
                add(a + 0.15, a + q["frames"] / FPS - 0.1, "Who", G[lang][q["who"]], fade=(150, 150))
        i = j + 1
    if lang in RTL:   # (the whole line right to left; libass's bidi does the rest)
        ev = [e.replace(",,0,0,0,,", ",,0,0,0,,\u200f", 1) if ",Cap," in e or ",Who," in e else e for e in ev]
    p = os.path.join(BUILD, f"{lang}.ass")
    open(p, "w", encoding="utf-8-sig").write(ass_header(lang) + "\n".join(ev) + "\n")
    return p


def ensure_fonts():
    os.makedirs(FONTS, exist_ok=True)
    for f in FONT_FILES:
        src = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts", f)
        dst = os.path.join(FONTS, f)
        if os.path.exists(src) and not os.path.exists(dst):
            open(dst, "wb").write(open(src, "rb").read())


def filter_path(p):
    # (ffmpeg filter arguments: forward slashes, the drive colon escaped)
    return p.replace("\\", "/").replace(":", "\\:")


# ---------------------------------------------------------------- video
def render(lang, quality=17):
    ensure_fonts()
    base = base_of(lang)
    d = os.path.join(BUILD, base)
    tl = json.load(open(os.path.join(d, "timeline.json"), encoding="utf-8"))
    ass = make_ass(lang)
    os.makedirs(FINAL, exist_ok=True)
    out = os.path.join(FINAL, f"{lang}.mp4")
    fsize = W * H * 3 // 2
    enc = subprocess.Popen([FF, "-hide_banner", "-loglevel", "error", "-y",
                            "-f", "rawvideo", "-pix_fmt", "yuv420p", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "-",
                            "-i", os.path.join(d, "audio.wav"),
                            "-vf", f"ass='{filter_path(ass)}':fontsdir='{filter_path(FONTS)}',format=nv12",
                            "-c:v", "h264_qsv", "-preset", "slower", "-global_quality", str(quality), "-look_ahead", "0",
                            "-g", "120", "-bf", "2", "-c:a", "aac", "-b:a", "256k", "-ar", str(RATE),
                            "-movflags", "+faststart", "-shortest", out], stdin=subprocess.PIPE)
    black = bytes([16]) * (W * H) + bytes([128]) * (W * H // 2)
    last = black
    total = 0
    for p in tl:
        frames = p["frames"]
        if p["clip"] is None:
            for _ in range(frames):
                enc.stdin.write(black)
            total += frames
            continue
        src = clip_file(base, p["clip"], ".mp4")
        vf = "format=yuv420p"
        if p["key"] == "title":
            vf = "fade=t=in:s=0:n=30," + vf
        if p["key"] == "bye":
            vf = f"fade=t=out:s={max(0, frames - 45)}:n=45," + vf
        dec = subprocess.Popen([FF, "-hide_banner", "-loglevel", "error", "-ss", f"{p['a']:.3f}", "-i", src,
                                "-frames:v", str(frames), "-vf", vf, "-f", "rawvideo", "-pix_fmt", "yuv420p", "-"],
                               stdout=subprocess.PIPE, bufsize=fsize * 4)
        got = 0
        while got < frames:
            buf = dec.stdout.read(fsize)
            if not buf or len(buf) < fsize:
                break
            enc.stdin.write(buf)
            last = buf
            got += 1
        dec.stdout.close(); dec.wait()
        if got < frames:
            print(f"  {p['clip']}: {frames - got} frames short, held the last")
            for _ in range(frames - got):
                enc.stdin.write(last)
        total += frames
    enc.stdin.close()
    enc.wait()
    print(f"{lang}: {out} ({total} frames, {total / FPS:.1f} s, {os.path.getsize(out) / 1e6:.0f} MB)")


# ---------------------------------------------------------------- thumbnail
THUMB = ("select", 6.5)   # (clip, second): her face on the right, the list on the left (covered)


def thumb(lang):
    ensure_fonts()
    base = base_of(lang)
    w, h = 1280, 720
    g = G[lang]
    f = FONT.get(lang, "Segoe UI")
    rtl = lang in RTL
    pm = g["peace_mode"]
    sub = text_of(lang, "subtitle")
    x = w - 60 if rtl else 60
    an = 6 if rtl else 4
    head = ass_header(lang, w, h)
    # (the text kept to the panel's width: margins decide where a long line wraps)
    ml, mr = (w - 700, 60) if rtl else (60, w - 700)
    fs = 100 if len(pm) <= 10 else 84 if len(pm) <= 16 else 70
    ev = [
        f"Dialogue: 1,0:00:00.00,0:00:10.00,Title,,{ml},{mr},0,,{{\\an{an}\\pos({x},{h // 2 - 95})\\fs44\\bord4\\3c&H301828&}}MiSide",
        f"Dialogue: 1,0:00:00.00,0:00:10.00,Title,,{ml},{mr},0,,{{\\an{an}\\pos({x},{h // 2 - 10})\\fs{fs}\\bord6\\3c&HA0409A&}}{pm}",
        f"Dialogue: 1,0:00:00.00,0:00:10.00,Sub,,{ml},{mr},0,,{{\\an{an}\\pos({x},{h // 2 + 90})\\fs38\\q0}}{sub}",
    ]
    if rtl:
        ev = [e.replace(",,{", ",,\u200f{", 1) for e in ev]
    ap = os.path.join(BUILD, f"{lang}_thumb.ass")
    open(ap, "w", encoding="utf-8-sig").write(head + "\n".join(ev) + "\n")
    os.makedirs(FINAL, exist_ok=True)
    out = os.path.join(FINAL, f"{lang}.jpg")
    clip, sec = THUMB
    # (a dark panel over the menu's list, fading out smoothly towards her; mirrored, with her, right to left)
    X = f"({w - 1}-X)" if rtl else "X"
    t = f"clip(({X}-430)/390,0,1)"
    fc = (f"[0:v]scale={w}:{h}:flags=lanczos{',hflip' if rtl else ''},format=rgb24[bg];"
          f"color=c=black:s={w}x{h}:d=1,format=rgba,geq=r=24:g=10:b=30:a='236*(1-{t}*{t}*(3-2*{t}))'[p];"
          f"[bg][p]overlay=format=auto,ass='{filter_path(ap)}':fontsdir='{filter_path(FONTS)}'")
    subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y", "-ss", str(sec), "-i", clip_file(base, clip, ".mp4"),
                    "-filter_complex", fc, "-frames:v", "1", "-q:v", "2", out], check=True)
    print(f"{lang}: {out} ({os.path.getsize(out) // 1024} KB)")


def meta():
    os.makedirs(FINAL, exist_ok=True)
    for lang in LANGS:
        m = dict(lang=lang, title=text_of(lang, "yt_title"), description=text_of(lang, "yt_desc"))
        json.dump(m, open(os.path.join(FINAL, f"{lang}.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"{len(LANGS)} titles and descriptions")


if __name__ == "__main__":
    what = sys.argv[1]
    args = sys.argv[2:]
    if args == ["all"]:
        args = LANGS
    if what == "audio":
        for b in args: build_audio(b)
    elif what == "ass":
        for l in args: print(make_ass(l))
    elif what == "video":
        for l in args: render(l)
    elif what == "thumb":
        for l in args: thumb(l)
    elif what == "meta":
        meta()
    elif what == "all":
        meta()
        for l in args:
            b = base_of(l)
            if not os.path.exists(os.path.join(BUILD, b, "audio.wav")):
                build_audio(b)
            render(l); thumb(l)
