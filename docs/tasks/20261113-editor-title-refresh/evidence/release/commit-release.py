from pathlib import Path
import json,shutil,subprocess

primary=Path(__file__).resolve().parents[5]
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task=Path('docs/tasks/20261113-editor-title-refresh')
base='c5a2fe90b45ea5343a66f086c2663d35c6f6eac6'
def git(*args):return subprocess.check_output(['git','-C',str(candidate),*args]).decode().strip()
assert git('rev-parse','HEAD')==base
assert not git('diff','--name-only')
assert subprocess.run(['git','-C',str(candidate),'show-ref','--verify','--quiet','refs/tags/v0.25.5']).returncode==1
remote=git('ls-remote','origin','refs/heads/main','refs/tags/v0.25.5')
assert remote==base+'\trefs/heads/main',remote
result=json.loads((primary/task/'evidence/release/releaseTestResult.json').read_text(encoding='utf-8'))
summary=result['structuredContent']['result']['data']['result']['summary']
assert summary['passed']==8 and summary['failed']==0 and summary['skipped']==0
for file,marker in [('binaries-staged.txt','Static integrity passed'),('fonts-staged.txt','PASS Git'),('bundle-staged.txt','Verified 12 bundled framework skills, 34 files')]:
    assert marker in (primary/task/'evidence/release'/file).read_text(encoding='utf-8-sig')
audit=json.loads((primary/task/'evidence/release/candidate-audit.json').read_text(encoding='utf-8'))
paths=git('diff','--cached','--name-only','-z').split('\0')
paths=[p for p in paths if p]
assert set(paths)==set(audit['staged_paths'])
shutil.copyfile(primary/task/'evidence/release/candidate-audit.json',candidate/task/'evidence/release/candidate-audit.json')
subprocess.run(['git','-C',str(candidate),'add','--',(task/'evidence/release/candidate-audit.json').as_posix()],check=True)
subprocess.run(['git','-C',str(candidate),'diff','--cached','--check'],check=True)
subprocess.run(['git','-C',str(candidate),'commit','-m','fix(editor): 消除窗口标题周期同步 Git 阻塞并发布 0.25.5'],check=True)
commit=git('rev-parse','HEAD')
assert git('rev-parse','HEAD^')==base
assert set(git('diff-tree','--no-commit-id','--name-only','-r',commit).splitlines())==set(paths)
assert not git('status','--porcelain'), 'Release checkout changed after commit'
subprocess.run(['git','-C',str(candidate),'tag','-a','v0.25.5','-m','Ember 0.25.5: nonblocking Editor window-title refresh'],check=True)
assert git('rev-parse','v0.25.5^{}')==commit
proof={'version':'0.25.5','commit':commit,'parent':base,'tag':'v0.25.5',
 'annotated_tag_object':git('rev-parse','v0.25.5'),'file_count':len(paths),
 'branch':git('branch','--show-current'),'candidate_clean':True}
(primary/task/'evidence/release/local-commit.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(proof,ensure_ascii=False,indent=2))
