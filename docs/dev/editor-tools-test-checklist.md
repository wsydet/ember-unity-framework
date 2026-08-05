# Editor 工具测试清单

> 在 Unity 中逐个打开测试，通过打 ✅，有 bug 记下来。
> 最后更新：2026-08-05（第三轮修复后更新）

---

## 基础设施

| # | 测试项 | 操作 | 预期结果 | 结果 |
|---|--------|------|---------|------|
| I1 | EmberEditorWindow Footer | 打开任意工具窗口 | 底部显示带背景色的 "vX.X \| Ember Tools" | ✅ |
| I2 | 全局语言切换 | 打开两个不同工具窗口 → 在其中一个切换 EN/中文 | **所有**已打开窗口的标题栏、按钮文字同时切换中英文 | ✅ |
| I3 | 皮肤适配 | 暗色/亮色 Unity 主题下打开工具 | Odin 字段文字颜色可读，不出现黑字融入背景 | ✅ |
| I4 | 面板布局 🆕 | 打开任意有 Odin 配置字段的工具（FontReplacementTool / BatchRenamerEditor） | 配置区（Odin 字段）和操作区（按钮）之间布局统一、不抖动、不错位 | ⬜ |
| I5 | Assets/右键菜单 | Project 窗口右键 | 能看到 Ember 子菜单（批量重命名、查找脚本引用等） | ✅ |
| I6 | GameObject/右键菜单分隔线 🆕 | Hierarchy 右键 → Ember → 碰撞体显示 / 字体替换 / 布局助手 等子菜单 | 每个子菜单中"打开面板"与具体功能按钮之间有横线分隔 | ⬜ |
| I7 | EmberDebug 迁移 | 编译通过 | 无 CS 编译错误，所有 EmberDebug 引用正常 | ✅ |
| I8 | 消息对话框跟随语言 | 切换语言后执行任意操作（批量重命名、代码校验等） | EditorUtility.DisplayDialog 弹出文字跟随当前语言（中/英） | ✅ |
| I9 | 全局语言持久化 | 切换为英文 → 关闭所有窗口 → 重新打开任意工具 | 工具恢复上次的语言设置（英文），不重置为中文 | ✅ |

> **已知限制**：Odin 属性标签（如 `[LabelText("目标 TMP 字体")]`）是编译时常量，无法运行时切换语言。
> 这些标签当前固定在中文，后续可考虑用 Odin 的 `$` 动态标签语法逐工具改造。

---

## 菜单结构

| # | 测试项 | 操作 | 预期结果 | 结果 |
|---|--------|------|---------|------|
| M1 | 顶级菜单 Ember | 点击顶部菜单栏 Ember | 看到 Scene / Tool 两个子菜单，中间有横线分隔 | ✅ |
| M2 | Ember/Scene | Ember → Scene | 看到"跳转到 FrameworkScene"和"快速打开场景" | ✅ |
| M3 | Ember/Tool 分隔线 🆕 | Ember → Tool | 面板工具列表（批量重命名~次要纹理批量绑定）内部**无**分隔线；列表底部与 3 个维护工具（校验代码规范/清空本地缓存/删除项目空文件夹）之间有**一条**横线分隔 | ✅ |
| M4 | 旧菜单路径已移除 | 点击 Tools 菜单 | **不再**出现 Tools/Ember 子菜单 | ✅ |
| M5 | 右键分隔线（碰撞体显示） | Hierarchy 右键 → Ember → 碰撞体显示 | "打开面板"和"切换 2D/3D"之间有横线分隔 | ⬜ |
| M6 | 右键分隔线（字体替换） | GameObject/右键 → Ember → 字体替换 | "打开面板"和"替换/转换"之间有横线分隔 | ⬜ |
| M7 | 重复物体查找已移除 | GameObject/右键 → Ember | **不再**出现"查找重复物体"菜单项 | ✅ |

> **关于 M5/M6**：如果仍然看不到分隔线，这是 Unity 6 在 3 级深度菜单中的已知限制（优先级差机制在深层子菜单中可能不生效）。
> 届时可将操作按钮扁平化为 2 级结构（如 `GameObject/Ember/切换 2D 碰撞体填充`）绕过此限制。

---

## 001 — FontReplacementTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| F1 | 窗口打开 | Ember → Tool → UI 综合管理工具 | 窗口显示，配置区与操作区布局清晰不抖动 | ⬜ |
| F2 | 字体替换 | 选 TMP 字体 → 点"替换已加载场景中的 TMP 字体" | 场景中所有 TMP_Text 字体被替换 | ✅ |
| F3 | 预制体替换 | 点"替换工程预制体字体" | 所有 Prefab 中的 TMP_Text 字体被替换，进度条正常 | ✅ |
| F4 | Legacy→TMP | 场景中有旧 Text 组件 → 点"转换" | Text 变成 TextMeshProUGUI，内容/颜色/字号保留 | ✅ |
| F5 | 排除关键字 | 填关键字 → 替换 | 名字含关键字的物体被跳过 | ✅ |
| F6 | 右键快速替换 | GameObject/Ember/字体替换/替换所有 TMP 字体 | 用上次字体+上次过滤关键字一键替换，灰态校验正确 | ✅ |
| F7 | 右键转换 | GameObject/Ember/字体替换/转换 Legacy Text → TMP | 一键转换，无需开面板 | ✅ |
| F8 | 多场景标脏 | 多个场景叠加时替换字体 | 保存时所有场景的修改都不丢失 | ✅ |

---

## 002 — BatchRenamerEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| B1 | 窗口打开 | Ember → Tool → 批量重命名 | 配置区与操作区布局清晰不抖动 | ⬜ |
| B2 | 场景物体重命名 | 选 3 个物体 → 右键 Ember → 批量重命名 | 3 个物体按规则改名 | ✅ |
| B3 | 项目资源重命名 | Project 选 3 个资源 → 右键 → Assets/Ember/批量重命名 | 3 个资源按规则改名 | ✅ |
| B4 | 右键文件夹自动填充 | Project 右键文件夹 → 批量重命名 | 窗口打开且文件夹自动填入 | ✅ |
| B5 | 位数警告 → 取消 | 设编号位数 1，重命名 15 个物体 → 弹出警告 → 点"取消"或关闭弹窗 | **不执行重命名**，不弹出成功弹窗 | ⬜ |
| B6 | 位数警告 → 自动修正 | 同上 → 点"自动修正" | 位数自动补足，正常重命名 | ✅ |
| B7 | 位数警告 → 保持 | 同上 → 点"保持" | 按当前位数继续重命名 | ✅ |
| B8 | 预览 | 调参数 | 预览区域实时显示示例名称 | ✅ |

---

## 003 — LayoutHelperEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| L1 | 复制并偏移 | 选 1 个物体 → 设偏移 → 点按钮 | 新物体在原物体位置 + 偏移 | ✅ |
| L2 | UI 物体偏移 | 选 UI 物体（有 RectTransform）→ 复制偏移 | 用 anchoredPosition3D | ✅ |
| L3 | 快速打组 | 选 3 个物体 → 点打组 | 3 个物体被包进新父节点，选中父节点 | ✅ |
| L4 | 右键复制偏移 | 选物体 → GameObject/Ember/布局助手/复制并偏移 | 用上次偏移量一键操作，灰态校验正确 | ✅ |
| L5 | 右键打组 | 选多个物体 → GameObject/Ember/布局助手/快速打组 | 一键打组，灰态校验正确 | ✅ |

---

## 004 — PrefabReplacerEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| P1 | 替换 | 拖 Prefab → 选场景物体 → 点替换 | 物体被新 Prefab 替换，Transform 保留 | ✅ |
| P2 | 选项 | 去掉"保留坐标" → 替换 | 坐标不保留（用 Prefab 默认位置） | ✅ |
| P3 | 保留名称 | 勾选"保留名称" → 替换 | 新物体保持旧物体名字 | ✅ |
| P4 | 右键替换 | 选物体 → GameObject/Ember/资产替换/替换选中 | 一键替换，灰态校验正确 | ✅ |

---

## 005 — ColliderHelperEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| C1 | 2D 填充切换 | 打开 → 点"2D 填充"按钮 | 2D 碰撞体填充变透明/恢复 | ✅ |
| C2 | 右键 2D（有 Collider2D） | 选有 Collider2D → GameObject/Ember/碰撞体显示 | 2D 菜单项可点击，3D 灰掉 | ✅ |
| C3 | 右键 3D（有 Collider） | 选有 Collider → GameObject/Ember/碰撞体显示 | 3D 菜单项可点击，2D 灰掉 | ✅ |
| C4 | 右键无 Collider | 选无碰撞体 → GameObject/Ember/碰撞体显示 | 2D/3D 菜单项都灰掉 | ✅ |

---

## 006 — ColliderSnapperEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| S1 | 向下贴合 | 物体悬浮平面上方 → 向下 → 执行 | 物体贴到平面 | ✅ |
| S2 | 偏移生效 | 设 Offset = 0.5 → 执行 | 贴合后与表面保持 0.5 间距 | ✅ |
| S3 | 预览线 | 勾选 Show Preview → 移动物体 | Scene 视图显示黄色虚线 + 绿色目标线框 | ✅ |
| S4 | 右键快速贴合 | 选物体 → Ember/碰撞体贴合/向下贴合 | 一键贴到下方表面 | ✅ |
| S5 | Layer 过滤 | 设 Layer → 执行 | 只贴合指定 Layer 的碰撞体 | ✅ |
| S6 | 无表面提示 | 物体上方无碰撞体 → 执行 | Console 输出清晰提示 | ✅ |

---

## 007 — ScriptFinderEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| F1 | 拖脚本查找 | 拖 .cs → 点查找 | 列出场景中挂了这个脚本的 GameObject | ✅ |
| F2 | 输入名称查找 | 输入类名 → 查找 | 同上 | ✅ |
| F3 | 右键从 Asset 查找 | Project 中.cs 右键 → Assets/Ember/查找场景中使用此脚本的物体 | 一键查找并全选 | ✅ |
| F4 | 多场景 | 多个场景叠加时有目标脚本 | 全部找到 | ✅ |

> 注意：原 009 UnusedScriptFinderEditor 和原 007 DuplicateFinderEditor 已删除，本节重新编号为 007。

---

## 008 — MissingScriptFinderWindow

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| M1 | Prefab 扫描 | 拖一个 Prefab（含丢失脚本）→ 扫描 | 列表显示丢失脚本的节点名 | ✅ |
| M2 | 移除 | 扫描后点移除 → 确认 | 丢失脚本被清除 | ✅ |
| M3 | 右键查找 | Hierarchy 右键 → Ember → 查找丢失脚本 | 自动扫描并填充列表，灰态校验正确 | ✅ |
| M4 | 无丢失脚本 | 拖正常 Prefab → 扫描 | 弹窗"未发现丢失脚本" | ✅ |
| M5 | 列表可点击 | 点击列表中的节点 | Ping/选中对应 GameObject | ✅ |

---

## 009 — PrefabApplyTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| A1 | 扫描 Override | 改动 Prefab 实例属性 → 打开工具 → 扫描 | 列出有 Override 的 Prefab | ✅ |
| A2 | 单个 Apply | 点某个 Prefab 的"应用" | 改动写回源 Prefab | ✅ |
| A3 | 全部 Apply | 点"应用所有改动" | 全部写回 | ✅ |
| A4 | 右键 Apply | 选 Prefab 实例 → Ember/预制体改动/应用选中 | 一键 Apply，灰态校验正确 | ✅ |
| A5 | 嵌套过滤 | 场景中有嵌套 Prefab | 默认不显示嵌套 Prefab 的 Override | ✅ |
| A6 | Revert 按钮 | 点某个 Prefab 的"还原" | 改动被还原到源 Prefab 状态 | ✅ |

---

## 010 — ShadowGeneratorTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| G1 | 2D 阴影生成 | 选带 SpriteRenderer 的物体 → 生成 | 子物体 _Shadow 被创建 | ✅ |
| G2 | 阴影参数 | 调颜色/位移/层级 → 生成 | 阴影使用新参数 | ✅ |
| G3 | 右键生成 | 选物体 → Ember/2D 阴影/生成阴影 | 一键生成，灰态校验正确 | ✅ |
| G4 | 右键移除 | Ember/2D 阴影/移除阴影子物体 | 清除 _Shadow 子物体，灰态校验正确 | ✅ |
| G5 | 3D 物体调用 | 选无 SpriteRenderer → 生成 | 静默跳过，不报错 | ✅ |

---

## 011 — SecondaryTextureBinderTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| T1 | 次级纹理绑定 | 主文件夹 a.png，副文件夹 a_Emission.png → 执行 | SecondaryTexture 被绑定 | ✅ |
| T2 | Dry Run | 勾选 Dry Run → 执行 | 只打日志，不修改文件 | ✅ |
| T3 | 进度条 | 大量图片 → 执行 | 进度条显示，可取消 | ✅ |

---

## 012 — SpriteBatchImportAndPivotTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| SP1 | 批量设 Pivot | 选 Sprite 文件夹 → 选 Bottom → 应用 | 所有 Sprite 的 Pivot 改为 Bottom | ⬜ |
| SP2 | 只改某尺寸 | 选特定尺寸组 → 应用 | 只有该尺寸的 Sprite 被修改 | ⬜ |
| SP3 | 参考 Sprite | 拖 Reference → 点读取 | 导入参数被读取填充 | ⬜ |

> 注意：项目暂无图片资源，延后测试。

---

## 013 — SpriteFrameFolderReplacerTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| SF1 | 帧替换 | 新旧文件夹各 10 帧 → Safe Single → 执行 | GUID 保留，内容替换 | ⬜ |
| SF2 | 备份 | 勾选 Create Backup → 执行 | 备份文件夹在项目根目录生成 | ⬜ |
| SF3 | 预览 | 选好两个文件夹 → 刷新 | 显示帧数统计 | ⬜ |

> 注意：项目暂无图片资源，延后测试。

---

## 014 — ImageBatchSettingsEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| IB1 | 添加单元 | 点"+ 添加单元" | 新单元出现 | ⬜ |
| IB2 | 应用设置 | 设文件夹 + MaxSize=512 → APPLY | 文件夹内图片 MaxSize 变 512 | ⬜ |
| IB3 | JSON 导出/导入 | 导出 → 删单元 → 导入 | 配置恢复 | ⬜ |

> 注意：项目暂无图片资源，延后测试。

---

## 015 — ConsoleLogExporter

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| CL1 | 窗口打开 | Ember → Tool → 控制台日志导出 | 窗口显示，自动开始采集 | ⬜ |
| CL2 | 日志采集 | 进行操作（打开场景等） | 窗口实时显示新日志 | ⬜ |
| CL3 | 类型过滤 | 取消勾选 Warning → 应用过滤 | Warning 日志消失 | ⬜ |
| CL4 | 文本过滤 | 输入关键词 → 应用过滤 | 只显示含关键词的日志 | ⬜ |
| CL5 | EmberDebug SO 过滤 | 有 EmberDebugConfig 时打开 | 显示已禁用标签数量 | ⬜ |
| CL6 | 导出全部到桌面 | 点"导出全部到桌面" | 桌面生成 .txt，自动打开文件夹 | ⬜ |
| CL7 | 导出过滤后 | 设过滤 → 点"导出过滤后日志" | 只导出符合条件的日志 | ⬜ |
| CL8 | 暂停/继续 | 点暂停 → 操作 → 点继续 | 暂停期间不采集 | ⬜ |
| CL9 | 快速导出（菜单） | Ember → Tool → 导出最近 100 条日志到桌面 | 直接导 Editor.log 最近 100 行 | ⬜ |
| CL10 | 导出文件名跟随语言 | 英文模式 → 导出 | 文件名为 `Unity_Console_*.txt`；中文模式为 `Unity_控制台_*.txt` | ⬜ |
| CL11 | 导出对话框跟随语言 | 切换语言后导出 | 成功弹窗文字跟随当前语言 | ⬜ |

---

## 016 — EmberCodeValidator

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| CV1 | 编译时检查 | 写 `Debug.Log("test")` → 编译 | Console 输出 Warning | ✅ |
| CV2 | GameObject.Find 检测 | 写 `GameObject.Find("xxx")` → 编译 | Console 输出 Warning | ✅ |
| CV3 | FindObjectOfType 检测 | 写 `FindObjectOfType<Foo>()` → 编译 | Console 输出 Warning | ✅ |
| CV4 | EmberDebug 不误报 | 写 `EmberDebug.Log(TAG, "ok")` → 编译 | 不报 Warning | ✅ |
| CV5 | 手动校验 → 通过 | Ember → Tool → 校验代码规范（代码干净时） | 弹出对话框"代码规范校验通过，未发现违规" | ⬜ |
| CV6 | 手动校验 → 违规 | 写违规代码后手动校验 | 列出违规项 | ✅ |
| CV7 | 校验对话框跟随语言 | 切换英文 → 手动校验（代码干净） | 弹窗显示英文 "All code standards checks passed" | ⬜ |

---

## 017 — FileEncodingUtility + ScriptEncodingPostprocessor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| E1 | BOM 检测 | ANSI 编码 .cs → HasBOM | 返回 false | ✅ |
| E2 | 转换 | ConvertToUTF8BOM | 文件变为 UTF-8 BOM | ✅ |
| E3 | 自动转换 | 放 ANSI .cs → Reimport | 自动转为 UTF-8 BOM | ✅ |

---

## 维护工具

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| Q1 | 清空本地缓存 | Ember → Tool → 清空本地缓存 → 确认 | PlayerPrefs 和 persistentDataPath 已清空，对话框跟随语言 | ✅ |
| Q2 | 删除空文件夹 | Ember → Tool → 删除项目空文件夹 → 确认 | 空文件夹被删除，.meta 也删了，对话框跟随语言 | ✅ |
| Q3 | 维护工具名称 | Ember → Tool | 三个维护工具名称：校验代码规范 / 清空本地缓存 / 删除项目空文件夹，与前方面板工具之间有一条横线分隔 | ✅ |

---

## 快捷键

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| K1 | 跳转 FrameworkScene | 按 Ctrl+Shift+G | 跳转到 FrameworkScene（无冲突提示） | ⬜ |

---

## 兼容性检查

| # | 测试项 | 预期 | 结果 |
|---|--------|------|------|
| Z1 | 编译无报错 | 无 CS 编译错误 | ✅ |
| Z2 | 出包不包含 Editor 代码 | Build 时不报 Editor 引用错误 | ⬜ |
| Z3 | 没有 Odin 缺失报错 | 所有 Odin 属性正常解析 | ✅ |
| Z4 | Console 无异常 | 打开/使用/关闭所有工具，Console 干净 | ✅ |
| Z5 | Debug.Log 违规扫描通过 | 编译后无 "Code Check" Warning | ✅ |

---

## 本次更新记录 (2026-08-05 第三轮)

### 修复
- **I4/B1/F1**: 移除 `HasOdinFields()` 反射方法（每帧调用 + 返回值不可靠），改为 Odin 区与 DrawContent 区间固定 10px 间距，消除面板布局抖动/错位
- **M3**: 面板工具优先级压缩为 10 间隔（100→250），维护工具单独起 350，确保仅一条分隔线
- **M5/M6**: 3 级右键菜单分隔线 — 优先级差增至 50 + 补齐所有 `true` 校验方法的优先级（之前默认 1000，与实际执行方法不匹配）
- **快捷键**: `Ctrl+Shift+F` → `Ctrl+Shift+G`（原快捷键与 Unity 查找功能冲突）

### 新增
- **K1**: 快捷键测试项

### 涉及文件统计
- `EmberEditorWindow.cs` — 布局简化
- `FrameworkSceneToolbarButton.cs` — 快捷键
- `BatchRenamerEditor.cs` / `ColliderHelperEditor.cs` / `ColliderSnapperEditor.cs` / `FontReplacementTool.cs` / `LayoutHelperEditor.cs` / `MissingScriptFinderWindow.cs` / `PrefabApplyTool.cs` / `PrefabReplacerEditor.cs` / `ShadowGeneratorTool.cs` — validate 优先级补齐
- `ConsoleLogExporter.cs` / `EmberCodeValidator.cs` / `ImageBatchSettingsEditor.cs` / `ScriptFinderEditor.cs` / `SecondaryTextureBinderTool.cs` / `SpriteBatchImportAndPivotTool.cs` / `SpriteFrameFolderReplacerTool.cs` / `QuickMaintenanceTools.cs` — 菜单优先级重排

### 需要重点回归测试
- ⬜ **I4/B1/F1**: 面板布局不抖动不错位
- ⬜ **I6/M5/M6**: 右键 3 级子菜单分隔线（如果仍不显示，是 Unity 6 深层菜单已知限制）
- ⬜ **M3**: Ember/Tool 仅一条分隔线
- ⬜ **B5**: 位数警告取消后不重命名
- ⬜ **K1**: 快捷键 Ctrl+Shift+G 不冲突
- ⬜ **CV5/CV7**: 校验通过有反馈 + 跟随语言
