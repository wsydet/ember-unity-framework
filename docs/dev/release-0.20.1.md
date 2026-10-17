# Ember 0.20.1 发布说明

Ember **0.20.1** / tag **v0.20.1** 交付已由项目中心正式保存并封存的 **platformer-2d 0.6.0**。本次框架包升 patch，用于分发模板升级；框架内核、第三方依赖和公共 Skill bundle 未变。其余模板保持 base 0.8.3、source3d-2p5d 0.5.0、visual-novel 0.20.1，兼容声明均保留 0.20.0。

## 交付内容

- PlayerActionState / PlayerActionSession / PlayerActionLease：真实 FSM 会话与激活代数、七组职责、部分释放、合法结束及陈旧 owner 隔离；沿现有 Motor 写入链执行。
- 零速度/零重力悬停、持续速度、一次性反冲脉冲，以及释放轴后的外部运动恢复。
- 弹射接受前退出事务，保留 Busy/Guard 拒绝；死亡、反应、禁用、销毁、转场、准备失败清理。
- IPlayerConfigurationCapture：项目可变 SO 冻结、完整独立复制和恢复候选生命周期。动作暂态拒绝即时快照，已提交成果走检查点组合保存。
- 公开调用顺序、优先级、生命周期和独立消费程序集例子随模板交付。详见 [T01 维护与证据索引](mountain-rebuild/12-t01-action-contract.md)。

模板 contentHash / versionedContentHash：`3c18689d3f9099062436d8eb107b2c7e`。父基线 base 0.8.3 / `1270ce6dfcae3c01d050c09ecfd60337`。本次不重复模板 Bump、不重写快照或部署记录。

## 本轮验证与边界

Unity 6000.5.4f1：Player PlayMode 全集 587/587；最后增量后受影响专项 77/77（含动作契约 29 项）；方案 EditMode 37/37，均无失败/跳过。测试集合重叠，不相加；587 全集未在最后增量后重复，最后增量由 77 项覆盖。源码 SHA-256 与测试后的清单一致，复用这些同一内容的本轮证据。

框架自己的独立存储正式 Main→Gameplay→Main 重放通过：真实 Input System 起跳、旧 ReplaceVelocity 下落复现、新动作悬停、真实 FSM/Animator、暂停、弹射、旧 owner、快照拒绝与转场清理。该重放通过公开转换入口，不是主菜单鼠标点击测试。

长测试会话连续无 Domain Reload 重放曾出现 UI 观察者的已销毁 RectTransform 日志；域重载后完成正式重放，该热重启 UI 问题未在本版修复。Windward 消费和 S2、完整产品挥旗/风场、手柄、发行构建/IL2CPP/其他平台均未验收，不引用 0.20.0 的发行构建结果代替本版验证。

## 获取与消费

已有项目通过 **Ember/UPM Manager** 选择 **0.20.1** 并等待解析与编译。包 URL：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.20.1`；已有项目不手改 manifest/lock。依赖声明见 [release-0.20.1.json](../../Packages/com.ember/Dependencies~/release-0.20.1.json)。

框架包升级不会修改已部署 Assets。Windward 仍由原实施对话维护：保护 0.5.0 精确基线、业务修改、资源 GUID、方案、输入及存档，再按跨 minor 冲突恢复流程消费 0.6.0。禁止直接完整覆盖现有工程。FlagWave、风规则和关卡仍属于项目；本版不代表 Windward S2 通过。

## 发布检查

2026-10-18 03:16 Unity MCP 发布检查：包版本 0.20.1，scriptCompilationFailed=false，Console 0 错误；项目中心 0 错误、0 警告、0 差异。30 个受测 Player 文件与活动 Assets、正式保存模板的 SHA-256 一致。

03:17 四模板经项目中心的实际存储/封存/父谱系检查，均 0 错误、0 警告。候选提交共享二进制完整性通过（含 core.autocrlf=false/true 干净检出），共享字体配置通过；工作区动态字体数据未暂存。证据文本只清理栈行尾空格，断言、数值和异常内容保留。

公共 Skill bundle 不变：常规生成器 Check 因工作区模板发现副本改动而拒绝；另行核对 32 文件原始 SHA-256 全部符合 bundle manifest，记录的 sourceCommit、HEAD 与公共维护源文本逐文件一致（仅归一化文本换行）。没有删除已有技能改动、重建 bundle 或伪造 sourceCommit。依赖 manifest 相比 0.20.0 只有 com.ember tag 改变；新发布声明补齐已有 ember-vn-sync-images 目录项，不改历史声明。

只纳入已保存模板和本次发布资料。活动模板切换、动态字体数据、开发依赖及其他已有改动保留在工作区，不混入发布。
