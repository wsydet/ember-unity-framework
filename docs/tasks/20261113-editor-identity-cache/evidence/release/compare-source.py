from pathlib import Path
import json, hashlib, subprocess

root = Path(__file__).resolve().parents[5]
task = root / 'docs/tasks/20261113-editor-identity-cache'
base = 'e31fedd240678d82dbd6fdb71f79a77d8deba70f'
entries = json.loads((task/'evidence/source-manifest.json').read_text())
for item in entries:
    assert hashlib.sha256((root/item['path']).read_bytes()).hexdigest() == item['sha256'], item['path']
tree = subprocess.check_output(['git','ls-tree','-r','-z',base,'--','Packages/com.ember'], cwd=root)
objects = {}
for entry in tree.split(b'\0'):
    if entry:
        info, path = entry.split(b'\t', 1)
        objects[path.decode()] = info.split()[2].decode()
ours = {item['path'] for item in entries}
paths = [p for p in objects if p.endswith(('.cs','.asmdef','.asmref')) and '/Templates~/' not in p]
paths += [item['path'] for item in entries if not item['new'] and item['path'] not in paths]
data = subprocess.check_output(['git','cat-file','--batch'], cwd=root,
    input=('\n'.join(objects[p] for p in paths)+'\n').encode())
offset = 0
normal = lambda b: b.decode('utf-8-sig').replace('\r\n','\n')
before_mismatch, other_diff = [], []
for path in paths:
    end = data.index(b'\n', offset)
    size = int(data[offset:end].split()[2])
    blob = data[end+1:end+1+size]
    offset = end+2+size
    if path in ours:
        original = task/'evidence/before'/path
        if original.exists() and normal(original.read_bytes()) != normal(blob): before_mismatch.append(path)
    elif normal((root/path).read_bytes()) != normal(blob): other_diff.append(path)
excluded = {'Packages/com.ember/UPMManager/Editor/EmberProjectRulesSync.cs',
            'Packages/com.ember/UPMManager/Tests/Editor/EmberProjectRulesSyncEditTests.cs'}
result = {'base':base,'task_files_verified':len(entries),'other_code_files_compared':len(paths)-len([p for p in paths if p in ours]),
          'before_vs_v0253_mismatches':before_mismatch,'other_package_code_diff':other_diff,
          'excluded_unrelated_workflow_source':sorted(excluded),
          'exclusion_reason':'Rules-entry prose and required workflow-document checks only; separate task, no cache/identity API dependencies. Candidate keeps published v0.25.3 versions.'}
(Path(__file__).parent/'source-base-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
assert not before_mismatch and set(other_diff) == excluded
