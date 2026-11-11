from pathlib import Path
import hashlib,json,shutil,stat,re
repo=Path('C:/Users/wuyu/My/ember-unity-framework')
taskid='20261111-patch-preview-long-paths'
source=repo/'.utmp/ember-workflow'/taskid
dest=repo/'docs/tasks'/taskid
assert source.resolve()==source and source.name==taskid
for p in [source,source.parent,source.parent.parent]+list(source.rglob('*')):
    assert not p.is_symlink() and not (p.lstat().st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT),str(p)
assert {p.name for p in source.iterdir()}=={'handoff.md','requirements.md','design.md','implementation.md','consumer-validation-prompt.md','evidence','prompts'}
identity=json.loads((source/'evidence/source-identity.json').read_text(encoding='utf-8'))
for e in identity['entries']:
    assert hashlib.sha256((repo/'Packages/com.ember'/e['path']).read_bytes()).hexdigest()==e['sha256'],e['path']
assert not dest.exists(), 'Inspect existing archive before resuming'
shutil.copytree(source,dest)
# Preserve exact received documents as historical evidence before adding the accepted decision.
inputs=dest/'evidence/archive-inputs';inputs.mkdir()
for p in source.glob('*.md'): shutil.copyfile(p,inputs/p.name)
decision='''2026-11-12（Asia/Shanghai）用户明确接受文件 symlink 场景“环境受限、待验证，不记通过”，不再阻塞本地收束；授权直接正式归档、安全清理本任务临时资料，收束后发布新版本。AC06 的该场景仍待验证，不降格为通过；本轮性质仍为实施自检。
'''
for name,title in [('handoff.md','正式交接 r7：本地交付完成，消费端尚未验证'),('implementation.md','实施记录 r2：本地交付完成，保留用户接受的补测限制')]:
    p=dest/name;old=p.read_text(encoding='utf-8')
    p.write_text('# '+title+'\n\n'+decision+'\n完成日期：2026-11-12。正式归档入口为 [README.md](README.md)，正式消费端提示词为 [consumer-validation-prompt.md](consumer-validation-prompt.md)。源码与上一轮测试所验证的16文件指纹一致。原文中的“未完成/阻塞收束”及“未授权发布”是2026-11-11历史状态，已由本节用户新决定取代；测试结果和补测要求不变。下方完整保留实施事实与历史修订，逐项AC中的文件symlink仍为待验证（用户接受的非阻断限制）。\n\n---\n\n'+old,encoding='utf-8')
p=dest/'requirements.md';p.write_text(p.read_text(encoding='utf-8')+'\n\n## 2026-11-12 收束决定\n\n'+decision+'R01–R08、AC01–AC08不变，文件symlink未实测不得记通过。发布授权由本次新指令增加，消费端升级/真实应用仍不在授权内。\n',encoding='utf-8')
p=dest/'consumer-validation-prompt.md';s=p.read_text(encoding='utf-8')
s=s.replace('：消费端只读验证提示词（未完成交付草稿）','：消费端只读验证提示词')
s=s.replace('记录日期：2026-11-11，Asia/Shanghai。','记录日期：2026-11-12，Asia/Shanghai。')
s=s.replace('不能称本地交付完成或独立验收完成。消费端尚未验证。','2026-11-12用户接受此环境限制并授权收束，本地交付完成；该场景仍待验证，结果不是独立验收。消费端尚未验证。')
s=s.replace('实际未发布身份','实际源码身份（发布前工作区指纹）')
s=s.replace('**未提交、未发布**；不是已发布 0.25.2 tag 的承诺。','为修复验证时的**未提交、未发布**身份；随后授权发布 v0.25.3，最终提交以该 annotated tag 的 peeled commit 及正式发布说明核对。此段保留的0.25.2只是修复验证时的包版本，不是旧0.25.2 tag已含修复的承诺。')
s=s.replace('未完成交付','交付')
p.write_text(s,encoding='utf-8')
readme='''# 20261111-patch-preview-long-paths：Windows 暂存长路径修复

**本地交付完成；消费端尚未验证。** 完成日期：2026-11-12，Asia/Shanghai。仅方案、实施两阶段，同对话检查为实施自检，不冒充独立验收。

用户2026-11-12明确接受文件 symlink 创建权限受限（Win32 1314）的待验证项，允许直接收束并随后发布；该项仍未通过。目录 junction、安全权限拒绝、实际并发/嵌套及其他相关故障验证已通过。UNC仅格式验证，实网、其他OS及消费项目未测。

根因是可普通读取的源文件在项目Temp/完整GUID暂存副本中超过MAX_PATH。保留原目录结构，在UPM共享文件系统的最终IO边界支持Windows长路径，补齐Core/AISkills/UPM/defaults可达IO；保持hash/GUID/冲突/stale-plan/事务顺序；补owned scope与主次异常、已提交后清理失败的明确语义。

## 正式资料

- [有效需求及收束决定](requirements.md)、[方案r2及决策](design.md)。
- [实际交付、逐项R/AC自检与编译测试](implementation.md)、[正式交接及历史](handoff.md)。
- [消费端只读验证提示词](consumer-validation-prompt.md)：含独立背景、源码指纹、接入需另有授权、UPM核对、只读保护及回报格式。
- [IO链路自检](evidence/io-audit.md)、[源码身份](evidence/source-identity.json)、[最终原始源码快照](evidence/delivery-source/)、[修改前真实工作区备份](evidence/implementation-start/)。
- [具名过滤](evidence/test-filters.json)、[复跑74例](evidence/test2final.json)、[最终3例补跑](evidence/test3final.json)、[编译/域重载](evidence/compile4state.json)、[实际程序集MVID](evidence/compile4loaded.json)、[Console](evidence/consolefinal.json)。

验证环境：Unity6000.5.4f1 WindowsEditor，CLR4.0.30319.42000，Windows NT10.0.26200.0，NTFS，LongPathsEnabled=0。74个独立case最终73通过/0失败/1待验证；最后修改的3例补跑3/3通过，不能相加称77例。源/live meta227/235普通读取成功，Old/Incoming/Local暂存289/294/291；短树249/254/251且计划/hash一致；事务备份272提交与回滚通过。

## 已接受限制与补测

AC06文件symlink场景仍“环境受限、待验证”，不是通过。后续具备文件符号链接创建权限的WindowsEditor环境核对工程根、UPM、源码身份；通过Unity MCP主动编译、确认本轮加载及Console，再仅运行`Ember.UI.Tests.EmberTemplateFileSystemEditTests.Owned_LinksAreRejectedWithoutFollowingTargets("file")`（约1秒）。确认实际执行、未skipped，目标字节不变；记录结果。方法级过滤若包含其他3个参数case须如实记录。不得用junction或静态检查代替该场景，不为测试暗改全局策略。

## 归档与发布边界

原实施基线：`16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`本地快照；修复16文件聚合SHA256为`84084d51b877aacabf644720ab149b07d7a2b5f4d241112436e64fad91e6ce75`。发布候选从v0.25.2建立，拟为v0.25.3，仅纳入本任务；模板、依赖、技能bundle及其他本地改动不进入发布。最终发布身份及检查另见[发布说明](../../dev/release-0.25.3.md)（发布准备阶段建立）。

原始阶段记录、提示词和evidence脚本保留为历史；它们含原临时路径的文字描述不是当前依赖或执行入口，正式记录及证据均在本目录独立承接。原始文档字节见evidence/archive-inputs。清理只针对已完整承接的本任务精确临时目录；检查结果见evidence/archive-manifest.json和evidence/cleanup.json。其他任务与备份保留。
'''
(dest/'README.md').write_text(readme,encoding='utf-8')
files=[{'path':p.relative_to(source).as_posix(),'size':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(source.rglob('*')) if p.is_file()]
for e in files:
    original=dest/e['path']
    if e['path'] in ['handoff.md','requirements.md','implementation.md','consumer-validation-prompt.md']: original=inputs/e['path']
    assert hashlib.sha256(original.read_bytes()).hexdigest()==e['sha256'],e['path']
manifest={'taskId':taskid,'sourceAbsolutePath':str(source),'archiveRelativePath':dest.relative_to(repo).as_posix(),'owner':'this task per handoff r6 and current user authorization','date':'2026-11-12','reparsePoints':[],'unknownContents':[],'allSourceBytesArchived':True,'files':files}
(dest/'evidence/archive-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'archivedFiles':len(files),'source':str(source),'archive':str(dest)}))
