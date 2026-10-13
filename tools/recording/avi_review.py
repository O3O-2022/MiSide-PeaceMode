import struct, io, sys
from PIL import Image, ImageDraw
d=open(sys.argv[1],"rb").read()
off=12; movi=None; idx=None
while off<len(d):
    fcc=d[off:off+4]; size=struct.unpack("<I",d[off+4:off+8])[0]
    if fcc==b"LIST" and d[off+8:off+12]==b"movi": movi=off+8
    if fcc==b"idx1": idx=(off+8,size)
    off+=8+size+(size&1)
n=idx[1]//16
def frame(i):
    ck,fl,o,l=struct.unpack("<4sIII",d[idx[0]+i*16:idx[0]+i*16+16])
    return Image.open(io.BytesIO(d[movi+o+8:movi+o+8+l])).convert("RGB")
picks=list(range(15,n,300))[:24]
tw,th=320,180
sheet=Image.new("RGB",(tw*6,th*((len(picks)+5)//6)))
dr=ImageDraw.Draw(sheet)
for k,i in enumerate(picks):
    sheet.paste(frame(i).resize((tw,th)),((k%6)*tw,(k//6)*th))
    dr.text(((k%6)*tw+4,(k//6)*th+4),f"{i/30:.0f}s",fill="yellow")
sheet.save(sys.argv[2],quality=80)
