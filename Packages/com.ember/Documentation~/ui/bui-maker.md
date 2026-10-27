# BUI 制作

`Ember.UIExtension.Editor.EmberBuiService` 1.0.0 消费 BUI schemaVersion 3、独立 EUI map schemaVersion 1 和 EUI contract schemaVersion 1/toolVersion 1.1。入口为 **Ember → UI → UI 开发中心 → BUI 制作**。窗口、MCP C# 调用和编辑器批处理使用同一服务；不增加运行时依赖或独立包。

## 输入与 API

制作请求是 `EmberBuiRequest` JSON，使用项目相对路径。必须提供 `mapPath`、`ownerTemplateId`、独立 `receiptPath`、全部 Target 和全部 nodeId 的明确映射。`moduleType` 用于具有 `EmberModule` 特性的业务模块，指定后必须存在且启用；不依赖模块的通用 UI 可留空。活动模板必须匹配。

```csharp
var plan = EmberBuiService.Analyze(requestPath);
if (!plan.valid) throw new InvalidOperationException(string.Join("\n", plan.issues));
var diagnostic = EmberBuiService.Preview(plan);
var receipt = EmberBuiService.Apply(plan);
// 检查 receipt.status，不把 API 返回当作制作成功。
var verification = EmberBuiService.VerifySaved(requestPath);
```

- `Analyze`：只读检查输入、摘要、真实身份、创建冲突与写入范围，输出固定 digest。输入、输出或请求变化后必须重新分析。
- `Preview`：在隔离 Prefab contents 中构建、检查并清理临时纹理和对象，不保存正式资产。首版返回诊断，**不是可驻留的可视化预览窗口**；视觉验收需另行在 Game View 或候选页进行。
- `Apply`：重新 Analyze 比较 digest，再执行隔离构建预检，备份已有受影响文件及 meta，先写 reserved 回执，再按 Target 依赖排序创建、编辑、保存、正式生成、回读。
- `VerifySaved`：核对回执身份、当前输出摘要、契约、绑定、嵌套源和共享源；只读返回结果，不改回执。
- `ResumeGeneration`：仅接受全部 Target 已保存且输出未变化的 saved/partial 回执，补做缺失生成与回读，**不重新应用美术**。
- `RecoverNestedNames`：针对 Unity 更换嵌套源重置实例名的窄恢复接口。仅处理原 partial 回执中可由唯一精确源 GUID 定位的实例，然后续接生成。
- `Write`：原子替换 JSON 证据/回执。回执存在时不得通过换文件名重放结果未知的请求。

## 明确映射

Target 的 `mode` 为 `update`、`create` 或 `candidate`。更新绑定真实 `prefabGuid`/`prefabPath`；创建使用 `sourceSkeletonPath`、`className`、`classPath`、Page 内容槽 `contentPath` 及 `inheritBinding`/`structureOnly`，经过 `EUICreationService.TryBuildPlan/Create`。Page 为源骨架 Variant，候选保留旧资源，不自动替换工程全部消费者。

节点 `operation` 为 `update/create/reference/preserve`。每个节点必须明确处理；preserve 必须有原因。已有节点按 `expectedObjectId` 定位，名字不替代身份。`reference` 只用于 GeneratedTarget，由动态宿主引用独立模板，不生成父页固定 Item。

`geometry`、`appearance`、`active` 分别决定几何、外观、是否应用 BUI 可见性。`active=false` 表示不写可见性，不等于对象隐藏。`stateSlot` 把 selected/disabled 映射到现有 EUIToggleEx，interactable 继续由业务驱动。

`replaceSourceGuid` 显式声明原嵌套源，目标源从 BUI 和 publicReferences 取值。这种替换可能改变对象 local ID；保持外层页面 GUID 和绑定接口不等于所有内部对象身份不变。公开槽覆盖经 `EUILibraryService.ResolveInstanceSlot` 验证，支持外层 Page Variant 中的嵌套实例，不写共享源和私有内部节点。

## 支持边界

- 能力声明：`ember-eui-map-v1`、`dynamic-list-v1`。未知必需能力、节点类型、关键命名选项、重复身份、循环、资源缺失/摘要变化、未解析文本及不支持效果阻断。
- 支持 Group/Bg/Top/Center/Bottom、Image/RawImage、Text、ExternalPrefab/GeneratedTarget、单模板纵向 List。不是完整通用 JSON Schema 引擎，也不执行任意业务表达式。
- 图片只按 BUI 精确引用校验，按请求 `imagePath` 导入。Sprite Single、100 PPU、无 mipmap、Uncompressed、Clamp/Bilinear；九宫由 left/top/right/bottom 转为 Unity left/bottom/right/top。设计框与 PNG 像素尺寸分开，不按目录批量导入。
- 几何只支持目标中心坐标、中心 anchor/pivot 与无旋转缩放路径。通过锚点帧计算，兼容 Canvas 零缩放/零尺寸的磁盘状态及 EUISafeArea 驱动的矩形；设计时采用完整安全区，刘海/异形屏需运行验证。可显式 `applyReferenceResolution`，默认不改 CanvasScaler。
- `copyLayoutPath` 沿用工程布局，例如进度满宽；`textBoxSize` 显式扩展静态文本容纳框并保留左上位置。动态长文案继续使用 EUITextRowLayout，不能用字形裁切框代替容纳框。
- 文字外观首版只应用字号和颜色。已有文字内容、字体、材质、皮肤、对齐保留；`textPolicy=design` 仅允许新建静态文字，且须显式 `newTextFontPath`。字体名称不自动映射 TMP 资产；ArialMT/字距/行距/描边/阴影/复杂混排/全语言覆盖不宣称还原。
- 动态 Icon 不静态赋图，Label/Progress/事件由原用户逻辑和 lease 更新。保留宿主、Content/Viewport、dataBinding、cacheCapacity、owner、refresh。候选页仅切换本候选宿主到其新建 Item 模板；其他消费者保持原引用。

## 恢复与复用

备份位于 `.utmp/EmberBui/<planDigest>/`，回执记录 saved、generated、after 和错误。`writing:<target>` 表示结果需检查，不能重放 Apply。先导出当前契约、核对实际保存阶段和文件摘要，再为已知资产准备显式恢复请求；保留原回执并记录恢复请求链接。首版不承诺跨 Unity 崩溃事务回滚或自动修复所有部分目标。

同请求完成后 Analyze 返回 `noChanges=true`，Apply 返回原回执，不再写资产。后续其他合法制作改变同一资产或公共注册文件后，旧请求会报告 OUTPUT_CHANGED；应使用最新交接快照，不更新旧摘要绕过保护。生成脚本的摘要只额外接受项目编码导入器添加 UTF-8 BOM 这一精确字节变换，其他变化仍阻断。

制作后显式通过 Unity MCP 请求编译、确认域重载并检查 Console；独立记录结构回读、视觉与真实运行结果。参见 [契约](eui-contract.md)、[公共库](prefab-library.md)、[动态列表](dynamic-lists.md)。
