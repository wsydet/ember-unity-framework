from pathlib import Path
import ctypes as C
import json
from win_debug import Symbols, bind, K, W, pe_bytes, SymEnum, SymEnumCallback

image=Path(r'C:\Users\wuyu\My\Windward\Builds\Windows64\B02-0242\UnityPlayer.dll')
search=r'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_player_development_mono'
current=bind(K, 'GetCurrentProcess', W.HANDLE)()
s=Symbols(current, search)
base=s.load(str(image),0x180000000)
result={'fault':s.resolve(base+0x1dd2e9),'bytesAtFunction':pe_bytes(image,0x1dd2e0,96).hex(' '),
        'profilerFunctions':s.enum(base,'*ExternalGPUProfiler*'),
        'gfxDestruction':s.enum(base,'*DestroyGfx*'),
        'windowDestruction':s.enum(base,'*DestroyWindow*')}
result['faultAfterSymbolLoad']=s.resolve(base+0x1dd2e9)
result['callee']=s.resolve(base+0x2304540)
result['calleeBytes']=pe_bytes(image,0x2304540,48).hex(' ')
result['screenFunctions']=s.enum(base,'*ScreenManager*',500)
aliases=[]
@SymEnumCallback
def alias_callback(info,size,user):
    address=C.c_ulonglong.from_address(info+56).value
    if address==base+0x1dd2e0:
        aliases.append(C.string_at(info+84).decode(errors='replace'))
    return True
SymEnum(s.process,base,b'*',alias_callback,None)
result['sameAddressAliases']=aliases
result['screenManagerGlobalRva']='0x52185c0'
out=Path('docs/implementation/evidence/exit-native-20261028/symbol-inventory.json')
out.write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='screenFunctions'},indent=2))
print(json.dumps([x for x in result['screenFunctions'] if any(t in x['symbol'] for t in ['Destroy','Cleanup','Shutdown','~','gScreen','SetScreenManager'])],indent=2))
s.close()
