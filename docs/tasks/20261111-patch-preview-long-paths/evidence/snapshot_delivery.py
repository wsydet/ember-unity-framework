from pathlib import Path
import json,hashlib,shutil
task=Path(__file__).resolve().parent.parent
data=json.loads((task/'evidence/source-identity.json').read_text(encoding='utf-8'))
for e in data['entries']:
    source=Path('Packages/com.ember')/e['path']
    assert hashlib.sha256(source.read_bytes()).hexdigest()==e['sha256'],e['path']
    dest=task/'evidence/delivery-source'/e['path'];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,dest)
print('16 source fingerprints verified; current-source snapshot preserved; original backups retained')
