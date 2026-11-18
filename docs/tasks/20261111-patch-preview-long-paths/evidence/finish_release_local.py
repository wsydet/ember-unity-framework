from pathlib import Path
import shutil,json,re,hashlib
primary=Path('C:/Users/wuyu/My/ember-unity-framework')
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
rel=Path('docs/tasks/20261111-patch-preview-long-paths');task=primary/rel
# The release candidate was based on v0.25.2; mirror only this task's owned archive.
shutil.copytree(candidate/rel,task,dirs_exist_ok=True)
for name in ['docs/dev/release-0.25.3.md','Packages/com.ember/Dependencies~/release-0.25.3.json','Packages/com.ember/Dependencies~/manifest-0.25.3.json']:
    shutil.copyfile(candidate/name,primary/name)
# Update just current release pointers; retain any unrelated local prose.
for name in ['Packages/com.ember/README.md','Packages/com.ember/Dependencies~/README.md','docs/dev/upm-migration-plan.md','docs/dev/framework-progress.md']:
    p=primary/name;local=p.read_text(encoding='utf-8-sig');release=(candidate/name).read_text(encoding='utf-8-sig')
    line=re.search(r'^当前发布：.*$',release,re.M).group(0)
    local=re.sub(r'^当前发布：.*$',lambda m:line,local,count=1,flags=re.M)
    if name.endswith('upm-migration-plan.md'):local=local.replace('当前发布版本为 **0.25.2**','当前发布版本为 **0.25.3**').replace('ember#v0.25.2','ember#v0.25.3').replace('`Dependencies~/manifest-0.25.2.json`','`Dependencies~/manifest-0.25.3.json`')
    elif name=='Packages/com.ember/README.md':local=local.replace('ember#v0.25.2','ember#v0.25.3').replace('[0.25.1 manifest](Dependencies~/manifest-0.25.1.json)','[0.25.3 manifest](Dependencies~/manifest-0.25.3.json)')
    elif name.endswith('Dependencies~/README.md'):local=local.replace('0.25.2完整 manifest','0.25.3完整 manifest').replace('[release-0.25.2.json](release-0.25.2.json)、[manifest-0.25.2.json](manifest-0.25.2.json)','[release-0.25.3.json](release-0.25.3.json)、[manifest-0.25.3.json](manifest-0.25.3.json)').replace('见[发布说明](../../../docs/dev/release-0.25.2.md)','见[发布说明](../../../docs/dev/release-0.25.3.md)')
    p.write_text(local,encoding='utf-8')
p=primary/'Packages/com.ember/CHANGELOG.md';s=p.read_text(encoding='utf-8-sig')
if '## [0.25.3]' not in s:
    entry=(candidate/'Packages/com.ember/CHANGELOG.md').read_text(encoding='utf-8-sig').split('## [0.25.3]')[1].split('## [0.25.2]')[0]
    s=s.replace('# Changelog\n','# Changelog\n\n## [0.25.3]'+entry,1);p.write_text(s,encoding='utf-8')
proof={'date':'2026-11-12 Asia/Shanghai','version':'0.25.3','repository':'https://github.com/wsydet/ember-unity-framework.git','releaseCommit':'e31fedd240678d82dbd6fdb71f79a77d8deba70f','remoteMain':'e31fedd240678d82dbd6fdb71f79a77d8deba70f','annotatedTag':'v0.25.3','remoteTagObject':'4a847aae16a76351297ba01bc93f68bcad86cde0','remoteTagPeeledCommit':'e31fedd240678d82dbd6fdb71f79a77d8deba70f','push':'git push --atomic origin HEAD:refs/heads/main refs/tags/v0.25.3 succeeded','verification':'git ls-remote confirmed main and peeled tag at release commit','postCommitBinaryCheck':'passed --revision HEAD','postCommitFontCheck':'passed --revision HEAD','consumerValidated':False,'fileSymlink':'pending: environment-limited Win32 1314, user accepted limitation; not passed','originalWorkingBranch':'preserved; not reset or switched'}
(task/'evidence/release-remote-proof.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2),encoding='utf-8')
p=task/'README.md';s=p.read_text(encoding='utf-8');s+='\n\n## 正式发布结果（2026-11-12）\n\n已发布 annotated `v0.25.3`，`origin/main` 与 tag peeled commit 均为 `e31fedd240678d82dbd6fdb71f79a77d8deba70f`。原子推送与远端核对、提交后检查见[发布证明](evidence/release-remote-proof.json)。本地交付及归档清理完成；消费端尚未验证。文件symlink仍待验证，用户已接受该限制，不记通过。\n';p.write_text(s,encoding='utf-8')
identity=json.loads((task/'evidence/source-identity.json').read_text(encoding='utf-8'))
for e in identity['entries']:assert hashlib.sha256((primary/'Packages/com.ember'/e['path']).read_bytes()).hexdigest()==e['sha256'],e['path']
assert not (primary/'.utmp/ember-workflow/20261111-patch-preview-long-paths').exists()
print('Release documents mirrored; 16 original source fingerprints preserved; original task temp absent; unrelated edits retained')
