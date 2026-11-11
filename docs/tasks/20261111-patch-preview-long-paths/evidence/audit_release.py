from pathlib import Path
import hashlib,json,subprocess,re
repo=Path.cwd();task=repo/'docs/tasks/20261111-patch-preview-long-paths'
def git(*args):return subprocess.check_output(['git',*args],cwd=repo)
p=task/'handoff.md';p.write_text(p.read_text(encoding='utf-8').rstrip()+'\n',encoding='utf-8')
subprocess.run(['git','add','--',str(p)],check=True,cwd=repo)
archive=json.loads((task/'evidence/archive-manifest.json').read_text(encoding='utf-8'))
for e in archive['files']:
    path='docs/tasks/20261111-patch-preview-long-paths/'+e['path']
    if e['path'] in ['handoff.md','requirements.md','implementation.md','consumer-validation-prompt.md']:path='docs/tasks/20261111-patch-preview-long-paths/evidence/archive-inputs/'+e['path']
    assert hashlib.sha256(git('show',':'+path)).hexdigest()==e['sha256'],path
pkg=repo/'Packages/com.ember'
old=json.loads((pkg/'Dependencies~/manifest-0.25.2.json').read_text(encoding='utf-8-sig'));new=json.loads((pkg/'Dependencies~/manifest-0.25.3.json').read_text(encoding='utf-8-sig'))
changed=[k for k in new['dependencies'] if new['dependencies'][k]!=old['dependencies'].get(k)]
assert changed==['com.ember'],changed
oldr=json.loads((pkg/'Dependencies~/release-0.25.2.json').read_text(encoding='utf-8-sig'));newr=json.loads((pkg/'Dependencies~/release-0.25.3.json').read_text(encoding='utf-8-sig'))
for k in ['templates','thirdPartyPackages','optionalPackageInstallTargets','optionalDevelopmentPackages']:assert oldr[k]==newr[k],k
assert oldr['aiSkills']['bundleSourceCommit']==newr['aiSkills']['bundleSourceCommit']
assert not git('diff','--cached','--name-only','--','Packages/com.ember/Templates~','Packages/com.ember/AISkills~','.agents','AGENTS.md','Packages/com.ember/SharedAssets')
# Include graph result delivered by the live read-only MCP check in primary evidence separately.
result={'version':'0.25.3','archiveSourceFilesExactInGit':len(archive['files']),'manifestDependencies':len(new['dependencies']),'changedDependencies':changed,'templatesAndParentsUnchanged':True,'bundleAndSourcesUnchanged':True,'unrelatedFontsWorkflowSkillsExcluded':True,'whitespaceCheck':'run separately after this audit','binaryCheck':'PASS staged index and clean checkouts core.autocrlf=false and true','fontCheck':'PASS staged index and candidate workspace','bundleCheck':'PASS 12 skills / 34 files','codeProof':'all 16 source files identical to validated task modulo Git CRLF text normalization','fileSymlink':'environment-limited pending, explicitly accepted by user; not passed'}
(task/'evidence/release-static-checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
(task/'evidence/release-product.diff').write_bytes(git('diff','--cached','--','Packages/com.ember/Core','Packages/com.ember/UPMManager','Packages/com.ember/Tests'))
subprocess.run(['git','add','--',str(task/'evidence/release-static-checks.json'),str(task/'evidence/release-product.diff')],check=True,cwd=repo)
print(json.dumps(result))
