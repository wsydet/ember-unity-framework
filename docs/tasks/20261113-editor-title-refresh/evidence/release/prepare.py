from pathlib import Path
import hashlib, json, re, shutil, subprocess

primary = Path(__file__).resolve().parents[5]
candidate = Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task = Path('docs/tasks/20261113-editor-title-refresh')
evidence = primary / task / 'evidence/release'
base = 'c5a2fe90b45ea5343a66f086c2663d35c6f6eac6'
def git(root, *args):
    return subprocess.check_output(['git','-C',str(root),*args])
def normal(data): return data.decode('utf-8-sig').replace('\r\n','\n')
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

if not (evidence/'preservation-before.json').exists():
    paths = set(filter(None, git(primary,'diff','--name-only','-z','HEAD').decode().split('\0')))
    paths.update(filter(None, git(primary,'ls-files','--others','--exclude-standard','-z').decode().split('\0')))
    paths = sorted(p for p in paths if not p.startswith(task.as_posix()+'/'))
    records = [{'path':p,'sha256':sha(primary/p)} for p in paths if (primary/p).is_file()]
    (evidence/'preservation-before.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
    (evidence/'status-before.txt').write_bytes(git(primary,'status','--porcelain=v1','--untracked-files=all'))
assert git(candidate,'rev-parse','HEAD').decode().strip() == base
assert not git(candidate,'status','--porcelain'), 'Existing release checkout has changes; do not overwrite'
assert not git(primary,'diff','--cached','--name-only'), 'Unexpected primary index content'
before = primary/task/'evidence/EditorMainWindowTitle.before.cs.txt'
assert normal(before.read_bytes()) == normal(git(candidate,'show','HEAD:Packages/com.ember/Basic/Editor/EditorMainWindowTitle.cs'))
entries = json.loads((primary/task/'evidence/source-manifest.json').read_text())
for entry in entries:
    path = Path(entry['path'])
    assert sha(path).lower() == entry['sha256'].lower(), str(path)
subprocess.run(['git','-C',str(candidate),'switch','-c','codex/release-0.25.5',base],check=True)
for entry in entries:
    source = Path(entry['path'])
    relative = source.relative_to(primary)
    target = candidate/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(source,target)
shutil.copytree(primary/task,candidate/task,dirs_exist_ok=True)
print('Reused clean v0.25.4 checkout; copied only 10 title source/meta/solution files and task records.')
