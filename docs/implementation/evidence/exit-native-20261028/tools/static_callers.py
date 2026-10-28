from pathlib import Path
import ctypes as C
import json
import struct
from win_debug import *

image=Path(r'C:\Users\wuyu\My\Windward\Builds\Windows64\B02-0242\UnityPlayer.dll')
data=image.read_bytes()
nt=struct.unpack_from('<I',data,0x3c)[0]
count=struct.unpack_from('<H',data,nt+6)[0]
section_table=nt+24+struct.unpack_from('<H',data,nt+20)[0]
process=bind(K,'GetCurrentProcess',W.HANDLE)()
symbols=Symbols(process,r'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_player_development_mono')
base=symbols.load(str(image),0x180000000)
symbols.enum(base,'*GetScreenManager*',10)
callers=[]
for i in range(count):
    offset=section_table+40*i
    name=data[offset:offset+8].rstrip(b'\0').decode()
    vsize,va,rawsize,raw=struct.unpack_from('<4I',data,offset+8)
    if name!='.text':continue
    code=data[raw:raw+rawsize]
    p=0
    while True:
        p=code.find(b'\xe8',p)
        if p<0 or p+5>len(code):break
        target=va+p+5+struct.unpack_from('<i',code,p+1)[0]
        if target==0x1dd2e0:
            callers.append({'rva':hex(va+p),'symbol':symbols.resolve(base+va+p),'bytes':code[max(0,p-8):p+16].hex(' ')})
        p+=1
out={'targetAliases':['PlatformAccessibilityTestUtilities::GetWindow','ExternalGPUProfiler::GetGameViewWindowHandle'],
     'relativeCallSites':callers}
Path('docs/implementation/evidence/exit-native-20261028/static-callers.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
print(json.dumps(out,indent=2))
symbols.close()
