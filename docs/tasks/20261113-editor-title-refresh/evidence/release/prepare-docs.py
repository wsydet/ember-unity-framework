from pathlib import Path
import re, sys

primary=Path(__file__).resolve().parents[5]
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
target=primary if sys.argv[1]=='primary' else candidate
summary='当前发布：Ember **0.25.5**，将窗口标题周期 Git 查询与身份文件读取移到单次在途后台快照，descriptor/fallback 复用一次构建的标题；保留格式、更新入口与资格门禁。模板、第三方依赖和公共技能 bundle 不变；Windward 暂未升级。验证与接入边界见 '
for name,link in [
 ('Packages/com.ember/README.md','../../docs/dev/release-0.25.5.md'),
 ('Packages/com.ember/Dependencies~/README.md','../../../docs/dev/release-0.25.5.md'),
 ('docs/dev/upm-migration-plan.md','release-0.25.5.md'),
 ('docs/dev/framework-progress.md','release-0.25.5.md')]:
    path=target/name
    text=path.read_text(encoding='utf-8-sig')
    assert len(re.findall(r'^当前发布：.*$',text,re.M))==1,name
    text=re.sub(r'^当前发布：.*$',lambda _:summary+'[0.25.5 发布说明]('+link+')。',text,count=1,flags=re.M)
    if name.endswith('upm-migration-plan.md'):
        text=text.replace('当前发布版本为 **0.25.4**','当前发布版本为 **0.25.5**').replace('ember#v0.25.4','ember#v0.25.5').replace('`Dependencies~/manifest-0.25.4.json`','`Dependencies~/manifest-0.25.5.json`')
    elif name=='Packages/com.ember/README.md':
        text=text.replace('ember#v0.25.4','ember#v0.25.5').replace('[0.25.4 manifest](Dependencies~/manifest-0.25.4.json)','[0.25.5 manifest](Dependencies~/manifest-0.25.5.json)')
    elif name.endswith('Dependencies~/README.md'):
        text=text.replace('0.25.4完整 manifest','0.25.5完整 manifest').replace('[release-0.25.4.json](release-0.25.4.json)、[manifest-0.25.4.json](manifest-0.25.4.json)','[release-0.25.5.json](release-0.25.5.json)、[manifest-0.25.5.json](manifest-0.25.5.json)').replace('见[发布说明](../../../docs/dev/release-0.25.4.md)','见[发布说明](../../../docs/dev/release-0.25.5.md)')
    path.write_text(text,encoding='utf-8')
entry='''## [0.25.5] - 2026-11-13

- 将窗口标题周期 Git 查询和身份文件读取移到后台显示快照；单次在途、请求合并、真实超时、stdout/stderr 排空和取消释放，主线程不再同步等待 Git。
- 每次刷新只构建一次标题，Unity descriptor 与原生 fallback 复用同一字符串；保留项目/子路径/分支/模板/构建目标/Git 根格式及手动、Play、项目变化入口。
- embedded 与 consumer 分别使用正式编辑/活动部署身份；迟到结果按项目与代次拒绝，显示缓存不参与资格门禁。
- 8 项直接相关 EditMode 回归通过，框架 MCP 已确认编译/域重载与新代码加载；16.107 秒框架 EditMode 采样中标题主线程最大 0.3191ms，消费端四条件仍待验证。
- 模板、Runtime、第三方依赖与公共技能 bundle 不变；不升级 Windward，项目中心 GUI/GC 尾帧问题继续单独承接。

'''
path=target/'Packages/com.ember/CHANGELOG.md'
text=path.read_text(encoding='utf-8-sig')
assert '## [0.25.5]' not in text
path.write_text(text.replace('# Changelog\n','# Changelog\n\n'+entry,1),encoding='utf-8')
print('Updated only current release pointers and added 0.25.5 changelog; historical sections retained:',target)
