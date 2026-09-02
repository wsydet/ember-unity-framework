# 框架转 UPM 包迁移方案

> 状态：✅ 已发布 v0.9.0 / v0.9.1 / v0.9.2；v0.10.0 UI 框架收口已完成 Unity 编译与 Play 验证，package.json / CHANGELOG 已进入发布版本；基础模板 v0.5.0 已由用户通过模板编辑器手动保存并通过静态复检，当前待用户提交、tag、推送
> 创建：2026-08-22
> 对应待办：[framework-progress.md](framework-progress.md) P0「框架转为 UPM 包」

---

## 〇、当前状态快照（2026-09-02，v0.10.0 发布交接用）

### 已完成（P0 → P4-b → 模板体系 → 发布 v0.9.x → v0.10.0 UI 收口）

| 里程碑 | 状态 |
|--------|------|
| P0 预备：GUID 引用修复 / 私有仓库（Odin/DOTween/ConsolePro 入仓带 tag） | ✅ |
| P1-P2：Assets/Ember 8 模块带 meta 迁入包；三类硬编码路径改造；EmberUPMManager 面板 | ✅ |
| P3：编译 + S1-S9 + Edit Mode 12 项 + 编辑器工具回归 | ✅（用户实测通过） |
| P4-a：v0.2.0 发布 + 消费端实测（暴露 UPM 依赖形态规则：git URL 只能在项目 manifest 直接声明） | ✅ |
| P4-b：11 包合并为单一 com.ember（模块中心布局：每模块一个文件夹，内含 Runtime/Editor，22 个 asmdef 程序集名不变） | ✅ |
| 版本语义定稿：开发期 major=0；第二位=框架变化（强制更新）；第三位=小修补（可选）；EmberUPMManager 一键升级（检查更新 + 强制/可选标注） | ✅ |
| UniTask 内置（Unity 6000.5 TreeView 泛型修复版，MIT 随包分发）；UniRx 走 OpenUPM 自动解析 | ✅ |
| 共享字体入包（v0.8.0）：钉钉进步体/阿里妈妈东方大楷（许可证随包）→ `SharedAssets/Fonts/`，多模板共享、模板不再重复携带；包内共享资产 LFS 豁免；思印宋无授权已删 | ✅ |
| 模板升级协同 P-A（v0.8.0）：frameworkVersion/channel 元数据 + 兼容闸门 + 部署记录 + 升级提示矩阵；设计文档 template-upgrade-system.md（P-B/P-C 定稿待实施） | ✅ |
| 模板体系（用户哲学：框架交付的就是演示形态）：Templates~/base/Assets 全量镜像 dev 业务层；Setup 整树部署（.meta 随行 GUID 全链有效）；4 完整演示场景；自动剥离 dev 测试对象 | ✅ |
| 初始化窗口（EmberSetupWindow：状态总览 + 模板自动扫描 + 一键部署/补齐/重部署）；模板编辑器（EmberTemplateEditorWindow：保存/加载/新建模板，新建可选 fromBase bool；目标模板下拉选择 + 模板独立版本号主/次/补丁 bump） | ✅ |
| 同步工具：scripts/sync-scaffold.ps1 + scripts/strip-template-scene-objects.ps1 | ✅ |
| v0.8.0 后架构改动（2026-08-27/28）：编辑器 SO 布局统一（Assets/Ember/Editor/SOs/）；EUIBinding 双路径合并 + 框架/用户块标记（Lifecycle 单块 + XxxUser 钩子）+ IsFramework 绑定保护；框架 UI 迁出包至业务层并重命名（Panel 尾缀）；UI 生命周期补充；Loading 双模式；状态机连接线模型（GetEdges/TransitionTo）；头标记双版本格式 + 补齐标记刷新；初始化窗口场景/配置归入模板线框；GUIStyle 静态初始化 NRE 修复 | ✅ |
| 发布前功能测试：dev 按测试计划 51 项全部通过（编译/代码生成/模板编辑器/初始化窗口部署与闸门/两级标记/字体/演示链路），问题 1-7 全部已决或已修复，报告写入测试计划「结果记录」区 | ✅ |
| 🚀 **v0.9.0 发布**（框架 0.9.0 + 基础模板 0.3.0）：tag v0.9.0 + push --tags | ✅ |
| 🔧 **v0.9.1 发布**：修复消费端部署 meta GUID 竞态（DeployTemplate/LoadTemplate DisallowAutoRefresh，Farm 首版部署全链 Missing Prefab Asset 的根因）+ UPM Manager tag 解析修复（v 前缀）+ UI 预制体管理器（总览/一键清理/一键删除 UI）+ package.json bump（发布纪律入档） | ✅ |
| 🔍 **v0.9.2 发布**（patch）：UPM Manager「远程最新」展示 + 帮助文本 tag 更新；同时完成 **Farm 升级能力实测**（检查更新 → 可选升级分级 → 一键升级写回 → 同-tag 幂等全链路通过）；Farm 冒烟通过（一键部署 → 场景预制体实例正常 → Play 全链路） | ✅ |
| 🧩 **v0.10.0 UI 框架收口**：资源/代码模块路由、UI 开发中心、PageType/层级、Popup 遮罩、UIUpdate 可选钩子、统一过渡、SafeArea、Main→Gameplay 状态 UI 清理、GM 退出入口与 UPM 默认安装 tag | ✅（Unity 编译成功，用户 Play 验证通过） |
| 📦 **v0.10.0 发布文件准备**：先 bump `package.json` 至 0.10.0，再建立 CHANGELOG 0.10.0 条目，符合固定发布纪律 | ✅ |

### 已发布（v0.9.0 / v0.9.1 / v0.9.2，2026-08-31 全部随 tag 提交，详见 CHANGELOG 与上方里程碑表）

- v0.9.0：模板体系 + 两窗口 + 字体入包 + P-A 升级协同 + 两级标记 + GamePages 拆分 + 08-27/28 全部架构改动（SO 布局/EUIBinding 块标记/框架 UI 迁移/生命周期/Loading 双模式/状态机边模型/头标记双版本/面板调整/GUIStyle 修复）；基础模板 v0.3.0
- v0.9.1：部署/加载 meta GUID 竞态修复 + UPM Manager tag 解析修复 + UI 预制体管理器
- v0.9.2：UPM Manager「远程最新」展示；docs/dev/ui-logic-backup.md 已删（Farm 冒烟通过后）

### v0.10.0 发布候选（2026-09-02）

- 框架代码、dev 六个通用 UI、公共 SafeArea 与配套生成代码已完成编译及 Play 回归；本轮补齐 `EmberBgMask` 渲染/点击、UIUpdate 可选用户钩子、Main→Gameplay 旧页清理，以及 GM 退出按钮调用 `GameLauncher.Instance.Quit`。
- `Packages/com.ember/package.json` 已先 bump 为 0.10.0，`Packages/com.ember/CHANGELOG.md` 已新增 0.10.0 条目；UPM Manager 无 manifest 时的默认安装 tag 已对准 v0.10.0。
- 基础模板已由用户在 Unity「模板编辑器」中手动保存为 v0.5.0 / frameworkVersion 0.10.0 / stable；五个镜像根目录与 dev 内容一致，路径、GUID、GamePages、SafeArea、Binding 和关键状态逻辑复检通过，可以进入提交与 tag 阶段。

### 待办（按发布顺序）

1. 用户执行发布 git 操作：检查改动 → commit → tag v0.10.0 → `push --tags`。
2. 发布后进入第二模板 P-C 全流程与消费端/Farm v0.10.0 安装、升级、Play 联调。
3. 第三方包托管与 P5（CI/registry）另起规划。

### 关键经验（新会话必读）

- **UPM 铁律**：git URL 依赖只能写在项目 manifest.json 直接声明；包内 package.json 只放 registry 版本
- **一个文件夹一个 asmdef**（Unity 规则，合并时踩过坑）
- **消费端清单**（核心 3 行）：com.ember（GitHub #tag）+ com.sirenix.odin-inspector + com.demigiant.dotween（私有仓库，需 git 凭据）；UniRx 走 OpenUPM scope com.neuecc（2026-08-28 起 dev 也走 registry，与消费端同源）；可选编辑器工具（ConsolePro/rainbow-folders/rainbow-hierarchy/inputdevicedetector）同私有仓
- **版本语义**：major 开发期恒 0；minor=框架变化（强制）；patch=小修（可选）
- **私有仓库** github.com/wsydet/ember-thirdparty-upm：Odin(odin-v4.0.2)/DOTween(dotween-v1.2.815)/ConsolePro(consolepro-v3.9.81)/RainbowFolders(rainbow-folders-v2.4.5)/RainbowHierarchy(rainbow-hierarchy-v2.6.5)/InputDeviceDetector(inputdevicedetector-v1.0.0，后三者 tag 待推送)
- **⚠️ 发布纪律（v0.9.1 事故教训，2026-08-31）**：打 tag 前**必须先把 `Packages/com.ember/package.json` 的 version bump 到目标版本并随 tag 提交**——tag 名与包版本必须一致。v0.9.1 曾漏 bump：Farm 拉到的 v0.9.1 tag 内包版本仍为 0.9.0，面板误报「可升级到 0.9.1」且一键升级报「URL 未找到 tag」。发布序列固定为：① bump package.json → ② 更新 CHANGELOG → ③ 提交 → ④ 打 tag → ⑤ push --tags；若 tag 刚发且无人消费，可 `git tag -f` + 删除远端旧 tag 重推修正
- **发布习惯**：git push origin main --tags（tag 不会随 main 自动推）
- **沙箱限制**：本仓库用 git-lfs，AI 沙箱无法 git add/commit（管道受限）；git 操作由用户执行
- **LFS × UPM**：包内资产不走 LFS（UPM git 安装对 LFS 支持不可靠）；`.gitattributes` 已对 `Packages/com.ember/SharedAssets/**` 豁免，Assets 的 `*.png/*.ttf` LFS 规则不适用于包
- **PowerShell 5.1**：无 BOM 的 UTF-8 脚本中文会乱码；脚本写完需补 BOM
- **dev 项目与消费端的区别**：dev = embedded（file: 安装，可编辑包）；消费端 = git 安装（Library/PackageCache 只读）

### 上一阶段新对话提示词（2026-08-31 历史交接记录；当前状态以上方 2026-09-02 快照为准）

```text
你是 ember-unity-framework（Unity 6 通用游戏框架，UPM 单包 Packages/com.ember，已发布 v0.9.2，v0.10.0 开发收尾中）的下一阶段开发助手。

必读文档（按顺序）：
1. CLAUDE.md —— 项目编码规范（写码前必查 API 速查手册）
2. docs/dev/upm-migration-plan.md §〇 当前状态快照 + 变更日志尾部（2026-08-31 v0.10.0 批次）—— 上一阶段全部成果与经验（含发布纪律）
3. docs/dev/template-upgrade-system.md —— 模板升级协同体系（P-B/P-C 设计定稿）
4. docs/user/UI开发参考.md —— UI 框架用户文档（本阶段新增）

本阶段目标（用户主导，具体需求在对话中逐步给出）：
1. UI 框架调整收尾验证（v0.10.0 批次已落码待验证，见「待验证清单」）
2. 多模板体系（创建第二个模板并走通 P-C 全流程：新模板 version 0.1.0、channel=preview 起步 → 成熟转 stable → 弃用转 deprecated；模板编辑器「新建模板」支持 fromBase 复制起点；初始化窗口多模板列表/升级矩阵已就绪待实测）
3. 框架与项目的联调（框架改动在 dev 验证后，同步到实际业务项目/消费端形态验证，包含 Farm 冒烟与升级链路）

工作规则：
- 先方案后写码：设计讨论用 ember-solution-design skill，方案经用户确认后才动手
- 模板保存一律由用户在「模板编辑器」窗口手动完成（助手只提醒）；不执行 scripts/sync-scaffold.ps1
- 用户改动代码期间 Unity 频繁增量重编译，操作指引提醒「等编译完全结束后再点」
- 版本纪律：框架改动第二位 +1（v0.10.0 由用户定稿）；发布前必须先 bump package.json version 再打 tag（v0.9.1 漏 bump 事故）；序列 = bump → CHANGELOG → 提交 → tag → push --tags
- 发现偏差先记录、修复方案征得用户同意再动手
- 编辑包内 .cs 若报文件锁错误（Unity 编译导入期 EIO 1175）：重新 read 文件后重试即可，勿换其他方式绕过

环境事实：
- dev 项目 com.ember 为 embedded（Packages/com.ember 可编辑）；消费端 git 安装（Library/PackageCache 只读）
- AI 沙箱不能执行 git（git 由用户执行）；本仓库 git-lfs
- 编辑器 SO 位于 Assets/Ember/Editor/SOs/；字体在 Packages/com.ember/SharedAssets/Fonts/
- 部署记录/编辑记录为运行时生成的项目级状态（Assets/Editor/*.json）
- UI 资源布局（v0.10.0 起）：`Assets/GameResource/Resources/UI/Common/` 放模板通用 UI；用户页面按模块进入 `UI/Module/<模块>/`。预制体位于各自的 `Prefabs/`，配套资源使用同级 `Animator/Atlas/`；`uiResourceRoot` 配置在 EmberCSharpImplementation.asset
- 模板 base 当前：version 0.3.0 / frameworkVersion 0.9.0 / channel stable（v0.10.0 发布时由用户定稿 bump）；头标记双版本格式 vX.Y.Z (framework vX.Y.Z)
- 包清单见 docs/user/package-inventory.md（UniTask 已内置 com.ember；Odin/DOTween 等私有仓 git URL）

v0.10.0 已落码改动要点（改造前先读相关代码）：
- 页面骨架：Framework .cs 单 Lifecycle 管理块 + 块外 6 个基础 XxxUser 钩子；OnUpdateUser 仅随 UIUpdate 生成，取消时默认空钩子自动删除、含用户代码则确认；面板可选的 NeedUpdate/OnUpdate/Popup 高级覆写生成在管理块内，OnUpdate/OnClickMask 转发至块外用户钩子；注册框架→GamePages.cs、用户→GamePages.User.cs；绑定条目 IsFramework 保护；包内预制体生成拦截；EUIBinding 页面类型统一使用互斥 PageType
- EUILogic：NeedUpdate 虚拟化（public virtual + protected setter：静态 override 或动态赋值并存）；OnClickMask 默认按 Page.ClickMaskToClose 关闭、AutoCreateClickableMask 代码开关；HasSafeArea/SafeAreaRoot 懒加载 + OnSafeAreaChanged 覆写点；生命周期 OnBeginLoad/OnPreload/OnReopen/OnResetDefault + HidePageViewOnly/ShowPageViewOnly
- 遮罩体系：EUIBinding「遮罩设置」区（useMask/maskColor/clickMaskToClose，仅 Popup / FullScreenPopup 显示）→ EUIBindingBridge 注入 EUIPage（UseMask/MaskColorOverride/ClickMaskToClose）→ ShowBgMaskForPopup 按页读取；全局兜底 EUIManager.PopupMaskColor；遮罩 Canvas 必须 ScreenSpaceCamera + UI 相机（默认 Overlay 会盖住一切）；多弹窗防黑叠加 = 上层弹窗透明遮罩或 useMask=false
- 过渡动画：普通 UI 在 None / PresetFade（默认）/ Animator / CustomCode 中四选一，不再隐式串联；Animator 约定状态名 EmberOpen/EmberClose，尾帧事件 OnEmberOpenAnimationEnd/OnEmberCloseAnimationEnd 经运行时挂载的 EmberPageAnimatorBridge 转发，并有 5 秒超时兜底；方块过渡仅 EUITransitionBlock/Loading 使用，保持「方块扫入→Custom Enter / Custom Exit→方块扫出」特殊链路
- 全屏弹窗：`PageType.FullScreenPopup` 为独立互斥类型，沿用 Popup 栈与遮罩；`EUIPageDef.IsFullScreen` 由类型内部推导，仅全屏弹窗把下层推裁剪面远端（planeDistance 100000），普通 Popup 下层保持渲染、靠遮罩拦截交互；旧 `Popup + isFullScreen:true` 注册可兼容归一化
- 层级体系（1000 对齐）：UILayer Background 0 / Normal 1000 / Popup 2000 / TopMost 25000；EUIPageContext 常量 MainPageBaseOrder 1000 / PageGrowStep 500 / TopMostBaseOrder 25000 / FreePage 30000；SubPage 步长 50；UIRoot 子节点脏标记自动按 sortingOrder 稳定排序（仅 Hierarchy 可视序，渲染由 sortingOrder 决定；root canvas 的 sortingOrder 直接生效，Override Sorting 选项只在 nested canvas 出现；开屏 BootSplash sortingOrder 30000）
- 模板文件操作安全化：LoadTemplate/SaveTemplate 先整树复制到暂存区 → 全部成功后才备份落位（备份目录 ~ 后缀不参与导入），失败自动回滚；EUIBindingSettingData.GetOrCreateSettings 自愈空引用
- 状态机：GetEdges() 边包 + TransitionTo 统一入口；模板编辑器/初始化窗口多模板就绪（GetTemplates/GetTemplateScenes 扫描、兼容闸门 major.minor、升级矩阵 patch/minor/major、部署/加载 DisallowAutoRefresh 防 meta GUID 竞态）；UI 开发中心（Ember/UI/UI 开发中心：创建 / 总览 / 清理与删除）

待验证/待办（v0.10.0 收尾，优先问用户现状再动手）：
1. Unity 编译 0 error 确认（Animator 过渡批次改动后）
2. 用户 Unity 操作：验证 EUIBinding「使用 UIUpdate / 生成遮罩创建覆写 / 生成遮罩点击钩子」勾选与重新生成同步；EUISettingPanel 选择 FullScreenPopup，配 EmberOpen/EmberClose 动画 + 尾帧事件实测 Animator 过渡
3. Play 回归：开屏 30000 置顶 → 背景垫底 → 页面/弹窗/遮罩层级、UIRoot 排序、遮罩点击开关、全屏弹窗隐藏下层、Animator 过渡
4. 模板编辑器保存 base（同步全部预制体/场景/GamePages 改动）；docs/dev/ui-logic-backup.md 已使用完成、用户确认后可删
5. 版本定稿 v0.10.0 → 发布纪律序列
6. 下一阶段：多模板 P-C 全流程 + 消费端/Farm 联调
```

---

## 一、目标

把 `Assets/Ember/*` 下的框架代码转为标准 UPM 包（`Packages/com.ember.*`），并让使用本框架的**其他项目**能够：

1. 通过 git 地址按**版本 tag** 安装框架包
2. 通过**改 tag 重新 resolve** 升级框架版本（不拷贝源码）

---

## 二、已定稿决策

| 决策点 | 结论 |
|--------|------|
| 包粒度 | ~~方案 A 细粒度 11 包~~ → **2026-08-26 决策：合并为单一 `com.ember` 包**（v0.3.0）。模块边界由包内 asmdef 保证；「按需引用」改为 EmberUPMManager 面板面向未来扩展包预留 |
| 版本策略 | **lockstep 单包 + 定制 SemVer**（2026-08-26 定稿）：开发期 major 恒 0；**第二位 = 框架变化（强制更新，强烈建议跟随）**；**第三位 = 小修补（可选更新）**。EmberUPMManager 按此语义标注「强制/可选」 |
| 分发方式 | **① Git URL + tag**：monorepo `?path=` 引用，零基础设施；稳定后视需要演进到私有 registry（②）/ OpenUPM（③） |
| Odin 依赖 | **框架带 Odin**：Odin 做成私有 git 包（`com.sirenix.odin-inspector`），框架 package.json 声明依赖自动安装 |
| 脚手架 | **包内模板 + 一键 Setup 向导**：导包后一次点击复制模板到 Assets/，生成可运行的最小框架（详见 §六） |
| Assets 升级策略 | **Assets 归用户所有**：框架升级只换包、永不覆盖用户文件；反射兜底保运行 + 升级向导 diff 展示（详见 §七） |
| dev 仓库形态 | **dev Assets = 用户导包 + Setup 输出（黄金基准）**：生成区由模板同源渲染、不手写；空项目冒烟 diff 校验 1:1（详见 §6.6） |

---

## 三、转包后形态

### 3.1 包清单与依赖图（v0.3.0 起：单一包）

> 2026-08-26 决策：11 包合并为单一 `com.ember`。模块边界由包内 21 个 asmdef 保证（程序集名不变）。

```
Packages/com.ember/                    ← 单一框架包（v0.3.0）
├── Runtime/                           22 个模块程序集 + 框架预制体
├── Editor/                            编辑器工具 + UPMManager + Templates~/Roslyn~/Resources
├── Tests/                             UI Edit Mode 测试
└── Documentation~/                    模块 README（按模块分子目录）

依赖（package.json，全部 registry 版本，随包自动解析）：
  com.unity.ugui 2.5.0 / com.cysharp.unitask 2.5.10 / com.neuecc.unirx 7.1.0
  / com.unity.cinemachine 3.1.7 / com.unity.inputsystem 1.19.0

前置（git 来源，项目 manifest 直接声明，或由 EmberUPMManager 面板引导安装）：
  com.sirenix.odin-inspector（付费，私有仓库 #odin-v4.0.2）
  com.demigiant.dotween（免费但许可禁止再分发，私有仓库 #dotween-v1.2.815）
```

### 3.2 消费端安装方式

> ⚠️ **UPM 铁律**：git URL 依赖只能出现在项目 manifest.json 直接声明处。
> 框架本体一行安装；Odin/DOTween/UniTask 由 `Ember/UPM Manager` 面板检测后引导安装（或老手直接写 manifest）。

```json
// 其他项目的 Packages/manifest.json（节选，官方 com.unity.* 依赖照常保留）
{
  "scopedRegistries": [
    { "name": "OpenUPM", "url": "https://package.openupm.com",
      "scopes": ["com.neuecc"] }
  ],
  "dependencies": {
    "com.ember": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.8.0",
    "com.sirenix.odin-inspector": "https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.sirenix.odin-inspector#odin-v4.0.2",
    "com.demigiant.dotween": "https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.demigiant.dotween#dotween-v1.2.815"
  }
}
```

> **UniTask 已内置**（v0.3.1 起）：OpenUPM 的 2.5.10 在 Unity 6000.5 编译失败（旧版 TreeView API），MIT 许可允许随包分发，故将修复版 vendor 进 `com.ember/UniTask/`，消费端零操作。UniRx 仍从 OpenUPM 自动解析（scope 仅 com.neuecc）。
> 前置条件：机器需能访问两个仓库（私有仓库需 git 凭据）。
> 网络受限地区可改用 Gitee 镜像（两个仓库各推一份，URL 换 gitee.com）。

**升级框架版本** = 改 `#v0.8.0` 为新 tag（单包单 tag），删 `packages-lock.json` 后 Unity 重新 resolve。

### 3.4 仓库拓扑（双仓库）

```
仓库 1（计划公开）: ember-unity-framework
├── Packages/com.ember.*        ← 11 个框架包住在这里
│    发布 = 打 tag vX.Y.Z，升级 = 改 tag，不需要任何新仓库
└── Assets/...                  ← dev 项目（黄金基准，见 §6.6）

仓库 2（必须私有）: ember-thirdparty-upm
│   本地工作副本在仓库外平级目录（C:\Users\wuyu\My\ember-thirdparty-upm，已搬出框架仓库）
├── com.sirenix.odin-inspector  ← Odin 4.0.2.3（付费，tag: odin-v4.0.2）
└── com.demigiant.dotween       ← DOTween 1.2.815（免费，tag: dotween-v1.2.815）
```

#### 新开发机 clone 说明

| 场景 | 需要 clone 的仓库 |
|------|------------------|
| P2 前开发框架 | 仅框架仓库（Odin 还在 Assets/Plugins、DOTween vendored，随框架仓库走） |
| P2 后开发框架 | 仅框架仓库；Unity 打开项目时按 manifest.json **自动**拉私有仓库到 Library/PackageCache（机器需配好 GitHub 凭据） |
| 更新 Odin/DOTween 包版本 | 手动 clone 私有仓库 1 次 → 改文件 → 打 tag → push |
| 使用框架的其他项目 | 一个都不 clone，manifest.json 写 URL 由 Unity 自动拉 |

- **框架包发布不需要第二个仓库**——消费端用 `?path=` + tag 引用仓库 1 即可
- **仓库 2 只因为 Odin 付费**：公开仓库不能包含 Odin；DOTween 免费但为管理方便同仓
- 团队内消费端 manifest（装框架包时会自动拉 Odin，需私有仓库访问权限）：

```json
{
  "dependencies": {
    "com.ember": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.8.0",
    "com.sirenix.odin-inspector": "https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.sirenix.odin-inspector#odin-v4.0.2"
  }
}
```

#### ⚠️ 公开化的依赖矛盾（真正开源前必须解决）

框架包 package.json 若声明私有 Odin 依赖 → **无私有仓库权限的外部用户装不了任何依赖 Odin 的包**。

路线：
- **当前阶段（团队内部）**：Odin 硬依赖可接受，双仓库照常运行
- **真正开源前**：需把 Odin 从硬依赖降级为软依赖——`ODIN_INSPECTOR` defineConstraints 条件编译解耦（无 Odin 也能编译，有 Odin 自动增强），或提供无 Odin 变体包。此为 P5「付费依赖不能发布」问题的具体化，工作量较大，暂缓

### 3.3 包目录布局（v0.3.0 单一包，模块中心布局）

```
Packages/com.ember/
├── package.json / CHANGELOG.md / README.md / LICENSE.md
├── Basic/          Runtime/ + Editor/    ← 基础库 + 编辑器工具
├── Extensions/     Runtime/              ← 扩展方法
├── Core/           Runtime/ + Editor/    ← 核心：事件/状态机/Update/Manager + Setup 向导
├── Resource/       Runtime/ + Editor/
├── Scene/          Runtime/ + Editor/
├── Audio/          Runtime/ + Editor/
├── Camera/         Runtime/ + Editor/
├── Input/          Runtime/ + Editor/
├── UI/             Runtime/ + Editor/    ← UI 框架 + 框架预制体
├── UIExtension/    Runtime/ + Editor/    ← UI 绑定与增强组件
├── FrameworkTools/ Editor/               ← 场景映射等框架级工具
├── UPMManager/     Editor/               ← UPM 依赖管理器（独立 asmdef）
├── Tests/                                ← UI Edit Mode 测试
└── Documentation~/                       ← 模块 README（按模块分子目录）
```

> 模块中心布局：新增模块 = 新建一个文件夹（内含 Runtime/Editor），不再需要同时改两处。每个 Runtime/Editor 目录各 1 个 asmdef（Unity 规则）。

---

## 四、分阶段实施计划

### P0 预备（约 0.5 天）

1. ✅ **修 extensions asmdef 的 GUID 引用** → 已删除两个悬空引用（`GUID:343dea...`、`GUID:2bafac...` 在 ember 与 burner 项目中均无对应 .meta 定义，系 burner 迁移残留；extensions 包实际只依赖 `Ember.Basic.Runtime`）
2. ✅ **extensions package.json 编码排查** → 文件本身为合法 UTF-8（「乱码」为 PowerShell 5.1 `Get-Content` 按 GBK 解码的显示假象），无需修改
3. ✅ **Odin 私有 git 包** `com.sirenix.odin-inspector`：
   - 已从 `Assets/Plugins/Sirenix/` 原样镜像到 `upm-stage/ember-thirdparty-upm/com.sirenix.odin-inspector/`（123 文件，DLL + 资源 + `.meta` 完整保留，Odin 4.0.2.3）
   - `upm-stage/` 已加入 `.gitignore`（付费授权禁止进公开仓库）
   - ⏳ ~~推送到私有仓库 + 打 tag~~ ——✅ **已推送（2026-08-22）**：`https://github.com/wsydet/ember-thirdparty-upm`（main + `odin-v4.0.2` + `dotween-v1.2.815`）
   - ⚠️ **合规发现**：`Assets/Plugins/Sirenix` 当前被跟踪在公开 GitHub 仓库（106 文件）；P2 移除后仍留存于 git 历史。**用户决策（2026-08-22）：暂不清理历史，P2 只删文件**
4. ✅ **DOTween git 包** `com.demigiant.dotween`：
   - 已从 `Packages/com.demigiant.dotween` 原样镜像到 `upm-stage/ember-thirdparty-upm/com.demigiant.dotween/`（49 文件，1.2.815）
   - 决策：与 Odin 同放一个私有仓库（一个仓库两个包，`?path=` 引用）；免费版许可允许分发但须保留 readme.txt
   - ⏳ ~~推送 + 打 tag~~——✅ **已推送（2026-08-22）**：同上仓库，tag `dotween-v1.2.815`
5. ✅ **lockstep bump 脚本** → `scripts/upm-bump-version.ps1`（`-Version 0.2.0` 一键改全部 `Packages/com.ember.*/package.json`，`-Check` 预览模式，末尾打印 tag/推送命令）；v0.2.0 基线已定，实际 bump 在 P4 打 tag 前统一执行

### P1 包骨架（约 1 天）✅ 已完成（2026-08-22）

- ✅ 8 个新包目录 + 全套元文件（package.json / CHANGELOG / README / LICENSE），由 `scripts/p1-create-package-skeletons.ps1` 数据驱动生成（32 文件，可重复运行）
- ✅ package.json 声明 dependencies（对应 3.1 依赖图）：ember 包互指 `#v0.2.0`、UniTask/UniRx 走 OpenUPM 版本号、Odin/DOTween 走私有仓库 git URL
- ✅ 脚手架模板落位：`com.ember.core/Editor/Templates~/`（State 4 个 + Launcher 1 个）+ `com.ember.ui/Editor/Templates~/GamePages.cs.tpl`，使用 `{NAMESPACE}` / `{EMBED_VERSION}` 占位符（与现有模板引擎一致）
- ✅ bump 脚本扩展：dependencies 中 `#vX.Y.Z` 同步替换（0.2.0→0.3.0 实测通过，registry 版本号不受影响）
- 📌 **偏差记录**：Runtime/Editor 空目录未预创建（git 不跟踪空目录，P2 随代码移动时创建）；LICENSE 暂用 MIT（版权人 wsydet，如需更换再改）
- 📌 **dormant 策略**：8 个新包不进 manifest.json——若此时激活会解析 Odin git 依赖，与 `Assets/Plugins/Sirenix` 产生重复程序集。P2 迁移代码时再统一激活

### P2 迁移（约 1-2 天）🔄 进行中（2026-08-24）

1. ✅ 按依赖序逐包移动：core → resource/scene/audio/camera/input/editor → ui（16 组 robocopy 完成）
2. ✅ **`.meta` 随文件一起移动**——GUID 保全（文件 meta 全量随行；文件夹 meta 由 Unity 重新生成，无引用风险）
3. ✅ asmdef 原样移入，**程序集名不变**——`Assets/Game` 业务层 asmdef 按名引用，零改动
4. ✅ 框架资产随包走：
   - `UI/Runtime/Prefabs/EUILoadingPage`、`EUIBackgroundPage`、`TestManager.prefab` → com.ember.ui
   - 8 个模块 README → 各包 `Documentation~/`
5. ✅ uiextension package.json 补依赖：basic + core + ui + UniTask + ugui + DOTween + Odin
6. ✅ dev manifest.json：8 个新包以 `file:` 激活（📌 偏差：计划 git `#main`，改 embedded 便于开发期直接编辑；P4 空项目冒烟验证 git 路径）
7. ✅ **代码生成路径改造**（§6.4）：
   - `k_SettingsPath` → `Assets/Editor/Ember/EUIBindingSettings.asset`
   - `frameworkCodeRoot` → `Packages/com.ember.ui/Runtime`（只读展示）+ codegen 增加 Packages/ 根生成拦截守卫
   - 3 个 UI 配置 asset + EmberSceneMapping.asset → `Assets/Editor/Ember/`（.meta 随行，模板 GUID 引用已验证有效）
   - EmberDebugConfig / EmberPerformanceConfig → `Assets/Resources/`（项目级可编辑，Creator 路径同步改，SO 注释更新）
   - EmberCodeValidator：FrameworkPackageRoots 扩展 8 个新包 + 移除 Assets/Ember 分支（用户提醒②）
   - GamePages 框架预制体路径 → `Packages/com.ember.ui/...`（用户提醒①③）
   - 全局 grep 验证：代码/资产中无 `Assets/Ember` 残留（仅右键菜单显示名保留，无害）
8. ✅ **EmberProjectSetup 脚手架向导**：`com.ember.core/Editor/EmberProjectSetup.cs`——`Ember/Setup/初始化项目`（目录 + 6 脚本模板 + GamePages + 程序化创建 FrameworkScene/MainScene + Build Settings 注册 + 编译后自动挂载 EUIDefaultMainAnimation）+ `Ember/Setup/校验生成物一致性`（标记版本对比）。📌 偏差：不复制 dev 场景为模板（dev 场景含业务/演示引用 RainbowHierarchy/FeelDemo/OdinDemo 等），场景由向导**程序化创建**保证引用只在消费端有效；新增 2 个 UI 模板（EUIMainAnimationStarter/EUIDefaultMainAnimation，MainState 等 OpeningAnimationEnd 的必需件）
9. ✅ dev 仓库生成区：7 个生成区文件（4 状态 + GamePages + 2 动画）已加 `Generated by Ember Setup v0.1.0` 版本标记
10. ✅ 删除 `Assets/Plugins/Sirenix`（Odin 改由私有包依赖提供，Unity 打开时自动拉取）

### P3 验证（约 1 天）

- 全量编译零错误（注意新旧位置不能同时存在同名 asmdef）
- S1-S9 场景集成链路 + UI 12 项 Edit Mode 测试回归
- 脚本引用丢失扫描（GUID 完整性检查）
- **黄金基准一致性校验**：重跑 Setup → 生成区 git diff 骨架层为空（无漂移）

### P4 发布与消费端冒烟（约 1-2 天）

1. lockstep bump 全包到 0.2.0 → 打 `v0.2.0` tag
2. **新建空 Unity 6000.x 项目**：
   - manifest 加 11 个 ember 包 git URL `#v0.2.0` + OpenUPM scope + Odin 私仓凭据
   - 验证自动安装（含 Odin 自动带上）
3. **空项目冒烟**：运行 `Ember/Setup/初始化项目` 向导 → 复制模板场景 + 生成业务状态 → 点 Play 跑通 Init→Main 启动链（验证 §6.1 目标体验）；同时验证 `Samples~` Import 通道；**输出与 dev 仓库生成区 diff 必须为 0**（黄金基准校验）
4. **升级演练**：bump 0.3.0 + tag → 消费端改 tag → 确认升级生效（验证核心目标）；并演练 §七 的升级向导 diff 流程

### P4-b 单包合并与 v0.3.0（2026-08-26 决策）🔄 进行中

背景：消费端实测 13 个 git 包同时解析 + 国内网络导致反复失败；11 包 lockstep 无实际按需引用场景。决策合并为单一包。

1. ✅ 11 包内容合入 `Packages/com.ember/`（780 文件：Runtime/Editor/Tests/Templates~/Documentation~ 全量，.meta 随行，21 个 asmdef 程序集名不变）
2. ✅ 删除 11 个旧包目录；dev manifest 改 `"com.ember": "file:com.ember"`
3. ✅ 硬编码引用全面修正：124 个版权头 + ExcludedFolders（CONFIG_PATH/FrameworkPackageRoots）+ EUIBindingSettingData.frameworkCodeRoot + EmberProjectSetup 包名常量 + QuickMaintenanceTools Roslyn 路径 + GamePages(.tpl/.cs) 预制体路径 + EUIBindingSettings.asset + 注释文案；全局 grep 零残留
4. ✅ EmberUPMManager 面板（独立 asmdef `Ember.UPMManager.Editor`，零 Sirenix 引用，反射检测 Odin/DOTween，Client.Add 一键安装 + 手动指引 + 未来模块预留区）
5. ✅ com.ember package.json v0.3.0（registry 依赖 5 个：ugui/unitask/unirx/cinemachine/inputsystem）
6. ✅ bump 脚本适配单包（`com.ember*` 匹配，实测 0.3.0→0.4.0 预览通过）
7. ✅ DOTween 决策：**不 vendored**（许可禁止再分发，2026-08-26 查证修正），保持私有仓库 + 面板引导安装
8. ⏳ 待用户：开 Unity 验证编译 + 回归 → 提交 + tag v0.3.0 + 推送 → Unity Farm 用 1 行 manifest 冒烟

### P5 可选远期

- CI（GitHub Actions）：自动 bump / 打 tag / 校验包结构
- 框架稳定 + 团队多项目后演进到私有 scoped registry（Verdaccio/Artifactory），Package Manager UI 一键更新
- 开源后发布 OpenUPM（③）——**前置条件：先完成 §3.4 的 Odin 软依赖解耦（`ODIN_INSPECTOR` defineConstraints）**

---

## 五、风险与注意事项

| 风险 | 缓解 |
|------|------|
| **GUID 断裂**（.meta 漏移 → prefab/场景引用丢失） | 迁移后全量脚本引用扫描；P4 空项目冒烟兜底 |
| **Odin 私仓凭据**（消费端访问私有仓库） | README 写明 git 凭据配置步骤；团队机器统一配置 |
| **DOTween 分发** | 免费版 git mirror；付费 Pro 版功能不进框架 |
| **程序集重名**（迁移期新旧并存） | 单步完成"移入+删除"，不留重复 asmdef |
| **git 包缓存**（改 tag 后不更新） | 文档写明需删 `packages-lock.json` 重新 resolve |
| **11 包 clone 开销**（monorepo `?path=` 每包独立 clone 整仓） | Unity 有本地缓存，初期可接受；长期演进 registry |
| **dev 项目 vendored 版本冲突**（file: 的 unitask/dotween vs 包声明的依赖） | 版本对齐或 dev 项目一并切到声明版本 |
| **Odin 许可合规** | 私仓仅限授权成员；绝不公开发布 |
| **Console Pro 合规**（付费，`Packages/com.flyingworm.consolepro` 被 git 跟踪） | 与 Odin 同处理：✅ **已入私有仓库**（`ember-thirdparty-upm`，tag `consolepro-v3.9.81`，2026-08-24）。编辑器开发工具，不进框架依赖；dev 项目 vendored 保留至仓库公开前移除 |
| **生成区漂移**（dev Assets 与模板不同步） | 纪律：生成区不手写、改模板重跑向导；菜单级 + CI 一致性校验兜底 |
| **模板场景双份同步**（模板 vs Assets 副本） | 模板为唯一真源，副本由向导刷新；场景引用 GUID 同源不破 |
| **UPM 依赖形态规则**（实战踩坑 2026-08-24/26）：**git URL 依赖只能写在项目 manifest.json 直接声明**——写在任何 package.json（embedded 或 git 安装的包都一样）都会被 UPM 跳过（`Skipping invalid dependency`），传递 git 依赖不会生效 | 包内 package.json 只声明 registry 版本依赖（ugui/unitask/unirx/cinemachine/inputsystem）；所有 git 来源包（11 个 ember + Odin + DOTween）由消费端项目 manifest 直接声明（见 §3.2 完整模板） |

---

## 六、项目脚手架与 Assets 落地机制

> 核心矛盾：**UPM 包只读**，而框架的代码生成与可运行骨架必须落在用户可写的 `Assets/`。
> 解决原则：包内放**模板 + 编辑器工具**，Assets/ 放**生成物 + 用户资产**。

### 6.1 目标体验（导包 → 可运行的最小框架）

```
1. 新项目 → manifest 加 11 个 ember 包 git URL #v0.2.0 → Unity 自动装完（含 Odin）
2. 菜单 Ember/Setup/初始化项目（一次点击）
   ├─ 复制模板场景：FrameworkScene + MainScene → Assets/Game/Scenes/
   ├─ 注册 Build Settings（FrameworkScene 为 index 0）
   ├─ 生成业务状态：GameInitState/MainState/GameplayState/SettingsState → Assets/Game/State/
   ├─ 生成 GamePages.cs 骨架 + EUIBindingSettings.asset → Assets/Editor/Ember/
   └─ 创建目录结构
3. 打开 FrameworkScene → 点 Play → 完整演示链路（BootSplash → Init → Main → GM 页/设置/玩法）→ 可运行 ✅
```

此刻已经是一个能跑的最小框架，业务逻辑从 `GameMainState.OnMainEnter` 开始写。

### 6.2 实现机制：三种做法组合

| 方案 | 机制 | 定位 |
|------|------|------|
| **A. 一键 Setup 向导**（主） | `Ember/Setup/初始化项目` 菜单：从包内模板批量生成业务骨架。**幂等**：文件已存在则跳过，可重跑补缺失 | 新项目开箱第一步 |
| **B. 单项生成菜单**（辅） | `Assets/Create/Ember/State/Init 状态` 等右键菜单，从单个 `.tpl` 生成一个文件 | 之后日常增量 |
| **C. 反射兜底**（保底） | `GameLauncher.FindSubclass<T>()` 找不到子类时用框架默认状态——不生成任何文件也能运行 | 任何情况下框架可启动 |

另有 Unity 自带通道：包内 `Samples~/` 在 Package Manager 出现 Import 按钮，点击即复制进 Assets——作为 Setup 向导之外的轻量补充。

### 6.3 向导生成物清单

| 生成物 | 目标位置 | 模板来源 |
|--------|---------|---------|
| GameInitState / GameMainState / GameGameplayState / GameSettingsState | `Assets/Game/State/` | `com.ember/Editor/Templates~/State/*.tpl` |
| GameLauncher 子类（可选） | `Assets/Game/` | 同上 |
| GamePages.cs 注册表骨架 | `Assets/Game/UI/` | `com.ember/Editor/Templates~/` |
| EUIBindingSettings.asset | `Assets/Editor/Ember/` | `k_SettingsPath` 改指向此处 |
| 启动场景 FrameworkScene + MainScene | `Assets/Game/Scenes/` | 包内模板场景**复制** + Build Settings 注册 |
| 目录结构 | `Assets/Game/{State,UI/Runtime,Module}/` | — |

**模板场景可复制的前提**：包内模板场景引用的预制体（BootSplash/Loading）与脚本（GameLauncher）都在包内，`.meta` 随迁移保全 GUID → 复制出的场景所有引用自动有效，无需重连。

**启动场景"最小外壳"原则**：FrameworkScene 只保留 `GameLauncher + MainCamera/UICamera + EventSystem + UIRoot`，其余（Canvas 层、Loading 页、遮罩）由框架运行时自建（先例：EUIManager 的 EnsureLayerRoot）。外壳越薄，框架升级时用户场景越不易过时。

### 6.4 必须配套的路径改造

1. `EUIBindingSettingData.k_SettingsPath` → `Assets/Editor/Ember/`，`GetOrCreateSettings` 自动创建/迁移
2. 删除/重定义 `frameworkCodeRoot`：生成只落 businessCodeRoot；框架页面（EUILoading/EUIBackground）的 `.Binding.cs` 作为源码随包进 `com.ember/Runtime/Pages/`
3. `Assets/Ember/UI/Editor/` 下 3 个配置 asset（EUIBindingSettings / EmberUIBindingSettings / EmberCSharpImplementation）→ `Assets/Editor/Ember/`
4. 生成文件头打版本标记（`// Generated by Ember Setup v0.2.0`）——供 §七 升级检测使用

### 6.5 关键技术细节

1. **定位包内文件**：消费端（git 包）实际路径是 `Library/PackageCache/com.ember@v0.3.0/...`，**不能写死 `Packages/...`**。用 `PackageInfo.FindForAssembly(typeof(EmberProjectSetup).Assembly).resolvedPath` 动态解析，dev（embedded）与消费端（cache）都能正确拿到模板路径
2. **模板禁用 `.cs` 扩展名**：包内 `.cs` 会被 Unity 编译，复制进 Assets 后产生重复类编译错误。模板一律 `.tpl`/`.txt`，放 `Editor/Templates~/`（`~` 后缀目录不参与构建），生成时才落 `.cs`
3. **模板参数替换**：复用现有 `{var}`/`{for}`/`{if}` 模板引擎（CSharpLogicImplementationData），替换命名空间、项目名等

### 6.6 dev 仓库 = 用户项目的黄金基准

> 要求：dev 仓库的 Assets/ 就是用户「导包 + Setup」之后的样子——所见即所得，且作为消费端冒烟的自动对照物。

#### 分区定义

```
dev 仓库 Assets/（= 一个真实的"用户项目"）
├── 生成区（1:1 对应用户导包 + Setup 输出）   ← 不手写，由向导从包内模板渲染
│   ├── Game/State/GameInitState.cs 等 4 个状态
│   ├── Game/Scenes/FrameworkScene + MainScene（模板副本）
│   ├── Game/UI/GamePages.cs + GamePages.User.cs（partial 拆分：框架区/用户区）
│   └── Editor/Ember/*.asset（3 个配置）
└── 示例区（= 用户写业务后的样子）           ← 演示框架能力：GM 页、流送模块、示例 UI
```

- 生成区与用户导包结果**同源**（模板渲染），可做 1:1 diff 校验
- 示例区是「用户接着写业务」的自然形态，示范框架用法，自由编写
- ⚠️ 划分按**文件清单 + 生成标记**，不按目录——生成区与示例区同在 `Assets/Game` 下

#### 当前结构 → 转包后归属对照

| 当前路径 | 转包后归属 |
|---------|-----------|
| `Assets/Ember/{Core,Resource,UI,Scene,Audio,Camera,Input,Editor}`（框架源码） | **不是生成区** → 整体移入 `Packages/com.ember.*`（包内只读），Assets 下消失 |
| `Assets/Ember/UI/Runtime/Prefabs`、`Core/Runtime/Resources` 等框架资产 | 随包走 |
| `Assets/Game/State/`（4 个状态子类） | ✅ 生成区（向导从模板渲染） |
| `Assets/Game/Scenes/FrameworkScene + MainScene` | ✅ 生成区（模板副本，最小可运行） |
| `Assets/Game/Scenes/GameplayScene + SettingsScene` | 示例区（示例业务场景） |
| `Assets/Game/UI/GamePages.cs`（框架区）+ `GamePages.User.cs`（用户区） | ✅ 生成区（partial 拆分：框架页面/用户页面，codegen 写 User 文件） |
| `Assets/Game/UI/Runtime/`（页面逻辑脚本 + Binding，MainMenu/Settings/InGameUI/GMPage 等） | 示例区 |
| `Assets/GameResource/Resources/UI/Common/` + `UI/Module/<模块>/`（通用 UI 与业务模块 UI，含 Prefabs/Animator/Atlas） | 示例区 |
| `Assets/Game/Module/`（PlayerPrefs/Streaming/GlobalLight/PlayerData/RedDot/Guide） | 示例区 |
| ~~`Assets/Game/Fonts`~~ → `Packages/com.ember/SharedAssets/Fonts/` | ✅ 随包走（v0.8.0）：多模板共享、许可证随包、不再随模板部署；思印宋无授权已删 |
| `Assets/Art/Icons`（23k 素材库） | dev 专属：全量图库不进包/模板；模板/编辑器实际用到的图标按需挑精选子集入 `SharedAssets/Icons/` |
| `Assets/Ember/UI/Editor/` 下 3 个配置 asset | → 迁到 `Assets/Editor/Ember/`，✅ 生成区（项目级配置） |

**用户视角**：导包 + 跑 Setup 后，Assets 里只有生成区；示例区是用户将来自己写业务的结果。dev 仓库 = 「生成区 + 已写好示例业务的样子」。

#### 三条维持一致的纪律

1. **生成区文件不手写**——一律由 `Ember/Setup` 从包内模板渲染，文件头带 `Generated by Ember Setup vX.Y.Z` 标记
2. **改生成区 = 改模板 → 重跑向导**——骨架文件（状态类）已存在则跳过（用户代码不覆盖，与 §七 永不覆盖一致）；纯生成物（`.Binding.cs`、GamePages、场景副本）刷新
3. **一致性校验**：
   - 菜单级：`Ember/Setup/校验生成物一致性`（临时渲染模板 → 与 Assets 生成区对比 → 报告漂移）
   - 冒烟级：P4 空项目跑向导 → 与 dev 仓库生成区 **diff 必须为 0**
   - CI 级（远期）：自动化该冒烟

#### 模板场景的双份同步

- 模板场景放 `com.ember/Editor/Templates~/Scenes/`——dev 仓库是 embedded 包，**模板在 Unity 里可直接编辑**；改完重跑向导刷新 Assets 副本
- `~` 目录不进资源数据库：模板只当只读源（不在 Build Settings），副本才是运行对象
- 场景复制走 `File.Copy` + `AssetDatabase.ImportAsset`

#### 一致性口径说明

当前 dev 的 `GameMainState` 等已含业务逻辑（开屏动画等）。按纪律：生成区文件 = 向导骨架 + 业务增量共存——**骨架类生成一次后归用户**（与 §七 策略一致）。因此「1:1 一致」指**骨架层**（初始生成状态）：校验对比骨架层与标记版本，不涉及用户业务增量。

### 6.7 模板体系（多模板框架，2026-08-26 定稿）

> **核心原则（用户拍板）**：框架交付的就是“演示形态”——用户拿到框架生成的场景/代码 = 框架仓库里的演示样子；继承、绑定、场景对象全部替用户做好，用户只在约定的钩子函数里填自己的代码（类 Unity Mono 生命周期）。「干净与否」由框架自己决定，不由消费者配置。

#### 模板架构

`
Packages/com.ember/Templates~/
├── base/                          ← 基础模板（当前唯一）
│   ├── template.json              # { id, displayName, description, version（模板独立版本）, frameworkVersion（目标框架，major.minor 闸门）, channel（stable/preview/deprecated）, order }
│   └── Assets/                    # 全量演示镜像：Game/ + GameResource/ + Resources/ + Ember/Editor/ + Settings/Profile
│       ├── Game/State/*.cs        #   完整演示状态类（继承已写好，用户只改钩子）
│       ├── Game/UI/**             #   演示 UI 逻辑脚本 + 绑定代码（.cs / .Binding.cs）
│       ├── Game/Module/**         #   演示模块：流送/全局灯光/存档/红点
│       ├── Game/Scenes/*.unity    #   4 个完整演示场景（对象树/引用全量）
│       ├── GameResource/Resources/UI/Common/**          #   模板通用 UI（Prefabs/Animator/Atlas）
│       ├── GameResource/Resources/UI/Module/<模块>/**   #   业务模块 UI（Prefabs/Animator/Atlas）
│       ├── Resources/*.asset      #   日志/性能配置
│       └── Ember/Editor/SOs/*.asset   #   UI 绑定/场景映射/图片批设置配置
└── <未来模板>/                    ← 同一机制扩展：如 platformer2d（2D 平台游戏一键部署）
    ├── template.json
    └── Assets/...
`

#### 工作机制

1. **部署 = 整树复制**：Setup 从 Templates~/<模板>/Assets/ 复制到消费端 Assets/（.meta 随行 → 场景/预制体/脚本引用 GUID 全链有效），幂等跳过已存在文件（用户改动不覆盖）
2. **多模板**：扫描 Templates~/*/template.json → 模板管理窗口列出 → 一键部署/切换；未来 2D 平台模板、网络模板同机制接入
3. **模板源头 = dev 仓库业务层**（黄金基准）：改 dev 的演示代码 → 跑 scripts/sync-scaffold.ps1 同步进包模板 → 发版。消费者拿到的永远是 dev 里演示的样子
4. **未来蓝图可视化**：在此基础之上生成实例场景，文档教会用户在哪里加代码即可
5. 第三方受限内容（Feel 等不可再分发）不进模板——模板场景中仅剥离此类对象，其余演示内容全量保留

---

## 七、框架升级与 Assets 协同策略

> 核心问题：框架更新只换包，那 Assets/ 里的生成物（状态类、场景、配置）怎么办？
> 总原则：**Assets 里的一切属于用户，升级永不覆盖**。

### 7.1 五层策略

| 层 | 策略 | 说明 |
|----|------|------|
| 1 | **铁律：永不覆盖** | 框架更新只换包；用户改过的状态类、场景、预制体、配置一律不动 |
| 2 | **反射兜底** | `FindSubclass<InitState>()` 找不到就用框架默认状态 → 用户 Assets 过时也能编译、能运行 |
| 3 | **版本标记 + 升级向导** | 生成物带 `Generated by Ember Setup vX.Y.Z` 标记；升级后向导 diff 展示，**只展示差异、由用户决定** |
| 4 | **框架兼容纪律** | 加法演进（基类只加带默认实现的虚钩子）；SemVer；CHANGELOG + 迁移指南；场景最小外壳 |
| 5 | **减少 Assets 承载** | 能运行时自建的不落 Assets（Canvas 三件套先例） |

### 7.2 升级向导交互（diff 式，绝不静默覆盖）

```
你的项目脚手架由 v0.2.0 生成，当前框架 v0.3.0：
├─ GameMainState.cs    框架新增了 OnMainPreload 虚钩子 → [查看差异] [合并新钩子] [跳过]
├─ FrameworkScene      框架新增 UIRoot/SafeArea 节点     → [查看差异] [补全场景] [跳过]
└─ GamePages.cs        模板无变化
```

- **骨架文件**（状态类）用户改过 → 只展示差异，合并时**只插入新钩子，不动用户代码**
- **纯生成物**（`.Binding.cs`、GamePages 注册表）→ 可安全刷新（先例：现有 codegen「重新生成只刷新 .Binding.cs，.cs 骨架不受影响」）
- **场景** → 「补全模式」只增缺失节点，不动用户已改内容（思路同 FrameworkSceneBootstrapper 的增量同步）

### 7.3 Assets 各类文件升级处置一览

| Assets 文件 | 谁写的 | 框架升级时 |
|------------|--------|-----------|
| 业务状态子类（GameMainState 等） | 向导生成 → **用户编辑** | 反射兜底保编译；升级向导 diff 展示新钩子，用户决定合并 |
| UI 逻辑骨架（MainMenu.cs 等） | 代码生成器 → **用户编辑** | 永不覆盖 |
| 绑定文件（MainMenu.Binding.cs） | 代码生成器，纯生成 | 可安全重新生成刷新 |
| GamePages.cs（框架区） | 框架预写，全文件框架标记 | 可安全刷新（用户页面注册在 GamePages.User.cs，永不覆盖） |
| 配置 SO（EUIBindingSettings 等） | 向导创建 → **用户数据** | 永不覆盖；新字段给默认值 |
| 场景 | 向导复制模板 → **用户修改** | 补全模式只增缺节点 |
| 用户自己的业务代码 | 用户 | 完全无关 |

**一句话总结**：包负责「框架的现在」，Assets 负责「用户的积累」；升级时框架用**兜底保证能跑 + 向导让用户看清差异自主选择**，而不是替用户改文件。

---

## 八、变更日志

| 日期 | 变更 |
|------|------|
| 2026-08-22 | 方案定稿：细粒度 8+3 包、lockstep 版本、Git URL + tag 分发、Odin 私有 git 包自动依赖 |
| 2026-08-22 | 新增 §六 项目脚手架与 Assets 落地机制（Setup 向导 + 模板复制 + 路径改造）与 §七 框架升级与 Assets 协同策略（五层策略 + diff 升级向导） |
| 2026-08-22 | 新增 §6.6 dev 仓库 = 用户项目黄金基准：Assets 分「生成区 + 示例区」，生成区由模板同源渲染、不手写，三级一致性校验（菜单/空项目 diff/CI） |
| 2026-08-22 | §6.6 补充「当前结构 → 转包后归属对照表」：明确 Assets/Ember 框架源码属包内而非生成区，生成区/示例区按文件清单划分 |
| 2026-08-22 | 🚀 **P0 预备执行**：extensions asmdef 悬空 GUID 引用已删除；extensions package.json 确认无编码问题（Get-Content 显示假象）；Odin 4.0.2.3（123 文件）与 DOTween 1.2.815（49 文件）已镜像到 `upm-stage/ember-thirdparty-upm/`（已 gitignore，待用户推私有仓）；`scripts/upm-bump-version.ps1` 就绪；⚠️ 发现 Odin 现被跟踪于公开 GitHub 仓库的历史合规隐患 |
| 2026-08-22 | 新增 §3.4 仓库拓扑（双仓库）：框架包从现有仓库 tag 发布（不需新仓库）；第二仓库仅因 Odin 付费。记录用户决策「仓库计划公开」及开源前的 Odin 软依赖解耦前置条件（P5 已关联） |
| 2026-08-22 | ✅ **P0 收尾**：私有依赖仓库 `https://github.com/wsydet/ember-thirdparty-upm` 推送成功（main + `odin-v4.0.2` + `dotween-v1.2.815`，GitHub 408 抖动重试通过）。P0 全部完成 |
| 2026-08-22 | §3.4 更新：私有仓库本地工作副本搬出框架仓库（平级目录）；补充「新开发机 clone 说明」（P2 后 Unity 按 manifest 自动拉私有仓库，无需手动 clone） |
| 2026-08-22 | ✅ **P1 包骨架完成**：8 个新包骨架（32 文件，数据驱动脚本生成）+ 6 个脚手架模板（core 的 State×4/Launcher×1 + ui 的 GamePages）+ bump 脚本扩展 lockstep 依赖 tag 同步。新包保持 dormant（不进 manifest，避免 Odin 重复程序集），P2 激活 |
| 2026-08-22 | 🔍 **付费包盘点**：共 2 个——Odin Inspector（已处理）+ Console Pro 3（新增风险条目，公开仓库前需处理）。免费但禁止再分发：MMFeedbacks/Nice Vibrations/MMTools。DOTween 确认为免费版 |
| 2026-08-22 | 📦 **Console Pro 私有化**：已镜像到 `upm-stage/com.flyingworm.consolepro`（22 文件，3.32 MB）+ 更新版 README 暂存；沙箱无法写工作区外目录，由用户本机合并进私有仓库并打 tag `consolepro-v3.9.81` |
| 2026-08-24 | ✅ **Console Pro 入仓完成**：`ember-thirdparty-upm` main 470c394→2850466 + tag `consolepro-v3.9.81` 推送成功（用户本机执行）。私有仓库现有 3 包 3 tag：Odin / DOTween / Console Pro |
| 2026-08-24 | 🚚 **P2 主体迁移完成**：8 模块 16 组带 meta 迁入 8 包；8 包 file: 激活；uiextension 补依赖；删除 Assets/Ember 与 Plugins/Sirenix；三类路径全部改造（EUIBinding 路径守卫/CodeValidator 8 包范围/6 个 SO 迁移 + Creator 路径同步）；全局无 Assets/Ember 残留。待用户开 Unity 验证编译 |
| 2026-08-24 | 🧙 **P2 脚手架向导完成**：EmberProjectSetup（初始化项目 + 一致性校验两菜单，两阶段生成，场景程序化创建而非复制 dev 场景）；新增 2 个 UI 模板（开屏动画基类/默认实现）；7 个 dev 生成区文件加版本标记。P2 全部动作完成，编译验证交 P3 |
| 2026-08-24 | 🐛 **修复 UPM 依赖形态问题**：Unity 打开报 414 个 Sirenix 缺失错误——embedded 包依赖不能用 git URL（被 UPM 跳过），Odin 未安装。修复：Odin 直接声明进 dev manifest.json。规则已记入风险表 |
| 2026-08-24 | 🔍 **P4 依赖审计**（逐包 asmdef vs package.json）：basic 缺 Odin+ugui 依赖声明（Runtime 用 Sirenix、Editor 引用 TMPro/UI）、extensions 缺 basic 依赖声明；已补齐并挪 v0.2.0 tag（首次发布未消费前修正）。其余 9 包核对通过 |
| 2026-08-26 | 🔧 **UPM 规则再修正（消费端实测）**：git URL 依赖在任何 package.json 里都会被跳过（含 git 安装的包）→ 全部 11 包 package.json 清空 git 依赖，只留 registry 版本；Odin/DOTween/ember 互指改为项目 manifest 直接声明（§3.2 模板已更新为 13 项完整清单）；8 个包 README 依赖说明重写；风险表规则同步更正。待用户重推 tag 后重测 |
| 2026-08-26 | 🎯 **单包合并决策**：消费端 13 包同时解析 + 国内网络反复失败 → 11 包合并为单一 `com.ember`（v0.3.0）；DOTween 许可查证后决定不 vendored（禁止再分发）；EmberUPMManager 面板承担 Odin/DOTween 检测与安装引导 |
| 2026-08-26 | 🚚 **P4-b 单包合并执行**：11 包 780 文件合入 com.ember（.meta 随行、21 asmdef 程序集名不变）；硬编码引用全量修正（版权头 124 + 7 类路径/常量）；EmberUPMManager 面板落地；dev manifest 改单包；bump 脚本适配；全局 grep 零残留。待用户验证 + tag v0.3.0 |
| 2026-08-26 | ✏️ **命名澄清 + 文档清扫**：面板更名 EmberUPMManager（避免与框架 Manager/Module 体系混淆，菜单 `Ember/UPM Manager`）；5 个功能文档路径引用更新（api-reference/debug/odin×2/transition-block）；package-inventory 更新为单包；§3.3/§6.3-6.6 存活引用更新；删除过时脚本 p1-create-package-skeletons.ps1 |
| 2026-08-26 | 🔎 **合并冲突审计**：系统扫描 11 包同名文件——发现 1 处内容覆盖（core 的 Editor/README.md 被 editor 模块覆盖，已从 git 历史恢复至 `Documentation~/core/README-Editor.md`）+ 9 处文件夹 meta 冲突（已验证无资产按 GUID 引用，Unity 重建无影响） |
| 2026-08-26 | 🐛 **修复 asmdef 同目录冲突**（用户编译报错）：Unity 规则一个文件夹仅允许一个 asmdef——11 个 asmdef 平铺合并导致报错。按程序集分目录重组（Runtime 10 子目录 + Editor 11 子目录，各 1 asmdef，程序集名不变）；8 处路径常量随新布局同步（Roslyn/ExcludedFolders/Templates~/frameworkCodeRoot/预制体路径）；新增可重跑脚本 scripts/reorg-package-assemblies.ps1 |
| 2026-08-26 | 🧱 **模块中心布局重构**（用户提议）：由「Runtime/Editor 顶层 + 模块子目录」改为「模块顶层 + 内部 Runtime/Editor」——新增模块只需建一个文件夹。12 个模块目录 + 8 处路径常量再同步；验证通过（0 多 asmdef 目录 / 0 缺 meta / 0 旧路径残留）；⚠️ 澄清：此前"412 缺 meta"系检查脚本自身 bug，修正后实测 0 缺失 |
| 2026-08-26 | 🐛 **UniTask TreeView 修复版**（消费端实测报 CS0619）：OpenUPM 2.5.10 的编辑器追踪器用旧版非泛型 TreeView，Unity 6000.5 编译失败；dev vendored 副本已被 API Updater 升级为 `TreeView<int>` 泛型版。方案：修复版推私有仓库（tag `unitask-v2.5.10-ember1`），消费端 manifest 直接声明；§3.2 模板与 OpenUPM scope 同步更新 |
| 2026-08-26 | 📦 **UniTask 内置化（v0.3.1）**（用户指正：UniRx/UniTask 应随 ember 自动安装，不应每项目重复配置）：UniTask MIT 许可允许分发 → 修复版 vendor 进 `com.ember/UniTask/`（334 文件，6 个 asmdef 含 versionDefines 外部集成自动排除）；com.ember 依赖移除 unitask；dev manifest 与旧 vendored 包清理；消费端清单减至 3 行，UniTask 零操作 |
| 2026-08-26 | 🧩 **脚手架补全（v0.3.2）**（消费端实测反馈）：① Setup 向导从 2 场景扩为 **4 场景**（Framework/Main/Gameplay/Settings，与四状态一一对应，全部注册 Build Settings）；② 场景映射 SO 修复——`EmberSceneMappingCreator` 单层建目录在全新项目失败（Assets/Editor 不存在）→ 多层建目录 + 新增 `EnsureAndRescan`；EmberSceneMapping + Creator 移入 Core/Editor（依赖方向允许，向导同程序集直接调用，映射 SO 在场景创建后立即刷新）；③ 校验清单扩至 4 场景 + SO |
| 2026-08-26 | 🔧 **v0.3.3**：修复 EmberSceneMappingCreator 的 `Path` 常量与 `System.IO.Path` 同名遮蔽编译错误（CS1061），常量改名 MappingAssetPath |
| 2026-08-26 | 🆙 **一键升级（方案 B）**：EmberUPMManager 新增框架版本区——显示当前版本 + [检查更新]（`git ls-remote --tags` 远程比对 SemVer）+ [升级到 vX.Y.Z]（正则改写 manifest 的 #tag + `Client.Resolve` 触发重解析）+ 失败降级提示。git 包从此拥有类 registry 的一键升级体验，零服务器 |
| 2026-08-26 | 📐 **版本语义定稿（v0.4.0）**（用户提出）：开发期 major 恒 0；第二位=框架变化→强制更新；第三位=小修补→可选。0.3.x 系列（UniTask 内置/4 场景/一键升级）均属框架变化 → 合并发布为 **v0.4.0**；面板按语义自动标注「强制更新/可选更新」；决策表与 CHANGELOG 同步 |
| 2026-08-26 | ✂️ **模板编辑器（v0.7.0）**：Ember/Setup/模板编辑器 窗口（框架开发仓库专用，消费者只读提示）——保存当前业务层为模板 / 加载模板到业务层编辑 / 新建模板（**bool 选择：从头开始 or 复制基础模板作为起点**）；引擎新增 CreateTemplate/SaveTemplate/LoadTemplate/IsEmbeddedPackage + C# 版场景对象剥离（StripSceneObjects，与 pwsh 脚本同算法） |
| 2026-08-26 | 🪟 **初始化窗口（v0.6.0）**：Ember/Setup/初始化项目 改为弹出 EmberSetupWindow——框架/模板状态总览 + 模板列表（自动扫描 Templates~/*/template.json，未来模板自动出现）+ 一键部署/补齐缺失/重新部署 + Build Settings/场景映射 SO 状态；EmberProjectSetup 重构为公共引擎 API（Initialize/GetTemplates/IsTemplateDeployed） |
| 2026-08-26 | 🏛️ **模板体系（v0.5.0，用户哲学定稿）**：框架交付的就是演示形态——Setup 从代码生成改为**整树部署**：包内 Templates~/base/Assets 全量镜像 dev 业务层（状态类全继承/演示 UI 四页/演示模块/4 完整场景/配置资产，.meta 随行 GUID 全链有效，场景 GUID 与 dev 实测一致）；剥离 4 个 dev 测试对象（Feel/Odin/UniTask 测试器 + Rainbow 规则集）；删除过时 .tpl 模板；新增 scripts/sync-scaffold.ps1（dev 改动一键同步模板）+ scripts/strip-template-scene-objects.ps1；§6.7 多模板框架留档（未来 2D 平台等模板同机制接入，用户一键部署/切换）。按新语义第二位+1 |
| 2026-08-26 | 🔧 **v0.4.1（patch，小修补）**：修复升级面板 CS0815——`Client.Resolve()` 在该 Unity 版本返回 void，改用「提取 manifest URL → 替换 #tag → `Client.Add` 重装」（与安装流程同 API，返回请求句柄可轮询） |
| 2026-08-26 | 🔤 **共享字体入包（v0.8.0）**（用户决策）：演示字体从模板移入包内 `SharedAssets/Fonts/`（钉钉进步体/阿里妈妈东方大楷，许可证随包；思印宋因无授权删除），多模板共享零重复；`.gitattributes` 增加 `Packages/com.ember/SharedAssets/**` LFS 豁免（UPM git 安装对 LFS 支持不可靠）；23k 图标全量库留 dev `Assets/Art/` 不进包，模板需要时按精选子集入包；0.5.0-0.7.0 为未发布内部里程碑，首次发布合并为 **v0.8.0**（CHANGELOG 单条目，含 0.4.1 patch） |
| 2026-08-26 | 🎛 **模板编辑器交互升级（v0.8.0）**（用户反馈：手输 id 易失误）：目标模板改为下拉选择（列出 displayName(id)+版本，保存/删除/元数据/版本操作全部作用于选中模板，不再手输）；**模板版本号独立于框架版本**——新建模板从 0.1.0 起、保存内容不再覆盖版本（仅在显式 bump 时变）、编辑器内主/次/补丁 +1 一键 bump；引擎新增 BumpTemplateVersion/UpdateTemplateMetadata/DeleteTemplate，废弃 ReadPackageVersion 写模板版本的做法；EmberCodeValidator 排除包内 vendor 的 UniTask/（ExcludedFolders 支持 Packages/ 路径） |
| 2026-08-26 | 🚦 **模板升级协同 P-A（v0.8.0）**（设计定稿见 docs/dev/template-upgrade-system.md）：template.json 增 frameworkVersion/channel；兼容闸门（major.minor 一致才显示、deprecated 隐藏、preview 徽标）；部署记录 EmberDeployedTemplates.json + 部署时重写头标记版本；升级提示矩阵（patch 绿/minor 橙/major 红，只提示不合并）；引擎新增 GetFrameworkVersion/IsFrameworkCompatible/GetCompatibleTemplates/GetTemplateUpgradeLevel/DeclareFrameworkVersion/SetTemplateChannel/GetDeployedTemplate；P-B 升级向导与 [EmberManaged] 两级标记待实施 |
| 2026-08-26 | ↩️ **模板编辑器防误操作（v0.8.0）**（用户误点主版本+1 无法回退）：版本行改为可编辑文本框 + [应用版本]（SetTemplateVersion 校验 x.y.z，支持回退/对齐），bump 按钮保留为快捷；面板新增「当前正在编辑的模板」状态行（LoadTemplate 写 EmberEditingTemplate.json，目标与编辑不一致橙色警告，保存确认框二次提示）；DeleteTemplate 清理编辑记录；base 版本已回退 0.5.0 |
| 2026-08-26 | 🏷️ **两级标记铺入模板（v0.8.0）**（所有权模型定稿：框架项目出现的代码=框架所有，用户只补充）：① 全文件头标记铺满 42 个演示 .cs（模板共 49 个带 `Generated by Ember Setup` 头标记，部署时重写为真实版本）；② 块标记 `[EmberManaged:begin/end]` 铺 5 个混合文件（4 状态类钩子签名 + EUIDefaultMainAnimation.PlayOpeningAnimation，签名框架管/函数体用户填）；③ **GamePages 拆分**：框架区 `GamePages.cs`（partial，全文件框架）与用户区 `GamePages.User.cs`（无标记用户文件，TODO 锚点），EmberCSharpImplementation 的 pageDefFile 指改 User 文件、Guard 监控/提示文案/校验清单/EUIPageDef 文档同步；④ .Binding.cs 归 codegen 管理（每次整文件重生成），不进两级标记；同步 sync-scaffold 后模板 54 .cs = 49 头标记 + 4 Binding + 1 用户文件 |
| 2026-08-28 | 🛠 **v0.8.0 后架构改动批**（dev 测试期随测随改，用户主导）：编辑器 SO 布局统一（5 个 SO → Assets/Ember/Editor/SOs/，孤儿删除）；EUIBinding 双路径合并 + 框架/用户块标记（框架 .cs 单 Lifecycle 块 + 6 个 XxxUser 钩子；绑定条目 IsFramework 保护）；框架 UI 迁出包至业务层并重命名（Panel 尾缀 + 四目录）；UI 生命周期补充（OnBeginLoad/OnPreload/OnReopen/OnResetDefault/ViewHidden）；Loading 双模式；状态机连接线模型（GetEdges/TransitionTo/ReadOnly 边）；模板 base → v0.2.0 |
| 2026-08-31 | 🧪 **发布前功能测试完成**：dev 按测试计划 51 项全部通过（问题 1-4 全部已决/已修复）；头标记双版本格式 + 补齐标记刷新（问题-3 附带修复）；初始化窗口场景/配置归入模板线框 + 扫描泛化 |
| 2026-08-31 | 🚀 **v0.9.0 发布**（框架 0.9.0 + 基础模板 0.3.0）；随后 **v0.9.1**（修复部署 meta GUID 竞态 DisallowAutoRefresh + UPM tag 解析 v 前缀 + UI 预制体管理器）与 **v0.9.2**（UPM 远程最新展示）。Farm 冒烟通过：一键部署 → 场景预制体实例正常 → Play 全链路；Farm 一键升级链路实测通过（检查更新 → 可选升级分级 → 升级写回 → 幂等）。⚠️ 发布纪律入档：打 tag 前必须 bump package.json（v0.9.1 漏 bump 事故） |
| 2026-08-31 | 🔬 **dev 配置资产丢失排查 + 模板文件操作安全化**（v0.10.0 预算内）：① 现场：dev `Assets/Ember/Editor/SOs/` 缺 EmberCSharpImplementation/EmberImageBatchSettings 两个资产（含 .meta），EUIBindingSettings.asset 的 logicImplementations 被序列化清空（GUID 与模板一致 → 系引用目标消失后写盘清空，非自动重建）→ codegen 静默失效；② 根因：LoadTemplate「先删后拷、复制中断即丢文件无回滚」+ EUIBindingSettings 无自愈；③ 修复：LoadTemplate/SaveTemplate 改为「先整树复制到暂存区 → 全部成功后才旧目录改名备份（~ 后缀不参与导入）→ 落位，任一步失败自动回滚」（CleanDirectory 辅助）；EUIBindingSettingData.GetOrCreateSettings 增加自愈——引用为空且 EmberCSharpImplementation.asset 存在时自动补回 + 警告。待 Unity 编译验证；dev 数据恢复由用户走「加载 base」/删除损坏 SO 后「补齐缺失」 |
| 2026-08-31 | 🗂️ **UI 资源布局 + burner 规范对齐（v0.10.0 预算内，方案经用户确认，2026-09-01 完成模块路由收口）**：① 预制体与代码分离——CSharpLogicImplementationData 使用 `uiResourceRoot=Assets/GameResource/Resources/UI`，统一解析规则：框架模式 → `Common/Prefabs`，用户模式按 `classPath` 首段 → `Module/<模块>/Prefabs`；EUIPageDef 与 GeneratePrefab 共用同一解析器，Inspector 展示最终路径；DefaultUIResourceProvider 支持 Assets 路径自动剥离到 Resources 相对路径。② 逻辑分模块约定（业务新页面 `<模块>/Page|Component`，资源同模块归档）；③ 小改包：SubPage 经全局入口（无父页面）打开被拒绝并警告；Popup 遮罩颜色配置化；IEmberSafeAreaProvider 加 SafeAreaChanged 事件 + EUILogic 自动订阅；④ 模板快照目录 TemplateDirNames 增 GameResource；⑤ 产出 `docs/user/UI开发参考.md`。配套：模板由用户在模板编辑器手动保存同步，待 Unity 编译与 Play 回归验证。 |
| 2026-08-31 | 🎨 **渲染层级修复（v0.10.0 预算内，用户提 3 问）**：① BG Mask Canvas 未注入 UI 相机（默认 ScreenSpaceOverlay 永远盖在 Camera 模式页面之上）→ EUIBgMaskPool 构造注入 UICamera + CreateMask 设 ScreenSpaceCamera/worldCamera；② EnsureLayerCanvas 层 Canvas 补 overrideSorting=true（语义自洽）；③ Background 排序问题根因 = FrameworkScene 的 BootSplash Canvas 未开 Override Sorting（sortingOrder 字段 999 无效）→ 由用户在 Unity 勾选，BootSplash 真置顶、背景 0 垫底；④ 澄清：运行时动态修改全部作用于 Instantiate 克隆，不写回预制体资产（写资产仅 codegen 生成 / UI 预制体管理器清理等显式工具）；⑤ GamePages 拆分重建（框架条目回 GamePages.cs + User 文件回 TODO）+ sync-scaffold.ps1 补 GameResource |
| 2026-08-31 | 📏 **层级数值对齐（用户主导）**：UILayer 枚举对齐内部排序常量（Background 0 / Normal 1000=MainPageBaseOrder / Popup 2000 / TopMost 25000=TopMostBaseOrder，消除两套数字脱节）；EUIBindingEditorUtility.PageFlagsToSortingOrder 改引用 UILayer（补 using Ember.UI）；SubPage 步长**保持 50**（用户定稿：1000 层距下小步长余量更足）；UI 预制体管理器/排序功能（EUIViewEngine 脏标记 + UIRoot 子节点按 sortingOrder 稳定排序）同批 |
| 2026-08-31 | 🪟 **全屏弹窗标记（用户主导）**：区分「完全遮盖下层的全屏弹窗」与「普通弹窗」——PageFlags 加 `FullScreen` 位（仅与 Popup 组合，Inspector InfoBox 约束）；EUIPageDef 加 `IsFullScreen`（构造可选参）；EUIPageContext.AddPopup 仅全屏弹窗才把下层推裁剪面远端（SetPageVisible(false)），普通弹窗下层保持渲染、靠遮罩拦截交互；codegen GamePages 条目输出 `isFullScreen: true`；dev EUISettingPage 条目已标记全屏（预制体带全屏底，完全遮盖主界面） |
| 2026-08-31 | 🎭 **弹窗遮罩配置化（用户主导，解决多弹窗遮罩叠加变黑）**：EUIBinding 新增「遮罩设置」区（仅 Popup 显示）——`useMask`（是否创建遮罩，默认 true）/ `maskColor`（本弹窗专属遮罩颜色，默认黑 α0.5，可设透明避免叠加变黑）/ `clickMaskToClose`（点遮罩是否关闭，默认 true）；注入链：EUIBinding → EUIBindingBridge.Attach → EUIPage（UseMask/MaskColorOverride/ClickMaskToClose，与 Logic 无关同样生效）；ShowBgMaskForPopup 按页读取（useMask 与代码层 AutoCreateClickableMask 并存，任一 false 不建遮罩；颜色页级优先、缺省回退全局 PopupMaskColor）；EUILogic.OnClickMask 默认实现检查 ClickMaskToClose（override 自定义优先） |
| 2026-08-31 | 🎬 **Animator 过渡模式（用户主导，对标 Burner）**：EUIBinding 过渡动画区新增「使用 Animator 动画」（表现动画三选一：预设渐入渐出 ↔ 方块过渡（Loading 专属，HasTransitionBlock 显隐）↔ Animator，OnXxxChanged 互斥；自定义代码仍可叠加）；约定状态名 EmberOpen/EmberClose；完成通知 = 动画尾帧事件（运行时挂 EmberPageAnimatorBridge 承接，方法名 OnEmberOpenAnimationEnd/OnEmberCloseAnimationEnd）+ 5 秒超时兜底（事件拼错/漏加不卡死页面）；无 Animator/状态时警告跳过 |
| 2026-09-01 | 🧩 **页面可选代码能力（用户主导）**：EUIBinding 页面配置新增「使用 UIUpdate」，Popup 遮罩区新增「生成遮罩创建覆写 / 生成遮罩点击钩子」；Framework 模式的 NeedUpdate/OnUpdate/Popup 覆写仅按面板勾选生成在 Lifecycle 管理块内，OnUpdate/OnClickMask 只负责转发至块外 OnUpdateUser/OnClickMaskUser，用户不手改 bool 或框架覆写；当时取消勾选仍保留用户钩子内容（已由 2026-09-02 批次收口为可选钩子）。Business 模式保持整文件用户所有与自定义删除确认；标准遮罩行为仍由 useMask/maskColor/clickMaskToClose 数据配置控制。 |
| 2026-09-01 | 🎬 **UI 过渡时序收口（方案 A，用户确认）**：普通 UI 改为 None/PresetFade/Animator/CustomCode 单一过渡负责人，不再隐式串联；修复 Animator-only 不进入动画序列、Animator/Custom 根 Alpha 不可见、子节点 Animator 帧事件桥错位、退出阶段重复交互；Popup 遮罩和下层 OnResume 延后到退出完成。EUITransitionBlock 保持 Loading 专用特殊链路：进入仍为方块扫入→Custom Enter，退出仍为 Custom Exit→方块扫出。 |
| 2026-09-01 | 🧭 **EUIBinding 页面类型互斥化（方案 A，用户确认）**：配置层由可组合 PageFlags 统一为运行时 PageType 单选；新增 FullScreenPopup 独立类型（Popup 栈/遮罩 + 隐藏下层，IsFullScreen 内部推导），旧 `pageFlags=66` 与 `Popup + isFullScreen:true` 兼容归一化；修复 SubPage codegen 误生成 MainPage，并让简单格式的已有 GamePages 条目同步路径/层级/类型。模板镜像仍待用户通过模板编辑器手动保存。 |
| 2026-09-02 | 🧰 **UI 开发中心（v0.10.0 预算内，方案经用户确认）**：原 UI 预制体管理器整合升级为 `Ember/UI/UI 开发中心`，包含「创建 UI / UI 总览 / 清理与删除」。创建页先预检 prefab、脚本、Settings 与 GamePages 目标，再生成标准 Canvas/EUIBinding、复用公共 `EUICommon_Ani.controller` 的 Animator 容器和嵌套 `EUISafeArea.prefab`，脚本触发编译后仅在成功时自动打开 Prefab；标准 CanvasScaler 固定为 Scale With Screen Size / 2560×1440 / Match 0.5，dev 六个通用 UI 同步对齐。总览保持只读；生成与维护入口统一限制为规范 `Assets/` 路径，清理/删除增加影响预览、跨 Framework/User 的注释感知 PageDef 精确查重、全 Assets 引用复核、共享脚本保护及孤儿脚本/安全空叶子 dry-run；运行时打开完成统一恢复 Animator 容器交互，覆盖 Loading 方块、Custom 与 None。模板镜像仍未同步，验证完成后必须由用户在「模板编辑器」中手动保存 base，助手不运行 `scripts/sync-scaffold.ps1`。 |
| 2026-09-02 | 🐛 **弹窗遮罩 + UIUpdate 可选钩子收口（方案经用户确认）**：① `EmberBgMask` 独立 Canvas 原为 Default Layer，而 UI Camera 仅渲染 UI Layer，导致 Scene 可见但 Game 不显示；同时缺少 GraphicRaycaster，Button 无法接收点击。修复为每次获取/复用均继承所属弹窗 Layer、补 GraphicRaycaster、排序精确为弹窗 -1。② Framework `OnUpdateUser` 从固定骨架改为仅随「使用 UIUpdate」生成；取消时默认空钩子自动删除，含用户代码则交互确认/非交互取消同步并警告；dev 未启用 UIUpdate 的 Background/Main/GamePlay/Setting 四页已清理默认空钩子，Loading/GM 保留。待 Unity 编译、Edit Mode 与 Play 回归。 |
| 2026-09-02 | ✅ **v0.10.0 测试与发布准备收口**：用户完成 Play 验证并确认本轮测试结束；Main→Gameplay 替换式状态切换在 `GameMainState.OnMainExit` 主动关闭 EUIMainPage，避免旧 MainPage 留在栈中；状态机驱动的 Setting Popup 禁用遮罩点击关闭，统一由按钮执行状态机 Pop；GM 退出按钮改为注册 `GameLauncher.Instance.Quit` 方法组。Unity 手动编译成功。严格按发布顺序先 bump `Packages/com.ember/package.json` 至 0.10.0，再新增 CHANGELOG 0.10.0，并将 UPM 默认安装 tag 对准 v0.10.0。基础模板已由用户通过模板编辑器保存为 v0.5.0 / frameworkVersion 0.10.0 / stable，五个镜像根目录逐文件一致，路径、GUID、GamePages、SafeArea、Binding 与关键状态逻辑静态复检通过。 |
