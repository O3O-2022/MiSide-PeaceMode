"""Merges translated chunks into the table the plugin embeds (lang/<code>.json: Korean text -> translation).

Adding texts later:
  python lang/extract.py --missing en   -> lang/source.tsv lists only what en.json lacks (index, context, text)
  write lang/en/<any>.tsv, lines "index<TAB>translation" (\\n escaped), for those indices
  python lang/build.py en               -> merged into lang/en.json; then delete lang/en/*.tsv (its indices only
                                           mean something against that source.tsv)
Lines left out stay Korean-only on purpose (Korean word games, particles). Checks that every {placeholder} and
[player] survived.
"""
import glob, json, os, re, sys

ROOT = os.path.dirname(os.path.abspath(__file__))
TOKENS = re.compile(r"\{\w*|\[player\]")   # {name (a format like {2:00} may change)

# not in source.tsv: the diary joiner (Loc.T(", ")) and the peace page's back button (the game's own word)
EXTRA = {"en": {", ": ", and ", "돌아가기": "Back"}, "ja": {", ": "、", "돌아가기": "戻る"}, "zh": {", ": "，还", "돌아가기": "返回"}}


def unesc(s):
    out, i = [], 0
    while i < len(s):
        if s[i] == "\\" and i + 1 < len(s):
            out.append({"n": "\n", "t": "\t", "\\": "\\"}.get(s[i + 1], s[i + 1])); i += 2
        else:
            out.append(s[i]); i += 1
    return "".join(out)


def main(lang):
    source = {}
    for line in open(os.path.join(ROOT, "source.tsv"), encoding="utf-8"):
        idx, ctx, text = line.rstrip("\n").split("\t", 2)
        source[int(idx)] = unesc(text)
    path = os.path.join(ROOT, lang + ".json")
    table = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {}
    bad = 0
    for chunk in sorted(glob.glob(os.path.join(ROOT, lang, "*.tsv"))):
        for n, line in enumerate(open(chunk, encoding="utf-8"), 1):
            line = line.rstrip("\n")
            if not line.strip(): continue
            idx, text = line.split("\t", 1)
            ko, tr = source[int(idx)], unesc(text)
            kt, tt = TOKENS.findall(ko), TOKENS.findall(tr)
            numbered = all(t[1:].isdigit() for t in kt + tt)   # string.Format args may be reordered or dropped
            if (numbered and not set(tt) <= set(kt) | {"{" + str(i) for i in range(10)}) or (not numbered and sorted(kt) != sorted(tt)):
                print(f"{os.path.basename(chunk)}:{n} [{idx}] placeholders differ: {ko!r} -> {tr!r}"); bad += 1
            table[ko] = tr
    table.update(EXTRA.get(lang, {}))
    json.dump(table, open(path, "w", encoding="utf-8", newline="\n"), ensure_ascii=False, indent=0, sort_keys=True)
    missing = [i for i, ko in source.items() if ko not in table]
    print(f"{lang}.json: {len(table)} texts, {len(missing)} of {len(source)} left Korean, {bad} placeholder problems")
    return missing


if __name__ == "__main__":
    miss = main(sys.argv[1])
    if "-v" in sys.argv:
        src = {int(l.split("\t", 2)[0]): l.rstrip("\n") for l in open(os.path.join(ROOT, "source.tsv"), encoding="utf-8")}
        for i in miss: print(src[i])
