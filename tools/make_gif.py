"""Build a README-friendly GIF from captured frames (DevRunner `frames`/`camshot` PNGs).

usage: python tools/make_gif.py <out.gif> <frame1.png> [frame2.png ...] [--width 640] [--ms 120] [--hold-last 1200]
Frames are resized to --width, palette-quantized together and written as a looping GIF.
"""
import sys
from PIL import Image


def main(argv):
    width, ms, hold = 640, 120, 0
    files, out = [], None
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--width": width = int(argv[i + 1]); i += 2; continue
        if a == "--ms": ms = int(argv[i + 1]); i += 2; continue
        if a == "--hold-last": hold = int(argv[i + 1]); i += 2; continue
        if out is None: out = a
        else: files.append(a)
        i += 1
    if not out or not files:
        print(__doc__); return 1
    frames = []
    for f in files:
        im = Image.open(f).convert("RGB")
        h = round(im.height * width / im.width)
        frames.append(im.resize((width, h), Image.LANCZOS))
    # one shared palette keeps colors stable between frames
    pal = frames[len(frames) // 2].quantize(colors=255, method=Image.MEDIANCUT)
    q = [fr.quantize(palette=pal, dither=Image.FLOYDSTEINBERG) for fr in frames]
    durations = [ms] * len(q)
    if hold: durations[-1] = hold
    q[0].save(out, save_all=True, append_images=q[1:], duration=durations, loop=0, optimize=True, disposal=1)
    print(f"{out}: {len(q)} frames, {width}px")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
