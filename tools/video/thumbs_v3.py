"""The release video's thumbnails, third design (2026-10-25): the original's eerie grin on the left, dark and red, and
peace mode's smiling Mita on the right, bright and pink, split on a slant; "공포 0%" / "HORROR 0%" big across both, a
tag on each side, the unofficial notice. One per YouTube language (33), built in PhotoCraft (layered: a .psd is kept
for ko and en to edit by hand).
usage: python tools/video/thumbs_v3.py [prep] [lang ...]   -> dev_output/thumb_v3/out/thumbnail.<yt>.jpg (+ .psd)
The stills: devcam portraits from the game (dev_output/_claude_build/test_stills*.txt), copied to dev_output/thumb_v3/src."""
import os, shutil, sys
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from pc import PhotoCraft
import trailer as T

REPO = os.path.dirname(os.path.dirname(HERE))
GAME = os.path.dirname(REPO)
WORK = os.path.join(REPO, "dev_output", "thumb_v3")
SRC = os.path.join(WORK, "src")
OUT = os.path.join(WORK, "out")
W, H = 1280, 720
SPLIT_TOP, SPLIT_BOTTOM = 560, 430   # the slanted line between the two halves

HAPPY, EERIE = "t_tongue", "t_creepy"

# "공포 0%": the word, then the number (drawn in yellow)
HORROR = {
    "ko": ("공포", "0%"), "en": ("HORROR", "0%"), "ja": ("恐怖", "0%"), "zh": ("恐怖", "0%"), "zht": ("恐怖", "0%"),
    "ru": ("ХОРРОР", "0%"), "uk": ("ЖАХИ", "0%"), "be": ("ЖАХІ", "0%"), "bg": ("УЖАС", "0%"), "kk": ("ҚОРҚЫНЫШ", "0%"),
    "sr": ("HOROR", "0%"), "hr": ("HOROR", "0%"), "cs": ("HOROR", "0 %"), "sk": ("HOROR", "0 %"), "pl": ("HORROR", "0%"),
    "hu": ("HORROR", "0%"), "ro": ("HORROR", "0%"), "de": ("HORROR", "0 %"), "fr": ("HORREUR", "0 %"), "it": ("HORROR", "0%"),
    "es": ("TERROR", "0 %"), "esla": ("TERROR", "0 %"), "pt": ("TERROR", "0%"), "ptbr": ("TERROR", "0%"), "sv": ("SKRÄCK", "0 %"),
    "tr": ("KORKU", "%0"), "id": ("HOROR", "0%"), "fil": ("HORROR", "0%"), "vi": ("KINH DỊ", "0%"), "th": ("สยองขวัญ", "0%"),
    "ar": ("رعب", "0٪"), "arz": ("رعب", "0٪"), "fa": ("ترس", "۰٪"),
}
# the tag over the eerie half: the original game
ORIGINAL = {
    "ko": "원작", "en": "ORIGINAL", "ja": "原作", "zh": "原作", "zht": "原作", "ru": "ОРИГИНАЛ", "uk": "ОРИГІНАЛ", "be": "АРЫГІНАЛ",
    "bg": "ОРИГИНАЛ", "kk": "ТҮПНҰСҚА", "sr": "ORIGINAL", "hr": "ORIGINAL", "cs": "ORIGINÁL", "sk": "ORIGINÁL", "pl": "ORYGINAŁ",
    "hu": "EREDETI", "ro": "ORIGINAL", "de": "ORIGINAL", "fr": "ORIGINAL", "it": "ORIGINALE", "es": "ORIGINAL", "esla": "ORIGINAL",
    "pt": "ORIGINAL", "ptbr": "ORIGINAL", "sv": "ORIGINALET", "tr": "ORİJİNAL", "id": "ASLI", "fil": "ORIHINAL", "vi": "BẢN GỐC",
    "th": "ต้นฉบับ", "ar": "الأصلية", "arz": "الأصلية", "fa": "اصلی",
}
FONT = {"ko": "Noto Sans KR", "ja": "Yu Gothic", "zh": "Microsoft YaHei", "zht": "Microsoft JhengHei", "th": "Leelawadee UI",
        "ar": "Dubai", "arz": "Dubai", "fa": "Dubai"}
RTL = {"ar", "arz", "fa"}


def font(lang): return FONT.get(lang, "Inter")


# ---------------------------------------------------------------- the picture (PIL): two halves, graded, on a slant

def cover(im, w, h, cx, cy, zoom):
    """crop a w:h window around (cx, cy) (0..1 of the image), zoom = how much of the image's width the window spans"""
    sw = im.width * zoom
    sh = sw * h / w
    x = min(max(0, im.width * cx - sw / 2), im.width - sw)
    y = min(max(0, im.height * cy - sh / 2), im.height - sh)
    return im.crop((int(x), int(y), int(x + sw), int(y + sh))).resize((w, h), Image.LANCZOS)


def prep():
    os.makedirs(SRC, exist_ok=True)
    for n in (HAPPY, EERIE):
        shutil.copy(os.path.join(GAME, "BepInEx", "config", n + ".png"), os.path.join(SRC, n + ".png"))
    happy = Image.open(os.path.join(SRC, HAPPY + ".png")).convert("RGB")
    eerie = Image.open(os.path.join(SRC, EERIE + ".png")).convert("RGB")
    # right: her smile, warm and bright
    right = cover(happy, W, H, 0.5, 0.42, 1.0)
    right = right.transform((W, H), Image.AFFINE, (1, 0, -250, 0, 1, 0), resample=Image.BICUBIC, fillcolor=(236, 180, 140))
    right = ImageEnhance.Color(right).enhance(1.18)
    right = ImageEnhance.Contrast(right).enhance(1.06)
    right = ImageEnhance.Brightness(right).enhance(1.04)
    # left: the grin, drained and stained dark red, the edges sinking into black
    left = cover(eerie, W, H, 0.5, 0.45, 0.72)
    g = ImageOps.grayscale(left)
    g = ImageEnhance.Contrast(g).enhance(1.35)
    left = ImageOps.colorize(g, black=(14, 0, 4), mid=(120, 16, 34), white=(255, 196, 196))
    vig = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(vig)
    d.ellipse((-260, -200, SPLIT_TOP + 160, H + 200), fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(120))
    left = Image.composite(left, Image.new("RGB", (W, H), (8, 0, 2)), vig)
    # the left half shifted so her face sits in it
    left = left.transform((W, H), Image.AFFINE, (1, 0, 380, 0, 1, 0), resample=Image.BICUBIC)
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).polygon([(0, 0), (SPLIT_TOP, 0), (SPLIT_BOTTOM, H), (0, H)], fill=255)
    base = Image.composite(left, right, mask)
    # a soft pink haze along the bottom of the bright half (the big words sit there)
    haze = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(haze)
    for y in range(H // 2, H):
        a = int(150 * ((y - H / 2) / (H / 2)) ** 1.6)
        hd.line([(0, y), (W, y)], fill=(40, 6, 34, a))
    base = Image.alpha_composite(base.convert("RGBA"), haze).convert("RGB")
    base.save(os.path.join(WORK, "base.png"))
    print("base.png")


# ---------------------------------------------------------------- the words and shapes (PhotoCraft, layered)

def bounds(pc, layer):
    for l in pc.run("layer.list")["layers"]:
        if l["id"] == layer: return l["bounds"]   # [x, y, w, h]
    return None


def text(pc, s, x, y, size, color, lang, bold=900):
    r = pc.run("type.create", {"x": x, "y": y, "text": s, "font": font(lang), "size": size, "color": color})
    layer = r["layer"]
    pc.run("type.setStyle", {"layer": layer, "weight": bold, **({"direction": "rtl"} if lang in RTL else {})}, check=False)
    return layer


def fit_width(pc, layer, max_w, size):
    b = bounds(pc, layer)
    while b and b[2] > max_w and size > 20:
        size = int(size * max_w / b[2] * 0.97)
        pc.run("type.setStyle", {"layer": layer, "size": size})
        b = bounds(pc, layer)
    return size, b


def move_to(pc, layer, x, y):
    b = bounds(pc, layer)
    pc.run("layer.translate", {"layer": layer, "dx": int(round(x - b[0])), "dy": int(round(y - b[1]))})
    return bounds(pc, layer)


def pill(pc, x, y, w, h, fill, stroke=None):
    p = {"kind": "roundedRect", "rect": [x, y, w, h], "radii": h / 2, "fill": fill}
    if stroke: p["stroke"] = stroke
    return pc.run("shape.create", p)


def tag(pc, s, x, y, lang, fill, color="#ffffff", size=30, right=False):
    t = text(pc, s, 0, 100, size, color, lang, 800)
    b = bounds(pc, t)
    pw, ph = b[2] + 44, 54
    px = x - pw if right else x
    shape = pill(pc, px, y, pw, ph, fill)["layer"]
    pc.run("layer.moveTo", {"layer": t, "target": shape, "position": "above"})   # (the words over their pill)
    move_to(pc, t, px + 22, y + (ph - b[3]) / 2)
    return t


def build(lang, pc, note_low=False):
    pc.call("doc.new", {"width": W, "height": H})
    pc.call("doc.place", {"path": "MiSide-KR-PeaceMode/dev_output/thumb_v3/base.png"})
    # the light along the split
    pc.run("shape.create", {"kind": "path", "fill": "#ffffff", "path": {"subpaths": [{"knots": [[SPLIT_TOP - 4, 0], [SPLIT_TOP + 4, 0], [SPLIT_BOTTOM + 4, H], [SPLIT_BOTTOM - 4, H]]}]}})
    pc.run("layer.layerStyle.outerGlow", {"color": "#ff5cc8", "opacity": 85, "size": 26, "spread": 10})
    # the two tags
    tag(pc, ORIGINAL[lang], 28, 26, lang, "#c8102e")
    name = T.G[lang]["peace_mode"]
    peace = tag(pc, name, SPLIT_TOP + 26, 26, lang, {"gradient": {"stops": [[0, "#ff4fb4"], [1, "#9b3cf0"]], "angle": 0}})
    # the notice (always): top right, small; a row lower when a long mode name reaches it
    note = T.TR[lang]["unofficial"]
    nt = tag(pc, note, W - 24, 90 if note_low else 24, lang, "#160a1c", size=20, right=True)
    pb, nb = bounds(pc, peace), bounds(pc, nt)
    if not note_low and pb[0] + pb[2] + 40 > nb[0] - 22:
        pc.call("doc.close", {}, check=False)
        return build(lang, pc, note_low=True)
    # the big words: the word in white, the number in yellow, both outlined dark and glowing pink
    word, num = HORROR[lang]
    s = f"{num} {word}" if lang == "tr" else f"{word} {num}"
    big = text(pc, s, 0, 300, 170, "#ffffff", lang, 900)
    start = s.index(num)
    pc.run("type.setStyle", {"layer": big, "range": [start, start + len(num)], "color": "#ffe23d"})
    size, b = fit_width(pc, big, W - 140, 170)
    b = move_to(pc, big, (W - b[2]) / 2, H - b[3] - 34)
    pc.run("layer.layerStyle.stroke", {"size": max(8, size // 14), "position": "outside", "color": "#2a0a2e"})
    pc.run("layer.layerStyle.outerGlow", {"color": "#ff3fb0", "opacity": 70, "size": 34, "spread": 6, "add": True})
    pc.run("layer.layerStyle.dropShadow", {"color": "#000000", "opacity": 70, "distance": 9, "size": 14, "angle": 110, "add": True})
    # "MiSide" over it, small
    ms = text(pc, "MiSide", 0, 100, 40, "#ffffff", "en", 800)
    mb = bounds(pc, ms)
    move_to(pc, ms, (W - mb[2]) / 2, b[1] - mb[3] - 10)
    pc.run("layer.layerStyle.stroke", {"size": 5, "position": "outside", "color": "#2a0a2e"})
    yt = T.YT[lang]
    os.makedirs(OUT, exist_ok=True)
    png = f"out/thumbnail.{yt}.png"
    pc.call("doc.save", {"path": png})
    if lang in ("ko", "en"): pc.call("doc.save", {"path": f"out/thumbnail.{yt}.psd"})
    pc.call("doc.close", {}, check=False)
    p = os.path.join(WORK, png)
    Image.open(p).convert("RGB").save(p[:-4] + ".jpg", quality=92)
    os.remove(p)
    print("thumbnail." + yt + ".jpg")


# ---------------------------------------------------------------- the making-of video (-2BiLVkBQ5o): "it never came out, so I made it"
# left: the official Peaceful Mode as a grey "coming soon" card; right: Mita waving from the mod; "직접 만들었다" big.
MADE = {   # (small line over it, the big words, the "official: coming soon" card)
    "ko": ("평화모드가 안 나와서", "직접 만들었다", "공식 평화모드", "출시 미정…"),
    "en": ("NO PEACEFUL MODE YET?", "I MADE IT", "OFFICIAL PEACEFUL MODE", "COMING SOON…"),
    "ja": ("平和モードが出ないので", "自分で作った", "公式 平和モード", "未定…"),
    "ru": ("МИРНОГО РЕЖИМА НЕТ?", "СДЕЛАЛ САМ", "ОФИЦИАЛЬНЫЙ МИРНЫЙ РЕЖИМ", "СКОРО…"),
}


def prep_made():
    hi = Image.open(os.path.join(SRC, "t_hi.png")).convert("RGB")
    right = cover(hi, W, H, 0.5, 0.40, 0.78)
    right = right.transform((W, H), Image.AFFINE, (1, 0, -270, 0, 1, 0), resample=Image.BICUBIC, fillcolor=(236, 180, 140))
    right = ImageEnhance.Color(right).enhance(1.18)
    right = ImageEnhance.Contrast(right).enhance(1.05)
    # left: a dim grey-violet wall with a faint grid (the "not yet" side)
    left = Image.new("RGB", (W, H), (34, 30, 44))
    d = ImageDraw.Draw(left)
    for x in range(0, W, 40): d.line([(x, 0), (x, H)], fill=(44, 40, 58))
    for y in range(0, H, 40): d.line([(0, y), (W, y)], fill=(44, 40, 58))
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).polygon([(0, 0), (SPLIT_TOP - 60, 0), (SPLIT_BOTTOM - 60, H), (0, H)], fill=255)
    base = Image.composite(left, right, mask)
    haze = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(haze)
    for y in range(H // 2, H):
        a = int(160 * ((y - H / 2) / (H / 2)) ** 1.5)
        hd.line([(0, y), (W, y)], fill=(40, 6, 34, a))
    base = Image.alpha_composite(base.convert("RGBA"), haze).convert("RGB")
    base.save(os.path.join(WORK, "base_made.png"))
    print("base_made.png")


def build_made(lang, pc):
    small, big_s, card1, card2 = MADE[lang]
    pc.call("doc.new", {"width": W, "height": H})
    pc.call("doc.place", {"path": "MiSide-KR-PeaceMode/dev_output/thumb_v3/base_made.png"})
    sx0, sx1 = SPLIT_TOP - 60, SPLIT_BOTTOM - 60
    pc.run("shape.create", {"kind": "path", "fill": "#ffffff", "path": {"subpaths": [{"knots": [[sx0 - 4, 0], [sx0 + 4, 0], [sx1 + 4, H], [sx1 - 4, H]]}]}})
    pc.run("layer.layerStyle.outerGlow", {"color": "#ff5cc8", "opacity": 85, "size": 26, "spread": 10})
    # the "official: coming soon" card, tilted, greyed out, with a red stamp
    card = pc.run("shape.create", {"kind": "roundedRect", "rect": [46, 70, 380, 200], "radii": 22, "fill": "#4a4458",
                                   "stroke": {"width": 4, "color": "#8a83a0", "align": "inside", "dashes": [3, 2]}})["layer"]
    t1 = text(pc, card1, 0, 100, 34, "#d8d2e6", lang, 800)
    _, b1 = fit_width(pc, t1, 300, 34)
    move_to(pc, t1, 46 + (380 - b1[2]) / 2, 104)
    t2 = text(pc, card2, 0, 100, 52, "#ffffff", lang, 900)
    _, b2 = fit_width(pc, t2, 330, 52)
    move_to(pc, t2, 46 + (380 - b2[2]) / 2, 168)
    stamp = text(pc, "?", 0, 100, 120, "#ff3b5c", "en", 900)
    move_to(pc, stamp, 400, 30)
    pc.run("layer.layerStyle.stroke", {"size": 6, "position": "outside", "color": "#2a0a2e"})
    # the notice and the mode's name, top right
    tag(pc, T.TR[lang]["unofficial"], W - 24, 24, lang, "#160a1c", size=20, right=True)
    tag(pc, "MiSide · " + T.G[lang]["peace_mode"], W - 24, 86, lang,
        {"gradient": {"stops": [[0, "#ff4fb4"], [1, "#9b3cf0"]], "angle": 0}}, size=26, right=True)
    # the words: the small line in yellow, the big one white (outlined, glowing)
    big = text(pc, big_s, 0, 300, 160, "#ffffff", lang, 900)
    size, b = fit_width(pc, big, W - 140, 160)
    b = move_to(pc, big, 50, H - b[3] - 40)
    pc.run("layer.layerStyle.stroke", {"size": max(8, size // 14), "position": "outside", "color": "#2a0a2e"})
    pc.run("layer.layerStyle.outerGlow", {"color": "#ff3fb0", "opacity": 70, "size": 34, "spread": 6, "add": True})
    pc.run("layer.layerStyle.dropShadow", {"color": "#000000", "opacity": 70, "distance": 9, "size": 14, "angle": 110, "add": True})
    sm = text(pc, small, 0, 100, 52, "#ffe23d", lang, 900)
    _, sb = fit_width(pc, sm, W - 160, 52)
    move_to(pc, sm, 56, b[1] - sb[3] - 14)
    pc.run("layer.layerStyle.stroke", {"size": 6, "position": "outside", "color": "#2a0a2e"})
    out = f"out_made/thumbnail.{T.YT[lang]}.png"
    os.makedirs(os.path.join(WORK, "out_made"), exist_ok=True)
    pc.call("doc.save", {"path": out})
    pc.call("doc.close", {}, check=False)
    p = os.path.join(WORK, out)
    Image.open(p).convert("RGB").save(p[:-4] + ".jpg", quality=92)
    os.remove(p)
    print(os.path.basename(p)[:-4] + ".jpg")


def made(langs=None):
    prep_made()
    with PhotoCraft(GAME, WORK) as pc:
        for l in langs or MADE: build_made(l, pc)


# ---------------------------------------------------------------- the Shorts (vertical 1080x1920)
SW, SH = 1080, 1920
SHORTS = {
    # id: (picture, (cx, cy, zoom), small line, big words, number in yellow inside the big words or None)
    "ttsg8zD6sUs": ("split", None, "미사이드 평화모드", "공포 0%", "0%"),
    "mDInEJetH4M": ("t_surprise", (0.5, 0.40, 0.34), "Alt+F4로 끄면…", "미타가\n기억함", None),
    "-oP7DnICbR0": ("hammer_10.0", (0.45, 0.55, 0.31), "캐피랑 하는", "망치 게임", None),
}


def prep_short(vid):
    kind, crop, *_ = SHORTS[vid]
    if kind == "split":
        # top: the grin (dark red), bottom: her smile, split on a slant
        eerie = Image.open(os.path.join(SRC, EERIE + ".png")).convert("RGB")
        happy = Image.open(os.path.join(SRC, HAPPY + ".png")).convert("RGB")
        top = cover(eerie, SW, SH, 0.5, 0.42, 0.34)
        g = ImageEnhance.Contrast(ImageOps.grayscale(top)).enhance(1.35)
        top = ImageOps.colorize(g, black=(14, 0, 4), mid=(120, 16, 34), white=(255, 196, 196))
        bottom = cover(happy, SW, SH, 0.5, 0.42, 0.34)
        bottom = ImageEnhance.Color(bottom).enhance(1.18)
        bottom = bottom.transform((SW, SH), Image.AFFINE, (1, 0, 0, 0, 1, -330), resample=Image.BICUBIC, fillcolor=(236, 180, 140))
        top = top.transform((SW, SH), Image.AFFINE, (1, 0, 0, 0, 1, 430), resample=Image.BICUBIC, fillcolor=(8, 0, 2))
        mask = Image.new("L", (SW, SH), 0)
        ImageDraw.Draw(mask).polygon([(0, 0), (SW, 0), (SW, 820), (0, 980)], fill=255)
        base = Image.composite(top, bottom, mask)
    else:
        src = Image.open(os.path.join(SRC, kind + ".png")).convert("RGB")
        base = cover(src, SW, SH, *crop)
        base = ImageEnhance.Color(base).enhance(1.15)
    haze = Image.new("RGBA", (SW, SH), (0, 0, 0, 0))
    hd = ImageDraw.Draw(haze)
    for y in range(int(SH * 0.55), SH):
        a = int(200 * ((y - SH * 0.55) / (SH * 0.45)) ** 1.4)
        hd.line([(0, y), (SW, y)], fill=(40, 6, 34, a))
    base = Image.alpha_composite(base.convert("RGBA"), haze).convert("RGB")
    base.save(os.path.join(WORK, f"base_short_{vid}.png"))


def build_short(vid, pc):
    kind, crop, small, big_s, num = SHORTS[vid]
    pc.call("doc.new", {"width": SW, "height": SH})
    pc.call("doc.place", {"path": f"MiSide-KR-PeaceMode/dev_output/thumb_v3/base_short_{vid}.png"})
    if kind == "split":
        pc.run("shape.create", {"kind": "path", "fill": "#ffffff", "path": {"subpaths": [{"knots": [[0, 976], [SW, 816], [SW, 824], [0, 984]]}]}})
        pc.run("layer.layerStyle.outerGlow", {"color": "#ff5cc8", "opacity": 85, "size": 30, "spread": 10})
        tag(pc, "원작", 40, 40, "ko", "#c8102e", size=40)
        tag(pc, "평화 모드", 40, 1010, "ko", {"gradient": {"stops": [[0, "#ff4fb4"], [1, "#9b3cf0"]], "angle": 0}}, size=40)
    if vid == "-oP7DnICbR0": tag(pc, "비공식 팬 모드", 36, 40, "ko", "#160a1c", size=30)   # (the recording's own HUD is top right)
    else: tag(pc, "비공식 팬 모드", SW - 36, 40, "ko", "#160a1c", size=30, right=True)
    lines = big_s.split("\n")
    y = SH - 170
    layers = []
    for ln in reversed(lines):
        big = text(pc, ln, 0, 300, 200, "#ffffff", "ko", 900)
        if num and num in ln:
            st = ln.index(num)
            pc.run("type.setStyle", {"layer": big, "range": [st, st + len(num)], "color": "#ffe23d"})
        size, b = fit_width(pc, big, SW - 110, 200)
        b = move_to(pc, big, (SW - b[2]) / 2, y - b[3])
        pc.run("layer.layerStyle.stroke", {"size": max(9, size // 13), "position": "outside", "color": "#2a0a2e"})
        pc.run("layer.layerStyle.outerGlow", {"color": "#ff3fb0", "opacity": 70, "size": 40, "spread": 6, "add": True})
        pc.run("layer.layerStyle.dropShadow", {"color": "#000000", "opacity": 70, "distance": 10, "size": 16, "angle": 110, "add": True})
        y = b[1] - 16
    sm = text(pc, small, 0, 100, 76, "#ffe23d", "ko", 900)
    _, sb = fit_width(pc, sm, SW - 120, 76)
    move_to(pc, sm, (SW - sb[2]) / 2, y - sb[3] - 10)
    pc.run("layer.layerStyle.stroke", {"size": 8, "position": "outside", "color": "#2a0a2e"})
    if vid == "mDInEJetH4M":
        # an Alt + F4 keycap pair over her
        for i, k in enumerate(["Alt", "F4"]):
            x = 150 + i * 500
            key = pc.run("shape.create", {"kind": "roundedRect", "rect": [x, 330, 280, 220], "radii": 34, "fill": "#f4f0f8",
                                          "stroke": {"width": 10, "color": "#2a0a2e", "align": "outside"}})["layer"]
            pc.run("layer.layerStyle.dropShadow", {"color": "#000000", "opacity": 60, "distance": 14, "size": 10, "angle": 90})
            t = text(pc, k, 0, 100, 110, "#2a0a2e", "en", 900)
            tb = bounds(pc, t)
            move_to(pc, t, x + (280 - tb[2]) / 2, 330 + (190 - tb[3]) / 2)
        plus = text(pc, "+", 0, 100, 110, "#ffffff", "en", 900)
        pb = bounds(pc, plus)
        move_to(pc, plus, 150 + 280 + 110 - pb[2] / 2, 380)
        pc.run("layer.layerStyle.stroke", {"size": 8, "position": "outside", "color": "#2a0a2e"})
    os.makedirs(os.path.join(WORK, "out_shorts"), exist_ok=True)
    out = f"out_shorts/{vid}.png"
    pc.call("doc.save", {"path": out})
    pc.call("doc.close", {}, check=False)
    p = os.path.join(WORK, out)
    Image.open(p).convert("RGB").save(p[:-4] + ".jpg", quality=92)
    os.remove(p)
    print(vid + ".jpg")


def shorts():
    with PhotoCraft(GAME, WORK) as pc:
        for v in SHORTS:
            prep_short(v)
            build_short(v, pc)


# ---------------------------------------------------------------- the GitHub banner / social preview (1280x640)
def banner(version="v1.1"):
    base = Image.open(os.path.join(WORK, "base.png")).convert("RGB").crop((0, 30, W, 30 + 640))
    base.save(os.path.join(WORK, "base_banner.png"))
    with PhotoCraft(GAME, WORK) as pc:
        pc.call("doc.new", {"width": W, "height": 640})
        pc.call("doc.place", {"path": "MiSide-KR-PeaceMode/dev_output/thumb_v3/base_banner.png"})
        pc.run("shape.create", {"kind": "path", "fill": "#ffffff", "path": {"subpaths": [{"knots": [[SPLIT_TOP - 4 - 12, 0], [SPLIT_TOP + 4 - 12, 0], [SPLIT_BOTTOM + 4 + 3, 640], [SPLIT_BOTTOM - 4 + 3, 640]]}]}})
        pc.run("layer.layerStyle.outerGlow", {"color": "#ff5cc8", "opacity": 85, "size": 26, "spread": 10})
        tag(pc, "ORIGINAL · 원작", 28, 26, "en", "#c8102e", size=24)
        tag(pc, "PEACEFUL MODE · 평화 모드", SPLIT_TOP + 20, 26, "ko", {"gradient": {"stops": [[0, "#ff4fb4"], [1, "#9b3cf0"]], "angle": 0}}, size=24)
        tag(pc, "Unofficial fan mod · 비공식 팬 모드", W - 24, 96, "ko", "#160a1c", size=20, right=True)
        t = text(pc, "MiSide Peace Mode", 0, 300, 112, "#ffffff", "en", 900)
        size, b = fit_width(pc, t, W - 120, 112)
        st = len("MiSide ")
        pc.run("type.setStyle", {"layer": t, "range": [st, st + len("Peace Mode")], "color": "#ffe23d"})
        b = move_to(pc, t, (W - b[2]) / 2, 640 - b[3] - 104)
        pc.run("layer.layerStyle.stroke", {"size": 9, "position": "outside", "color": "#2a0a2e"})
        pc.run("layer.layerStyle.outerGlow", {"color": "#ff3fb0", "opacity": 70, "size": 30, "spread": 6, "add": True})
        pc.run("layer.layerStyle.dropShadow", {"color": "#000000", "opacity": 70, "distance": 8, "size": 12, "angle": 110, "add": True})
        s = text(pc, "공포 0% · 미타와 하루   |   A day with Mita, no horror   ·   " + version, 0, 100, 32, "#ffffff", "ko", 800)
        _, sb = fit_width(pc, s, W - 160, 32)
        move_to(pc, s, (W - sb[2]) / 2, 640 - sb[3] - 44)
        pc.run("layer.layerStyle.stroke", {"size": 5, "position": "outside", "color": "#2a0a2e"})
        pc.call("doc.save", {"path": "banner.png"})
        pc.call("doc.save", {"path": "banner.psd"})
    p = os.path.join(WORK, "banner.png")
    out = os.path.join(REPO, "docs", "media", "banner.png")
    Image.open(p).convert("RGB").save(out, optimize=True)
    print(out, os.path.getsize(out) // 1024, "KB")


if __name__ == "__main__" and len(sys.argv) > 1 and sys.argv[1] == "banner":
    banner()
    sys.exit()

if __name__ == "__main__" and len(sys.argv) > 1 and sys.argv[1] == "made":
    made(sys.argv[2:] or None)
    sys.exit()

if __name__ == "__main__" and len(sys.argv) > 1 and sys.argv[1] == "shorts":
    shorts()
    sys.exit()

if __name__ == "__main__":
    args = sys.argv[1:]
    if "prep" in args or not os.path.exists(os.path.join(WORK, "base.png")):
        prep()
        args = [a for a in args if a != "prep"]
    langs = args or T.LANGS
    with PhotoCraft(GAME, WORK) as pc:
        for l in langs: build(l, pc)



