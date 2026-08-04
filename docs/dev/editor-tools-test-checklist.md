# Editor 工具测试清单

> 在 Unity 中逐个打开测试，Mark 通过的打 ✅，有 bug 的记下来。

---

## 基础设施

| # | 测试项 | 操作 | 预期结果 | 结果 |
|---|--------|------|---------|------|
| I1 | EmberEditorWindow 基类 | 打开任意工具窗口 | 窗口显示 Ember Tools footer，右键有 GameObject/Ember 菜单 | ⬜ |
| I2 | 语言切换 | 打开任意工具，点 EN/中文 按钮 | UI 文字实时切换中英文 | ⬜ |
| I3 | Assets/Ember 右键菜单 | Project 窗口右键 | 能看到 Ember 子菜单（批量重命名、查找脚本引用等） | ⬜ |
| I4 | GameObject/Ember 右键菜单 | Hierarchy 右键 | 能看到 Ember 子菜单（批量重命名、碰撞体贴合等） | ⬜ |

---

## 001 — FontReplacementTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| F1 | 窗口打开 | Tools → Ember → UI 综合管理工具 | 窗口正常显示 | ⬜ |
| F2 | 字体替换 | 选一个 TMP 字体 → 点"替换场景中的 TMP 字体" | 场景中所有 TMP_Text 字体被替换 | ⬜ |
| F3 | 预制体替换 | 点"替换工程预制体字体" | 所有 Prefab 中的 TMP_Text 字体被替换，进度条正常 | ⬜ |
| F4 | Legacy→TMP | 场景中有旧 Text 组件 → 点"转换" | Text 变成 TextMeshProUGUI，内容/颜色/字号保留 | ⬜ |
| F5 | 排除关键字 | 填关键字 → 替换 | 名字含关键字的物体被跳过 | ⬜ |
| F6 | 右键快速替换 | GameObject/Ember/字体替换/替换所有 TMP 字体 | 用上次字体一键替换，弹窗显示数量 | ⬜ |
| F7 | 右键转换 | GameObject/Ember/字体替换/转换 Legacy Text → TMP | 一键转换，无需开面板 | ⬜ |
| F8 | 多场景标脏 | 多个场景叠加时替换字体 | 保存时所有场景的修改都不丢失 | ⬜ |

---

## 002 — BatchRenamerEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| B1 | 窗口打开 | Tools → Ember → 批量重命名 | 窗口显示，预览区域有示例 | ⬜ |
| B2 | 场景物体重命名 | 选 3 个物体 → 右键 Ember → 批量重命名 → 点按钮 | 3 个物体按规则改名 | ⬜ |
| B3 | 项目资源重命名 | Project 选 3 个资源 → 右键 → 批量重命名 | 3 个资源按规则改名 | ⬜ |
| B4 | 文件夹模式 | 拖文件夹 → 点重命名 | 文件夹内所有内容按规则改名，文件夹自身也改名 | ⬜ |
| B5 | 位数警告 | 设编号位数 1，重命名 15 个物体 | 弹窗提示需要 2 位数 | ⬜ |
| B6 | 预览 | 调参数 | 预览区域实时显示示例名称 | ⬜ |

---

## 003 — LayoutHelperEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| L1 | 复制并偏移 | 选 1 个物体 → 设偏移 → 点按钮 | 新物体在原物体位置 + 偏移 | ⬜ |
| L2 | UI 物体偏移 | 选 UI 物体（有 RectTransform）→ 复制偏移 | 用 anchoredPosition3D | ⬜ |
| L3 | 快速打组 | 选 3 个物体 → 点打组 | 3 个物体被包进新父节点，选中父节点 | ⬜ |
| L4 | 右键复制偏移 | 选 1 个物体 → GameObject/Ember/布局助手/复制并偏移 | 用上次偏移量一键操作 | ⬜ |
| L5 | 右键打组 | 选多个物体 → GameObject/Ember/布局助手/快速打组 | 一键打组 | ⬜ |

---

## 004 — PrefabReplacerEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| P1 | 替换 | 拖 Prefab → 选场景物体 → 点替换 | 物体被新 Prefab 替换，Transform 保留 | ⬜ |
| P2 | 选项 | 去掉"保留坐标" → 替换 | 坐标不保留（用 Prefab 默认位置） | ⬜ |
| P3 | 保留名称 | 勾选"保留名称" → 替换 | 新物体保持旧物体名字 | ⬜ |
| P4 | 右键替换 | 选物体 → GameObject/Ember/资产替换/替换选中为上次预制体 | 一键替换 | ⬜ |

---

## 005 — ColliderHelperEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| C1 | 2D 填充切透明 | 打开 → 点"2D 填充"按钮 | 2D 碰撞体填充变透明/恢复，Scene 视图实时刷新 | ⬜ |
| C2 | 右键 2D | GameObject/Ember/碰撞体显示/切换 2D 碰撞体填充 | 同上，不开窗口 | ⬜ |
| C3 | 3D Gizmos 切换 | 点"Gizmos"按钮 | Scene 视图 Gizmos 开/关切换 | ⬜ |
| C4 | 右键 3D | GameObject/Ember/碰撞体显示/切换 3D Gizmos | 同上 | ⬜ |

---

## 006 — ColliderSnapperEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| S1 | 向下贴合 | 物体悬浮平面上方 → 设方向 Down → 执行 | 物体贴到平面 | ⬜ |
| S2 | 预览线 | 勾选 Show Preview → 移动物体 | Scene 视图显示黄色虚线 + 绿色目标线框 | ⬜ |
| S3 | 右键快速贴合 | 选物体 → GameObject/Ember/碰撞体贴合/向下贴合 | 一键贴到下方表面 | ⬜ |
| S4 | Layer 过滤 | 设 Layer → 执行 | 只贴合指定 Layer 的碰撞体 | ⬜ |

---

## 007 — DuplicateFinderEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| D1 | 扫描重复 | Ctrl+D 复制一个物体（同位置同网格）→ 扫描 | 找到重复物体，自动选中 | ⬜ |
| D2 | 删除 | 扫描后点"删除" → 确认 | 重复项被删，源保留 | ⬜ |
| D3 | 右键扫描 | GameObject/Ember/查找重复物体 | 一键扫场景并选中重复物体 | ⬜ |
| D4 | UI 过滤 | 场景中有 UI 物体 | UI 物体不被检测（RectTransform 跳过） | ⬜ |

---

## 008 — ScriptFinderEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| F1 | 拖脚本查找 | 拖一个 .cs 到窗口 → 点查找 | 列出场景中所有挂了这个脚本的 GameObject | ⬜ |
| F2 | 输入名称查找 | 输入类名 → 查找 | 同上 | ⬜ |
| F3 | 右键从 Asset 查找 | Project 中.cs 文件右键 → Assets/Ember/查找场景中使用此脚本的物体 | 一键查找并全选 | ⬜ |
| F4 | 多场景 | 多个场景叠加时有目标脚本 | 全部找到 | ⬜ |

---

## 009 — UnusedScriptFinderEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| U1 | 全项目扫描 | 打开 → 点扫描 | 列出未被 Scene/Prefab/SO 引用的脚本 | ⬜ |
| U2 | 过滤 Editor | 勾选 Ignore Editor → 扫描 | Editor 文件夹内的脚本不显示 | ⬜ |
| U3 | 过滤 Plugins | 勾选 Ignore Plugins → 扫描 | Plugins 文件夹内的脚本不显示 | ⬜ |
| U4 | 右键单文件检查 | Project 中 .cs 文件右键 → Assets/Ember/查找此脚本的引用 | 弹窗显示有没有引用 | ⬜ |
| U5 | 右键全项目扫描 | Project 右键 → Assets/Ember/扫描未引用脚本 | 打开窗口并自动扫描 | ⬜ |

---

## 010 — MissingScriptFinderWindow

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| M1 | Prefab 扫描 | 拖一个 Prefab（含丢失脚本）→ 扫描 | 找到丢失脚本的节点 | ⬜ |
| M2 | 移除 | 扫描后点移除 → 确认 | 丢失脚本被清除 | ⬜ |
| M3 | 右键查找 | Hierarchy 右键 → Ember → 查找丢失脚本 | 自动扫描选中物体 | ⬜ |
| M4 | 无丢失脚本 | 拖正常 Prefab → 扫描 | 弹窗"未发现丢失脚本" | ⬜ |

---

## 011 — PrefabApplyTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| A1 | 扫描 Override | 改动 Prefab 实例属性 → 打开工具 → 扫描 | 列出有 Override 的 Prefab | ⬜ |
| A2 | 单个 Apply | 点某个 Prefab 的"应用" | 改动写回源 Prefab | ⬜ |
| A3 | 全部 Apply | 点"应用所有改动" | 全部写回 | ⬜ |
| A4 | 右键 Apply | 选 Prefab 实例 → GameObject/Ember/预制体改动/应用选中到预制体 | 一键 Apply | ⬜ |
| A5 | 嵌套过滤 | 场景中有嵌套 Prefab | 默认不显示嵌套 Prefab 的 Override | ⬜ |

---

## 012 — ShadowGeneratorTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| G1 | 2D 阴影生成 | 选带 SpriteRenderer 的物体 → 生成 | 子物体 _Shadow 被创建，SpriteRenderer 已配置 | ⬜ |
| G2 | 阴影参数 | 调颜色/位移/层级 → 生成 | 阴影使用新参数 | ⬜ |
| G3 | 右键生成 | 选物体 → GameObject/Ember/2D 阴影/生成阴影 | 一键生成 | ⬜ |
| G4 | 右键移除 | GameObject/Ember/2D 阴影/移除阴影子物体 | 清除 _Shadow 子物体 | ⬜ |
| G5 | 3D 物体调用 | 选无 SpriteRenderer 的 3D 物体 → 生成 | 静默跳过，不报错 | ⬜ |

---

## 013 — SecondaryTextureBinderTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| T1 | 次级纹理绑定 | 主文件夹有 a.png，副文件夹有 a_Emission.png → 设后缀 _Emission → 执行 | a.png 的 SecondaryTexture 被绑定 | ⬜ |
| T2 | Dry Run | 勾选 Dry Run → 执行 | 只打日志，不修改文件 | ⬜ |
| T3 | 进度条 | 大量图片 → 执行 | 进度条显示，可取消 | ⬜ |

---

## 014 — SpriteBatchImportAndPivotTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| SP1 | 批量设 Pivot | 选 Sprite 文件夹 → 选 Bottom → 应用 | 文件夹内所有 Sprite 的 Pivot 改为 Bottom | ⬜ |
| SP2 | 只改某尺寸 | 选特定尺寸组 → 应用 | 只有该尺寸的 Sprite 被修改 | ⬜ |
| SP3 | 参考 Sprite | 拖一个 Sprite 到 Reference → 点读取 | 导入参数从该 Sprite 读取填充 | ⬜ |
| SP4 | 右键打开 | Project 中文件夹右键 → Assets/Ember/批量修改 Sprite 锚点 | 打开窗口并自动加载文件夹 | ⬜ |

---

## 015 — SpriteFrameFolderReplacerTool

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| SF1 | 帧替换 | 新旧文件夹各 10 帧 → Safe Single → 执行 | 旧文件夹变成新图片，GUID 保留 | ⬜ |
| SF2 | 备份 | 勾选 Create Backup → 执行 | 备份文件夹在项目根目录生成 | ⬜ |
| SF3 | 预览 | 选好两个文件夹 → 刷新 | 显示帧数统计 | ⬜ |
| SF4 | 右键打开 | Assets/Ember/Sprite 帧替换 | 打开并自动填第一个文件夹 | ⬜ |

---

## 016 — ImageBatchSettingsEditor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| IB1 | 添加单元 | 点"+ 添加单元" | 新单元出现 | ⬜ |
| IB2 | 应用设置 | 设文件夹 + MaxSize=512 → 点 APPLY | 文件夹内图片 MaxSize 变 512 | ⬜ |
| IB3 | 平台覆写 | 设 Android MaxSize=2048 → 应用 | 只有 Android 平台被覆写 | ⬜ |
| IB4 | JSON 导出/导入 | 导出 → 删单元 → 导入 | 配置恢复 | ⬜ |
| IB5 | 右键添加 | Project 中文件夹右键 → Assets/Ember/添加到图片批量设置 | 自动创建单元并打开窗口 | ⬜ |

---

## 017 — FileEncodingUtility + ScriptEncodingPostprocessor

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| E1 | BOM 检测 | 用 ANSI 编码创建 .cs 文件 → Tools 里调 HasBOM | 返回 false | ⬜ |
| E2 | 转换 | ConvertToUTF8BOM | 文件变为 UTF-8 BOM | ⬜ |
| E3 | 自动转换 | 放一个 ANSI .cs 到项目 → Reimport | 自动转为 UTF-8 BOM | ⬜ |

---

## 维护工具

| # | 测试项 | 操作 | 预期 | 结果 |
|---|--------|------|------|------|
| Q1 | 清空本地缓存 | Tools/Ember/清空本地缓存 → 确认 | PlayerPrefs 和 persistentDataPath 已清空 | ⬜ |
| Q2 | 删除空文件夹 | Tools/Ember/删除项目空文件夹 → 确认 | 空文件夹被删除，.meta 也删了 | ⬜ |

---

## 兼容性检查

| # | 测试项 | 预期 | 结果 |
|---|--------|------|------|
| Z1 | 编译无报错 | 无 CS 编译错误 | ⬜ |
| Z2 | 出包不包含 Editor 代码 | Build 时不报 Editor 引用错误 | ⬜ |
| Z3 | 没有 Odin 缺失报错 | 所有 Odin 属性正常解析 | ⬜ |
| Z4 | Console 无异常 | 打开/使用/关闭所有工具，Console 干净 | ⬜ |
