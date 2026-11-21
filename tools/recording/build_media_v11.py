"""The README media for v1.1's features (docs/media/v11_*), from the stills and frames of
dev_output/_claude_build/record_v11.txt (motions) and record_v11b.txt (menus, photo mode, album), the diary / records /
settings screenshots, the plant stages (test_pot.txt) and the marker shot (test_marker.txt).
usage: python tools/recording/build_media_v11.py"""
import glob, os, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
from build_media_paths import game_dir

CFG = os.path.join(game_dir(), "BepInEx", "config")
FR = os.path.join(CFG, "frames")
OUT = os.path.join(REPO, "docs", "media")
FONT = "C:/Windows/Fonts/malgunbd.ttf"


def load(name, crop=None, width=640):
    im = Image.open(name if os.path.isabs(name) else os.path.join(CFG, name)).convert("RGB")
    if crop:
        w, h = im.size
        im = im.crop(tuple(int(c * (w if i % 2 == 0 else h)) for i, c in enumerate(crop)))
    return im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)


def gif(name, frames, ms, hold_last=0):
    """frames: images (same size); ms: one duration for all, or one per frame"""
    durs = ms if isinstance(ms, list) else [ms] * len(frames)
    if hold_last: durs[-1] += hold_last
    tw, th = frames[0].width // 2, frames[0].height // 2
    picks = [frames[int(i * (len(frames) - 1) / 7)] for i in range(8)] if len(frames) > 1 else frames
    sheet = Image.new("RGB", (tw * 4, th * 2))
    for k, im in enumerate(picks): sheet.paste(im.resize((tw, th)), ((k % 4) * tw, (k // 4) * th))
    pal = sheet.quantize(colors=220, method=Image.MEDIANCUT)
    q = [f.quantize(palette=pal, dither=Image.FLOYDSTEINBERG) for f in frames]
    p = os.path.join(OUT, name)
    q[0].save(p, save_all=True, append_images=q[1:], duration=durs, loop=0, optimize=True)
    print(f"{name}: {len(frames)} frames, {os.path.getsize(p) // 1024} KB")


def fade(a, b, n=3):
    return [Image.blend(a, b, (i + 1) / (n + 1)) for i in range(n)]


def slideshow(name, stills, hold=1300, step=70):
    frames, durs = [], []
    for i, s in enumerate(stills):
        frames.append(s); durs.append(hold)
        nxt = stills[(i + 1) % len(stills)]
        for f in fade(s, nxt): frames.append(f); durs.append(step)
    gif(name, frames, durs)


def save(name, im, q=88):
    p = os.path.join(OUT, name)
    im.save(p, quality=q) if name.endswith(".jpg") else im.save(p, optimize=True)
    print(f"{name}: {im.size} {os.path.getsize(p) // 1024} KB")


def main():
    os.makedirs(OUT, exist_ok=True)
    # the menus: main -> sub-menu -> its sub-menu -> back -> other sub-menus (the left 60 % of the screen)
    menu = [load(f"g_menu_{i}.png", (0, 0, 0.62, 1), 560) for i in range(1, 6)]
    slideshow("v11_menu.gif", menu)
    # photo mode: choose the pose and frame, 3-2-1, the photo itself
    ph = [load(f"g_photo_{i}.png", None, 640) for i in range(1, 7)]
    taken = load(os.path.join(REPO, "dev_output", "photo_test", "v11_taken.png"), None, 640)
    frames, durs = [], []
    for k, s in enumerate(ph):
        frames.append(s); durs.append(1100 if k < 3 else 650)
    white = Image.new("RGB", taken.size, (255, 255, 255))
    frames += [Image.blend(white, taken, t) for t in (0.0, 0.35, 0.7)]; durs += [60, 60, 60]
    frames.append(taken); durs.append(2600)
    gif("v11_photo.gif", frames, durs)
    # the album: pages turned
    slideshow("v11_album.gif", [load(f"g_album_{i}.png", (0.15, 0.1, 0.85, 0.92), 560) for i in range(1, 4)], hold=1400)
    # the diary, the records, the settings (stills)
    save("v11_diary.jpg", load("v11_diary.png", (0.24, 0.13, 0.76, 0.87), 720))
    save("v11_records.jpg", load("v11_records.png", (0.27, 0.06, 0.73, 0.94), 560))
    save("v11_settings.jpg", load("v11_settings.png", (0.0, 0.08, 0.5, 0.95), 560))
    save("v11_life.jpg", load("v11_life.png", (0.0, 0.15, 0.5, 0.8), 560))
    # the plant: its stages in a row, labelled
    names = ["빈 화분", "새싹", "떡잎", "줄기", "꽃봉오리", "활짝", "시들면"]
    tiles = []
    for i in range(7):
        im = Image.open(os.path.join(CFG, f"k_{i}.png")).convert("RGB")
        w, h = im.size
        tiles.append(im.crop((int(w * 0.24), int(h * 0.02), int(w * 0.50), int(h * 0.62))).resize((220, 290), Image.LANCZOS))
    strip = Image.new("RGB", (7 * 220, 330), (24, 10, 30))
    d = ImageDraw.Draw(strip)
    fo = ImageFont.truetype(FONT, 22)
    for i, t in enumerate(tiles):
        strip.paste(t, (i * 220, 0))
        tw = d.textlength(names[i], font=fo)
        d.text((i * 220 + (220 - tw) / 2, 296), names[i], font=fo, fill=(255, 190, 225) if i < 6 else (200, 200, 140))
    save("v11_plant.jpg", strip)
    # the marker
    save("v11_marker.jpg", load("m_01.png", (0.45, 0.05, 1.0, 0.75), 640))
    # her own motions: wave, cheer, nod, tilt (her face and body)
    frames = []
    for pre in ("v11wave", "v11cheer", "v11nod", "v11tilt"):
        for f in sorted(glob.glob(os.path.join(FR, pre + "_*.png"))):
            frames.append(load(f, (0.25, 0.0, 0.75, 1.0), 360))
    gif("v11_motions.gif", frames, 120, hold_last=600)


if __name__ == "__main__":
    main()
