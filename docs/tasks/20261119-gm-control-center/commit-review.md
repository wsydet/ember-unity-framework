# 2026-11-20 提交前审查与本地提交

用户在主聊天调用 ember-commit-review 并明确要求“提交新版本”。本轮按本地 Git 提交处理：未指定新版本号，也未请求框架发版，因此不修改 package.json、不创建 tag、不 push，不保存/Bump/切换模板。

## 基线与审查结论

- 仓库：ember-unity-framework；分支 codex/release-0.26.0；审查起点65ae680732323d83de9f898f89e9e8cc576ee4c8。
- 初始index为空，工作区含28个修改文件与45个未跟踪文件；Assets原有字体单独排除。本轮新增本审查记录。
- 包版本仍0.26.0；活动编辑base0.11.1。manifest/lock、模板快照、.meta原GUID均未变更。
- 完整读取源码差异与11个新增C#文件，核对13个新增资产及meta、九个目标Prefab制作回执/稳定GUID；没有新增asmdef或缺失meta，没有双路径跟踪Skill。
- 当前全部生产与保护文件SHA-256匹配V2基线，未更改任何产品代码或资源。git diff --check通过；暂存时再检查确切路径集合与cached差异，避免混入其它改动。
- 未发现阻断本次提交的问题。Windows Editor/URP的24条AC及四个返工问题关闭，沿用[独立验收](acceptance-v2.md)的编译/域重载/Play证据；本轮没有重新运行Unity编译、全量测试或设备测试。静态审查本身不是新增Unity通过证据。

## 提交分组与结果

| 分组 | 新增 | 修改 | 删除 | 文件数 | 提交 |
| --- | ---: | ---: | ---: | ---: | --- |
| fix(ui): 修正 FPS 启动与重新启用时的采样 | 0 | 1 | 0 | 1 | f678a55 |
| feat(ui): 重构 GM 控制中心并接入帧率策略与收藏 HUD | 26 | 26 | 0 | 52 | 1bf7092 |
| docs(tasks): 归档 GM 控制中心方案与独立验收 | 20 | 0 | 0 | 20 | 本记录所在的docs提交 |

三个分组使用明确文件路径，不使用 git add .。第三组包含本审查记录和19份正式任务记录；提交自身hash从Git历史读取，不在文件中写自引用hash。

## 保留与维护提示

- Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset 是GM任务开始前已有的动态字体图集/字形缓存清空差异（1024图集变1×1），9行新增/1785行删除。本轮不提交、不恢复、不删除；字节保持进场SHA-256 14C9718D55D832593C689643CD752A277E0915FE418BB71F8F85C883653C0E4D。
- 完整截图、日志、探针、备份与npm工具缓存位于项目规则忽略的.utmp/ember-task-archives/20261119-gm-control-center/，不提交原始包；正式记录登记本地包SHA与使用入口。
- 两项非阻断维护观察：EUISoftGlassGMCenterItem.cs 有4个region标题编码乱码；GMControlCenterAuthoring的回执目录绑定历史I1任务路径。前者不改变编译或运行，后者只影响再次执行制作/Inspect后的证据落点；本轮记录事实，不为提交扩大产品改动或伪称已经修复。
- 本次提交仅保存已验收的维护工程源码/资源与记录。保存模板版本、Bump、发布tag、推送与消费端升级都没有执行；Player/硬件切Hz/移动XR/物理触屏的原边界保持。

## 各组文件与可复用的PowerShell命令

### fix(ui): 修正 FPS 启动与重新启用时的采样

- Packages/com.ember/UI/Runtime/Components/FPSDisplay.cs

```powershell
git add -- @(
    'Packages/com.ember/UI/Runtime/Components/FPSDisplay.cs'
)
git commit -m 'fix(ui): 修正 FPS 启动与重新启用时的采样'
```

### feat(ui): 重构 GM 控制中心并接入帧率策略与收藏 HUD

- Assets/Game/Documentation/base/soft-glass-ui.md
- Assets/Game/State/GameInitState.cs
- Assets/Game/UI/Editor/UIFoundation/GMControlCenterAuthoring.cs
- Assets/Game/UI/Editor/UIFoundation/GMControlCenterAuthoring.cs.meta
- Assets/Game/UI/Editor/UIFoundation/SoftGlassUIAuthoringWindow.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.GM.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.Startup.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.Startup.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMActionItem.Binding.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMActionItem.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMCenterItem.Binding.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMCenterItem.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMPage.Binding.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMPage.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMStateItem.Binding.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMStateItem.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMTimeItem.Binding.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMTimeItem.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMActionGesture.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMActionGesture.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/GMActionRegistry.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMCircleHit.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMCircleHit.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/GMControlLayout.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMControlLayout.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/GMFavorites.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMFavorites.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/GMHudController.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMHudController.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/GMHudLayout.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMHudLayout.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/GMPanelDragHandle.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/GMPanelDragHandle.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRateControls.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRateControls.cs.meta
- Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRatePolicy.cs
- Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRatePolicy.cs.meta
- Assets/GameResource/Resources/Config/UIFoundation/GMBackdropSettings.asset
- Assets/GameResource/Resources/Config/UIFoundation/GMBackdropSettings.asset.meta
- Assets/GameResource/Resources/Config/UIFoundation/SoftGlassDialog.asset
- Assets/GameResource/Resources/Config/UIFoundation/SoftGlassDialog.asset.meta
- Assets/GameResource/Resources/UI/Common/Prefabs/EUILoadingPage.prefab
- Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingPage.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassAlertPage.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassConfirmPage.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMActionItem.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMCenterItem.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMPage.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMStateItem.prefab
- Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMTimeItem.prefab
- Packages/com.ember/UIExtension/Runtime/Behaviour/EUIControlGlyph.cs
- Packages/com.ember/UIExtension/Runtime/Behaviour/EUIFloatingEntry.cs

```powershell
git add -- @(
    'Assets/Game/Documentation/base/soft-glass-ui.md'
    'Assets/Game/State/GameInitState.cs'
    'Assets/Game/UI/Editor/UIFoundation/GMControlCenterAuthoring.cs'
    'Assets/Game/UI/Editor/UIFoundation/GMControlCenterAuthoring.cs.meta'
    'Assets/Game/UI/Editor/UIFoundation/SoftGlassUIAuthoringWindow.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.GM.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.Startup.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUIAppearanceBootstrap.Startup.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMActionItem.Binding.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMActionItem.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMCenterItem.Binding.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMCenterItem.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMPage.Binding.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMPage.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMStateItem.Binding.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMStateItem.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMTimeItem.Binding.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMTimeItem.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMActionGesture.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMActionGesture.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMActionRegistry.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMCircleHit.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMCircleHit.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMControlLayout.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMControlLayout.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMFavorites.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMFavorites.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMHudController.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMHudController.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMHudLayout.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMHudLayout.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMPanelDragHandle.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/GMPanelDragHandle.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRateControls.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRateControls.cs.meta'
    'Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRatePolicy.cs'
    'Assets/Game/UI/Runtime/Module/UIFoundation/UIFrameRatePolicy.cs.meta'
    'Assets/GameResource/Resources/Config/UIFoundation/GMBackdropSettings.asset'
    'Assets/GameResource/Resources/Config/UIFoundation/GMBackdropSettings.asset.meta'
    'Assets/GameResource/Resources/Config/UIFoundation/SoftGlassDialog.asset'
    'Assets/GameResource/Resources/Config/UIFoundation/SoftGlassDialog.asset.meta'
    'Assets/GameResource/Resources/UI/Common/Prefabs/EUILoadingPage.prefab'
    'Assets/GameResource/Resources/UI/Common/Prefabs/EUISettingPage.prefab'
    'Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassAlertPage.prefab'
    'Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassConfirmPage.prefab'
    'Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMActionItem.prefab'
    'Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMCenterItem.prefab'
    'Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMPage.prefab'
    'Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMStateItem.prefab'
    'Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlassGMTimeItem.prefab'
    'Packages/com.ember/UIExtension/Runtime/Behaviour/EUIControlGlyph.cs'
    'Packages/com.ember/UIExtension/Runtime/Behaviour/EUIFloatingEntry.cs'
)
git commit -m 'feat(ui): 重构 GM 控制中心并接入帧率策略与收藏 HUD'
```

### docs(tasks): 归档 GM 控制中心方案与独立验收

- docs/tasks/20261119-gm-control-center/README.md
- docs/tasks/20261119-gm-control-center/acceptance-v2.md
- docs/tasks/20261119-gm-control-center/acceptance.md
- docs/tasks/20261119-gm-control-center/approval.md
- docs/tasks/20261119-gm-control-center/archive.json
- docs/tasks/20261119-gm-control-center/cleanup.json
- docs/tasks/20261119-gm-control-center/closure-check.json
- docs/tasks/20261119-gm-control-center/commit-review.md
- docs/tasks/20261119-gm-control-center/current-approval-r14.md
- docs/tasks/20261119-gm-control-center/decisions.md
- docs/tasks/20261119-gm-control-center/design.md
- docs/tasks/20261119-gm-control-center/dispatch.json
- docs/tasks/20261119-gm-control-center/evidence-index.md
- docs/tasks/20261119-gm-control-center/final-verification.json
- docs/tasks/20261119-gm-control-center/glass-ui-audit.md
- docs/tasks/20261119-gm-control-center/handoff.md
- docs/tasks/20261119-gm-control-center/implementation-i2.md
- docs/tasks/20261119-gm-control-center/implementation.md
- docs/tasks/20261119-gm-control-center/links-check.json
- docs/tasks/20261119-gm-control-center/requirements.md

```powershell
git add -- @(
    'docs/tasks/20261119-gm-control-center/README.md'
    'docs/tasks/20261119-gm-control-center/acceptance-v2.md'
    'docs/tasks/20261119-gm-control-center/acceptance.md'
    'docs/tasks/20261119-gm-control-center/approval.md'
    'docs/tasks/20261119-gm-control-center/archive.json'
    'docs/tasks/20261119-gm-control-center/cleanup.json'
    'docs/tasks/20261119-gm-control-center/closure-check.json'
    'docs/tasks/20261119-gm-control-center/commit-review.md'
    'docs/tasks/20261119-gm-control-center/current-approval-r14.md'
    'docs/tasks/20261119-gm-control-center/decisions.md'
    'docs/tasks/20261119-gm-control-center/design.md'
    'docs/tasks/20261119-gm-control-center/dispatch.json'
    'docs/tasks/20261119-gm-control-center/evidence-index.md'
    'docs/tasks/20261119-gm-control-center/final-verification.json'
    'docs/tasks/20261119-gm-control-center/glass-ui-audit.md'
    'docs/tasks/20261119-gm-control-center/handoff.md'
    'docs/tasks/20261119-gm-control-center/implementation-i2.md'
    'docs/tasks/20261119-gm-control-center/implementation.md'
    'docs/tasks/20261119-gm-control-center/links-check.json'
    'docs/tasks/20261119-gm-control-center/requirements.md'
)
git commit -m 'docs(tasks): 归档 GM 控制中心方案与独立验收'
```
