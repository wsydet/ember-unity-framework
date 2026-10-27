# Ember 0.24.1 发布门禁

2026-10-27，依据用户明确授权发布0.24.1补丁。基线 `b47fe03be500db38dafd5156fdeef8a810b55758`，发布目标 origin/main 与 annotated tag `v0.24.1`。本记录只涵盖对象适配器解析修复、工作台接线、相关测试及发布资料；不升级或部署 Windward。

## 候选隔离与范围

发布前原暂存区为空。逐路径暂存 LevelAuthoring 的3个 Editor实现、测试程序集/夹具/回归、公开接入文档、该任务回执和0.24.1发布资料；其余已修改/未跟踪文件保留。UI、FSM、共享字体、项目Assets与ProjectSettings等不进入候选。

静态门禁在独立受管候选工作树执行。首轮候选 tree 为 `34938df7594f62e0051b7de5b33d0451e090df92`；之后只补本发布门禁回执。候选使用已暂存 Git 内容，不从脏工作区复制共享字体或未提交技能。代码与测试文件和上一阶段14/14验证内容相同，程序集MVID亦相同，复用同内容定向测试；没有因纯升版再运行全量测试。

## Unity与模板

- 显式请求 MCP `refresh_unity(compile=request)`；[状态](compilation-state.json)确认本轮编译开始 `1793104168770`、结束 `1793104174030`、域重载完成 `1793104193070`（Unix毫秒），编辑器非编译/非更新/无待重载。
- [已加载包](loaded-package.json)确认0.24.1；Editor MVID `14f1db77-b700-459b-a016-b310e65dd78f`、Tests MVID `e3696986-f006-4b86-8f0c-80f81c46a445`。[Console](console.json)错误/警告0条。
- [模板实际校验](templates.json)：五个模板的实际内容hash=metadata=封存hash；四个派生模板的父版本/hash与真实ParentSnapshot均匹配base0.10.0；ValidateTemplateGraph无问题。全部模板字节、版本与兼容声明保持0.24.0发布基线，不保存/Bump/部署模板。
- 相关测试与实际GUI、像素、Undo/Redo、失败回滚证据为[14/14回执](../gui/README.md)。本次不复用旧发行构建作为0.24.1构建证据；未运行新构建、全量用例或Windward消费验收。

## 候选静态门禁

[机器回执](gates.json)汇总：

| 门禁 | 结果 |
|---|---|
| `python scripts/check-shared-binaries.py --revision : --checkout` | 4份共享字体原始字节与Git blob校验通过；core.autocrlf=false/true干净检出均通过 |
| `python scripts/check-shared-font-config.py --revision :` | 工作树与候选index的Dynamic/Multi Atlas、源字体/GUID、fallback、材质及许可证通过 |
| `pwsh -NoProfile -File scripts/build-ai-skill-bundle.ps1 -Check` | 11个通用技能、33个文件通过；不重新生成或修改bundle |
| 发布metadata生成器 | 基线0.24.0→0.24.1，依赖保持原声明，仅框架tag/revision与本次覆盖/验证更新 |
| `git diff --cached --check` | 通过 |
| 无关工作区保留 | 1,244个预先记录文件的SHA256全部保持，无无关内容纳入暂存 |

候选初始化时将文件检出为原始Git文本字节，以避免Windows CRLF误判bundle指纹；没有改变主工作区配置或字节。二进制检查最初因沙箱临时index写入受限未完成，随后在授权范围正常运行通过；未改门禁脚本或跳过检查。

版本脚本已预览后执行，仅将package.json版本改为0.24.1；platformer-2d仍为0.13.0。发布说明见[release-0.24.1](../../release-0.24.1.md)，正式引用以远端main及v0.24.1的同一peeled commit为准。
