"""Lists every Korean text of the mod (the keys of the translation tables).

python lang/extract.py            -> lang/source.tsv  (index, context, Korean text with \\n escaped)
python lang/extract.py --missing en  -> the texts lang/en.json doesn't have yet

Context = file and the nearest enclosing persona case / method, to know whose voice a line is.
"""
import json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "PeaceMode")
SKIP_FILES = {"DevRunner.cs", "TraceHooks.cs", "DevWalk.cs"}
HANGUL = re.compile("[가-힣]")


def literals(code):
    """(position, value) of every string literal outside comments: "..." and @"..." (not $"...")."""
    i, n = 0, len(code)
    while i < n:
        c = code[i]
        if code.startswith("//", i):
            j = code.find("\n", i)
            i = n if j < 0 else j
            continue
        if code.startswith("/*", i):
            j = code.find("*/", i)
            i = n if j < 0 else j + 2
            continue
        if c == "'":   # char literal
            j = i + 1
            while j < n and code[j] != "'":
                j += 2 if code[j] == "\\" else 1
            i = j + 1
            continue
        if c == '"' or (c in "@$" and i + 1 < n and code[i + 1] in '"@$'):
            k = i
            verbatim = interp = False
            while code[k] in "@$":
                verbatim |= code[k] == "@"
                interp |= code[k] == "$"
                k += 1
            if code[k] != '"':
                i += 1
                continue
            j = k + 1
            buf = []
            while j < n:
                ch = code[j]
                if verbatim:
                    if ch == '"':
                        if j + 1 < n and code[j + 1] == '"':
                            buf.append('"'); j += 2; continue
                        break
                    buf.append(ch); j += 1; continue
                if ch == "\\":
                    nx = code[j + 1]
                    buf.append({"n": "\n", "t": "\t", '"': '"', "\\": "\\", "r": "\r", "0": "\0"}.get(nx, nx))
                    j += 2; continue
                if ch == '"':
                    break
                buf.append(ch); j += 1
            if not interp:
                yield k, "".join(buf)
            i = j + 1
            continue
        i += 1


def context_at(code, pos, fname):
    head = code[:pos]
    ctx = fname[:-3]
    m = list(re.finditer(r'static\s+\w[\w<>\[\], ]*\s+(\w+)\s*\(', head))
    if m: ctx += ":" + m[-1].group(1)
    c = list(re.finditer(r'case\s+"(\w+)"', head))
    d = list(re.finditer(r'\b_\s*=>|default\s*:', head))
    if c and (not d or c[-1].start() > d[-1].start()): ctx += ":" + c[-1].group(1)
    elif d and m and d[-1].start() > m[-1].start(): ctx += ":crazy"
    return ctx


def collect():
    seen, out = set(), []
    for fname in sorted(os.listdir(SRC)):
        if not fname.endswith(".cs") or fname in SKIP_FILES: continue
        code = open(os.path.join(SRC, fname), encoding="utf-8").read()
        for pos, val in literals(code):
            if not HANGUL.search(val) or val in seen: continue
            line = code[code.rfind("\n", 0, pos) + 1: code.find("\n", pos)]
            if "cfg.Bind(" in line or "Log.Log" in line: continue
            seen.add(val)
            out.append((context_at(code, pos, fname), val))
    return out


def esc(s): return s.replace("\\", "\\\\").replace("\n", "\\n").replace("\t", "\\t")


if __name__ == "__main__":
    items = collect()
    if len(sys.argv) > 2 and sys.argv[1] == "--missing":
        path = os.path.join(ROOT, "lang", sys.argv[2] + ".json")
        have = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {}
        items = [it for it in items if it[1] not in have]
    with open(os.path.join(ROOT, "lang", "source.tsv"), "w", encoding="utf-8", newline="\n") as f:
        for i, (ctx, val) in enumerate(items):
            f.write(f"{i}\t{ctx}\t{esc(val)}\n")
    print(len(items), "texts")
