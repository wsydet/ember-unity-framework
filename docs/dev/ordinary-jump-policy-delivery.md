# 普通跳独立竖直策略交付记录

2026-10-19：实现、专项与完整 Player 回归完成；项目中心正式保存并封存 platformer-2d 0.7.0，Ember 0.20.2 发布资料完成。远端发布结果以发布提交/tag 核对为准。

## 范围与接口

归属模板升级，不改变框架内核。Unity 6000.5.4f1；新模板 hash `8198525b8d263ba6fb9c489571171e70`，父基线 base 0.8.3 / `1270ce6dfcae3c01d050c09ecfd60337`，兼容声明保持 0.20.0。其他模板、依赖和公共 Skill bundle 不变。

已完整阅读 Windward 交接单并只读核对真实消费基线 Ember 0.20.1 / platformer-2d 0.6.0 / Unity 6000.5.4f1。修改前受管 Assets 与旧模板无差异。工作区模板切换和其他既有改动保留，不混入发布。

正式接口与生命周期见[普通跳独立竖直策略](../../Packages/com.ember/Templates~/platformer-2d/Assets/Game/Documentation/platformer-2d/ordinary-jump-policy.md)，版本及消费说明见 [release-0.20.2.md](release-0.20.2.md)。

## 实际验证

- 修改前基线 594/594：[baseline.json](evidence/ordinary-jump/baseline.json)。
- 专项 35/35，0 失败/跳过：[special-regression.json](evidence/ordinary-jump/special-regression.json)，含完整轨迹和实测 FPS。
- 最终 PlayMode 全集 629/629，0 失败/跳过，1120.727 秒：[playmode-regression.json](evidence/ordinary-jump/playmode-regression.json)。
- EditMode 全集 75/75，0 失败/跳过，271.689 秒：[editor-regression.json](evidence/ordinary-jump/editor-regression.json)。包含新增正式方案工作流专项；此后仅落地运行边界和 PlayMode 断言改变，由最终全集覆盖，配置/编辑器代码未变。
- 正式保存预览 24 项本次差异；SaveTemplate 保存 1426 文件，再 minor Bump。保存后 0 错误/0 警告/0 差异，模板图及父谱系正常，无待应用同步：[template-validation.json](evidence/ordinary-jump/template-validation.json)。
- 18 个测试后 C# 文件与已保存模板逐字节一致：[source-hashes.json](evidence/ordinary-jump/source-hashes.json)。
- 共享字体、二进制与 autocrlf=false/true 干净检出通过。公共 bundle 32 文件原始 SHA-256 匹配，sourceCommit 与 HEAD 公共源文本一致；常规生成器被既有技能改动阻止，未删除改动或重建 bundle。

集合重叠，不相加；状态见 [validation-status.json](evidence/ordinary-jump/validation-status.json)。

## 失败与修正

首次专项 7 项失败涉及零重力限速被误判为上升保持及碰撞位置未稳定。修正测试后暴露 3 项实际下降限速失败：进入探地范围提前清理普通策略，使最后一段下降回到共享重力。运行清理已延后至真实地面接触，保留原限速阈值，并同步渲染矩阵的落地结束条件。最终专项和全集通过。

MCP 曾在域重载及长结果查询期间断连，也出现初始化超时。重连取回 75/75 结果，证明该次断连未终止测试。当前插件 WebSocket 接收循环串行等待命令完成、ping 共用该循环，长等待可能延迟 ping；但另有 keepalive，不能据此断言唯一根因。采用 wait_timeout=0 短查询及编译后域重载等待，本轮专项、全集、保存/Bump完成。未修改 PackageCache，未用日志、BatchMode 或 dotnet 替代编译验证。历史失败原始结果保留在证据目录。

## 消费与验收边界

用户已明确授权全部通过后发布，覆盖最初禁止提交/发布限制。Windward、Mountain 未修改，没有使用 Windward 编辑器。

消费方通过 Ember/UPM Manager 升级，再保护本地改动按跨 minor 正式流程吸收模板。普通跳策略默认关闭；Windward 自行调参并完成键盘白盒与原路线复验。

**Windward S2 仍未通过，不计入 S3 或完整三关。** 原路线只完成两级台阶，缺口准备支撑失败不证明缺口不可达；旧 Mountain 数值是源码估算而非 Unity 实测。完整产品挥旗/风场、手柄、发行构建/IL2CPP、其他平台未验证。
