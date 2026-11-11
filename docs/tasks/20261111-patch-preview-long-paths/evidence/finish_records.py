from pathlib import Path
import json,re
task=Path(__file__).resolve().parent.parent
identity=json.loads((task/'evidence/source-identity.json').read_text(encoding='utf-8'))
p=task/'consumer-validation-prompt.md'
with p.open('a',encoding='utf-8') as f:
    f.write('\n| 包内路径 | 原始 SHA256 |\n|---|---|\n')
    for e in identity['entries']: f.write('| `'+e['path']+'` | `'+e['sha256']+'` |\n')
# Scoped record/backup link checks, not a substitute for Unity behavior.
checks=[]
for name in ['implementation.md','consumer-validation-prompt.md']:
    for target in re.findall(r'\]\(([^)]+)\)',(task/name).read_text(encoding='utf-8')):
        if '://' in target: continue
        dest=(task/target.split('#')[0]).resolve()
        checks.append({'document':name,'target':target,'exists':dest.exists()})
(task/'evidence/record-link-check.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
assert all(c['exists'] for c in checks),checks
print('record links valid: '+str(len(checks))+'; no archive/cleanup: AC06 pending')
