"""The trailer: ONE video (the game played in Korean), fast cuts of the rendered clips over one music bed, with
nothing language-bound burned in but the title, an "unofficial fan mod" badge and a notice at the start to turn the
subtitles on. What each cut shows is told by YouTube subtitles (one .srt per language, 33 languages) and by the
chapters in the description.

usage: python tools/video/trailer.py audio        the sound track
       python tools/video/trailer.py video        the video            -> dev_output/trailer/final/MiSide_PeaceMode_trailer.mp4
       python tools/video/trailer.py subs         the subtitle files   -> final/subs/trailer.<youtube language>.srt
       python tools/video/trailer.py meta         titles and descriptions (with chapters) -> final/meta/<lang>.json, final/description.txt
       python tools/video/trailer.py thumb        the thumbnail        -> final/thumbnail.jpg
       python tools/video/trailer.py sheet        a contact sheet of the cuts (to check them)
       python tools/video/trailer.py all

The clips come from make_trailer.py / run_trailer.sh (dev_output/trailer/ko/ko_<clip>.mp4 + .wav). The music bed is the
main menu's theme, recorded in the same run (ko_bed); the clips were recorded with the house music off, so their own
sound (her voice, the games) sits on top of it.
"""
import json, os, subprocess, sys, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
os.environ.setdefault("PEACE_VID", os.path.join(REPO, "dev_output", "trailer"))
import assemble as A                      # (fonts, ffmpeg, the showcase video's texts in 34 languages)
from i18n_trailer import TR, DOWNLOAD, REPO_URL

VID = os.environ["PEACE_VID"]
BUILD = os.path.join(VID, "build")
FINAL = os.path.join(VID, "final")
G, FF = A.G, A.FF
BASE = "ko"
W, H, FPS, RATE = 3840, 2160, 60, 48000
K = W / 1920
BED_GAIN, CLIP_GAIN = 0.9, 1.0
OUT_NAME = "MiSide_PeaceMode_trailer"

# our language codes -> YouTube's subtitle / translation languages (pre-revolutionary Russian has none there)
YT = {"ko": "ko", "en": "en", "ja": "ja", "zh": "zh-Hans", "zht": "zh-Hant", "ru": "ru", "uk": "uk", "be": "be", "bg": "bg",
      "kk": "kk", "sr": "sr-Latn", "hr": "hr", "cs": "cs", "sk": "sk", "pl": "pl", "hu": "hu", "ro": "ro", "de": "de", "fr": "fr",
      "it": "it", "es": "es-ES", "esla": "es-419", "pt": "pt-PT", "ptbr": "pt-BR", "sv": "sv", "tr": "tr", "id": "id",
      "fil": "fil", "vi": "vi", "th": "th", "ar": "ar", "arz": "ar-EG", "fa": "fa"}
LANGS = [l for l in A.LANGS if l in YT]

# ---------------------------------------------------------------- the cut
# (clip, caption key, start s, length s, whose name goes with it, the clip's own sound: 1 full .. 0 none, chapter)
STORY = []
CHAPTER = [None]


def chapter(key):
    CHAPTER[0] = key


def seg(clip, key=None, a=0.0, n=2.5, who=None, snd=1.0):
    STORY.append(dict(clip=clip, key=key, a=a, n=n, who=who, snd=snd, chapter=CHAPTER[0]))
    CHAPTER[0] = None


# the hook, the title, the seven of them
chapter("intro")
seg("greet_crazy", "hook", 0.3, 3.4)
seg("select", "title", 0.3, 4.4, snd=0.0)
for who in ("crazy", "kind", "sleepy", "cap", "mila", "short", "chibi"):
    seg("greet_" + who, "roster", 0.6, 1.5, who)
# talking, touch, a gift
chapter("talk")
seg("choices", "talk", 0.3, 1.8)
seg("talk", "talk", 3.4, 2.5)
seg("pat", "pat", 0.3, 2.6)
seg("hug", "hug", 0.2, 2.8)
seg("hold", "hold", 1.0, 2.8)
seg("gift", "gift", 2.6, 4.2)
# games
chapter("games")
seg("rps", "rps", 3.6, 2.9)
seg("cham", "cham", 3.2, 2.6)
seg("photo", "photo", 3.0, 2.6)
seg("cards", "cards", 2.0, 2.6)
seg("dance1", "dance", 9.5, 2.6, snd=0.0)
seg("dance3", "dance", 3.0, 2.8, snd=0.0)
seg("hammer", "hammer", 4.0, 3.4)
# the special times
chapter("special")
seg("window", "window", 2.2, 3.4)
seg("music", "music", 2.2, 3.4)
seg("lap", "lap", 11.5, 3.4)
# the day, the weather
chapter("day")
seg("cook", "cook", 1.0, 2.6)
seg("propose", "sofa", 11.5, 3.2, snd=0.4)
seg("bed", "bed", 7.0, 3.2)
seg("rain", "rain", 0.5, 2.0); seg("snow", "rain", 0.5, 2.0)
seg("seasons", "seasons", 1.0, 5.6)
# how she takes you; the world outside the game
chapter("heart")
seg("top", "aff_top", 1.2, 3.2)
seg("cold", "cold", 0.2, 2.4); seg("contempt", "cold", 0.6, 1.6)
seg("altf4", "altf4", 2.6, 3.4)
seg("battery", "battery", 0.4, 3.2)
seg("story", "story", 0.2, 3.2, "kind")
chapter("end")
for c in ("lang_en", "lang_ja", "lang_zh"):
    seg(c, "languages", 1.0, 1.9)
seg("bye", "bye", 2.6, 3.4)
seg(None, "end", 0, 5.0)

# the chapters of the description (YouTube wants each at least ten seconds long, the first at 0:00): Korean / English
CHAPTERS = {
    "intro": "MiSide 평화 모드 (비공식 팬 모드) · Unofficial Peaceful Mode fan mod",
    "talk": "대화 · 교감 · 선물 · Talk, touch, gifts",
    "games": "같이 놀자 · Games",
    "special": "특별한 시간 · Special time",
    "day": "하루 일과 · 날씨와 계절 · Daily life, weather and seasons",
    "heart": "호감도 · 현실 연동 · Affection and reactions to the real world",
    "end": "언어 · 작별 · Languages, goodbye",
}


def clip_file(clip, ext):
    return os.path.join(VID, BASE, f"{BASE}_{clip}{ext}")


def timeline():
    out, f = [], 0
    for s in STORY:
        frames = int(round(s["n"] * FPS))
        out.append((s, f, frames))
        f += frames
    return out


def text_of(lang, key):
    g = G[lang]
    if key == "hook": key = "subtitle"
    src = TR[lang] if key in TR[lang] else A.T[lang]
    t = src[key].format(pm=g["peace_mode"], cap=g["cap"], kind=g["kind"])
    if key == "cook": t = t.split(" — ")[0]   # (the cut shows her at the stove, not the carrot cutting the second half is about)
    return t


def yt_title(lang):
    g = G[lang]
    if "tr_title" in TR[lang]:
        return TR[lang]["tr_title"].format(pm=g["peace_mode"], cap=g["cap"], kind=g["kind"])
    t = f"MiSide {g['peace_mode']} — {TR[lang]['unofficial']} | {A.T[lang]['subtitle']}"
    return t if len(t) <= 100 else f"MiSide {g['peace_mode']} — {TR[lang]['unofficial']}"   # (YouTube's limit)


def chapter_lines():
    out = []
    for s, f0, frames in timeline():
        if s["chapter"]:
            sec = int(f0 / FPS)   # (rounded down: a chapter never starts after its first cut)
            out.append(f"{sec // 60}:{sec % 60:02d} {CHAPTERS[s['chapter']]}")
    return out


def yt_desc(lang, chapters=False):
    parts = A.text_of(lang, "yt_desc").split("\n\n")
    extra = "\n".join("• " + text_of(lang, k) for k in ("gift", "music", "battery", "story"))
    foot = parts[2].replace("1080p", "4K")
    body = [DOWNLOAD[lang] + "\n" + REPO_URL, text_of(lang, "notofficial") + ".", parts[0], parts[1] + "\n" + extra]
    if chapters: body.append("\n".join(chapter_lines()))
    return "\n\n".join(body + [foot, "#MiSide #Mita #mod"])


# ---------------------------------------------------------------- sound
def read_wav(path):
    with wave.open(path) as w:
        ch, rate = w.getnchannels(), w.getframerate()
        a = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).reshape(-1, ch).astype(np.float32)
    if ch == 1: a = np.repeat(a, 2, axis=1)
    assert rate == RATE, f"{path}: {rate} Hz"
    return a[:, :2]


def build_audio():
    d = os.path.join(BUILD, BASE)
    os.makedirs(d, exist_ok=True)
    tl = timeline()
    total = sum(fr for _, _, fr in tl)
    n_all = total * RATE // FPS
    mix = np.zeros((n_all, 2), np.float32)
    # the clips' own sound, a short fade at each cut
    for s, f0, frames in tl:
        if s["clip"] is None or s["snd"] <= 0: continue
        n = frames * RATE // FPS
        a = read_wav(clip_file(s["clip"], ".wav"))
        start = int(round(s["a"] * RATE))
        x = a[start:start + n].copy()
        if len(x) < n: x = np.concatenate([x, np.zeros((n - len(x), 2), np.float32)])
        fade = int(RATE * 0.08)
        x[:fade] *= np.linspace(0, 1, fade, dtype=np.float32)[:, None]
        x[-fade:] *= np.linspace(1, 0, fade, dtype=np.float32)[:, None]
        p0 = f0 * RATE // FPS
        mix[p0:p0 + n] += x * (CLIP_GAIN * s["snd"])
    # the music bed under everything: in at once, out over the end card
    bed = read_wav(clip_file("bed", ".wav"))
    lead = int(RATE * 1.0)   # (past the recording's first moment)
    bed = bed[lead:lead + n_all]
    if len(bed) < n_all: bed = np.concatenate([bed, np.zeros((n_all - len(bed), 2), np.float32)])
    bed = bed.copy()
    # (levels can't be set by ear here: the bed's loudness is matched to the clips' by their RMS)
    rms = lambda z: float(np.sqrt(np.mean(z ** 2)) + 1e-6)
    voiced = mix[np.abs(mix).max(axis=1) > 300]
    target = rms(voiced) * BED_GAIN if len(voiced) > RATE else 2500.0
    gain = target / rms(bed)
    bed *= gain
    fin, fout = int(RATE * 0.5), int(RATE * 3.5)
    bed[:fin] *= np.linspace(0, 1, fin, dtype=np.float32)[:, None]
    bed[-fout:] *= np.linspace(1, 0, fout, dtype=np.float32)[:, None]
    mix += bed
    raw = os.path.join(d, "audio_raw.wav")
    with wave.open(raw, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(np.clip(mix, -32768, 32767).astype(np.int16).tobytes())
    r = subprocess.run([FF, "-hide_banner", "-i", raw, "-af", "loudnorm=I=-14:TP=-1.5:LRA=11:print_format=json", "-f", "null", "-"],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    m = json.loads(r.stderr[r.stderr.rindex("{"):r.stderr.rindex("}") + 1])
    af = (f"loudnorm=I=-14:TP=-1.5:LRA=11:measured_I={m['input_i']}:measured_TP={m['input_tp']}:measured_LRA={m['input_lra']}"
          f":measured_thresh={m['input_thresh']}:offset={m['target_offset']}:linear=true,aresample={RATE}")
    secs = total / FPS
    subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y", "-i", raw, "-af", af + f",apad,atrim=0:{secs:.6f}",
                    "-ar", str(RATE), "-ac", "2", "-c:a", "pcm_s16le", os.path.join(d, "audio.wav")], check=True)
    print(f"sound {secs:.1f} s, measured {m['input_i']} LUFS -> -14 (music bed x{gain:.2f}, clips' own sound rms {rms(voiced) if len(voiced) else 0:.0f})")


# ---------------------------------------------------------------- what is burned into the picture
FN = {"ko": "Malgun Gothic", "ja": "Yu Gothic", "zh": "Microsoft YaHei", "lat": "Segoe UI"}
# (each language in a font that has it)
NOTICE = [
    ("ko", "자막(CC)을 켜면 설명이 나옵니다"), ("lat", "Turn on subtitles (CC) for your language"),
    ("ja", "字幕(CC)をオンにしてください"), ("zh", "请打开字幕(CC)"),
    ("lat", "Включите субтитры · Activa los subtítulos · Ative as legendas"),
]


def fn(script, text):
    return "{\\fn" + FN[script] + "}" + text + "{\\r}"


def ass_header():
    k = K
    f = FN["ko"]
    return f"""[Script Info]
ScriptType: v4.00+
PlayResX: {W}
PlayResY: {H}
WrapStyle: 2
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Title,{f},{int(150 * k)},&H00FFFFFF,&H00FFFFFF,&H00A0409A,&H64000000,1,0,0,0,100,100,0,0,1,{int(8 * k)},{int(4 * k)},5,{int(80 * k)},{int(80 * k)},0,1
Style: Title2,{FN['lat']},{int(64 * k)},&H00FFFFFF,&H00FFFFFF,&H00A0409A,&H64000000,1,0,0,0,100,100,0,0,1,{int(5 * k)},{int(3 * k)},5,{int(80 * k)},{int(80 * k)},0,1
Style: Sub,{f},{int(40 * k)},&H00FFFFFF,&H00FFFFFF,&H00301828,&H64000000,1,0,0,0,100,100,0,0,1,{int(4 * k)},{int(2 * k)},5,{int(120 * k)},{int(120 * k)},0,1
Style: Badge,{f},{int(50 * k)},&H00281430,&H00FFFFFF,&H0046D6FF,&H0046D6FF,1,0,0,0,100,100,0,0,3,{int(14 * k)},0,5,{int(80 * k)},{int(80 * k)},0,1
Style: Mark,{f},{int(26 * k)},&H00FFFFFF,&H00FFFFFF,&H96301828,&H96301828,1,0,0,0,100,100,0,0,3,{int(8 * k)},0,7,{int(36 * k)},{int(36 * k)},{int(30 * k)},1
Style: Note,{f},{int(34 * k)},&H00FFFFFF,&H00FFFFFF,&HA0201020,&HA0201020,1,0,0,0,100,100,0,0,3,{int(12 * k)},0,7,{int(50 * k)},{int(50 * k)},{int(110 * k)},1
Style: Small,{f},{int(32 * k)},&H00E6DCF0,&H00FFFFFF,&H00301828,&H64000000,0,0,0,0,100,100,0,0,1,{int(3 * k)},0,2,{int(80 * k)},{int(80 * k)},{int(70 * k)},1
Style: Box,Arial,10,&H00000000,&H00000000,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,0,0,0,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""


def make_ass():
    tl = [dict(s, f0=f0, frames=fr) for s, f0, fr in timeline()]
    ev = []

    def add(t0, t1, style, text, layer=1, fade=(180, 180)):
        ev.append(f"Dialogue: {layer},{A.ts(t0)},{A.ts(t1)},{style},,0,0,0,,{{\\fad({fade[0]},{fade[1]})}}{text}")

    cx, cy = W // 2, H // 2
    pm_ko, pm_en = G["ko"]["peace_mode"], G["en"]["peace_mode"]
    badge = f" {TR['ko']['unofficial']}  ·  {fn('lat', TR['en']['unofficial'].upper())} "
    for p in tl:
        t0 = p["f0"] / FPS
        t1 = t0 + p["frames"] / FPS
        if p["key"] in ("title", "end"):
            # a dark veil over the picture, the mod's name, the yellow badge, the line that says what it is not
            add(t0, t1, "Box", f"{{\\an7\\pos(0,0)\\1c&H1A0A16&\\1a&H{'48' if p['key'] == 'title' else '00'}&\\bord0\\shad0\\p1}}m 0 0 l {W} 0 {W} {H} 0 {H}{{\\p0}}", layer=0, fade=(150, 250))
            add(t0 + 0.15, t1, "Title2", f"{{\\pos({cx},{cy - int(300 * K)})}}MiSide", fade=(300, 300))
            add(t0 + 0.15, t1, "Title", f"{{\\pos({cx},{cy - int(170 * K)})}}{pm_ko}", fade=(300, 300))
            add(t0 + 0.3, t1, "Title2", f"{{\\pos({cx},{cy - int(40 * K)})}}{pm_en}", fade=(300, 300))
            add(t0 + 0.5, t1, "Badge", f"{{\\pos({cx},{cy + int(90 * K)})}}{badge}", fade=(250, 300))
            add(t0 + 0.8, t1, "Sub", f"{{\\pos({cx},{cy + int(200 * K)})}}{text_of('ko', 'notofficial')}", fade=(300, 300))
            add(t0 + 0.8, t1, "Sub", f"{{\\pos({cx},{cy + int(262 * K)})}}{fn('lat', text_of('en', 'notofficial'))}", fade=(300, 300))
            if p["key"] == "end":
                add(t0 + 1.0, t1, "Small", fn("lat", "MiSide © AIHASTO · fan-made, not affiliated with AIHASTO"), fade=(400, 400))
    # the notice at the very start: the subtitles tell what each cut is, in the viewer's language
    hook_end = (tl[1]["f0"] + tl[1]["frames"]) / FPS
    lines = "\\N".join(" " + fn(s, t) + " " for s, t in NOTICE)
    add(0.3, hook_end - 0.2, "Note", "{\\an7}" + lines, layer=3, fade=(350, 350))
    # the corner mark, the whole video long (the title and end cards say it in full)
    first = tl[2]["f0"] / FPS
    last = tl[-1]["f0"] / FPS
    add(first, last, "Mark", f" {TR['ko']['unofficial']} · {fn('lat', TR['en']['unofficial'])} ", layer=2, fade=(300, 300))
    p = os.path.join(BUILD, "trailer.ass")
    os.makedirs(BUILD, exist_ok=True)
    open(p, "w", encoding="utf-8-sig").write(ass_header() + "\n".join(ev) + "\n")
    return p


# ---------------------------------------------------------------- subtitles
def srt_time(sec):
    ms = int(round(max(0.0, sec) * 1000))
    return f"{ms // 3600000:02d}:{ms % 3600000 // 60000:02d}:{ms % 60000 // 1000:02d},{ms % 1000:03d}"


def make_srt(lang):
    """one cue a caption (cuts with the same caption share it); the seven Mitas' names with theirs"""
    tl = [dict(s, f0=f0, frames=fr) for s, f0, fr in timeline()]
    cues = []
    i = 0
    while i < len(tl):
        p = tl[i]
        j = i
        if p["key"] != "roster":
            while j + 1 < len(tl) and tl[j + 1]["key"] == p["key"]: j += 1
        t0 = p["f0"] / FPS
        t1 = (tl[j]["f0"] + tl[j]["frames"]) / FPS
        key = p["key"]
        if key in ("title", "end"):
            text = f"{A.text_of(lang, 'title')} — {text_of(lang, 'unofficial')}\n{text_of(lang, 'notofficial')}"
        elif key == "languages":
            text = "한국어 · English · 日本語 · 中文"
        elif key == "roster":
            text = f"{text_of(lang, 'roster')}\n{G[lang][p['who']]}"
        else:
            text = text_of(lang, key)
        cues.append((t0 + 0.05, t1 - 0.05, text))
        i = j + 1
    d = os.path.join(FINAL, "subs")
    os.makedirs(d, exist_ok=True)
    p = os.path.join(d, f"trailer.{YT[lang]}.srt")
    with open(p, "w", encoding="utf-8", newline="\n") as f:
        for n, (a, b, text) in enumerate(cues, 1):
            f.write(f"{n}\n{srt_time(a)} --> {srt_time(b)}\n{text}\n\n")
    return p


def subs():
    for l in LANGS: make_srt(l)
    print(f"{len(LANGS)} subtitle files in {os.path.join(FINAL, 'subs')}")


# ---------------------------------------------------------------- video
def render():
    A.ensure_fonts()
    d = os.path.join(BUILD, BASE)
    tl = timeline()
    ass = make_ass()
    os.makedirs(FINAL, exist_ok=True)
    out = os.path.join(FINAL, OUT_NAME + ".mp4")
    fsize = W * H * 3 // 2
    # (software x264 at a high quality: YouTube re-encodes what it gets)
    enc = subprocess.Popen([FF, "-hide_banner", "-loglevel", "error", "-y",
                            "-f", "rawvideo", "-pix_fmt", "yuv420p", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "-",
                            "-i", os.path.join(d, "audio.wav"),
                            "-vf", f"ass='{A.filter_path(ass)}':fontsdir='{A.filter_path(A.FONTS)}',format=yuv420p",
                            "-c:v", "libx264", "-preset", "medium", "-crf", "15", "-profile:v", "high", "-level", "5.2",
                            "-g", "120", "-bf", "2", "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709",
                            "-c:a", "aac", "-b:a", "320k", "-ar", str(RATE), "-movflags", "+faststart", "-shortest", out], stdin=subprocess.PIPE)
    black = bytes([16]) * (W * H) + bytes([128]) * (W * H // 2)
    last = black
    total = 0
    for k, (p, f0, frames) in enumerate(tl):
        if p["clip"] is None:
            for _ in range(frames): enc.stdin.write(black)
            total += frames
            continue
        vf = f"scale={W}:{H}:flags=lanczos,format=yuv420p"
        if k == 0: vf = "fade=t=in:s=0:n=20," + vf
        if p["key"] == "bye": vf = f"fade=t=out:s={max(0, frames - 40)}:n=40," + vf
        dec = subprocess.Popen([FF, "-hide_banner", "-loglevel", "error", "-ss", f"{p['a']:.3f}", "-i", clip_file(p["clip"], ".mp4"),
                                "-frames:v", str(frames), "-vf", vf, "-f", "rawvideo", "-pix_fmt", "yuv420p", "-"],
                               stdout=subprocess.PIPE, bufsize=fsize * 2)
        got = 0
        while got < frames:
            buf = dec.stdout.read(fsize)
            if not buf or len(buf) < fsize: break
            enc.stdin.write(buf); last = buf; got += 1
        dec.stdout.close(); dec.wait()
        if got < frames:
            print(f"  {p['clip']}: {frames - got} frames short, held the last")
            for _ in range(frames - got): enc.stdin.write(last)
        total += frames
    enc.stdin.close(); enc.wait()
    print(f"{out} ({total} frames, {total / FPS:.1f} s, {os.path.getsize(out) / 1e6:.0f} MB)")


def sheet():
    """a contact sheet: two frames of each cut with its clip name and time (to check the cuts without watching)"""
    from PIL import Image, ImageDraw, ImageFont
    cells = []
    tmp = os.path.join(BUILD, "sheet"); os.makedirs(tmp, exist_ok=True)
    for k, (p, f0, frames) in enumerate(timeline()):
        if p["clip"] is None: continue
        for part, frac in (("a", 0.2), ("b", 0.8)):
            out = os.path.join(tmp, f"{k:02d}{part}.jpg")
            if os.path.exists(out): os.remove(out)
            subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y", "-ss", f"{p['a'] + p['n'] * frac:.3f}", "-i", clip_file(p["clip"], ".mp4"),
                            "-frames:v", "1", "-vf", "scale=640:360:flags=lanczos", "-q:v", "4", out])
            cells.append((out if os.path.exists(out) else None, f"{p['clip']} {p['a'] + p['n'] * frac:.1f}s  @{f0 / FPS:.0f}s"))
    cols = 6
    rows = (len(cells) + cols - 1) // cols
    img = Image.new("RGB", (cols * 640, rows * 360))
    font = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 24)
    for i, (f, label) in enumerate(cells):
        im = Image.open(f).convert("RGB") if f else Image.new("RGB", (640, 360), (90, 0, 0))
        ImageDraw.Draw(im).text((8, 6), label + ("" if f else "  MISSING"), font=font, fill="yellow", stroke_width=2, stroke_fill="black")
        img.paste(im, ((i % cols) * 640, (i // cols) * 360))
    out = os.path.join(BUILD, "sheet.jpg")
    img.save(out, quality=80)
    print(out, f"{len(cells)} cells, {sum(1 for c in cells if not c[0])} missing")


# ---------------------------------------------------------------- thumbnail
THUMB = ("select", 3.0)   # (clip, second): the select page: her face fills the right half


def thumb():
    A.ensure_fonts()
    w, h = 1280, 720
    f = FN["ko"]
    head = f"""[Script Info]
ScriptType: v4.00+
PlayResX: {w}
PlayResY: {h}
WrapStyle: 2
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: T,{f},150,&H00FFFFFF,&H00FFFFFF,&H00A0409A,&H96000000,1,0,0,0,100,100,0,0,1,9,5,4,50,0,0,1
Style: M,{FN['lat']},64,&H00FFFFFF,&H00FFFFFF,&H00301828,&H96000000,1,0,0,0,100,100,0,0,1,5,3,4,50,0,0,1
Style: B,{f},54,&H00281430,&H00FFFFFF,&H0046D6FF,&H0046D6FF,1,0,0,0,100,100,0,0,3,14,0,4,50,0,0,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""
    ev = [
        f"Dialogue: 1,0:00:00.00,0:00:10.00,M,,0,0,0,,{{\\pos(54,{h // 2 - 220})}}MiSide",
        f"Dialogue: 1,0:00:00.00,0:00:10.00,T,,0,0,0,,{{\\pos(50,{h // 2 - 105})}}{G['ko']['peace_mode']}",
        f"Dialogue: 1,0:00:00.00,0:00:10.00,M,,0,0,0,,{{\\pos(54,{h // 2 + 20})}}{G['en']['peace_mode']}",
        f"Dialogue: 1,0:00:00.00,0:00:10.00,B,,0,0,0,,{{\\pos(64,{h // 2 + 135})\\frz3}} {TR['ko']['unofficial']} ",
        f"Dialogue: 1,0:00:00.00,0:00:10.00,B,,0,0,0,,{{\\pos(64,{h // 2 + 230})\\frz3\\fn{FN['lat']}\\fs44}} UNOFFICIAL FAN MOD ",
    ]
    ap = os.path.join(BUILD, "thumb.ass")
    os.makedirs(BUILD, exist_ok=True)
    open(ap, "w", encoding="utf-8-sig").write(head + "\n".join(ev) + "\n")
    os.makedirs(FINAL, exist_ok=True)
    out = os.path.join(FINAL, "thumbnail.jpg")
    clip, sec = THUMB
    t = "clip((X-330)/380,0,1)"
    # (the page's own list on the left goes under a dark panel that fades out towards her face; a little more colour)
    fc = (f"[0:v]scale={w}:{h}:flags=lanczos,eq=saturation=1.2:contrast=1.05,format=rgb24[bg];"
          f"color=c=black:s={w}x{h}:d=1,format=rgba,geq=r=34:g=8:b=44:a='240*(1-{t}*{t}*(3-2*{t}))'[p];"
          f"[bg][p]overlay=format=auto,ass='{A.filter_path(ap)}':fontsdir='{A.filter_path(A.FONTS)}'")
    subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y", "-ss", str(sec), "-i", clip_file(clip, ".mp4"),
                    "-filter_complex", fc, "-frames:v", "1", "-q:v", "2", out], check=True)
    print(f"{out} ({os.path.getsize(out) // 1024} KB)")


def meta():
    d = os.path.join(FINAL, "meta")
    os.makedirs(d, exist_ok=True)
    for lang in LANGS:
        m = dict(lang=lang, youtube=YT[lang], title=yt_title(lang), description=yt_desc(lang))
        assert len(m["title"]) <= 100, (lang, len(m["title"]), m["title"])
        json.dump(m, open(os.path.join(d, f"{YT[lang]}.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    # the video's own (default) title and description: Korean, the English under it, the chapters between
    title = yt_title("ko")
    desc = "\n\n".join([
        DOWNLOAD["ko"] + "\n" + DOWNLOAD["en"] + "\n" + REPO_URL,
        "🌐 자막(CC)에서 33개 언어를 고를 수 있습니다 · Subtitles (CC) in 33 languages",
        yt_desc("ko", chapters=True).split("\n\n", 1)[1].rsplit("\n\n", 1)[0],
        "— English —\n" + yt_title("en") + "\n\n" + yt_desc("en").split("\n\n", 1)[1].rsplit("\n\n", 1)[0],
        "#MiSide #Mita #mod #미사이드 #평화모드",
    ])
    comment = "\n".join([DOWNLOAD["ko"], DOWNLOAD["en"], REPO_URL, "",
                         "비공식 팬 모드입니다. AIHASTO의 공식 평화 모드가 아닙니다.", "Unofficial fan mod. Not AIHASTO's official Peaceful Mode."])
    open(os.path.join(FINAL, "pinned_comment.txt"), "w", encoding="utf-8", newline="\n").write(comment + "\n")
    open(os.path.join(FINAL, "title.txt"), "w", encoding="utf-8", newline="\n").write(title + "\n")
    open(os.path.join(FINAL, "description.txt"), "w", encoding="utf-8", newline="\n").write(desc + "\n")
    print(f"{len(LANGS)} translated titles and descriptions; default title {len(title)} chars, description {len(desc)} chars")
    print("\n".join(chapter_lines()))


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    if what == "audio": build_audio()
    elif what == "ass": print(make_ass())
    elif what == "subs": subs()
    elif what == "video": render()
    elif what == "thumb": thumb()
    elif what == "sheet": sheet()
    elif what == "meta": meta()
    elif what == "all":
        meta(); subs(); build_audio(); render(); thumb()
