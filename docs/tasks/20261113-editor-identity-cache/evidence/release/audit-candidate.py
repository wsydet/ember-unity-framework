from pathlib import Path
import copy,hashlib,json,re,subprocess

source=Path(__file__).resolve().parents[5]
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task=Path('docs/tasks/20261113-editor-identity-cache')
base='e31fedd240678d82dbd6fdb71f79a77d8deba70f'
def git(*args):return subprocess.check_output(['git','-C',str(candidate),*args])
def blob(path,revision=':'):return git('show',revision+path if revision==':' else revision+':'+path)
def read(path,revision=':'):return json.loads(blob(path,revision))
normal=lambda b:b.decode('utf-8-sig').replace('\r\n','\n')
entries=json.loads((source/task/'evidence/source-manifest.json').read_text())
proof=[]
for e in entries:
    live=(source/e['path']).read_bytes();working=(candidate/e['path']).read_bytes();staged=blob(e['path'])
    assert hashlib.sha256(live).hexdigest()==e['sha256'] and live==working,e['path']
    assert normal(live)==normal(staged),e['path']
    proof.append({'path':e['path'],'implementation_sha256':e['sha256'],
                  'candidate_bytes_identical':True,'git_blob_sha256':hashlib.sha256(staged).hexdigest(),
                  'git_text_identical_after_crlf_normalization':True})
extras={'Packages/com.ember/package.json','Packages/com.ember/CHANGELOG.md','Packages/com.ember/README.md',
 'Packages/com.ember/Dependencies~/README.md','Packages/com.ember/Dependencies~/release-0.25.4.json',
 'Packages/com.ember/Dependencies~/manifest-0.25.4.json','docs/dev/release-0.25.4.md',
 'docs/dev/upm-migration-plan.md','docs/dev/framework-progress.md'}
allowed={e['path'] for e in entries}|extras
paths=[p for p in git('diff','--cached','--name-only','-z').decode().split('\0') if p]
assert all(p in allowed or p.startswith(task.as_posix()+'/') for p in paths)
assert not git('diff','--name-only'), 'Candidate has unstaged tracked changes'
package=read('Packages/com.ember/package.json');old_package=read('Packages/com.ember/package.json',base)
assert package['version']=='0.25.4'
package['version']=old_package['version'];assert package==old_package
manifest=read('Packages/com.ember/Dependencies~/manifest-0.25.4.json')
old_manifest=read('Packages/com.ember/Dependencies~/manifest-0.25.3.json',base)
assert manifest['dependencies']['com.ember'].endswith('#v0.25.4')
manifest['dependencies']['com.ember']=old_manifest['dependencies']['com.ember'];assert manifest==old_manifest
release=read('Packages/com.ember/Dependencies~/release-0.25.4.json')
old_release=read('Packages/com.ember/Dependencies~/release-0.25.3.json',base)
assert release['templates']==old_release['templates']
expected=copy.deepcopy(old_release)
for field in ('frameworkVersion','frameworkTag','manifest','status','coverage','validation'):expected[field]=release[field]
expected['aiSkills']['revision']='v0.25.4'
assert release==expected
actual=json.loads((source/task/'evidence/release/templates-actual.json').read_text())['structuredContent']['data']['result']
assert len(actual['templates'])==7 and not actual['graphIssues']
assert all(t['matches'] and t['parentMatches'] for t in actual['templates'])
assert all(next(x for x in release['templates'] if x['id']==t['id'])['contentHash']==t['actual'] for t in actual['templates'])
# Ordinary published documentation has local references; raw imported evidence/snippets retain historical destinations.
broken=[]
for name in ['docs/dev/release-0.25.4.md',str(task/'README.md'),str(task/'consumer-handoff.md')]:
    p=candidate/name
    for target in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8-sig')):
        if not (p.parent/target.split('#')[0]).resolve().exists():broken.append({'file':name,'target':target})
assert not broken,broken
result={'base':base,'staged_file_count':len(paths),'repair_files':proof,'package_only_version_changed':True,
 'manifest_dependency_count':len(manifest['dependencies']),'manifest_only_framework_tag_changed':True,
 'release_other_dependencies_templates_bundle_unchanged':True,'templates_actual_verified':7,
 'parent_snapshots_verified':6,'broken_document_links':broken,'staged_paths':paths}
(source/task/'evidence/release/candidate-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ('repair_files','staged_paths')},ensure_ascii=False,indent=2))
