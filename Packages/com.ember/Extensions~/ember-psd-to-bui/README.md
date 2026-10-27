# Ember PSD Exporter 1.0.0

Photoshop UXP 源码分发目录。沿用已解析的 `com.burner.ui.prefabmaker 2.0.0-preview.17` 内 UXP 3.0.14 的解析、切图、BUI 和文件桥实现，保留上游版权与内部模块名；来源摘要见 `provenance.json`。Ember 配置不含 Burner 产品目录、素材或 Prefab GUID。`Extensions~` 不进入 Unity 编译。本目录不提供 S5 Prefab 制作器。

## 安装与入口

1. Photoshop 25.0+；本次实测 Photoshop 27.5.0。打开 Adobe UXP Developer Tools，Add Plugin 选择本目录 `manifest.json`，Load。
2. Photoshop 中打开已保存的 `*.renamed.psd`，插件面板为 **Ember PSD Exporter**。保留面板人工导出入口；本次实测采用文件桥。
3. 文件桥自动使用活动 PSD 所在目录的 `.local/maker/bridge`。先运行：

```powershell
node scripts/bridge.js inspect --root <PSD目录> --wait-ms 5000
```

按返回的 `details.path` 读取完整 inspect。导出请求格式参考试点的 `art/ui/S4/export-final-style-request.json`，但新导出必须使用**本次** document id/path/sha256、sessionId、全部 Target ID、配置摘要、时间窗口与新 requestId。请求 options 为 `mode=bui-png`、明确文件名与前缀、`reusePolicy=current-psd`。不要复用历史 document ID 或伪造 Rename 历史。

```powershell
node scripts/bridge.js call --root <PSD目录> --request <请求JSON> --wait-ms 5000
node scripts/bridge.js query --root <PSD目录> --id <原requestId>
```

CLI 原子提交依赖本地文件系统硬链接；Windows 受限沙箱可能拒绝，需使用有本地目录权限的正常终端。先确认请求是否已发布，不能因为 CLI 超时重新派发导出。`pending-claimed` 不是失败，继续 query 原 ID。终态必须检查 `productSaved`、`recordStatus`、文件摘要与错误。

覆盖首次返回 `OVERWRITE_DECISION_REQUIRED`，读取完整输出快照，确认只覆盖本次范围后，把其 `decisionSha256` 放入新请求 `outputPolicy.overwrite`。摘要失效须重新检查，不能用布尔值绕过。失败可能留下 PNG；`partialOutputPossible=true` 或 `unknown` 时保留原请求、claim、result 及实际文件，不删除现场、不自动重放。新一轮修复须用新输入和新请求，不能覆盖原请求字节。

## 已验证语法与版本

| 层次 | 版本/边界 |
| --- | --- |
| Ember 插件 | 1.0.0，manifest id `com.ember.psd.to.bui` |
| 命名规则 | 1.5.0，真实规则位于 `config/psd-naming-rules.json` |
| BUI | schemaVersion 3；协议文件 `protocol/bui.v3.schema.json` |
| 文件桥 | protocolVersion 1；inspect/export，原请求身份与不可重放 claim |
| EUI 契约 | schemaVersion 1 / toolVersion 1.1；dynamic-list-v1 |
| Ember 映射 | schemaVersion 1，必需 `ember-eui-map-v1` + `dynamic-list-v1` |
| Unity 消费器 | S5 尚未实现；不能把只读验证当作制作完成 |

真实试点语法：

```text
@Page_UIBase_Main
  @RawImage_Background @StaticTexture
  @Text_Title
  @PrefabRef_EUIActionButtonItem_Start @PrefabAsset_<真实32位GUID>
    @Text_Label @PrefabOverride_label
  @Image_Surface @9S_64_64_64_64
@Page_UIFoundation_DynamicList
  @List_Options @Ver
    @ListItem_UIFoundation_DynamicOptionRow
      @Text_Label
      @Image_Icon @DynamicTexture
      @Image_Progress
      @Group_Selected
      @Group_Disabled
```

`Page_UIBase_Main` 解析出的 BUI 名称是 `UIBaseMainPage`，映射至现有 `EUIMainPage`，不按 BUI 名字重建正式页。ListItem 提取为独立 Target，父 List 内仅为 GeneratedTarget PrefabRef。ExternalPrefab GUID 必须解析实际资产，公开槽位须查目录；SecondaryButton 的 `label` 设计槽显式映射至 `Label/TMPEx`，不能假设所有 Variant 槽位相同。

`@DynamicTexture` 不生成静态图片引用。文字保持 Text payload；PSD 文案不是运行时数据、本地化 key 或业务行为。状态用独立 Group，隐藏状态输出 `active=false`；根目标在 PSD 中隐藏不等于运行时 Page 永久禁用。EnableState/可见性与 Selectable.interactable 分开。

九宫参数顺序是 left/top/right/bottom。试点四边64，中心压缩为2，256×256设计框对应130×130 PNG。坐标为中心锚点、Y向上；节点布局使用目标坐标空间，嵌套消费者必须按协议换算，不把目标相对位置直接套为任意父级局部坐标。PNG透明裁切框与文字字形框不等于运行时布局框，长文案仍由 EUITextRowLayout/原容器约束控制。

## Rename 与校验

`scripts/run-photoshop.ps1 -Request <JSON>` 通过 Photoshop 原生脚本创建、读取、改名、着色或生成预览。创建拒绝覆盖；改名只接受 `.renamed.psd`，先校验全体 ID/原名，再改名，最后恢复可见性并保存重开。保留 source PSD，先复制工作稿；请求示例和逐层映射在 `art/ui/S4`。重新运行必须先 inspect 并核对已有工作稿，不能重复 create/盲目重放 rename。脚本不负责执行 UXP，实际导出仍通过上面的文件桥。

Ember 的运行时信息放在**独立** `*.eui-map.json`，绑定 BUI/PSD/源契约/目录/逻辑的 SHA256，不修改原始 BUI 冒充导出器支持动态生命周期：

```powershell
node scripts/verify-ember.js <仓库根> <eui-map.json> ember-eui-map-v1 dynamic-list-v1
```

这是只读结构/引用/资源/能力检查，不是完整通用 JSON Schema 引擎。它阻止缺失/变化资源、重复节点、关键未知诊断、未支持能力、缺失 List 映射、错误公开槽位和动态 Item 固定注册。消费端必须先运行等价检查；必需能力未知时阻断，禁止降级 Group。声明能力意味着消费者真正实现，不可只传参数绕过。

仅验证相关用例：`node --test scripts/verify-ember.test.js`。试点像素核对：`python scripts/check-pixels.py <仓库根>`，依赖 Pillow。测试中标为 fixture 的部分写入/丢失终态是模拟宿主，不是 Photoshop 故障注入实测。

## 限制

- S4 只验证 current-psd、BUI v3；上游保留的 retain、历史复用、SubPage/BUI v4/adaptation 不属于 Ember 支持承诺，不伪造历史启用。
- PSD 无动态绑定/事件所有权语法；必须同时交付映射和原 EUI 契约。静态 TabGroup、Dropdown、横向/网格宿主不因此获得动态能力。
- 隐藏文本没有文档合成裁片，`TEXT_REFERENCE_UNAVAILABLE` 如实保留；文字内容/样式仍在 BUI，可用独立状态预览核对。
- 字体来源 ArialMT 不是 TMP 字体映射；S5 保留当前字体或明确映射，不自动替换。原生阴影、描边、遮罩、智能对象和完整多语言效果仍为 best-effort/未实测，关键需求缺口须阻断对应消费。
- 本次未生成 CCX、未签名/发布。源码可通过 UDT 加载已实测；人工面板完整交互及干净消费项目安装尚未验收。
