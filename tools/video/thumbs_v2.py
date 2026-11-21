"""Thumbnails in the style of the channel's video that did well ("너무 안 나와서 직접 만들었다": a story in big Korean
words on the left, a face on the right, small scenes with labels), for the release video's A/B test.
usage: python tools/video/thumbs_v2.py <frames dir>   (frames from the clips: lap.jpg, hammer2.jpg, cards.jpg, ...)
writes dev_output/trailer/final/thumbs_v2/thumb_<A|B|C>.jpg (1280x720)"""
import os, sys
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
FR = sys.argv[1]
OUT = os.path.join(REPO, "dev_output", "trailer", "final", "thumbs_v2")
os.makedirs(OUT, exist_ok=True)
W, H = 1280, 720
BOLD = "C:/Windows/Fonts/malgunbd.ttf"
LAT = "C:/Windows/Fonts/segoeuib.ttf"
PINK = (255, 92, 190)
YELLOW = (255, 214, 70)
INK = (34, 16, 44)


def font(size, f=BOLD): return ImageFont.truetype(f, size)


def frame(name): return Image.open(os.path.join(FR, name + ".jpg")).convert("RGB")


def cover(im, w, h, fx=0.5, fy=0.5, zoom=1.0):
    """scale to cover w x h (times zoom), crop around (fx, fy)"""
    s = max(w / im.width, h / im.height) * zoom
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    x = min(max(0, round(im.width * fx - w / 2)), im.width - w)
    y = min(max(0, round(im.height * fy - h / 2)), im.height - h)
    return im.crop((x, y, x + w, y + h))


def base(hero, fx, fy, zoom, panel=0.62):
    im = cover(frame(hero), W, H, fx, fy, zoom)
    # a dark violet panel on the left that fades out towards the face
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(shade)
    edge = int(W * panel)
    for x in range(W):
        t = 1.0 if x < edge - 260 else max(0.0, (edge - x) / 260)
        a = int(235 * (t * t * (3 - 2 * t)))
        d.line([(x, 0), (x, H)], fill=(30, 8, 40, a))
    im = Image.alpha_composite(im.convert("RGBA"), shade)
    return im


def text(d, xy, s, size, fill=(255, 255, 255), stroke=INK, sw=None, f=BOLD, anchor="la"):
    d.text(xy, s, font=font(size, f), fill=fill, stroke_width=sw if sw is not None else max(3, size // 14),
           stroke_fill=stroke, anchor=anchor)


def label(d, x, y, s, size, bg=YELLOW, fg=INK, pad=(16, 8), radius=10, f=BOLD):
    fo = font(size, f)
    l, t, r, b = d.textbbox((0, 0), s, font=fo)
    d.rounded_rectangle((x, y, x + r - l + pad[0] * 2, y + b - t + pad[1] * 2), radius=radius, fill=bg)
    d.text((x + pad[0] - l, y + pad[1] - t), s, font=fo, fill=fg)
    return x + r - l + pad[0] * 2


def inset(im, name, cap, x, y, w=230, h=130, fx=0.5, fy=0.5, zoom=1.0):
    pic = cover(frame(name), w, h, fx, fy, zoom)
    d = ImageDraw.Draw(im)
    d.rectangle((x - 4, y - 4, x + w + 4, y + h + 4), fill=(255, 255, 255))
    im.paste(pic, (x, y))
    fo = font(24)
    l, t, r, b = d.textbbox((0, 0), cap, font=fo)
    d.rectangle((x - 4, y + h - (b - t) - 14, x + r - l + 18, y + h + 4), fill=PINK)
    d.text((x + 7 - l, y + h - (b - t) - 7 - t), cap, font=fo, fill=(255, 255, 255))


def save(im, name):
    # (always said, small, in the corner YouTube leaves free: not AIHASTO's official mode)
    d = ImageDraw.Draw(im)
    fo = font(24)
    s = "비공식 팬 모드 · UNOFFICIAL"
    l, t, r, b = d.textbbox((0, 0), s, font=fo)
    x0 = W - (r - l) - 40
    d.rounded_rectangle((x0 - 12, 22, W - 22, 22 + (b - t) + 18), radius=8, fill=(20, 8, 28))
    d.text((x0 - l, 31 - t), s, font=fo, fill=(255, 255, 255))
    p = os.path.join(OUT, name)
    im.convert("RGB").save(p, quality=92)
    print(p, os.path.getsize(p) // 1024, "KB")


# A: the release, said plainly - "드디어 출시" + what is new in it
im = base("lap", 0.367, 0.456, 1.5)
d = ImageDraw.Draw(im)
label(d, 44, 40, "미사이드 평화모드", 34)
text(d, (40, 104), "드디어", 116)
text(d, (40, 238), "출시!", 150, fill=PINK, stroke=(255, 255, 255), sw=7)
text(d, (46, 420), "무료 · 자동 설치 프로그램", 40, fill=YELLOW)
text(d, (48, 476), "MiSide PEACEFUL MODE — OUT NOW", 28, f=LAT)
inset(im, "hammer2", "망치 게임", 40, 560, fx=0.4, fy=0.5, zoom=1.6)
inset(im, "cards", "카드 게임", 300, 560, fy=0.45, zoom=1.2)
inset(im, "cook", "같이 요리", 560, 560, fx=0.4, fy=0.35, zoom=1.1)
save(im, "thumb_A.jpg")

# B: the feeling - her lap, her eyes on you
im = base("lap", 0.38, 0.47, 1.62, panel=0.58)
d = ImageDraw.Draw(im)
label(d, 44, 40, "미사이드 평화모드 출시", 34)
text(d, (40, 110), "공포 없이", 104)
text(d, (40, 236), "미타랑", 128, fill=PINK, stroke=(255, 255, 255), sw=6)
text(d, (40, 384), "하루 종일", 104)
x = label(d, 46, 540, "무료 설치", 40, bg=PINK, fg=(255, 255, 255))
label(d, x + 14, 540, "미타 7명", 40)
text(d, (48, 628), "NO HORROR, JUST A DAY WITH MITA", 30, f=LAT)
save(im, "thumb_B.jpg")

# C: the follow-up to the video that did well - "그 평화모드, 이제 직접 해 보세요"
im = base("hold", 0.387, 0.43, 1.5, panel=0.6)
d = ImageDraw.Draw(im)
label(d, 44, 40, "미사이드 평화모드", 34)
text(d, (40, 108), "이제 직접", 112)
text(d, (40, 238), "해 보세요", 120, fill=PINK, stroke=(255, 255, 255), sw=6)
text(d, (46, 392), "설치 프로그램 클릭 한 번", 42, fill=YELLOW)
text(d, (48, 450), "YOU CAN PLAY IT NOW (FREE)", 28, f=LAT)
inset(im, "lap", "무릎베개", 40, 560, fx=0.5, fy=0.4, zoom=1.5)
inset(im, "cards", "카드 게임", 300, 560, fy=0.45, zoom=1.2)
inset(im, "bed", "같이 잠자기", 560, 560, fx=0.45, fy=0.45, zoom=1.1)
save(im, "thumb_C.jpg")
