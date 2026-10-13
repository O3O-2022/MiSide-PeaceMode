"""Cut and join recorded frame sequences (DevRunner `record`) into an MJPEG .avi - no ffmpeg needed.

usage (as a module):
    from make_video import Video
    v = Video("out.avi", fps=30)
    v.clip("frames/greet_*.jpg", start=0, end=240, fade_in=10, fade_out=10)
    v.black(15)
    v.close()

MJPEG AVI plays in Windows Media Player / VLC and imports into video editors.
To get an .mp4 later: ffmpeg -i out.avi -c:v libx264 -crf 18 -pix_fmt yuv420p out.mp4
"""
import glob
import io
import struct

from PIL import Image


def _chunk(fourcc, data):
    pad = b"\0" if len(data) % 2 else b""
    return fourcc + struct.pack("<I", len(data)) + data + pad


class Video:
    def __init__(self, path, fps=30, size=(1280, 720), quality=88, reencode=False):
        self.path, self.fps, self.size, self.quality = path, fps, size, quality
        self.reencode = reencode    # False: copy recorded .jpg frames as they are (fast, keeps their quality/size)
        self.f = open(path, "wb")
        self.frames = []          # (offset in movi, length)
        self.max_len = 0
        # header is written at close(); reserve space now
        self.header_len = 12 + 8 + 4 + 8 + 56 + 8 + 4 + 8 + 56 + 8 + 40 + 12
        self.f.write(b"\0" * self.header_len)
        self.movi_start = self.f.tell() - 4   # offsets in idx1 count from the 'movi' fourcc

    # ---------------------------------------------------------------- frames
    def _jpeg(self, img):
        if img.size != self.size:
            img = img.resize(self.size, Image.LANCZOS)
        b = io.BytesIO()
        img.convert("RGB").save(b, "JPEG", quality=self.quality)
        return b.getvalue()

    def add_jpeg(self, data):
        off = self.f.tell() - self.movi_start
        self.f.write(_chunk(b"00dc", data))
        self.frames.append((off, len(data)))
        self.max_len = max(self.max_len, len(data))
        self.last = data

    def add_image(self, img):
        self.add_jpeg(self._jpeg(img))

    def clip(self, pattern, start=0, end=None, step=1, fade_in=0, fade_out=0, hold_last=0, crop=None):
        files = sorted(glob.glob(pattern))[start:end:step]
        n = len(files)
        for i, fn in enumerate(files):
            a = 1.0
            if fade_in and i < fade_in:
                a = min(a, (i + 1) / (fade_in + 1))
            if fade_out and i >= n - fade_out:
                a = min(a, (n - i) / (fade_out + 1))
            if a >= 1.0 and not crop and not self.reencode and fn.lower().endswith((".jpg", ".jpeg")) and Image.open(fn).size == self.size:
                with open(fn, "rb") as fh:
                    self.add_jpeg(fh.read())
            else:
                img = Image.open(fn).convert("RGB")
                if crop:
                    img = img.crop(crop).resize(self.size, Image.LANCZOS)
                if a < 1.0:
                    img = Image.blend(Image.new("RGB", img.size), img, a)
                self.add_image(img)
        if hold_last and files:
            data = self.last
            for _ in range(hold_last):
                self.add_jpeg(data)
        return n

    def black(self, count):
        data = self._jpeg(Image.new("RGB", self.size))
        for _ in range(count):
            self.add_jpeg(data)

    def still(self, path, count, fade_in=0, fade_out=0):
        img = Image.open(path).convert("RGB")
        for i in range(count):
            a = 1.0
            if fade_in and i < fade_in:
                a = (i + 1) / (fade_in + 1)
            if fade_out and i >= count - fade_out:
                a = (count - i) / (fade_out + 1)
            self.add_image(img if a >= 1.0 else Image.blend(Image.new("RGB", img.size), img, a))

    # ---------------------------------------------------------------- container
    def close(self):
        movi_end = self.f.tell()
        idx = b"".join(b"00dc" + struct.pack("<III", 0x10, off, ln) for off, ln in self.frames)
        self.f.write(_chunk(b"idx1", idx))
        end = self.f.tell()
        w, h = self.size
        n = len(self.frames)
        us = int(1_000_000 / self.fps)
        avih = struct.pack("<IIIIIIIIII4I", us, self.max_len * self.fps, 0, 0x10, n, 0, 1, self.max_len, w, h, 0, 0, 0, 0)
        strh = b"vids" + b"MJPG" + struct.pack("<IHHIIIIIIiI4h", 0, 0, 0, 0, 1, self.fps, 0, n, self.max_len, -1, 0, 0, 0, w, h)
        strf = struct.pack("<IiiHH4sIiiII", 40, w, h, 1, 24, b"MJPG", w * h * 3, 0, 0, 0, 0)
        strl = b"LIST" + struct.pack("<I", 4 + 8 + 56 + 8 + 40) + b"strl" + _chunk(b"strh", strh) + _chunk(b"strf", strf)
        hdrl_body = b"hdrl" + _chunk(b"avih", avih) + strl
        hdrl = b"LIST" + struct.pack("<I", len(hdrl_body)) + hdrl_body
        movi_head = b"LIST" + struct.pack("<I", movi_end - self.movi_start) + b"movi"
        riff = b"RIFF" + struct.pack("<I", end - 8) + b"AVI "
        header = riff + hdrl + movi_head
        assert len(header) == self.header_len, (len(header), self.header_len)
        self.f.seek(0)
        self.f.write(header)
        self.f.close()
        secs = n / self.fps
        print(f"{self.path}: {n} frames, {secs:.1f} s, {end / 1e6:.0f} MB")
