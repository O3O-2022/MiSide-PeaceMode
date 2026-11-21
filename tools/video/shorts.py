"""YouTube Shorts (1080x1920, 60 fps) cut from the release video's 4K clips, each pointing to the full video.
A title at the top for the whole short, a caption per cut in the lower third (above where the Shorts player puts its
own buttons and text), the clip's own sound with the menu theme under it.
usage: python tools/video/shorts.py [1 2 3]      -> dev_output/shorts/short_<n>.mp4"""
import os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import assemble as A

REPO = os.path.dirname(os.path.dirname(HERE))
CLIPS = os.path.join(REPO, "dev_output", "trailer", "ko")
OUT = os.path.join(REPO, "dev_output", "shorts")
W, H, FPS = 1080, 1920, 60
FONT = "Malgun Gothic"

# (clip, start s, length s, where the 9:16 window sits across the 16:9 picture 0..1, caption)
SHORTS = {
    1: dict(title=("미사이드 평화모드", "직접 만들었습니다"), cuts=[
        ("greet_crazy", 0.3, 2.8, 0.50, "공포 없이, 미타랑 하루"),
        ("hug", 0.2, 2.4, 0.50, "안아 주기"),
        ("lap", 11.4, 3.0, 0.50, "무릎베개"),
        ("cards", 1.5, 2.6, 0.50, "카드 게임"),
        ("hammer2", 2.4, 3.4, 0.48, "캐피랑 망치 게임"),
        ("cook", 1.0, 2.4, 0.22, "같이 요리"),
        ("bed", 7.0, 3.0, 0.70, "같이 잠자기"),
        ("bye", 2.6, 3.6, 0.50, "무료 설치 · 아래 관련 영상"),
    ]),
    2: dict(title=("Alt+F4로 끄면", "미타가 기억합니다"), cuts=[
        ("altf4", 0.0, 9.0, "fit", "Alt+F4로 끄고 다시 켜면…"),
        ("battery", 0.0, 7.0, "fit", "노트북 배터리 걱정도 해 줌"),
    ]),
    3: dict(title=("캐피한테", "망치 맞는 게임"), cuts=[
        ("hammer2", 0.0, 14.0, "fit", "먼저 버튼 누르는 쪽이 이김"),
        ("dance3", 3.0, 4.0, 0.50, "댄스 배틀도 있어요"),
    ]),
}


def ts(sec):
    cs = int(round(sec * 100))
    return f"{cs // 360000}:{cs // 6000 % 60:02d}:{cs // 100 % 60:02d}.{cs % 100:02d}"


def make_ass(n, spec, total):
    p = os.path.join(OUT, f"short_{n}.ass")
    head = f"""[Script Info]
ScriptType: v4.00+
PlayResX: {W}
PlayResY: {H}
WrapStyle: 2
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: T1,{FONT},92,&H00FFFFFF,&H00FFFFFF,&H00401030,&H96000000,1,0,0,0,100,100,0,0,1,7,4,8,40,40,170,1
Style: T2,{FONT},104,&H00BE5CFF,&H00FFFFFF,&H00FFFFFF,&H96000000,1,0,0,0,100,100,0,0,1,7,4,8,40,40,290,1
Style: Cap,{FONT},60,&H00FFFFFF,&H00FFFFFF,&H00301828,&HA0000000,1,0,0,0,100,100,0,0,3,16,0,8,60,60,430,1
Style: Tag,{FONT},30,&H00FFFFFF,&H00FFFFFF,&HB4140A1C,&HB4140A1C,1,0,0,0,100,100,0,0,3,10,0,7,36,36,92,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""
    ev = [f"Dialogue: 2,{ts(0)},{ts(total)},T1,,0,0,0,,{{\\fad(150,0)}}{spec['title'][0]}",
          f"Dialogue: 2,{ts(0.15)},{ts(total)},T2,,0,0,0,,{{\\fad(150,0)}}{spec['title'][1]}",
          f"Dialogue: 2,{ts(0)},{ts(total)},Tag,,0,0,0,, 비공식 팬 모드 · MiSide "]
    t = 0.0
    for clip, a, d, cx, cap in spec["cuts"]:
        if cap:
            ev.append(f"Dialogue: 1,{ts(t + 0.1)},{ts(t + d - 0.05)},Cap,,0,0,0,,{{\\fad(120,120)}} {cap} ")
        t += d
    open(p, "w", encoding="utf-8-sig").write(head + "\n".join(ev) + "\n")
    return p


def build(n):
    spec = SHORTS[n]
    os.makedirs(OUT, exist_ok=True)
    A.ensure_fonts()
    cuts = spec["cuts"]
    total = sum(c[2] for c in cuts)
    args, fc, vlabels, alabels = [], [], [], []
    for i, (clip, a, d, cx, cap) in enumerate(cuts):
        args += ["-ss", f"{a}", "-t", f"{d}", "-i", os.path.join(CLIPS, f"ko_{clip}.mp4"),
                 "-ss", f"{a}", "-t", f"{d}", "-i", os.path.join(CLIPS, f"ko_{clip}.wav")]
        v, s = 2 * i, 2 * i + 1
        if cx == "fit":
            # the whole 16:9 picture (her lines stay readable), a little zoomed, over a blurred, darkened fill of itself
            fc.append(f"[{v}:v]fps={FPS},trim=duration={d},setpts=PTS-STARTPTS,split[bg{i}][fg{i}]")
            fc.append(f"[bg{i}]crop=ih*9/16:ih,scale={W // 4}:{H // 4},boxblur=12:2,scale={W}:{H},eq=brightness=-0.18:saturation=0.8[bb{i}]")
            fc.append(f"[fg{i}]crop=iw*0.6:ih*0.92:iw*0.2:ih*0.06,scale={W}:-2:flags=lanczos[ff{i}]")
            fc.append(f"[bb{i}][ff{i}]overlay=0:(H-h)/2+110,setsar=1[v{i}]")
        else:
            # the 9:16 window (full height) placed at cx, kept inside the picture
            fc.append(f"[{v}:v]crop=ih*9/16:ih:max(0\\,min(iw-ih*9/16\\,iw*{cx}-ih*9/32)):0,scale={W}:{H}:flags=lanczos,fps={FPS},setsar=1,"
                      f"trim=duration={d},setpts=PTS-STARTPTS[v{i}]")
        fc.append(f"[{s}:a]aresample=48000,aformat=channel_layouts=stereo,atrim=duration={d},asetpts=PTS-STARTPTS,"
                  f"afade=t=in:d=0.04,afade=t=out:st={max(0.0, d - 0.06)}:d=0.06[a{i}]")
        vlabels.append(f"[v{i}]"); alabels.append(f"[a{i}]")
    k = len(cuts)
    music = 2 * k
    args += ["-ss", "4", "-t", f"{total}", "-i", os.path.join(CLIPS, "ko_theme.wav")]
    ass = make_ass(n, spec, total)
    fc.append("".join(v + a for v, a in zip(vlabels, alabels)) + f"concat=n={k}:v=1:a=1[vc][ac]")
    fc.append(f"[vc]ass='{A.filter_path(ass)}':fontsdir='{A.filter_path(A.FONTS)}',format=yuv420p[vo]")
    fc.append(f"[{music}:a]aresample=48000,aformat=channel_layouts=stereo,volume=0.30,afade=t=in:d=0.3,"
              f"afade=t=out:st={max(0.0, total - 1.2)}:d=1.2[m]")
    fc.append("[ac][m]amix=inputs=2:duration=first:normalize=0,loudnorm=I=-14:TP=-1.5:LRA=11[ao]")
    out = os.path.join(OUT, f"short_{n}.mp4")
    cmd = [A.FF, "-hide_banner", "-loglevel", "error", "-y"] + args + [
        "-filter_complex", ";".join(fc), "-map", "[vo]", "-map", "[ao]",
        "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-pix_fmt", "yuv420p", "-r", str(FPS),
        "-c:a", "aac", "-b:a", "256k", "-ar", "48000", "-movflags", "+faststart", out]
    subprocess.run(cmd, check=True)
    print(f"{out}: {total:.1f} s, {os.path.getsize(out) // 1024 // 1024} MB")


if __name__ == "__main__":
    for n in (sys.argv[1:] or ["1", "2", "3"]):
        build(int(n))
