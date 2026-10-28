import hashlib,json,struct
import sys
from pathlib import Path
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path('docs/implementation/evidence/exit-native-20261028/debugger-uia-once-dump')
items=[]
for p in sorted(root.glob('*.dmp')):
    b=p.read_bytes();count,directory=struct.unpack_from('<II',b,8)
    streams=[struct.unpack_from('<III',b,directory+12*i) for i in range(count)]
    _,size,rva=next(s for s in streams if s[0]==6)
    tid,=struct.unpack_from('<I',b,rva)
    code,=struct.unpack_from('<I',b,rva+8)
    addr,=struct.unpack_from('<Q',b,rva+24)
    contextsize,context=struct.unpack_from('<II',b,rva+160)
    item={'file':p.name,'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest(),'signature':b[:4].decode(),'streams':count,'threadId':tid,'code':hex(code),'address':hex(addr),'rax':hex(struct.unpack_from('<Q',b,context+120)[0]),'rip':hex(struct.unpack_from('<Q',b,context+248)[0]),'parameters':[hex(x) for x in struct.unpack_from('<QQ',b,rva+40)]}
    items.append(item)
(root/'dump-verification.json').write_text(json.dumps(items,indent=2),encoding='utf-8')
print(json.dumps(items,indent=2))
