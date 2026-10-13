import struct, sys, io
from PIL import Image
d=open(sys.argv[1],"rb").read()
assert d[:4]==b"RIFF" and d[8:12]==b"AVI ", "not AVI"
riff_size=struct.unpack("<I",d[4:8])[0]; assert riff_size+8==len(d), ("riff size", riff_size+8, len(d))
def walk(off,end,depth=0,out=None):
    while off<end:
        fcc=d[off:off+4]; size=struct.unpack("<I",d[off+4:off+8])[0]
        if fcc==b"LIST":
            kind=d[off+8:off+12]; print("  "*depth+f"LIST {kind} {size}")
            if kind!=b"movi": walk(off+12,off+8+size,depth+1,out)
            else: out["movi"]=off+8
        else:
            print("  "*depth+f"{fcc} {size}")
            if fcc==b"avih": out["avih"]=struct.unpack("<14I",d[off+8:off+8+56])
            if fcc==b"strh": out["strh"]=d[off+8:off+16]
            if fcc==b"idx1": out["idx"]=(off+8,size)
        off+=8+size+(size&1)
out={}
walk(12,len(d),0,out)
av=out["avih"]; print("avih us/frame",av[0],"frames",av[4],"streams",av[6],"size",av[8],av[9])
io_,sz=out["idx"]; n=sz//16; movi=out["movi"]
bad=0
for i in range(n):
    ck,fl,o,l=struct.unpack("<4sIII",d[io_+i*16:io_+i*16+16])
    p=movi+o
    if d[p:p+4]!=ck or struct.unpack("<I",d[p+4:p+8])[0]!=l: bad+=1
    if i in (0,n//2,n-1): Image.open(io.BytesIO(d[p+8:p+8+l])).load()
print("index entries",n,"bad",bad)
