# Ember 0.20.2 发布说明

交付 platformer-2d 0.7.0 的普通跳独立竖直策略。框架包升 patch 用于分发模板新增能力；框架内核、其他模板、第三方依赖及公共 Skill bundle 不变。模板已由项目中心正式保存并 minor Bump；contentHash/versionedContentHash 均为 `8198525b8d263ba6fb9c489571171e70`。父基线 base 0.8.3 / `1270ce6dfcae3c01d050c09ecfd60337`，兼容声明保留 0.20.0。

## 能力与兼容

`PlayerOrdinaryJumpSpec` 默认关闭，通过 `PlayerJumpFeel.ConfigureOrdinaryJump` 配置合法普通起跳的恒速最短/最长保持期、松键结束保持/限速/比例削速、独立上升/下降重力、下落限速和可要求按住的顶点减重力。运行配置按值冻结；正式方案 Copy/Validate/Compare/Save 接通。

策略以起跳来源和当前运行会话区分普通跳、外部发射、墙跳及自然下落，跨 Jump/Fall 持续。实际接触地面才清理，探地命中不提前切回共享重力。动作接管、外部覆盖、墙跳、受击/死亡、停止/退出清理；暂停不续接旧按住；活跃策略拒绝即时快照。模板仍是唯一运动提交者，T01 已交付动作契约保持。

旧方案不启用新策略，继续原行为；不通过反向补偿旗跳/回弹参数隔离共享重力，不改地空水平加速、制动、转向或惯性。参数语义与生命周期详见随模板的 `Assets/Game/Documentation/platformer-2d/ordinary-jump-policy.md`。

## 验证与实测边界

Unity 6000.5.4f1，修改前 Player PlayMode 基线 594/594。新增专项 35/35、方案 EditMode 全集 75/75（含新增 1 项）已通过，0 失败/跳过；最终 PlayMode 全集 **629/629** 通过，0 失败/跳过，1120.727 秒。EditMode 在最终落地运行边界修正之前完成；之后仅改变运行边界及 PlayMode 断言，由最终 629 项全集覆盖，配置/编辑器代码未再改变。各集合重叠，不相加。

固定步 0.02、同高场地，独立策略在短/中/长按 0.04/0.12/0.6 秒下的完整飞行高度分别约 1.240/2.640/3.560，上升时间 0.14/0.24/0.34 秒，下降至实际接触地面约 0.18/0.26/0.34 秒。这些是隔离测试配置的实测，不是 Windward 最终调参目标。

外部初速度 14/10 的旗跳和回弹等价模型分别采样 48/34 步，A/B 全程 x/y 每样本差小于 1e-5；墙跳完整轨迹也逐样本比较。共享重力诊断复现初速 14 不变但高度 3.190927→1.49600029。真实渲染帧矩阵请求 30/60/144 FPS，实际平均约 29.94–30.08 / 59.89–59.98 / 144.70–146.82；固定步覆盖 0.01、0.02、1/60 和约 1/30。输入采样离散化会产生高度差异，不承诺逐帧相同。

原始结果与轨迹见 [专项证据](evidence/ordinary-jump/special-regression.json)、[EditMode 证据](evidence/ordinary-jump/editor-regression.json)。早期测试修正了零重力下落限速误判为上升保持；实测随后发现探地早于接触导致提前清理，已修复并通过专项。MCP 曾在等待结果期间断连；重连取得完整 EditMode 通过结果。短查询避免持有长请求，但未证明 MCP 根因已修复。

## 消费

正式发布后，已有项目通过 **Ember/UPM Manager** 升级框架，再保护原模板精确基线、本地修改、资源 GUID、方案、输入及存档，按跨 minor 正式流程消费 platformer-2d 0.7.0。框架包升级不自动更新已部署 Assets；不手改消费 manifest/lock，不直接覆盖 Windward。

Windward 后续自行调整普通跳配置并完成真实键盘白盒和原路线复验。**本版模板验证不代表 Windward S2 通过，不计入 S3 或完整三关验收。** 原路线只确认两级台阶，缺口准备支撑检查失败，不能据此断言缺口不可达。Mountain 旧数值为源码规则估算，非旧 Unity 实测。手柄、发行构建/IL2CPP、其他平台及项目完整挥旗/风场均未验收。

## 发布检查

项目中心保存预览只有本次 24 项差异；正式 SaveTemplate 保存 1426 个文件，Bump 后项目校验 0 错误/0 警告/0 差异，模板图无问题，父快照一致且无需应用同步。源码与已保存模板 SHA-256 一致。完整结果见 [PlayMode 回归](evidence/ordinary-jump/playmode-regression.json) 和 [模板校验](evidence/ordinary-jump/template-validation.json)。

共享字体配置、共享二进制及 autocrlf=false/true 干净检出检查通过。公共 Skill bundle 32 文件原始 SHA-256 符合 manifest，sourceCommit 与 HEAD 公共源文本一致（换行归一化）；常规生成器 Check 因既有工作区技能改动拒绝，未重建 bundle 或伪造源提交。只提交正式模板和本次发布资料；活动 Assets 模板切换、动态字体、开发依赖和其他既有修改保留。

包 URL：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.20.2`；依赖声明见 [release-0.20.2.json](../../Packages/com.ember/Dependencies~/release-0.20.2.json)。
