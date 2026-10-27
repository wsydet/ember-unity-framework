# 美术替换与断点续接

适用于已有功能 UI 的 PSD/BUI 换肤。先在当前实际解析的 com.ember 中核对 `Documentation~/ui/bui-maker.md`、`eui-contract.md`、`prefab-library.md` 与 `dynamic-lists.md`。这些能力可能只存在于未发布工作区，版本号本身不证明可用；缺少 EmberBuiService 或对应协议时保留资料，不复制工具进 PackageCache。

## 选择起点

| 已有输入 | 下一步与产物 |
| --- | --- |
| 已保存 Prefab | EUIContractService.Export/Write，记录真实绑定、GUID、模式、PageType、用户逻辑与行为来源；准备美术目标与状态清单 |
| 源 PSD | 保留源稿，复制工作稿；读取真实图层 ID、旧契约和公共库槽位后 Rename，保存逐层映射 |
| 工作 PSD | 核对保存状态、路径和摘要；通过实际 UXP inspect/export 输出 BUI、精确引用 PNG 与文字参考 |
| BUI + EUI map | 重新导出当前契约并比较，准备本次明确制作请求；有效上游不重做 |
| 制作回执 | 先查询原操作及实际输出，按下表续接；文件改名不是安全重试 |

Photoshop 工具实际位于包内 `Extensions~/ember-psd-to-bui`。先读其 README 与当前桥接协议；inspect 是能力和文档身份来源。export 的 document 仅带协议支持的 id/path/saved/sha256，outputPolicy 必须显式带 overwrite:null 或准确覆盖决策摘要。使用新请求前核对上次请求已经终结；超时查询原 requestId。没有 Photoshop 或 UXP 时保留人工步骤与未导出状态，不合成假回执。

## 制作

1. 核对正式活动模板、模块条件、源资产与本地修改。备份待改 Prefab、meta、生成文件、用户代码与共享源摘要。内容归属按当前项目判断，试点路径不成为通用默认值。
2. 同时消费 BUI3、EUI map1、contract1/tool1.1；未知必需能力阻断。所有 Target/nodeId 明确 update/create/reference/preserve，preserve 写原因。既有节点绑定当前 objectId；嵌套引用核对精确 GUID 和实际 catalog 槽 ID，Variant 不保证槽 ID 相同。
3. `EmberBuiService.Analyze(requestPath)` 后检查 valid/issues/writes；保存 plan。`Preview(plan)` 是隔离构建诊断，会立即清理，不是驻留视觉窗口。检查后 `Apply(plan)`，读取 receipt.status，再 `VerifySaved(requestPath)`。
4. 正式创建、绑定和生成由 Maker 内部调用 EUI 中心。其他局部调整仍按本技能正文调用公开创建/绑定/生成 API，不手改 YAML、meta、Binding.cs 或 EmberManaged。原 Framework、IsFramework、PageType 与用户代码保持。
5. Label/Icon/Progress 留给原数据；稳定 key 与 index 分开，动态 Item 由宿主/lease 持有，不能另登记固定 UILogic。可见性、EnableState 与 interactable 独立验证。
6. 字体保留策略不代表 PSD 字体已映射。需要新字体时核实实际文件、授权、TMP FontAsset、材质与皮肤，按可用的 ember-add-font 接入。动态文字用工程容纳框；PSD 字形裁片不等于布局框。九宫区分设计框与 PNG 像素，border 为 left/top/right/bottom。效果或混排不受支持时说明限制，不烘焙动态文字。

## 恢复

| 实际状态 | 操作 |
| --- | --- |
| completed 且输出未变 | Analyze.noChanges、Apply 返回原回执；VerifySaved 后确认没有重复资产/类/注册 |
| saved/partial 且全部 Target 已保存、摘要未变 | ResumeGeneration，仅续生成/回读，不重新应用美术 |
| 唯一嵌套源更换造成实例根名变化 | 核对原 partial 回执与唯一精确 GUID，再使用 RecoverNestedNames |
| writing、超时或结果未知 | 停止重放，查询原请求、当前契约及实际文件；记录部分结果，明确恢复范围后续接 |
| 后续合法修改导致 OUTPUT_CHANGED | 旧回执保留历史；当前资产重新导出契约，为新改动建立独立请求，不能改旧摘要 |

小美术修改要保留上一版输入与回执，使用新工作稿/导出版本。当前服务对已经存在的 imagePath 要求字节摘要一致；新切图应使用新版本路径，不静默覆盖旧图来绕过门禁。

## 完成判定

分别留下输入摘要、计划、保存回读、生成差异、用户代码/公共源保护、编译域重载、Console、相关测试、同尺寸同状态视觉和实际交互证据。通过 Unity MCP 主动编译后再运行验证，按工程规则处理断连。

验证原导航/返回来源、遮罩阻止下层点击、三次开关事件与实例数、长文本/禁用/选中/滚动/重用、两种比例及明确安全区范围。项目皮肤在独立测试资产或项目副本修改允许的外观，保持规范名/Binding/事件，公共库源不变。

模板 Assets 有改动且已获普通保存授权时，通过项目中心保存并比较；保存、Bump、发布和消费部署分别记录。消费项目不依赖开发仓库的临时证据目录；正式使用说明随其所属模板维护。
