# Ember 0.26.0

2026-11-18。基于远端 `v0.25.5` 整合发布。新增公开 EUI 和资源接口，并升级模板兼容系列，因此发布次版本。包标识为 `com.ember`，Git 入口使用 `v0.26.0`。当前发布身份以远端 `main` 和 annotated tag 所指提交为准。

## UI 修复与新增能力

- 隐藏页直接完成关闭清理。缓存重开正确调用 `OnOpen` 与 `OnShow`，仍在栈内的隐藏页重新显示时支持 `OnReopen`。
- 预加载页面在首次显示前准备安全区与本页布局，再开启可见性和淡入。已预加载条目在资源就绪并正式展示时建立路由。
- TopMost 父页与受管子页共享 79 个自动排序位，范围为 25050–28950。退场动画结束后才释放占位，引导层保留 29000。
- 退场恢复只处理合格下层。同所有者、同路径的在途展示合并为一个页面和一个遮罩，后续参数通过 `OnReopen` 应用。
- 页面和 Item 进入新一轮数据绑定时，先注销旧绑定，再重置 `DataBinding.RegisterReset` 登记的可选字段。自定义异步结果回到 Unity 主线程后可以用 `TryApply` 拒绝过期回写。 [数据绑定契约](../../Packages/com.ember/Documentation~/ui/eui-contract.md)
- 新增柔光玻璃、模态遮幕、可选 URP 17 背景采样，以及图片异步加载、滚动定位、跑马灯和缓冲进度条。 [柔光玻璃](../../Packages/com.ember/Documentation~/ui/soft-glass.md) · [运行时扩展](../../Packages/com.ember/Documentation~/ui/runtime-extensions.md) · [缓冲进度条](../../Packages/com.ember/Documentation~/ui/buffered-progress-bar.md)
- 公共 `ember-solution-design` 提供完整多对话和简化单对话模式。完整证据包归档到 `.utmp/ember-task-archives/<task-id>/`，消费项目归档前验证 Git 忽略规则，正式交接及验收记录保留在 `docs/tasks/<task-id>/`。PPT Master 作为独立的项目级 Skill 维护，并记录官方来源及更新方式。 [公共工作流](../../Packages/com.ember/Documentation~/maintenance/ai-workflow.md) · [PPT Master 维护](../../Tools/SkillMaintenance/README.md)

## 模板

| 模板 | 封存版本 | 父模板 |
| --- | --- | --- |
| `base` | `0.11.1` | 独立模板 |
| `lane-defense-2d` | `0.1.5` | `base 0.11.1` |
| `platformer-2d` | `0.13.5` | `base 0.11.1` |
| `source3d-2p5d` | `0.6.5` | `base 0.11.1` |
| `topdown-2d` | `0.1.5` | `base 0.11.1` |
| `ui-game` | `0.2.5` | `base 0.11.1` |
| `visual-novel` | `0.22.6` | `base 0.11.1` |

七套模板均声明兼容框架 `0.26.0`。通过项目中心同步了六个派生模板，接收 11 处父级更新，保留 21 处经审阅的子模板覆盖。内容变化的子模板各升一个补丁版本。发布前活动模板为 `base 0.11.1`，项目中心比较结果为零差异。 [玻璃 UI 交付及此前验收](../tasks/20261117-base-soft-glass-migration/README.md)

## 验证范围

- Windows Editor 6000.5.4f1 通过 Unity MCP 编译、域重载和 Console 检查。整合分支的 111 项 EUI 与项目规则定向 EditMode 测试全部通过。同步后的 `0.26.0` 候选通过 85 项模板身份、元数据、同步计划和事务定向 EditMode 测试。
- 七套模板的实际内容哈希与各自封存记录一致，模板谱系校验无问题。六个派生模板的父快照各 889 个文件，与 `base 0.11.1` 逐字节一致。
- 共享字体配置、共享二进制 SHA-256 和公共技能分发校验通过。12 个公共技能的 34 个文件由发布历史中的 `639d5f3` 构建。
- Burner 原工程的章节引导、实际对话与气泡场景、真机非零安全区与旋转、正式 Player 构建及消费项目升级仍待验证。

## 消费项目接入

先通过 Ember UPM Manager 更新框架，再按需通过项目中心预览并升级业务模板。已有项目如果调整过自动 TopMost 顺序或遮罩，应验证 Guide 层和实际射线命中。启用柔光玻璃背景采样时，检查 URP Renderer 接入与回退表现。第三方依赖沿用 `0.25.5`。 [依赖清单](../../Packages/com.ember/Dependencies~/manifest-0.26.0.json) · [发布声明](../../Packages/com.ember/Dependencies~/release-0.26.0.json) · [UPM 维护流程](upm-migration-plan.md)
