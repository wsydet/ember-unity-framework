import hashlib,json,struct,uuid
from pathlib import Path
root=Path('docs/implementation/evidence/exit-native-20261028')
wind=Path(r'C:\Users\wuyu\My\Windward')
editor=Path(r'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_player_development_mono')
paths=[wind/'Builds/Windows64/B02-0242/UnityPlayer.dll',Path('Builds/B03/Protected/UnityPlayer.dll'),editor/'UnityPlayer.dll',editor/'UnityPlayer_Win64_player_development_mono_x64.pdb',wind/'Packages/manifest.json',wind/'Packages/packages-lock.json',wind/'ProjectSettings/EmberTemplateBaseline~/template.json',wind/'Builds/Windows64/B02-0242/Windward_Data/Managed/Game.Windward.dll',Path('Builds/B03/Protected/EmberRegression_Data/Managed/Ember.Regression.dll')]
identity=[]
paths += [wind/'Builds/Windows64/B02-0242/Windward_Data/Managed/Assembly-CSharp.dll',wind/'Builds/Windows64/B02-0242/Windward_Data/Managed/Game.WindwardPlayer.Runtime.dll',Path('Builds/B03/Protected/EmberRegression_Data/globalgamemanagers'),Path('Builds/B03/Protected/EmberRegression_Data/level0')]
for p in paths:
    if not p.exists():continue
    b=p.read_bytes();item={'path':str(p.resolve()),'size':len(b),'sha256':hashlib.sha256(b).hexdigest()}
    if p.suffix.lower()=='.dll' and b[:2]==b'MZ':
        # Record actual CodeView identity where present; PDB file hash is separate.
        i=b.find(b'RSDS')
        if i>=0 and i+24<len(b):
            item['codeview']={'guid':str(uuid.UUID(bytes_le=b[i+4:i+20])),'age':struct.unpack_from('<I',b,i+20)[0],'path':b[i+24:b.find(b'\0',i+24)].decode(errors='replace')}
    identity.append(item)
(root/'identity.json').write_text(json.dumps(identity,indent=2,ensure_ascii=False),encoding='utf-8')
matrix=[]
for folder in sorted(root.iterdir()):
    if not folder.is_dir():continue
    p=folder/'process.json'
    if p.exists():matrix.append({'case':folder.name,**json.loads(p.read_text(encoding='utf-8'))})
    elif (folder/'native-events.jsonl').exists():
        events=[json.loads(x) for x in (folder/'native-events.jsonl').read_text(encoding='utf-8').splitlines()]
        matrix.append({'case':folder.name,'exitCode':None,'lastEvent':events[-1]['kind'] if events else None})
(root/'matrix.json').write_text(json.dumps(matrix,indent=2),encoding='utf-8')
print(json.dumps([{'case':x['case'],'exit':x.get('exitHex'),'last':x.get('lastEvent')} for x in matrix]))
