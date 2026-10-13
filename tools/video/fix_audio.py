"""The sound of a `recordav` clip back to normal speed, in step with its picture.

usage: python tools/video/fix_audio.py <clip base path> [ffmpeg]
reads <clip>.raw.wav (the game's sound taken live, slowed F times) and <clip>.sync.txt (F, the frame time, the sound
sample count at each frame's end), writes <clip>.wav: F times faster, exactly as long as the picture.

Each frame was held to T real seconds, so each frame's sound should be T x rate samples. The marks are only as fine as
the mixer's blocks (1024 samples), so small differences are left alone; a frame that took longer than T (its sound kept
playing) shows up as a lasting surplus, and that surplus is cut out right there (with a short crossfade).
"""
import os, subprocess, sys, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))


def ffmpeg_path():
    if len(sys.argv) > 2:
        return sys.argv[2]
    root = os.path.join(os.path.dirname(os.path.dirname(HERE)), "dev_output", "ffmpeg")
    for d in os.listdir(root):
        p = os.path.join(root, d, "bin", "ffmpeg.exe")
        if os.path.exists(p):
            return p
    return "ffmpeg"


def read_sync(path):
    info = {}
    for line in open(path, encoding="utf-8"):
        k, _, v = line.strip().partition(" ")
        info[k] = v
    marks = [int(x) for x in info.get("marks", "").split()] if info.get("marks") else []
    return int(info["fps"]), float(info["frame_seconds"]), float(info["slow"]), int(info["rate"]), int(info["channels"]), marks


def fix(base, ff):
    fps, T, F, rate, ch, marks = read_sync(base + ".sync.txt")
    with wave.open(base + ".raw.wav") as w:
        ch = w.getnchannels()
        raw = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).reshape(-1, ch).astype(np.float32)
    spf = T * rate                       # sound samples one frame should have taken
    slack = int(spf * 1.2)                # (a frame may run late by up to ~T and be made up by the next: only more is a stall)
    out = []
    pos = 0                               # where we are in raw
    dropped = 0
    fade = 256
    for k, m in enumerate(marks, start=1):
        m = min(m, len(raw))
        expected = k * spf + dropped
        surplus = m - expected
        seg = raw[pos:m]
        if surplus > slack and len(seg) > fade * 2:
            cut = int(min(surplus, len(seg) - fade))
            keep = seg[: len(seg) - cut]
            # (crossfade the join to what follows the cut)
            tail = raw[m - fade:m] if m - fade >= 0 else None
            if tail is not None and len(keep) >= fade:
                r = np.linspace(0, 1, fade, dtype=np.float32)[:, None]
                keep = keep.copy()
                keep[-fade:] = keep[-fade:] * (1 - r) + tail * r
            seg = keep
            dropped += cut
        out.append(seg)
        pos = m
    slowed = np.concatenate(out) if out else raw
    frames = len(marks)
    want = int(round(frames * spf))      # the slowed length that matches the picture exactly
    if len(slowed) < want:
        slowed = np.concatenate([slowed, np.zeros((want - len(slowed), ch), np.float32)])
    slowed = slowed[:want]
    tmp = base + ".aligned.wav"
    with wave.open(tmp, "wb") as w:
        w.setnchannels(ch); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes(np.clip(slowed, -32768, 32767).astype(np.int16).tobytes())
    # F times faster, pitch back up; then exactly the picture's length
    dur = frames / fps
    subprocess.run([ff, "-y", "-loglevel", "error", "-i", tmp, "-af",
                    f"asetrate={int(rate * F)},aresample={rate}:filter_size=64:phase_shift=10:cutoff=0.97,apad,atrim=0:{dur:.6f}",
                    "-ac", str(ch), base + ".wav"], check=True)
    os.remove(tmp)
    print(f"{os.path.basename(base)}: {frames} frames = {dur:.2f} s, sound x{F:g}, {dropped / rate:.2f} s of overrun cut")


if __name__ == "__main__":
    fix(sys.argv[1], ffmpeg_path())
