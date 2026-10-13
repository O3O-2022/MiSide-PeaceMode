"""Build the README GIFs and roster (docs/media) from the recorded frames.

usage: python tools/recording/build_media.py [frames folder]
The frames come from record_A..D.txt (DevRunner `record`); without a folder, <game>/BepInEx/config/frames is used
(the game: MISIDE_DIR, or the folder around this repo, or D:\\Steam\\steamapps\\common\\MiSide).
"""
import glob
from PIL import Image, ImageDraw, ImageFont, ImageStat
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
from build_media_paths import game_dir

V = sys.argv[1] if len(sys.argv) > 1 else os.path.join(game_dir(), "BepInEx", "config", "frames")
OUT = os.path.join(REPO, "docs", "media")
FONT = "C:/Windows/Fonts/malgunbd.ttf"


def seq(prefix, start=6, end=None):
    # (from frame 6: a recording starts in the same frame as the action, before her pose has caught up)
    return sorted(glob.glob(os.path.join(V, prefix + "_[0-9][0-9][0-9][0-9].jpg")))[start:end]


def gif(name, files, width=420, ms=400, crop=None, hold=1500):
    if not files:
        print(f"{name}.gif  (no frames, skipped)")
        return
    ims = []
    for f in files:
        im = Image.open(f).convert("RGB")
        if crop: im = im.crop(crop)
        h = round(im.height * width / im.width)
        ims.append(im.resize((width, h), Image.LANCZOS))
    # (the palette from frames across the whole clip - one frame alone can be a black fade)
    picks = [ims[int(i * (len(ims) - 1) / 7)] for i in range(8)] if len(ims) > 1 else ims
    tw, th = picks[0].width // 2, picks[0].height // 2
    sheet = Image.new("RGB", (tw * 4, th * 2))
    for k, im in enumerate(picks): sheet.paste(im.resize((tw, th)), ((k % 4) * tw, (k // 4) * th))
    pal = sheet.quantize(colors=220, method=Image.MEDIANCUT)
    q = [im.quantize(palette=pal, dither=Image.Dither.NONE) for im in ims]
    dur = [ms] * len(q)
    dur[-1] = hold
    path = os.path.join(OUT, name + ".gif")
    q[0].save(path, save_all=True, append_images=q[1:], duration=dur, loop=0, optimize=True, disposal=1)
    print(f"{name}.gif  {len(q)} frames  {os.path.getsize(path) // 1024} KB")


def every(files, step):
    return files[::step]


def glitchy(f):
    r, g, b = ImageStat.Stat(Image.open(f).convert("RGB").resize((32, 18))).mean
    return r - (g + b) / 2


STEP = 12   # 0.4 s of the 30 fps recordings
FACE = (400, 0, 880, 360)      # Mita's head and shoulders in the showcase view

# crazy Mita (record_A)
gif("hero", every(seq("va_greet", 30), 15), width=480, ms=500)   # (the first second can still show a scene loading in)
gif("crazy_pat", every(seq("va_pat"), STEP))
gif("crazy_hug", every(seq("va_hug"), STEP))
gif("crazy_hold", every(seq("va_hold"), STEP))
# Alt+F4 reaction: keep the red flicker frames
alt = seq("va_altf4")
if alt:
    base = sum(glitchy(f) for f in alt[::30]) / len(alt[::30])
    pick = sorted(set(alt[::STEP]) | {f for f in alt[::2] if glitchy(f) > base + 12})
    print("altf4 glitch frames:", len(pick) - len(alt[::STEP]))
    gif("crazy_altf4", pick, ms=300)

# the others (record_B, record_C)
gif("kind_advice", every(seq("vb_kind_advice"), STEP))
gif("cap_guitar", every(seq("vb_cap_guitar"), STEP))
gif("cap_nod", every(seq("vb_cap_nod") + seq("vb_cap_shake"), 6), ms=200)
gif("mila_tea", every(seq("vb_mila_tea"), STEP))
gif("sleepy_coffee", every(seq("vb_sleepy_coffee"), STEP))
gif("short_lecture", every(seq("vc_short_lecture"), STEP))
gif("chibi_quiz", every(seq("vc_chibi_quiz") + seq("vc_chibi_answer"), STEP), crop=(256, 214, 1024, 646))
gif("meal", every(seq("vc_meal1") + seq("vc_meal2", 0, 540), 15), width=400, ms=500)
gif("bed", every(seq("vc_bed"), 15), width=400, ms=500)

# affection and languages (record_D)
gif("cold_pat", every(seq("vd_cold_pat"), STEP), width=360)
gif("contempt", every(seq("vd_contempt"), 6), width=360, ms=250, crop=FACE)
gif("aff_top", every(seq("vd_top"), STEP), width=360)
lang = []
for code in ("en", "ja", "zh"):
    lang += every(seq(f"vd_{code}_menu"), 20) + every(seq(f"vd_{code}_talk"), STEP)
gif("languages", lang, width=480)

# roster: one still per Mita from her greeting, plus the Mita select menu
roster = [("va_greet", "미치광이 미타"), ("vb_kind_greet", "착한 미타"), ("vb_cap_greet", "캐피 미타"), ("vb_mila_greet", "밀라"),
          ("vb_sleepy_greet", "잠꾸러기 미타"), ("vc_short_greet", "단발 미타"), ("vc_chibi_greet", "미니 미타")]
if all(seq(pre) for pre, _ in roster):
    tw = th = 400
    font = ImageFont.truetype(FONT, 30)
    sheet = Image.new("RGB", (tw * 4, th * 2), (255, 238, 246))
    for k, (pre, label) in enumerate(roster):
        fs = seq(pre)
        im = Image.open(fs[len(fs) // 3]).convert("RGB")
        box = (340, 20, 940, 620) if "chibi" not in pre else (490, 290, 790, 590)
        im = im.crop(box).resize((tw, th), Image.LANCZOS)
        d = ImageDraw.Draw(im)
        w = d.textlength(label, font=font)
        d.rounded_rectangle((tw / 2 - w / 2 - 14, th - 56, tw / 2 + w / 2 + 14, th - 12), radius=18, fill=(214, 64, 160))
        d.text((tw / 2 - w / 2, th - 53), label, font=font, fill="white")
        sheet.paste(im, ((k % 4) * tw, (k // 4) * th))
    menu = Image.open(os.path.join(OUT, "menu.jpg")).convert("RGB")
    cell = menu.crop((110, 70, 590, 640)).resize((400, 400 * 570 // 480))
    cell = cell.crop((0, (cell.height - 400) // 2, 400, (cell.height - 400) // 2 + 400))
    sheet.paste(cell, (3 * tw, th))
    sheet.save(os.path.join(OUT, "roster.jpg"), quality=85)
    print("roster.jpg", os.path.getsize(os.path.join(OUT, "roster.jpg")) // 1024, "KB")


# ---- every interaction (record_E..H: 10 fps recordings) ----
def g10(name, prefixes, step=4, ms=400, width=400, crop=None, start=4, cap=64, hold=1500):
    files = []
    for p in prefixes:
        files += seq(p, start)
    frames = every(files, step)
    if len(frames) > cap:   # (long scenes: fewer, evenly spread frames rather than a huge file)
        k = len(frames) / cap
        frames = [frames[int(i * k)] for i in range(cap)]
        ms = int(ms * k)
    gif(name, frames, width=width, ms=ms, crop=crop, hold=hold)

# talking, looking, leaving
g10("talk_gestures", ["va_talk"])
g10("kind_shy", ["vb_kind_shy"], step=3, ms=300)
g10("crazy_bye", ["va_bye"])
# the menus: talk, touch, play
g10("ask", ["ve_ask"])
g10("compliment", ["ve_compliment"])
g10("glasses", ["ve_glasses"])
g10("clothes", ["ve_clothes"])
g10("rps", ["ve_rps"])
g10("words", ["ve_words"])
g10("riddle", ["ve_riddle"])
g10("cham", ["ve_cham"], step=3, ms=300)
g10("photo", ["ve_photo"])
g10("follow", ["vg_follow"])
# special time
g10("window", ["ve_window"])
g10("lap", ["vg_lap"])
g10("song", ["ve_song"])
g10("heart", ["ve_heart"])
# around the house
g10("spot", ["vf_spot"])
g10("request", ["vf_request"])
g10("hide", ["vf_hide"])
g10("diary", ["vf_diary"])
g10("cards", ["vf_cards"])
g10("cook", ["vf_cook1", "vf_cook2"])
g10("tvgame", ["vg_tvgame"])
# the sofa, her suggestions, the weather
# (her suggestion runs straight into the sofa: the first part is the suggestion, the rest the TV together)
gif("propose", every(seq("vg_propose", 4, 110), 4), width=400)
gif("sofa_tv", every(seq("vg_propose", 100), 4), width=400)
g10("sofa_read", ["vg_read"])
g10("sofa_rest", ["vg_rest"])
g10("sofa_alone", ["vg_alone", "vg_join"])
g10("rain", ["vg_rain"], step=2, ms=200)
g10("snow", ["vg_snow"], step=2, ms=200)
seasons = [os.path.join(V, f"season_{i}.png") for i in range(4)]
if all(os.path.exists(f) for f in seasons):
    font = ImageFont.truetype(FONT, 34)
    ims = []
    for f, label in zip(seasons, ["봄", "여름", "가을", "겨울"]):
        im = Image.open(f).convert("RGB").resize((480, 270), Image.LANCZOS)
        d = ImageDraw.Draw(im)
        d.rounded_rectangle((12, 12, 108, 62), radius=14, fill=(214, 64, 160))
        d.text((24, 14), label, font=font, fill="white")
        ims.append(im)
    pal = ims[2].quantize(colors=220, method=Image.MEDIANCUT)
    q = [im.quantize(palette=pal, dither=Image.Dither.NONE) for im in ims]
    q[0].save(os.path.join(OUT, "seasons.gif"), save_all=True, append_images=q[1:], duration=1400, loop=0, optimize=True)
    print("seasons.gif", os.path.getsize(os.path.join(OUT, "seasons.gif")) // 1024, "KB")
# Cappie's minigames
g10("dance", ["vh_dance1", "vh_dance2"])
g10("dance_you", ["vh_dance3", "vh_dance4"])
g10("hammer", ["vh_hammer1"])
g10("hammer_round", ["vh_hammer2", "vh_hammer3"])
# gifts, her present, music, the rainy window, the real-world events, Kind Mita's story memory (record_K1, record_K2)
g10("gift", ["vk_gift"])
g10("gift_again", ["vk_gift_again"])
g10("gift_kind", ["vk_gift_kind"])
g10("present", ["vk_present"])
g10("music", ["vk_music"])
g10("window_rain", ["vk_window_rain"])
g10("battery", ["vk_battery"])
g10("photos_count", ["vk_photos"])
g10("dates", ["vk_dates"])
g10("story", ["vk_story"])
