# Ember Framework 0.13.0 发布说明

日期：2026-09-14。Tag：`v0.13.0`。归属：**框架升级**。

## AI Skill 独立安装与更新

UPM Manager 新增 **AI Skill · 独立安装与更新**。填写框架分支或标签后，可单独检查、下载和安装技能；本次固定基线为 `v0.13.0`，默认分支为 `main`。

框架仓库 `.agents/skills/` 是维护源，`catalog.json` 控制消费端可安装范围，首个发布技能为 `ember-eui-build`。下载采用 Git 浅克隆、partial clone 和稀疏检出，只展开技能目录。服务器不支持过滤时可能传输更多 Git 对象，安装范围保持不变。

安装记录保存来源提交和文件 SHA256。本地修改、未管理副本需明确选择备份覆盖；替换失败恢复旧目录。下载支持取消、超时与重试。安装不会改动其他技能、项目 Assets、依赖 manifest/lock 或模板，也不会自动运行技能脚本。

技能随此 Git tag 发布，独立于 UPM 包安装；以后可单独更新。详情见[使用与维护说明](../../Packages/com.ember/Documentation~/maintenance/ai-skills.md)。

## EUI 公开接口与 Player 修复

新增 `EUIBindingCodeGenUtility.TryRegenerateCode(EUIBinding binding, out string error)`，接收已保存于 Assets 的 Prefab 绑定并调用统一生成流程。保留用户代码与原有模式保护，不弹确认、不新建 Prefab、不主动 Refresh。

新版技能适配器通过公开 API 工作，并声明 `eui-regenerate-v1` 能力和最低框架版本 0.13.0。UnityFarm 已部署的业务适配器与调用方保留，更新技能不会自动替换 Assets 中的代码。

EUIBinding 的输出路径属性和自定义过渡源码检查增加 `UNITY_EDITOR` 隔离，处理 Player 构建引用编辑器委托的 CS0103；序列化字段不变。UnityFarm 另外报告的 Odin DLL 导入异常仍需单独处理。

## 交付与验证边界

- package、CHANGELOG、release 与 manifest 统一到 0.13.0，完整清单仍为 56 项直接依赖，仅推进 com.ember；第三方依赖和可选包安装标签不变。
- base 保持 0.6.3，source3d-2p5d 保持 0.3.5；仅推进框架兼容声明至 0.13.0，模板 Assets、内容 Hash、父快照及 GuideModule.Enabled=false 不变。
- 清理开发过程中生成的共享动态字体图集缓存；字体二进制、动态多图集与后备字体配置经过完整性检查。
- 已检查技能元数据与链接、C# / meta 配对、UPM 程序集隔离、发布声明、模板一致性和 Git 空白；已验证本地 Git 固定 tag 的稀疏下载路径，以及共享字体二进制和两种 autocrlf 干净检出。
- 已补充技能下载、安装保护/恢复、IMGUI 状态及 EUI 入口测试；Unity EditMode、实际技能安装、消费端升级与 Player 构建尚未执行。

当前 Unity MCP 不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

## 升级

已有项目通过 **Ember/UPM Manager** 升级至 **0.13.0**，完成编译后在 AI Skill 区域选择 `v0.13.0` 并检查更新。UnityFarm 的同名旧技能会作为已有项目副本提示备份覆盖；安装后重新加载 AI 会话。

本次不需要重新部署模板。不要直接修改消费项目的 manifest、lock 或 PackageCache 来升级。
