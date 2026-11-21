"""The re-cut trailer's thumbnail (PhotoCraft, layered .psd kept): three slanted panels from the new clips - the
vampire costume, the original Mita smiling, the Christmas costume - with "v1.1 업데이트" big along the bottom and tags
for what is new. Stills are taken from the 4K clips with ffmpeg, the panels laid out with PIL, everything drawn on
top (tags, words, outlines, glow) is PhotoCraft layers.

usage: python tools/video/thumb12.py [stills]   -> dev_output/trailer/fc/thumb/thumbnail_v11.jpg (+ .psd)
"""
import glob, os, subprocess, sys
from PIL import Image, ImageDraw, ImageEnhance

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from pc import PhotoCraft
import thumbs_v3 as TV

REPO = os.path.dirname(os.path.dirname(HERE))
GAME = os.path.dirname(REPO)
CLIPS = os.path.join(REPO, "dev_output", "trailer", "ko")
WORK = os.path.join(REPO, "dev_output", "trailer", "fc", "thumb")
FF = (glob.glob(os.path.join(REPO, "dev_output", "ffmpeg", "**", "ffmpeg.exe"), recursive=True) or ["ffmpeg"])[0]
W, H = 1280, 720
# (clip, second, centre x, centre y, zoom) - the panels left to right
STILLS = [("r12_vamp_hug", 0.6, 0.53, 0.66, 0.75), ("r12_hands", 3.4, 0.48, 0.42, 0.80), ("r12_xmas_hug", 0.6, 0.52, 0.64, 0.75)]
CUTS = [(430, 360), (900, 830)]   # the slanted edges between the panels: x at the top, x at the bottom


def stills():
    os.makedirs(WORK, exist_ok=True)
    for clip, sec, *_ in STILLS:
        out = os.path.join(WORK, f"s_{clip}.png")
        subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y", "-ss", str(sec), "-i", os.path.join(CLIPS, f"ko_{clip}.mp4"),
                        "-frames:v", "1", out], check=True)
    print("stills in", WORK)


def base():
    edges = [(0, 0)] + CUTS + [(W, W)]
    out = Image.new("RGB", (W, H))
    for k, (clip, sec, cx, cy, zoom) in enumerate(STILLS):
        im = Image.open(os.path.join(WORK, f"s_{clip}.png")).convert("RGB")
        x0 = min(edges[k]); x1 = max(edges[k + 1])
        pw = x1 - x0
        panel = TV.cover(im, pw, H, cx, cy, zoom * pw / W)
        panel = ImageEnhance.Color(panel).enhance(1.15)
        panel = ImageEnhance.Contrast(panel).enhance(1.05)
        layer = Image.new("RGB", (W, H)); layer.paste(panel, (x0, 0))
        mask = Image.new("L", (W, H), 0)
        (lt, lb), (rt, rb) = edges[k], edges[k + 1]
        ImageDraw.Draw(mask).polygon([(lt, 0), (rt, 0), (rb, H), (lb, H)], fill=255)
        out.paste(layer, (0, 0), mask)
    haze = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(haze)
    for y in range(H // 2, H):
        hd.line([(0, y), (W, y)], fill=(40, 6, 34, int(170 * ((y - H / 2) / (H / 2)) ** 1.5)))
    out = Image.alpha_composite(out.convert("RGBA"), haze).convert("RGB")
    out.save(os.path.join(WORK, "base.png"))


def build(pc, lang="ko", peace="평화 모드", note="비공식 팬 모드", row=None, big="v1.1 업데이트", name="thumbnail_v11", psd=True):
    """one thumbnail; the words in `lang` (yt12.py makes one per YouTube language)"""
    row = row or ["뱀파이어 · 크리스마스 의상", "목표 17가지 · 편지", "AI 대화"]
    rel = os.path.relpath(os.path.join(WORK, "base.png"), GAME).replace("\\", "/")
    pc.call("doc.new", {"width": W, "height": H})
    pc.call("doc.place", {"path": rel})
    for top, bottom in CUTS:   # the light along each cut
        pc.run("shape.create", {"kind": "path", "fill": "#ffffff", "path": {"subpaths": [{"knots": [[top - 3, 0], [top + 3, 0], [bottom + 3, H], [bottom - 3, H]]}]}})
        pc.run("layer.layerStyle.outerGlow", {"color": "#ff5cc8", "opacity": 85, "size": 22, "spread": 10})
    grad = {"gradient": {"stops": [[0, "#ff4fb4"], [1, "#9b3cf0"]], "angle": 0}}
    TV.tag(pc, peace, 26, 24, lang, grad, size=34)
    TV.tag(pc, note, W - 24, 24, lang, "#160a1c", size=20, right=True)
    # what is new, a row of small tags over the big words
    xs, y = [], H - 262
    probe = []
    for s in row:
        t = TV.text(pc, s, 0, 100, 30, "#ffffff", lang, 800)
        probe.append(TV.bounds(pc, t)[2] + 44)
        pc.run("layer.delete", {"layer": t}, check=False)
    total = sum(probe) + 16 * (len(row) - 1)
    x = (W - total) / 2
    if total > W - 60:   # (long words: the tags in a smaller size)
        k = (W - 60) / total; probe = [p * k for p in probe]; x = 30
    for i, (s, pw) in enumerate(zip(row, probe)):
        TV.tag(pc, s, x, y, lang, "#c8105e" if i == 0 else "#2a0a2e", size=30 if total <= W - 60 else int(30 * k))
        x += pw + 16
    # the big words
    v = big.find("v1.1")
    big = TV.text(pc, big, 0, 300, 160, "#ffffff", lang, 900)
    if v >= 0: pc.run("type.setStyle", {"layer": big, "range": [v, v + 4], "color": "#ffe23d"}, check=False)
    size, b = TV.fit_width(pc, big, W - 160, 160)
    TV.move_to(pc, big, (W - b[2]) / 2, H - b[3] - 36)
    pc.run("layer.layerStyle.stroke", {"size": max(8, size // 14), "position": "outside", "color": "#2a0a2e"})
    pc.run("layer.layerStyle.outerGlow", {"color": "#ff3fb0", "opacity": 70, "size": 34, "spread": 6, "add": True})
    pc.run("layer.layerStyle.dropShadow", {"color": "#000000", "opacity": 70, "distance": 9, "size": 14, "angle": 110, "add": True})
    pc.call("doc.save", {"path": f"{name}.png"})
    if psd: pc.call("doc.save", {"path": f"{name}.psd"})
    pc.call("doc.close", {}, check=False)
    p = os.path.join(WORK, f"{name}.png")
    Image.open(p).convert("RGB").save(p[:-4] + ".jpg", quality=92)
    os.remove(p)
    print(p[:-4] + ".jpg")


if __name__ == "__main__":
    if "stills" in sys.argv or not os.path.exists(os.path.join(WORK, f"s_{STILLS[0][0]}.png")):
        stills()
    base()
    with PhotoCraft(GAME, WORK) as pc:
        build(pc)
