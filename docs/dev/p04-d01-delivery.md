# P04-D01 承载下降减速维护交付（Ember 0.21.2）

2026-10-22。归属为 platformer-2d 模板补丁升级，通过 Ember 0.21.2 / tag v0.21.2 交付 platformer-2d 0.11.1（preview）。维护侧Unity验证与正式保存/Bump已完成，远端发布状态以tag回执为准。Windward未修改、未部署，Mountain只读。P04与S3仍由产品消费侧验收。

## 两项结论分别验收

旧逻辑的真实运行证据见 [修复前48项XML](evidence/p04-d01/before-fix-results.xml)：48/48提交不一致，48/48位移断言通过。代表性dt=.01、第23步：允许/支撑=-4，command=-6.40000057，物理后刚体=-4；玩家与平台位移均=-.03999996，脚底间距=.014998436。不能将提交残留直接归为乘员滑落。

本轮先保留旧逻辑失败，再恢复候选。早期候选40通过/8失败的 [原始结果](evidence/p04-d01/candidate-first-results.xml) 保留：8项的上升目标20被多Collider扫掠收窄到约15.5，测试错误比较目标而非允许速度。候选全部记录步实际符合允许速度加风，见 [轨迹复核](evidence/p04-d01/trace-audit.json)。修正断言后，正式 [48组合复跑](evidence/p04-d01/candidate-corrected-results.xml) 48/48通过；下降段独立保证-6.4→-4、-4.4→-4准确波形，未删除低谷。

## 根因与最终修改

`PlayerExternalMotion.RemoveContribution` 从上一物理步实际速度扣除上一外部贡献时，接触裁剪原先使用本拍宿主已写入的新表面速度。旧贡献-6.4按新表面-4裁剪后，留下-2.4，再加本拍支撑-4，提交变为-6.4。物理碰撞可将该残差消除，所以提交与真实运动不是同一通过项。

修复在上一合成时保存支撑来源与表面速度，扣除旧贡献时匹配接触支撑并使用旧表面速度；本拍合成仍取当前允许速度。生命周期初测又发现停用/瞬移时Revision变化不能让旧贡献扣除退回新速度，保留 [4项首次失败](evidence/p04-d01/descent-cross-first-results.xml)；最终旧物理快照不受本拍Revision变化影响。新的承载获取、失效与离台仍由原Support.Observe判定，清理释放快照，不续用旧支撑权属。

没有改重力、PlayerFeel、风公式、普通跳/墙跳/旗跳或产品下降数值。公共框架Runtime、第三方依赖、通用技能和其他模板内容不变。双玩家Collider上升时允许速度的既存收窄行为在本轮未改；本项承诺跟随允许速度，不把输入目标当成允许输出。

## 观测入口与验证矩阵

正式消费说明维护于模板 [carrier-motion-contract.md](../../Packages/com.ember/Templates~/platformer-2d/Assets/Game/Documentation/platformer-2d/carrier-motion-contract.md)。CommandVelocity在Compose/ExecuteStep后是物理前最后指令；Unity重力和碰撞解算不会回写它。物理后读Rigidbody2D.linearVelocity与position，并与同一步前值配对；下一宿主改写前可采上一物理步结果。MotionTrace.Record仍属于物理前，LateUpdate不能拆开同一渲染帧多个物理步。

- 48同步组合：两种回钳、dt=.01/.02/.04、单/双玩家Collider、普通/单向顶面、无输入/水平输入+向下风+水平支撑。提交与真实位移分别断言，FPS=N/A。
- 下降实际渲染矩阵：目标30/120/240，dt=.02与1/60，两种回钳，共12组；记录实际平均FPS。使用真实渲染帧驱动的隔离物理场景，不冒充产品PlayerModule/真实机关端到端。
- 生命周期交叉：两种低谷分别测试主动跳离、支撑停用、瞬移、替换、旧owner/凭证释放、落地、撞顶及会话停止，共16例。
- 整组Game.Player.PlayModeTests **839/839通过，0失败、0跳过**，Unity6000.5.4f1。本轮原始 [完整XML](evidence/p04-d01/player-regression-results.xml) 与 [分类摘要](evidence/p04-d01/final-validation.json) 已保存；包含上述48同步组合、16生命周期例与1个覆盖12场景的渲染矩阵测试，不重复累加计数。
- 测试时维护包声明为0.21.1，正式发布声明为0.21.2；测试后的运行时代码未再修改，保存内容与已测试项目源对应。包号修改、模板封存和发布声明不冒充新一轮运行测试。
- 关键交叉子集：P04第11步4/4、P02水平控制8/8、P03墙跳资格4/4、P06尾段50/50、普通节奏35/35、通用旗跳/回弹全轨迹2/2、墙面飞行23/23。部分分类交叠，不相加当作独立总数。
- 下降矩阵实测平均FPS：目标30为29.92–31.07，目标120为90.68–126.34，目标240为165.48–229.58；不宣称稳定达到目标。它是实际渲染帧驱动的隔离物理验证，不是产品真实机关的端到端验收。

通用旗跳/回弹轨迹测试不等于Windward空中挥旗准备→自动回弹→进入自有风场的完整产品轨迹。原机关乘员组合与原路线仍需Windward消费后独立复验；本轮未接触产品。

## 保存与消费

保存前比较当前受管目录与旧模板，仅本项运行源/契约/新测试存在差异，见 [最终源差异](evidence/p04-d01/template-source-delta-final.json)。项目中心SaveTemplate保存1476文件，BumpTemplateVersion(field=2)封存0.11.1；contentHash/versionedContentHash与正式重算均为 **22dd884949be5e2697a2606d5da00c46**。父base0.8.3，父hash及ParentSnapshot实际hash均为1270ce6dfcae3c01d050c09ecfd60337，Sync=Synced，graphIssues=0；编辑记录一致。兼容声明保留同minor的0.21.0。没有直接编辑模板快照、ParentSnapshot或hash。

消费时先保护Windward当前工作区、GUID、场景、方案、输入和旗跳/机关定制，通过Ember/UPM Manager升级正式包，再用旧部署基线/新模板/本地文件预览0.11.0→0.11.1三方补丁并处理冲突。预计必要业务改动集中在PlayerExternalMotion；EntityMotor2D本轮仅补注释，测试与契约按需吸收。不全量覆盖Windward，不手改manifest/lock，不改PackageCache。
