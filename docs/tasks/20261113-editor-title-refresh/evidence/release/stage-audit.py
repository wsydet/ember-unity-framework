from pathlib import Path
import copy,hashlib,json,re,shutil,subprocess

primary=Path(__file__).resolve().parents[5]
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task=Path('docs/tasks/20261113-editor-title-refresh')
base='c5a2fe90b45ea5343a66f086c2663d35c6f6eac6'
def git(*args):return subprocess.check_output(['git','-C',str(candidate),*args])
def normal(b):return b.decode('utf-8-sig').replace('\r\n','\n')
def blob(p,revision=':'):return git('show',revision+p if revision==':' else revision+':'+p)
def read(p,revision=':'):return json.loads(blob(p,revision))

assert git('rev-parse','HEAD').decode().strip()==base
entries=json.loads((primary/task/'evidence/source-manifest.json').read_text(encoding='utf-8-sig'))
source_paths=[Path(e['path']).relative_to(primary).as_posix() for e in entries]
extras=['Packages/com.ember/package.json','Packages/com.ember/CHANGELOG.md',
 'Packages/com.ember/README.md','Packages/com.ember/Dependencies~/README.md',
 'Packages/com.ember/Dependencies~/release-0.25.5.json','Packages/com.ember/Dependencies~/manifest-0.25.5.json',
 'docs/dev/release-0.25.5.md','docs/dev/upm-migration-plan.md','docs/dev/framework-progress.md']
shutil.copytree(primary/task,candidate/task,dirs_exist_ok=True)
shutil.copyfile(primary/'docs/dev/release-0.25.5.md',candidate/'docs/dev/release-0.25.5.md')
subprocess.run(['git','-C',str(candidate),'add','--',*source_paths,*extras,task.as_posix()],check=True)
subprocess.run(['git','-C',str(candidate),'diff','--cached','--check'],check=True)
staged=[p for p in git('diff','--cached','--name-only','-z').decode().split('\0') if p]
assert all(p in set(source_paths+extras) or p.startswith(task.as_posix()+'/') for p in staged)
assert not git('diff','--name-only'), 'Unstaged candidate tracked changes'
proof=[]
for e,p in zip(entries,source_paths):
    live=(primary/p).read_bytes();working=(candidate/p).read_bytes();stage=blob(p)
    assert hashlib.sha256(live).hexdigest().lower()==e['sha256'].lower(),p
    if p=='ember-unity-framework.slnx':
        expected_solution=blob(p,base).replace(b'</Solution>',b'  <Project Path="Ember.Basic.Editor.Tests.csproj" />\n</Solution>')
        assert normal(working)==normal(expected_solution)==normal(stage)
        proof.append({'path':p,'primary_sha256':e['sha256'],'candidate_is_base_plus_only_new_title_test_reference':True})
    else:
        assert live==working and normal(live)==normal(stage),p
        proof.append({'path':p,'implementation_sha256':e['sha256'],'candidate_exact_bytes':True,'staged_text_equal':True})
package=read('Packages/com.ember/package.json');old_package=read('Packages/com.ember/package.json',base)
assert package['version']=='0.25.5'
package['version']=old_package['version'];assert package==old_package
release=read('Packages/com.ember/Dependencies~/release-0.25.5.json')
old_release=read('Packages/com.ember/Dependencies~/release-0.25.4.json',base)
expected=copy.deepcopy(old_release)
for k in ('frameworkVersion','frameworkTag','manifest','status','coverage','validation'):expected[k]=release[k]
expected['aiSkills']['revision']='v0.25.5';assert expected==release
manifest=read('Packages/com.ember/Dependencies~/manifest-0.25.5.json')
old_manifest=read('Packages/com.ember/Dependencies~/manifest-0.25.4.json',base)
assert manifest['dependencies']['com.ember'].endswith('#v0.25.5')
manifest['dependencies']['com.ember']=old_manifest['dependencies']['com.ember'];assert manifest==old_manifest
for root in ('Packages/com.ember/Templates~','Packages/com.ember/AISkills~','.agents/skills'):
    assert not git('diff','--cached','--name-only','--',root),root
assert git('diff','--cached','--numstat','--','ember-unity-framework.slnx').decode().split()[:2]==['1','0']
actual=json.loads((primary/task/'evidence/release/releaseTemplates.json').read_text(encoding='utf-8-sig'))['structuredContent']['data']['result']
assert len(actual['templates'])==7 and not actual['graphIssues']
assert all(t['matches'] and t['parentMatches'] for t in actual['templates'])
assert all(next(x for x in release['templates'] if x['id']==t['id'])['contentHash']==t['actual'] for t in actual['templates'])

# The running primary Editor contains two previously existing workflow-sync changes.
# Compare every package C#/asmdef file; neither difference supplies title/cache APIs.
cs_paths=[p for p in git('ls-files','-z','--','Packages/com.ember/**/*.cs','Packages/com.ember/**/*.asmdef').decode().split('\0') if p]
differences=[]
for p in cs_paths:
    assert (primary/p).exists(),p
    if normal((primary/p).read_bytes())!=normal((candidate/p).read_bytes()):differences.append(p)
expected_differences=['Packages/com.ember/UPMManager/Editor/EmberProjectRulesSync.cs',
 'Packages/com.ember/UPMManager/Tests/Editor/EmberProjectRulesSyncEditTests.cs']
assert sorted(differences)==sorted(expected_differences),differences
for p in differences:
    text=normal((primary/p).read_bytes())
    assert 'EmberTitleSnapshot' not in text and 'EditorMainWindowTitle' not in text

allowed_primary_changes=set(extras)
preserved=[];intentional=[]
for item in json.loads((primary/task/'evidence/release/preservation-before.json').read_text(encoding='utf-8-sig')):
    p=primary/item['path'];current=hashlib.sha256(p.read_bytes()).hexdigest() if p.is_file() else None
    if current!=item['sha256']:
        assert item['path'] in allowed_primary_changes,item['path']
        intentional.append(item['path'])
    else:preserved.append(item['path'])
broken=[]
for name in ['docs/dev/release-0.25.5.md',str(task/'README.md'),str(task/'consumer-handoff.md')]:
    p=candidate/name
    for target in re.findall(r'\]\(([^)]+)\)',p.read_text(encoding='utf-8-sig')):
        if not (p.parent/target.split('#')[0]).resolve().exists():broken.append({'file':name,'target':target})
# Source references into the sibling Windward repo remain local provenance, not standalone release docs.
assert all('Windward/' in x['target'] for x in broken),broken
result={'base':base,'staged_file_count':len(staged),'source_files':proof,
 'package_only_version_changed':True,'manifest_only_framework_tag_changed':True,
 'dependency_count':len(manifest['dependencies']),'templates_dependencies_bundle_unchanged':True,
 'actual_templates_verified':7,'actual_parent_snapshots_verified':6,
 'compared_package_cs_asmdefs':len(cs_paths),'primary_only_unrelated_cs_differences':differences,
 'other_existing_files_unchanged':len(preserved),'intentional_primary_release_pointer_changes':intentional,
 'local_provenance_links':broken,'staged_paths':staged}
(primary/task/'evidence/release/candidate-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ('source_files','staged_paths')},ensure_ascii=False,indent=2))
