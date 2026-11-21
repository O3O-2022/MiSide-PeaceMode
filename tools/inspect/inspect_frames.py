"""Frame-by-frame inspection of recorded actions (DevRunner `recidle` / `record`: BepInEx/config/frames/<name>_NNNN.jpg).

usage: python tools/inspect/inspect_frames.py <frames dir> <out dir> [name ...]

For every sequence:
  - the change between each frame and the next (small grey copies, mean absolute difference): an animation that
    snaps (a pose popping, her teleporting, a T-pose for one frame, a flicker) shows as one frame changing far more
    than its neighbours - flagged with the frames around it;
  - a contact sheet of the whole action (every Nth frame) to look over, and a sheet of each flagged spot at full
    size (before / the frame / after);
  - summary.tsv: frames, flags, the biggest jumps.
Fades and camera cuts made on purpose are long, smooth changes or one cut between two still runs - the script marks
"cut" (a jump with still frames on both sides) apart from "pop" (a jump inside motion), but both get a look.
"""
import glob, os, re, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

src, out = sys.argv[1], sys.argv[2]
names = sys.argv[3:]
os.makedirs(out, exist_ok=True)
seqs = {}
for f in glob.glob(os.path.join(src, "*.jpg")):
    m = re.match(r"(.+)_(\d{4})\.jpg$", os.path.basename(f))
    if m: seqs.setdefault(m.group(1), []).append((int(m.group(2)), f))
if names: seqs = {k: v for k, v in seqs.items() if k in names}
try: font = ImageFont.truetype("malgun.ttf", 18)
except Exception: font = ImageFont.load_default()

rows = []
for name in sorted(seqs):
    files = [f for _, f in sorted(seqs[name])]
    small = [np.asarray(Image.open(f).convert("L").resize((128, 72)), dtype=np.float32) for f in files]
    d = np.array([np.abs(small[i + 1] - small[i]).mean() for i in range(len(small) - 1)]) if len(small) > 1 else np.zeros(0)
    flags = []
    for i in range(len(d)):
        lo, hi = max(0, i - 6), min(len(d), i + 7)
        around = np.concatenate([d[lo:i], d[i + 1:hi]])
        base = float(np.median(around)) if len(around) else 0.0
        if d[i] > max(4.0, base * 4.0):
            kind = "cut" if base < 0.6 else "pop"
            flags.append((i, kind, float(d[i]), base))
    # contact sheet: ~48 tiles over the action
    step = max(1, len(files) // 48)
    pick = list(range(0, len(files), step))
    tw, th = 320, 180
    cols = 8
    sheet = Image.new("RGB", (cols * tw, ((len(pick) + cols - 1) // cols) * th), "black")
    flagged = {f[0] for f in flags} | {f[0] + 1 for f in flags}
    for k, i in enumerate(pick):
        im = Image.open(files[i]).convert("RGB").resize((tw, th))
        dr = ImageDraw.Draw(im)
        dr.text((4, 2), f"{i}", fill=(255, 255, 0), font=font)
        if any(abs(i - j) <= step // 2 for j in flagged): dr.rectangle([0, 0, tw - 1, th - 1], outline=(255, 0, 0), width=3)
        sheet.paste(im, ((k % cols) * tw, (k // cols) * th))
    sheet.save(os.path.join(out, f"{name}_sheet.jpg"), quality=82)
    # each flagged spot: frames i-1, i, i+1, i+2 at half size
    for n, (i, kind, v, base) in enumerate(flags[:12]):
        strip = Image.new("RGB", (4 * 480, 270), "black")
        for c, j in enumerate(range(i - 1, i + 3)):
            if 0 <= j < len(files):
                im = Image.open(files[j]).convert("RGB").resize((480, 270))
                ImageDraw.Draw(im).text((4, 2), f"{j}", fill=(255, 255, 0), font=font)
                strip.paste(im, (c * 480, 0))
        strip.save(os.path.join(out, f"{name}_{kind}{n:02d}_f{i}.jpg"), quality=85)
    top = ", ".join(f"{i}:{v:.1f}" for i, v in sorted(enumerate(d), key=lambda x: -x[1])[:3]) if len(d) else "-"
    rows.append((name, len(files), len([f for f in flags if f[1] == "pop"]), len([f for f in flags if f[1] == "cut"]), top))
    print(f"{name}: {len(files)} frames, {len(flags)} flags ({', '.join(f'{k}@{i}' for i, k, _, _ in flags[:8])})")

with open(os.path.join(out, "summary.tsv"), "w", encoding="utf-8") as f:
    f.write("name\tframes\tpops\tcuts\ttop jumps\n")
    for r in rows: f.write("\t".join(map(str, r)) + "\n")
