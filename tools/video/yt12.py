"""YouTube materials for the v1.1 video (the clean cut, trailer12_fc.py with TRAILER_CLEAN=1): subtitles, titles and
descriptions in every language, and a thumbnail per language (PhotoCraft). Texts: i18n_trailer12.TR12.

usage: python tools/video/yt12.py subs | meta | thumbs | all
       -> dev_output/trailer/final/v11/subs/<yt code>.srt, meta/<yt code>.json, thumbs/thumbnail.<yt code>.jpg
"""
import json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import trailer12_fc as F
import assemble as A
from i18n_trailer import DOWNLOAD, REPO_URL, TR
from i18n_trailer12 import TR12

REPO = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(REPO, "dev_output", "trailer", "final", "v11")
YT = {"ko": "ko", "en": "en", "ja": "ja", "zh": "zh-Hans", "zht": "zh-Hant", "ru": "ru", "uk": "uk", "be": "be", "bg": "bg",
      "kk": "kk", "sr": "sr-Latn", "hr": "hr", "cs": "cs", "sk": "sk", "pl": "pl", "hu": "hu", "ro": "ro", "de": "de", "fr": "fr",
      "it": "it", "es": "es-ES", "esla": "es-419", "pt": "pt-PT", "ptbr": "pt-BR", "sv": "sv", "tr": "tr", "id": "id",
      "fil": "fil", "vi": "vi", "th": "th", "ar": "ar", "arz": "ar-EG", "fa": "fa"}
OLD_VIDEO = "https://youtu.be/mswlea_Ozpg"


def ts(sec):
    ms = int(round(sec * 1000))
    return f"{ms // 3600000:02d}:{ms // 60000 % 60:02d}:{ms // 1000 % 60:02d},{ms % 1000:03d}"


def cues(lang):
    """(start, end, text) for every caption run of the cut, as the Korean version shows them"""
    tl, _ = F.timeline()
    caps, names = TR12[lang]["caps"], A.G[lang]
    out, i = [], 0
    while i < len(tl):
        p = tl[i]
        j = i
        if p["key"] not in ("roster", "title", "end"):
            while j + 1 < len(tl) and tl[j + 1]["key"] == p["key"]: j += 1
        f0, f1 = p["f0"], tl[j]["f0"] + tl[j]["frames"]
        key = p["key"]
        if key == "roster":
            text = names[p["who"]]
            if i == next(k for k, q in enumerate(tl) if q["key"] == "roster"): text = caps["roster"] + "\n" + text
        elif key in ("title", "end"):
            text = None
        else:
            text = caps.get(key) or None
        if text: out.append(((f0 + 6) / F.FPS, (f1 - 4) / F.FPS, text))
        i = j + 1
    return out


def subs():
    d = os.path.join(OUT, "subs")
    os.makedirs(d, exist_ok=True)
    for lang in TR12:
        with open(os.path.join(d, YT[lang] + ".srt"), "w", encoding="utf-8", newline="\n") as f:
            for n, (a, b, t) in enumerate(cues(lang), 1):
                f.write(f"{n}\n{ts(a)} --> {ts(b)}\n{t}\n\n")
    print(len(TR12), "subtitle files in", d)


def desc(lang):
    t = TR12[lang]
    lines = [DOWNLOAD[lang], REPO_URL, "", t["intro"], ""] + ["• " + x for x in t["new"]] + [
        "", TR[lang]["notofficial"].format(pm=A.G[lang]["peace_mode"], cap=A.G[lang]["cap"], kind=A.G[lang]["kind"]) + ".", "", "v1.0 ▶ " + OLD_VIDEO, "", "#MiSide #Mita #mod"]
    return "\n".join(lines)


def meta():
    d = os.path.join(OUT, "meta")
    os.makedirs(d, exist_ok=True)
    for lang in TR12:
        m = {"title": TR12[lang]["title"], "description": desc(lang)}
        assert len(m["title"]) <= 100 and len(m["description"]) <= 5000, lang
        json.dump(m, open(os.path.join(d, YT[lang] + ".json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(len(TR12), "title/description files in", d)


def thumbs():
    import thumb12 as TH
    from pc import PhotoCraft
    d = os.path.join(TH.WORK, "lang")
    os.makedirs(d, exist_ok=True)
    TH.base()
    with PhotoCraft(TH.GAME, TH.WORK) as pc:
        for lang in TR12:
            t = TR12[lang]
            TH.build(pc, lang, A.G[lang]["peace_mode"], TR[lang]["unofficial"], t["thumb_tags"], t["thumb_big"],
                     name=f"lang/thumbnail.{YT[lang]}", psd=False)


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    if what in ("subs", "all"): subs()
    if what in ("meta", "all"): meta()
    if what in ("thumbs", "all"): thumbs()
