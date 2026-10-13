"""Several clip sheets (clip_sheet.py) stacked into one picture, each labelled, black rows cut off.
usage: python tools/video/sheets_montage.py <out.jpg> <sheet.jpg>..."""
import os, sys
from PIL import Image, ImageDraw, ImageFont

out, files = sys.argv[1], sys.argv[2:]
font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 30)
parts = []
for f in files:
    im = Image.open(f).convert("RGB")
    rows = im.height // 270
    used = rows
    while used > 1 and im.crop((0, (used - 1) * 270, 480, used * 270)).getextrema() == ((0, 0), (0, 0), (0, 0)): used -= 1
    while used > 1 and max(m[1] for m in im.crop((0, (used - 1) * 270, 480, used * 270)).getextrema()) < 12: used -= 1
    im = im.crop((0, 0, im.width, used * 270))
    d = ImageDraw.Draw(im)
    name = os.path.basename(f).rsplit(".", 1)[0]
    d.text((im.width - 330, 8), name, font=font, fill="cyan", stroke_width=3, stroke_fill="black")
    parts.append(im)
w = max(p.width for p in parts)
sheet = Image.new("RGB", (w, sum(p.height for p in parts)))
y = 0
for p in parts:
    sheet.paste(p, (0, y)); y += p.height
sheet = sheet.resize((1920, round(sheet.height * 1920 / w)), Image.LANCZOS)
sheet.save(out, quality=82)
print(out, sheet.size)
