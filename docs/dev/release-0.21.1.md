# Ember 0.21.1 发布说明

发布日期：2026-10-21。包含已通过项目中心 SaveTemplate/Bump 封存的 **platformer-2d 0.11.0（preview）**。本次归属模板升级，通过新的框架包 tag 交付；公共 Runtime API、第三方依赖、公共技能 bundle 及其他模板内容不变。

可选载具自身碰撞约束阻挡法向并保留切向，默认关闭，不改变普通 EntityMotor2D 整向量扫掠。支撑组件在玩家观察之前推进弹射计时、处理结束并求允许速度；PreserveControl 水平提交保留碰撞收窄后的 Y。修复单向平台 disabled contact 引起的支撑重复合成。正式接口、完整执行顺序与风/弹射接线见[载具消费契约](../../Packages/com.ember/Templates~/platformer-2d/Assets/Game/Documentation/platformer-2d/carrier-motion-contract.md)。

模板 hash/versionedContentHash：`48deb2fc08b00e1c7ee279890b289dad`；父 base **0.8.3**，父 hash/实际 ParentSnapshot hash：`1270ce6dfcae3c01d050c09ecfd60337`。兼容声明保留 **0.21.0**，适用于同 minor 的 0.21.1。实际内容重算、父谱系与编辑记录已核对，无重复 Bump。

## 验证结果

- Unity **6000.5.4f1**，框架实例 MCP 编译与模板实际 hash/谱系核对。
- P04 当前版本 **0.21.0 / 模板0.10.0** 原行为在正式 FrameworkScene→Main→LevelA、合成键盘、唯一 PlayerModule 中左右均复现；诊断成功不算行为验收通过。
- 修复后正式流程 **8/8**（左右、StopVertical/ContinueVelocity、顶棚有/无），到期及撞顶本步 Support/Command 与允许 Y 一致，无下一步滞后。
- 最终定向 **114/114**，到期/普通节奏/单向平台交叉 **49/49**，无失败或跳过。原 **821/821** 是此前完整回归结果，直接保留，未称最终全套重跑。
- 正式采样目标 FPS **120**，实际 **90.82–118.82**，不称稳定120。
- 发布候选共享二进制（含 autocrlf false/true 检出）与字体策略通过。公共技能构建器在原工作区因无关技能改动拒绝；干净环境仍有5文件既存CRLF/LF指纹差异。全部32个bundle目标原始字节符合manifest，源提交与候选文本仅换行不同，未重生成bundle。证据见[字节核对](evidence/carrier-contract/bundle-byte-verification.json)。原始报告、首次失败、修复轨迹及逐步 TSV 见[交付记录](carrier-contract-delivery.md)。

**边界**：多乘员全局净空、任意旋转/缩放效应器、所有碰撞拓扑及动态障碍未穷举。侧弹与不对称机器蒲公英参数有隔离测试，未将每种组合扩展到全部正式房间流程。真实产品路线、旗跳/挥旗完整产品流程、人工听感、手柄及 Player Build 未代验。详见交付记录的未验证清单。

## 消费迁移

1. 在消费项目通过 **Ember/UPM Manager** 升级包至 **0.21.1**，核对实际解析版本并编译。固定地址：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.21.1`。
2. 包升级不会更新已部署 Assets。platformer-2d **0.10.0→0.11.0** 跨 minor，保护自定义脚本、GUID、场景、方案与输入设置后，按项目中心迁移预览及人工三方合并吸收必要文件，不全量覆盖定制。
3. 宿主先提交产品驱动；支撑在玩家观察前完成本步弹射/碰撞/净空约束，玩家保留唯一 FixedTick。完整配置按消费契约执行，不复制通用 Motor、不反射计时字段、不覆盖玩家刚体。
4. 在 Windward 独立验证载具接线、来源单向平台、风及原房间路线，再处理产品验收单。**框架／模板侧 P01/P04 修复已交付；Windward 消费与场景验收待完成，P01/P04 不代关闭。**

Windward 与 Mountain 本轮均只读，未升级、迁移或修改。固定依赖及所有模板信息见[发布声明](../../Packages/com.ember/Dependencies~/release-0.21.1.json)。
