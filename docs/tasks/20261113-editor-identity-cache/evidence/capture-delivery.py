from pathlib import Path
import difflib
import hashlib
import json
import re
import subprocess

root = Path(__file__).resolve().parents[4]
evidence = Path(__file__).resolve().parent
before = evidence / 'before'
existing = sorted(str(p.relative_to(before)).replace('\\', '/') for p in before.rglob('*') if p.is_file())
added = [
    'Packages/com.ember/Core/Editor/EmberEditorMetadataCache.cs',
    'Packages/com.ember/Core/Editor/EmberEditorMetadataCache.cs.meta',
    'Packages/com.ember/Core/Editor/EmberExtensionProject.Cache.cs',
    'Packages/com.ember/Core/Editor/EmberExtensionProject.Cache.cs.meta',
    'Packages/com.ember/Tests/EditMode/EmberEditorIdentityCacheEditTests.cs',
    'Packages/com.ember/Tests/EditMode/EmberEditorIdentityCacheEditTests.cs.meta',
]
changes = []
patch = []
for name in existing + added:
    old = (before / name).read_text(encoding='utf-8-sig') if name in existing else ''
    new = (root / name).read_text(encoding='utf-8-sig')
    if old == new:
        continue
    patch.extend(difflib.unified_diff(old.splitlines(True), new.splitlines(True),
                                    fromfile='before/' + name if name in existing else '/dev/null', tofile='after/' + name))
    changes.append({'path': name, 'new': name in added,
                    'sha256': hashlib.sha256((root / name).read_bytes()).hexdigest()})
(evidence / 'task-only.diff').write_text(''.join(patch), encoding='utf-8')
(evidence / 'source-manifest.json').write_text(json.dumps(changes, ensure_ascii=False, indent=2), encoding='utf-8')

def split_diff(text):
    result = {}
    for part in re.split(r'(?=^diff --git )', text.replace('\r\n', '\n'), flags=re.M):
        if not part.startswith('diff --git '):
            continue
        header = part.split('\n', 1)[0]
        result[header] = part.rstrip()
    return result

initial = split_diff((evidence / 'preexisting.diff').read_text(encoding='utf-8-sig'))
current_text = subprocess.check_output(['git', '-c', 'core.quotepath=false', 'diff', '--binary'], cwd=root).decode('utf-8')
# Keep Git's original quoting mode for comparison with the captured baseline.
current_default = subprocess.check_output(['git', 'diff', '--binary'], cwd=root).decode('utf-8')
current = split_diff(current_default)
task_names = set(existing + added)
untouched = []
for header, part in initial.items():
    is_task = any(header == f'diff --git a/{name} b/{name}' for name in task_names)
    if not is_task:
        untouched.append({'diff_header': header, 'unchanged': current.get(header) == part})
(evidence / 'preexisting-preservation.json').write_text(json.dumps(untouched, ensure_ascii=False, indent=2), encoding='utf-8')
status = subprocess.check_output(['git', 'status', '--short'], cwd=root).decode('utf-8')
(evidence / 'status-final.txt').write_text(status, encoding='utf-8')
print(json.dumps({'task_files': len(changes), 'other_tracked_diffs': len(untouched),
                  'all_other_tracked_diffs_unchanged': all(x['unchanged'] for x in untouched)}, indent=2))
