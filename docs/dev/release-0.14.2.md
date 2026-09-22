# Ember Framework 0.14.2 发布说明

2026-09-22。框架 `0.14.2` 携带正式封存的 `visual-novel 0.7.1 / preview`。

修复 `ember-vn-import-images` 只有聊天提问、用户看不到可操作确认页面的问题。维护源位于
`Assets/Game/Documentation/TemplateSkills/ember-vn-import-images/`，增加回环 HTTP 服务、HTML 页面及隔离回归。
服务使用 Python 3.10+ / Pillow；Codex Windows 随附 Python 已实际运行，无 npm 构建或公网托管依赖。

页面展示逐图缩略图、文件名、尺寸、透明像素、重复提示，支持剧情/用途/角色批量设置和逐图覆盖。
立绘支持已有/新角色及显示名、ID、表情；背景支持场景/变体；其他用途包含标题 UI、CG 等。
生成计划后展示最终资源 ID、目标文件、Resources 路径、拟修改表行和已有引用。
冲突策略为改 ID、相同内容且同绑定复用、跳过；不同内容替换阻断。其他用途只准备资源，不擅自接线或扩展配表。

`draft.json` 不构成授权；仅页面明确提交产生 `decision.json / confirmed`。回执绑定独立批次 nonce、源图、
三张实际源表、表定义和 Assets 下 Resources 文件指纹，确认及执行前分别复核。取消/无回执/变化都不导入。
辅助脚本不写资产或配表；执行代理仍须通过实时 Unity MCP 身份闸门，按回执备份、导入和烘焙。
无 MCP 的页面可浏览/保存草稿，但禁用确认。页面不会主动唤醒 Codex；用户提交后返回当前任务，由任务读取结构化文件继续。

## 正式保存与验证

- 在已正式加载的 visual-novel 0.7.0 编辑副本修改源文件；Unity MCP Refresh 生成 `.meta`。
- 通过 `EmberProjectSetup.SaveTemplate` 保存 820 个文件，再 `BumpTemplateVersion("visual-novel", 2)` 封存 0.7.1。
- 保存时排除既有字体缓存变动，原文件备份并逐字节恢复；没有将其他工作区改动写进模板。
- 通过 `PreviewCurrentTemplateSkills` 和 `SyncCurrentTemplateSkills` 部署开发发现副本，执行身份闸门返回空原因。
- base 0.6.4 与 source3d-2p5d 0.3.7、父快照不变。兼容声明保留 0.14.1，与 0.14.2 的 major.minor 一致。
- 14 项隔离 Python 测试通过：确认/取消/草稿、无 MCP、源文件新增/变化、表变化、选择变化、目标冲突、表绑定冲突及未确认零资产写入。
- 实际浏览器验证：页面打开、图片可见、批量剧情/用途/已有角色、逐图改背景/跳过/其他、新角色、草稿保存及刷新恢复、计划、确认回读、离线确认禁用和取消回读。
- Unity MCP 刷新、保存、封存与同步成功，Console 查询 0 条错误；未改 C#，未执行本轮完整 Unity 测试集、真实素材导入或烘焙。

技能自带的隔离回归入口为 `scripts/test_confirmation.py`。系统 skill-creator 的 `quick_validate.py` 因随附 Python 缺少 PyYAML 未运行完成；正式模板技能安装器的清单/源文件检查已通过，不把前者记作通过。

## Call Me Heartless 如何更新

1. 在 **Ember/UPM Manager** 升级到发布标签 `v0.14.2`，由 Unity 管理 manifest/lock；不要手改这两个文件。
2. 先备份或提交消费项目，在独立工作副本中比较新模板与产品修改。打开 **Ember/项目中心**，预览 visual-novel 0.7.1 的业务目录和模板技能差异。
3. 当前协议按完整模板 version/hash 固定技能。包升级不会更新旧技能；跨版本“补齐缺失”或单独技能修复不能把 0.7.0 升为 0.7.1。正式方式是经差异确认的**完整重新部署**，然后合回产品自有改动并保留新技能源及正式身份。完整部署会替换五个受管目录，不能直接对尚未备份的产品工程执行。
4. 部署过程通过既有安装器同步发现副本与安装记录；若已部署的同版本/同 hash 副本缺失，再使用模板技能窗口的预览/同步修复。不要手工复制本技能到 `.agents/skills` 或伪改部署记录。
5. 新开/重载 Codex 会话，调用技能后应实际出现本地确认页面。先用少量图片验证草稿、取消和提交，再验收真实导入与烘焙。

本次未修改 Call Me Heartless 的 PackageCache、安装副本、业务 Assets 或部署记录。消费端正式部署与素材导入仍需在该项目验收。
