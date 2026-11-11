from pathlib import Path
import re,json
task=Path(__file__).resolve().parent.parent
results=[]
for name in ['README.md','requirements.md','design.md','implementation.md','handoff.md','consumer-validation-prompt.md','evidence/io-audit.md']:
    for target in re.findall(r'\]\(([^)]+)\)',(task/name).read_text(encoding='utf-8')):
        if target.startswith(('http:','https:','#')): continue
        target=target.split('#')[0]
        dest=((task/name).parent/target).resolve()
        results.append({'document':name,'target':target,'exists':dest.exists(),'dependsOnTemporaryDirectory':'.utmp/ember-workflow/20261111-patch-preview-long-paths' in dest.as_posix()})
assert all(e['exists'] and not e['dependsOnTemporaryDirectory'] for e in results),results
(task/'evidence/archive-link-check.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
print('Verified '+str(len(results))+' formal local links; no temporary dependency')
