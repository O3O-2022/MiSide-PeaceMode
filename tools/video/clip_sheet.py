"""A contact sheet of one rendered clip: a frame every <step> seconds with its time (to choose where to cut).
usage: python tools/video/clip_sheet.py <clip.mp4> [step=1.0] [out.jpg]"""
import os, subprocess, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import assemble as A

src = sys.argv[1]
step = float(sys.argv[2]) if len(sys.argv) > 2 else 1.0
out = sys.argv[3] if len(sys.argv) > 3 else os.path.splitext(src)[0] + "_sheet.jpg"
vf = (f"fps={1 / step},scale=480:270:flags=lanczos,"
      rf"drawtext=fontfile='C\:/Windows/Fonts/arialbd.ttf':text='%{{eif\:t\:d}}.%{{eif\:mod(t*10\,10)\:d}}s':x=8:y=6:fontsize=22:fontcolor=yellow:borderw=2,tile=6x4")
subprocess.run([A.FF, "-hide_banner", "-loglevel", "error", "-y", "-i", src, "-vf", vf, "-frames:v", "1", "-q:v", "4", out], check=True)
print(out)
