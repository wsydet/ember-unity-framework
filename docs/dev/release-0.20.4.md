# Ember 0.20.4 发布说明

交付 platformer-2d **0.9.0** 的竖直弹射水平控制与墙跳资格交接。新增可选能力归属模板，模板升 minor；框架内核不变，包升 patch 用于分发。其他模板、第三方依赖及公共 Skill bundle 保持原版。

项目中心正式 SaveTemplate 保存 1442 文件并显式 minor Bump。contentHash/versionedContentHash 均为 `f640fe07081a1958665c2ed9194196ea`；父基线 base 0.8.3 / `1270ce6dfcae3c01d050c09ecfd60337`，兼容声明沿用 0.20.0。

## 行为与兼容

- `LaunchRequest2D.Horizontal=PreserveControl` 仅用于 ConstantRise，保持自身 Y 并继续原水平输入、惯性、刹车与转向；风与支撑贡献只合成一次，行动输入仍受抑制。
- `WallQualification=RefreshFirstVertical` 在登记成功后、Accepted 回调前刷新首次竖直墙跳资格。拒绝不刷新；默认 Preserve 保持原行为。
- 平移载具通过 `TrySetHorizontalVelocity` 提交宿主水平结果，保留当前 Y；不引入消费项目的风公式。LaunchZone 配置接受后冻结。
- 受击在提交同一步清理 ExternalMotion，再建立受击输入缓冲，修复弹射凭证及输入锁延迟释放。
- 原 API 可选参数与序列化默认值兼容；未改内置方案、玩家手感参数、全局重力或产品业务。

公开说明位于模板 `Assets/Game/Documentation/platformer-2d/launch-policy.md`。实现历史、失败修复与曲线见 [T03 验证报告](t03-launch-policy-20261020.md)。

## 验证

Unity 6000.5.4f1：专项 **27/27**、Player PlayMode **679/679**（包含专项）、Player Editor **76/76**，全部通过且无跳过。复用本次发布内容对应的完整回归，封存仅更新发布文档、包版本与模板元数据，运行代码与测试源码 SHA256 未变。

左右墙、上/侧弹射均完成三轮不落地资格交接；覆盖 Y15/Y20、左右反向/松键、多来源风、支撑离台、停止/继续、暂停、替换、owner 销毁、生命流程、顶棚与快照。交叉回归包含普通跳、墙跳早期反向转换、模板 air-auto/ground-auto/ground-flag-jump 退出、战斗、生命及方案工作流。

输入为 SYNTHETIC。渲染测试记录真实平均 FPS，由真实帧累加器驱动局部 PhysicsScene2D；目标 30/120/240 不是保证帧率，也不是性能基准。详细轨迹与采样值保留在 XML。

- [专项 XML](evidence/t03-launch-policy/special-03.xml)
- [PlayMode XML](evidence/t03-launch-policy/playmode-full-01.xml)
- [Editor XML](evidence/t03-launch-policy/editor-full-01.xml)
- [项目中心实际内容与父谱系校验](evidence/t03-launch-policy/release-template-validation.json)：0 错误、0 警告、0 差异，全部模板实际 hash 与封存值一致。
- 共享二进制、字体配置与两种 autocrlf 干净检出检查通过。常规 bundle Check 被既有模板技能发现副本变化拦截；未重建未改动 bundle，另核对全部 32 文件原始 SHA256、真实 sourceCommit 与公共维护源，只有 CRLF/LF 归一化：[证据](evidence/t03-launch-policy/release-bundle-validation.json)。

最终 Unity 已识别框架 0.20.4，未编译、未播放；Console 仅留有既有故障注入测试预期的 `S03-runtime-combat-fault`。没有新增编译错误。

未验证：Windward 消费及 S3 产品接线、认可方案与自有风场完整路线、真实设备和手柄、音画/镜头主观验收、发行构建/IL2CPP/其他平台。本版不签收 Windward S2/S3。

## 消费

通过 **Ember/UPM Manager** 升级框架至 v0.20.4，再独立处理 platformer-2d 0.8.0 → 0.9.0 的跨 minor 迁移。先保护旧基线、部署记录、本地差异、GUID、方案、输入和存档，再走正式迁移与定制恢复；不能使用同 minor 的 patch 入口或直接整体覆盖产品。

本次没有升级或改动 Windward。包地址：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.20.4`。依赖见 [发布声明](../../Packages/com.ember/Dependencies~/release-0.20.4.json)。
