from pathlib import Path
import hashlib,json,re,shutil,subprocess

source = Path(__file__).resolve().parents[5]
candidate = Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task = Path('docs/tasks/20261113-editor-identity-cache')
base = 'e31fedd240678d82dbd6fdb71f79a77d8deba70f'
def git(*args): return subprocess.check_output(['git','-C',str(candidate),*args]).decode().strip()
assert git('rev-parse','HEAD') == base
assert not git('status','--porcelain'), 'Existing candidate is not clean'
subprocess.run(['git','-C',str(candidate),'switch','-c','codex/release-0.25.4',base],check=True)
entries=json.loads((source/task/'evidence/source-manifest.json').read_text())
for entry in entries:
    data=(source/entry['path']).read_bytes()
    assert hashlib.sha256(data).hexdigest()==entry['sha256'],entry['path']
    target=candidate/entry['path'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
shutil.copytree(source/task,candidate/task,dirs_exist_ok=True)
summary='当前发布：Ember **0.25.4**，修复 Editor 正式身份与扩展资格查询的重复解析、缓存失效和主线程周期检测开销；保留动态门禁与试玩 owner 清理。模板、依赖和公共技能 bundle 不变；Windward 消费端复测待进行。验证与接入边界见 '
for name,link in [
 ('Packages/com.ember/README.md','../../docs/dev/release-0.25.4.md'),
 ('Packages/com.ember/Dependencies~/README.md','../../../docs/dev/release-0.25.4.md'),
 ('docs/dev/upm-migration-plan.md','release-0.25.4.md'),
 ('docs/dev/framework-progress.md','release-0.25.4.md')]:
    path=candidate/name;text=path.read_text(encoding='utf-8-sig')
    text=re.sub(r'^当前发布：.*$',summary+'[0.25.4 发布说明]('+link+')。',text,count=1,flags=re.M)
    if name.endswith('upm-migration-plan.md'):
        text=text.replace('当前发布版本为 **0.25.3**','当前发布版本为 **0.25.4**').replace('ember#v0.25.3','ember#v0.25.4').replace('`Dependencies~/manifest-0.25.3.json`','`Dependencies~/manifest-0.25.4.json`')
    elif name=='Packages/com.ember/README.md':
        text=text.replace('ember#v0.25.3','ember#v0.25.4').replace('[0.25.3 manifest](Dependencies~/manifest-0.25.3.json)','[0.25.4 manifest](Dependencies~/manifest-0.25.4.json)')
    elif name.endswith('Dependencies~/README.md'):
        text=text.replace('0.25.3完整 manifest','0.25.4完整 manifest').replace('[release-0.25.3.json](release-0.25.3.json)、[manifest-0.25.3.json](manifest-0.25.3.json)','[release-0.25.4.json](release-0.25.4.json)、[manifest-0.25.4.json](manifest-0.25.4.json)').replace('见[发布说明](../../../docs/dev/release-0.25.3.md)','见[发布说明](../../../docs/dev/release-0.25.4.md)')
    path.write_text(text,encoding='utf-8')
entry='''## [0.25.4] - 2026-11-13

- 缓存 Editor 正式模板身份、完整父链、配置与扩展稳定绑定解析；菜单、项目中心、适配器及试玩 Tick 复用检查批次。
- 联动事务、配置、注册/释放、导入、包与内容变化失效，包括缺失输入后来创建/恢复；周期文件检测在后台执行，不重复枚举源码或计算 hash。
- 公开配置返回深副本、定义及解析集合只读；动态兼容条件继续求值，实际写入、Start/Retry 与受控文档操作前重新验证。
- 保留失效试玩原停止、owner 清理和编辑现场恢复路径。33/33 直接相关 EditMode 用例通过；MCP 编译/域重载及已加载程序集确认。
- 模板、Runtime/存档、第三方依赖与公共技能 bundle 不变。启用态产品 Play 和 Windward 前台四条件仍待消费升级后验证，不将 API 微基准视为游戏 FPS。

'''
path=candidate/'Packages/com.ember/CHANGELOG.md';text=path.read_text(encoding='utf-8-sig')
assert '## [0.25.4]' not in text
text=text.replace('# Changelog\n','# Changelog\n\n'+entry,1);path.write_text(text,encoding='utf-8')
print('Prepared 19 task-owned source/meta files on v0.25.3; unrelated source and font changes excluded.')
