# Ember Framework 0.12.7 发布说明

日期：2026-09-12。Tag：`v0.12.7`。归属：**框架 + 模板升级**。

## 引导步骤编辑器

参考 Burner 的左右分栏与阶段编辑，在模板业务模块 `Assets/Game/Module/Guide/Editor` 中实现独立窗口。左侧新增、深复制、删除和拖拽排序步骤；右侧用中文配置开始/结束事件、条件、执行器及嵌套 AND/OR。根据类型创建参数，重建错误参数需显式点击，支持 Undo，保存只写当前引导资产。

页面选择读取 GamePages 和 EUIBinding 中文用途，填入路径、层级和页面类型；目标节点选择与现有运行器按名称递归查找的行为一致。配置检查提示参数错配、空条件组、缺少完成唤醒来源等问题；不是运行预览，也不会自动为按钮接线。

`Ember/引导/引导编辑器` 菜单跟随 GuideModule 的 EmberModuleAttribute.Enabled，关闭时隐藏，启用并编译后显示。动态菜单通过反射适配 Unity 内部菜单 API，版本不兼容会输出诊断；Inspector 和双击资产入口保留。

资产打开回调直接接收 `EntityId` 并调用 `EditorUtility.EntityIdToObject`，修复旧 API 的 CS0619 及 int 转换的 CS0618。已只读核对本机 Unity 6000.5.4f1 程序集支持该回调签名；这不等于编译验证。

## UI 开发中心保存后布局修复

此前 OnProjectChange 清空目录后，工具栏在 Layout 中不画统计标签，随后 EnsureCatalog 重新扫描；Repaint 又多出标签，触发 Invalid GUILayout state / Getting control position 异常。

现在保存/导入通知仅标记待刷新，当前绘制保留目录快照。扫描、统计及编译/资源更新提示统一在新一轮 Layout 开始时更新；手动刷新也走同一流程，避免在工具栏绘制中改变控件数量。

## 模板与依赖

| 模板 | 版本 | 内容与封存 Hash | 父基线 |
|---|---|---|---|
| base | 0.6.3 / stable | `d822aa4b70ceedad14bac56c1989303b` | 无 |
| source3d-2p5d | 0.3.5 / preview | `5b72df281ab2916646b5ade4e137fdd5` | base 0.6.3 |

两份模板由用户通过模板面板保存并 Bump，实际内容、封存 Hash、ParentSnapshot 与当前编辑记录一致；引导编辑器及 meta 在两个模板中一致。两份模板的 **GuideModule 均为 Enabled=false**，保留框架兼容声明 0.12.0，按 major.minor 规则兼容本补丁。

完整依赖清单仍为 56 项，仅把 com.ember 推进至 v0.12.7；第三方继续复用 ember-v0.11.1。发布不包含动态字体缓存、自动生成 slnx、无内容差异的 Table 状态以及本地转模板备份记录。

## 验证与升级

静态核对模板内容/父快照/编辑记录、编辑器副本与 Enabled=false、原生 EntityId 签名、版本与依赖一致性、Git 空白，以及共享字体配置、二进制完整性和干净检出。

新增 5 个 EditMode 案例：开发工程 `Assets/Tests/Editor/GuideEditor` 的 3 个案例覆盖深复制、新建空步骤与 Undo、只读校验；随包 EUICatalogLayoutEditTests 的 2 个案例覆盖保存通知保留快照和实际 IMGUI 重绘。以上案例均未执行，窗口交互与消费端升级仍待验证。

当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

已有项目通过 `Ember/UPM Manager` 升级框架，代理不直接修改消费端 manifest/lock。UI 开发中心布局修复随包生效；新引导编辑器属于模板 Assets，单独升级框架不会覆盖既有业务代码，采用它需另行通过项目中心的模板流程处理并核对业务改动。用户计划在 UnityFarm 自行启用引导观察，本次发布不代表已为 UnityFarm 安装、启用或完成验收。
