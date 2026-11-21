"""A small client for PhotoCraft's headless server (photocraft-cli serve: JSON lines on stdio).
  with PhotoCraft() as pc:
      pc.call("doc.new", {"width": 1280, "height": 720})
      pc.run("type.create", {...})          # engine.execute: one engine command (photocraft-cli commands --json lists them)
      pc.call("doc.save", {"path": out})
Paths: relative to the read / write roots given at start, with forward slashes (it refuses drives and backslashes)."""
import json, os, subprocess

EXE = r"C:\Program Files\PhotoCraft\photocraft-cli.exe"


def fwd(path): return os.path.abspath(path).replace("\\", "/")


class PhotoCraft:
    # read_root / write_root: the folders it may read and write; paths given to it are relative to those
    def __init__(self, read_root, write_root, verbose=False):
        self.verbose = verbose
        self.p = subprocess.Popen([EXE, "serve", "--automation-read-root", fwd(read_root), "--automation-write-root", fwd(write_root)], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                  text=True, encoding="utf-8")
        self.n = 0

    def call(self, method, params=None, check=True):
        self.n += 1
        self.p.stdin.write(json.dumps({"id": self.n, "method": method, "params": params or {}}, ensure_ascii=False) + "\n")
        self.p.stdin.flush()
        line = self.p.stdout.readline()
        if not line: raise RuntimeError("PhotoCraft closed: " + self.p.stderr.read()[-2000:])
        r = json.loads(line)
        if self.verbose: print(method, "->", line[:300])
        if check and not r.get("ok"): raise RuntimeError(f"{method} {json.dumps(params, ensure_ascii=False)[:300]}: {r.get('error')}")
        return r.get("result")

    def run(self, command, params=None, check=True):
        return self.call("engine.execute", {"command": command, "params": params or {}}, check)

    def close(self):
        try: self.p.stdin.close()
        except Exception: pass
        try: self.p.wait(5)
        except Exception: self.p.kill()

    def __enter__(self): return self
    def __exit__(self, *a): self.close()
