from pathlib import Path
import json,shutil,hashlib,subprocess,re
source=Path('C:/Users/wuyu/My/ember-unity-framework')
dest=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task='docs/tasks/20261111-patch-preview-long-paths'
identity=json.loads((source/task/'evidence/source-identity.json').read_text(encoding='utf-8'))
for e in identity['entries']:
    p=source/'Packages/com.ember'/e['path']; assert hashlib.sha256(p.read_bytes()).hexdigest()==e['sha256']
    target=dest/'Packages/com.ember'/e['path'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,target)
shutil.copytree(source/task,dest/task,dirs_exist_ok=True)
shutil.copyfile(source/'docs/dev/release-0.25.3.md',dest/'docs/dev/release-0.25.3.md')
summary='当前发布：Ember **0.25.3**，修复 Windows 增量预览与共享事务的暂存长路径 IO，保留原 Temp/GUID 结构及三方比较、GUID 和事务语义。模板、依赖和公共技能 bundle 不变；文件 symlink 场景环境受限、待验证，用户已接受此非阻断限制。验证及接入边界见 '
for name,link in [('Packages/com.ember/README.md','../../docs/dev/release-0.25.3.md'),('Packages/com.ember/Dependencies~/README.md','../../../docs/dev/release-0.25.3.md'),('docs/dev/upm-migration-plan.md','release-0.25.3.md'),('docs/dev/framework-progress.md','release-0.25.3.md')]:
    p=dest/name;s=p.read_text(encoding='utf-8-sig')
    s=re.sub(r'^当前发布：.*$',summary+'[0.25.3 发布说明]('+link+')。',s,count=1,flags=re.M)
    if name.endswith('upm-migration-plan.md'):
        s=s.replace('当前发布版本为 **0.25.2**','当前发布版本为 **0.25.3**').replace('ember#v0.25.2','ember#v0.25.3').replace('`Dependencies~/manifest-0.25.2.json`','`Dependencies~/manifest-0.25.3.json`')
    elif name=='Packages/com.ember/README.md':
        s=s.replace('ember#v0.25.2','ember#v0.25.3').replace('[0.25.1 manifest](Dependencies~/manifest-0.25.1.json)','[0.25.3 manifest](Dependencies~/manifest-0.25.3.json)')
    elif name.endswith('Dependencies~/README.md'):
        s=s.replace('0.25.2完整 manifest','0.25.3完整 manifest').replace('[release-0.25.2.json](release-0.25.2.json)、[manifest-0.25.2.json](manifest-0.25.2.json)','[release-0.25.3.json](release-0.25.3.json)、[manifest-0.25.3.json](manifest-0.25.3.json)').replace('见[发布说明](../../../docs/dev/release-0.25.2.md)','见[发布说明](../../../docs/dev/release-0.25.3.md)')
    p.write_text(s,encoding='utf-8')
p=dest/'Packages/com.ember/CHANGELOG.md';s=p.read_text(encoding='utf-8-sig');s=s.replace('# Changelog\n','# Changelog\n\n## [0.25.3] - 2026-11-12\n\n- 保留项目 Temp/完整GUID暂存结构，统一Windows长路径IO；补齐预览复制/meta/hash/规范化/三方比较、补丁应用/共享事务、深层Skill和扩展defaults可达边界。\n- 特殊前缀仅用于最终IO，枚举还原普通路径；保持MD5/SHA256、CRLF/版本头、GUID、排序、stale-plan、冲突及事务顺序。\n- 补强owner lease及精确归属/链接清理；保留主次错误和恢复路径，预览清理失败明确失败，应用已提交后清理失败明确报告已提交与残留。\n- 定向74个独立case：73通过、0失败、1跳过；最终受改3例补跑3/3。MCP编译/域重载/新程序集加载及Console确认；文件symlink因Win32 1314待验证，用户接受此限制后收束。\n- 模板、第三方依赖、公共技能bundle不变；UNC实网、其他系统、消费端和全量/发行构建未测，不自动升级消费项目。\n',1);p.write_text(s,encoding='utf-8')
print('Prepared selective release candidate; original checkout preserved')
