# Ember 0.21.5 发布说明

日期：2026-10-24。用户已授权选择性提交并发布 v0.21.5，不包含消费项目升级。下文保留本地准备阶段的核对记录；实际发布提交与远端结果以 Git tag 为准。

本地准备与必要验证已完成，核对摘要见[preparation-checks.json](evidence/release-0.21.5/preparation-checks.json)。最终候选依赖56项，仅com.ember目标tag变化；Git diff --check及新增说明本地链接检查通过，暂存区保持为空。

## 版本与选择

| 对象 | 上一发布 | 本地准备 | 说明 |
| --- | --- | --- | --- |
| com.ember | 0.21.4 | **0.21.5** | 模板修复随新框架patch交付，公共Runtime未变 |
| platformer-2d | 0.11.1 | **0.11.2** | 正式项目中心patch Bump，无字段/GUID/场景重建 |
| base | 0.8.3 | 0.8.3 | 内容/父基线未变 |
| source3d-2p5d | 0.5.0 | 0.5.0 | 未改 |
| visual-novel | 0.21.2 | 0.21.2 | 未改 |
| 模板frameworkVersion | 0.21.0 | 0.21.0 | 同major/minor补丁保留兼容声明 |

platformer-2d的 `contentHash` 与 `versionedContentHash` 均为 **`986ceff5bf74695b8c82a33347a48d7d`**。旧0.11.1基线为 `22dd884949be5e2697a2606d5da00c46`。父版本base0.8.3、父hash与快照均为 `1270ce6dfcae3c01d050c09ecfd60337`。当前编辑记录同步为0.11.2。

本地HEAD、origin/main与远端main、v0.21.4 peeled commit均为 `d86c999e8182f7da1fafda8728066ee8f395ea2a`；只读查询远端全部语义版本tag，最高为0.21.4，未发现0.21.5冲突。拟发布tag为 **v0.21.5**，本轮未创建。

## 变更

修复真实支撑下微小向上解算速度使跳跃机会耗尽的问题。新增只读接触事实，按法线及表面相对速度确认支撑，由Controller统一供地空状态、落地、地面资源和承载消费；主动起跳与上升动作消费旧接触，防止近地重授。

容差限定为真实接触的0.001u/s相对法向速度，不扩大coyote或缓冲。Feel、跳跃高度/持跳、移动惯性、全局重力、风公式及正式场景几何未改。旧bool窗口入口保留；消费端应成组吸收Player/Entity接线，不能只替换单文件。

实际文件、公开API及失败链详见[修复交付](platformer-ground-support-delivery-20261024.md)和[支撑契约](../../Assets/Game/Documentation/platformer-2d/ground-support-contract.md)。本版也包含此前已保存的平台术语字典；这是既有草稿的正式封存，不将其声称为新运行时修复。

## 已执行流程与证据

1. 核对工作区、空暂存区、分支/upstream、远端main和tags；保留无关变动。
2. MCP绑定框架工程，读取实际项目路径、模板与编辑身份；ComputeParentSyncPlan返回Synced、无冲突、无需改子内容，实际内容hash与修复候选一致。
3. 通过 `EmberProjectSetup.BumpTemplateVersion("platformer-2d",2)` 封存0.11.2，没有重新加载、覆盖Assets或手改快照。
4. 先运行 `upm-bump-version.ps1 -Version 0.21.5 -Check` 预览，再正式更新包版本；package.json仅version改变。
5. 更新CHANGELOG、候选说明及当前工作区指针；安装示例保留真实已发布的0.21.4。用正式release_metadata.py预览后生成候选release/manifest，保留历史声明和未变依赖。

复用本会话同一修复内容的 **76项相关PlayMode通过**证据，最后测试夹具的渲染/固定步等待修正后 **8组专项通过**。没有重跑全量S3/S4。每组起跳一次、确实离地，实际平均FPS与慢帧原样保留，不能称为稳定目标帧率。

静态检查：共享二进制清单/原始字节与core.autocrlf=false/true检出通过；字体静态配置通过。二进制检查首次因沙箱临时目录拒绝写入失败，改用仓库内临时目录后同一脚本通过。暂存区为空，Git `:`检查读取原基线；相关共享文件不在拟发布改动内，不能把它说成已审阅新暂存提交。

标准 `build-ai-skill-bundle.ps1 -Check` 被既有非通用模板技能增删阻止；未删除这些工作区内容。进一步核对catalog中的11项通用技能、32文件，bundle每项原始SHA256匹配manifest，工作区/HEAD/记录源提交文本一致；4个文本仅换行不同。保留原 `sourceCommit=ebba92e3892d8407c73c4a3ce6d06a4bc99329e2`，没有生成或伪造新来源。

## 发布准备验证完成

包升版后的主动MCP编译请求返回Transport closed，最初送达状态不明，未重复提交；按公共规则间隔重连，第5次恢复。恢复后确认本轮编译完成时间戳1792781030532、域重载完成1792781051102，实际加载框架0.21.5，Player程序集MVID仍为已验证的 `6a464912-5218-4752-a2b0-edefe6f05392`。Console保留6条MCP连接失败与1条域重载后恢复失败日志，未发现C#编译错误，不将连接日志误写为空Console。

正式模板引擎校验四模板实际内容、meta/GUID、contentHash与versionedContentHash一致；base为Root，三个派生模板均Synced、无冲突、无待同步内容，所有父快照/父指针一致，ValidateTemplateGraph返回0问题。见[本轮Unity验证证据](evidence/release-0.21.5/unity-validation.json)。不再需要手动编译恢复；本次准备验证完成，发布仍需后续授权与选择性提交/tag/push。

Windward没有修改、升级或部署；原台面人工复验、项目风/定制动作接入以及发行构建仍待完成。首次有效受控记录与无效前置设备诊断的范围保持原交付说明，不追加产品验收结论。

## 拟发布范围与继续步骤

拟发布范围为 `Packages/com.ember/Templates~/platformer-2d/` 的本修复及已保存术语文档、package.json、CHANGELOG、包/依赖README、当版release/manifest、仓库当前进度与交付/证据资料。Assets维护副本与模板加载差异、其他规则草稿、共享字体动态缓存、开发manifest/lock、ProjectSettings、IDE文件和其他既有改动不混入提交。

若后续授权正式发布：逐文件选择性暂存、复查候选差异及必需检查，提交后创建annotated v0.21.5并仅推送目标分支和此tag；再次查询远端避免占用冲突。无新变更时复用本次Unity验证，不重复全量测试。本次未执行暂存、提交、tag或推送。

发布后由Windward对话通过UPM Manager升级包，核对实际解析版本及0.11.1部署原稿，再三方预览0.11.2模板补丁、保护定制并增量验证。最后用户原首台人工复验，S5不能由框架自动测试代签收。
