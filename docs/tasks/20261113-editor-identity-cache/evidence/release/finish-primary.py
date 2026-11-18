from pathlib import Path
import hashlib,json,re,shutil,subprocess
primary=Path(__file__).resolve().parents[5]
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task=primary/'docs/tasks/20261113-editor-identity-cache'
commit='c5a2fe90b45ea5343a66f086c2663d35c6f6eac6'
remote=subprocess.check_output(['git','ls-remote','origin','refs/heads/main','refs/tags/v0.25.4','refs/tags/v0.25.4^{}'],cwd=primary).decode()
refs={line.split()[1]:line.split()[0] for line in remote.splitlines()}
assert refs['refs/heads/main']==refs['refs/tags/v0.25.4^{}']==commit
assert refs['refs/tags/v0.25.4']=='ab6d444982c7608d3d5b7e61e04f62e66986fb5e'
for name in ['docs/dev/release-0.25.4.md','Packages/com.ember/Dependencies~/release-0.25.4.json','Packages/com.ember/Dependencies~/manifest-0.25.4.json']:
    shutil.copyfile(candidate/name,primary/name)
for name in ['Packages/com.ember/README.md','Packages/com.ember/Dependencies~/README.md','docs/dev/upm-migration-plan.md','docs/dev/framework-progress.md']:
    path=primary/name;local=path.read_text(encoding='utf-8-sig');released=(candidate/name).read_text(encoding='utf-8-sig')
    line=re.search(r'^当前发布：.*$',released,re.M).group(0)
    local=re.sub(r'^当前发布：.*$',lambda _:line,local,count=1,flags=re.M)
    if name.endswith('upm-migration-plan.md'):
        local=local.replace('当前发布版本为 **0.25.3**','当前发布版本为 **0.25.4**').replace('ember#v0.25.3','ember#v0.25.4').replace('`Dependencies~/manifest-0.25.3.json`','`Dependencies~/manifest-0.25.4.json`')
    elif name=='Packages/com.ember/README.md':
        local=local.replace('ember#v0.25.3','ember#v0.25.4').replace('[0.25.3 manifest](Dependencies~/manifest-0.25.3.json)','[0.25.4 manifest](Dependencies~/manifest-0.25.4.json)')
    elif name.endswith('Dependencies~/README.md'):
        local=local.replace('0.25.3完整 manifest','0.25.4完整 manifest').replace('[release-0.25.3.json](release-0.25.3.json)、[manifest-0.25.3.json](manifest-0.25.3.json)','[release-0.25.4.json](release-0.25.4.json)、[manifest-0.25.4.json](manifest-0.25.4.json)').replace('见[发布说明](../../../docs/dev/release-0.25.3.md)','见[发布说明](../../../docs/dev/release-0.25.4.md)')
    path.write_text(local,encoding='utf-8')
path=primary/'Packages/com.ember/CHANGELOG.md';text=path.read_text(encoding='utf-8-sig')
if '## [0.25.4]' not in text:
    release=(candidate/'Packages/com.ember/CHANGELOG.md').read_text(encoding='utf-8-sig')
    entry='## [0.25.4]'+release.split('## [0.25.4]',1)[1].split('## [0.25.3]',1)[0]
    text=text.replace('# Changelog\n','# Changelog\n\n'+entry,1);path.write_text(text,encoding='utf-8')
proof={'date':'2026-11-13 Asia/Shanghai','version':'0.25.4','commit':commit,'remote_main':refs['refs/heads/main'],
 'annotated_tag':'v0.25.4','remote_tag_object':refs['refs/tags/v0.25.4'],'remote_tag_peeled_commit':refs['refs/tags/v0.25.4^{}'],
 'push':'git push --atomic origin HEAD:refs/heads/main refs/tags/v0.25.4 succeeded',
 'post_commit_binary_check':'passed --revision HEAD','post_commit_font_check':'passed --revision HEAD',
 'staged_bundle_check':'PowerShell 7: 12 skills / 34 files passed; unchanged sourceCommit 31618f43ee8744d46ea9be9c45e881ba9e2c79be',
 'unity_release_version':'0.25.4','targeted_tests':{'passed':33,'failed':0,'skipped':0},
 'actual_templates_verified':7,'actual_parent_snapshots_verified':6,'windward_upgraded':False,
 'original_development_branch':'codex/local-snapshot-20261111-162819 retained, no reset or switch'}
(task/'evidence/release/remote-proof.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2),encoding='utf-8')
path=task/'README.md';text=path.read_text(encoding='utf-8')
if '## 正式发布结果（2026-11-13）' not in text:
    text+='\n\n## 正式发布结果（2026-11-13）\n\n已发布 annotated `v0.25.4`。`origin/main` 与 tag peeled commit 均为 `'+commit+'`；tag 对象为 `ab6d444982c7608d3d5b7e61e04f62e66986fb5e`。原子推送和远端查询一致，提交后 HEAD 二进制/字体检查通过，见[正式发布证明](evidence/release/remote-proof.json)、[推送](evidence/release/push.json)、[远端核对](evidence/release/remote-verification.json)。\n\n当前开发分支及其他任务修改保留，本次发布来自独立干净候选。Windward 未升级，前台四条件及产品功能复验仍待接续；必要的扩展 hash 重新绑定先展示实际预览并取得独立授权。\n'
    path.write_text(text,encoding='utf-8')
for e in json.loads((task/'evidence/source-manifest.json').read_text()):
    assert hashlib.sha256((primary/e['path']).read_bytes()).hexdigest()==e['sha256'],e['path']
assert subprocess.check_output(['git','branch','--show-current'],cwd=primary).decode().strip()=='codex/local-snapshot-20261111-162819'
print('Remote publication verified; release pointers mirrored; 19 repair fingerprints and original branch preserved.')
