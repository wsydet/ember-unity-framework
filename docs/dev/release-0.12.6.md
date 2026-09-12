# Ember Framework 0.12.6 发布说明

日期：2026-09-12。新 tag：`v0.12.6`。归属：**框架 + 模板升级**。

## EUI 用途与删除保护

EUIBinding 增加「UI 用途」文本，Page 与 Item 均可填写中文说明。UI 开发中心总览、清理与删除列表突出显示用途并保留原始名称和路径，总览支持按用途筛选；保存 Prefab 后目录刷新。绑定快照保留用途，不改变类名、页面标识和代码生成。

受保护 UI 在 Inspector、总览和维护列表显示禁止删除状态，面板不提供解除入口。删除按钮禁用，删除服务在预览和执行阶段重新检查真实 Prefab；陈旧或伪造计划、清空目录缓存标记也不能绕过执行检查。保护范围是 UI 开发中心及其删除服务，不是对操作系统文件删除或模板切换的全局限制。

## 已封存模板

两个模板由用户通过模板开发面板保存并 Bump，本次发布未手工修改模板快照或 Hash。

| 模板 | 版本 | 内容与封存 Hash | 父基线 |
|---|---|---|---|
| base | 0.6.1 / stable | `b293c611af0ae8b4b78000977e071652` | 无 |
| source3d-2p5d | 0.3.3 / preview | `62ca2df7346545478e164357ec1f6a12` | base 0.6.1 |

派生 ParentSnapshot 的实际 Hash 与 base 内容及 parentContentHash 一致。当前项目编辑记录指向 source3d-2p5d 0.3.3，Hash 一致；两份模板框架兼容声明保留 0.12.0，按 major.minor 规则兼容本补丁。

base 的 EUIBackgroundPanel、EUIMainPanel、EUIGamePlayPanel、EUISettingPanel、EUILoadingPanel、GMPanel 均已填写中文用途并启用删除保护。派生模板保留用户保存的覆盖：EUIGamePlayPanel 用途为「游戏主UI」且未保护，另外五个基础 UI 继承保护；EUISceneUIPanel 与 EUIBaseBubbleItem 分别填写「场景物体UI」「场景基础气泡」，未保护。

## 依赖与验证

交付 package.json、CHANGELOG、`Dependencies~/manifest-0.12.6.json`、`release-0.12.6.json`、框架源代码和测试、两个模板及对应开发副本。完整依赖声明仍为 56 项，仅推进 com.ember 到 v0.12.6，第三方继续使用 ember-v0.11.1。工作区的动态字体缓存和自动生成 slnx 不进入此次发布。

静态检查覆盖模板实际内容/封存 Hash、父快照、编辑记录、版本与依赖声明、Git 空白检查，以及既有共享字体配置、二进制完整性和干净检出检查。

新增 EUIDeletionProtectionEditTests 的 4 个案例：清空目录保护标记仍拒绝预览、陈旧计划执行时重新检查、伪造计划不能绕过保护、普通未保护 Prefab 仍可删除。调整已有无代码生成计划测试，避免使用真实受保护基础 UI。新增案例尚未执行。

当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

手动验收：修改并保存用途后核对总览、搜索与维护列表；确认 base 六个 UI 显示保护状态且不可删除，运行新增 EditMode 案例；切换两个模板后核对用途、保护配置及原有 UI 流程。

## 消费端升级

已有项目通过 `Ember/UPM Manager` 选择 v0.12.6，代理不直接修改消费端 manifest/lock。升级框架不会自动覆盖已部署的业务 Assets；采用新模板的用途与保护字段需另行通过项目中心的模板流程处理，并先核对现有业务改动。发布不代表已为任何消费项目安装或完成验收。
