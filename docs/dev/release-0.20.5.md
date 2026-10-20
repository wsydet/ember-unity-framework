# Ember 0.20.5 发布说明

交付 platformer-2d **0.10.0** 的可选弹射尾段契约。普通方向弹射可在恒速保持结束后继续使用独立的上升、低速区、下降和终速规则，接管当前真实自身Y。能力归属模板，模板升minor；框架包升patch用于分发。

项目中心正式 SaveTemplate 保存1456个文件，minor Bump封存为0.10.0。
封存hash：`58d152503e6f95494434f095a8df6ef4`。
父模板base 0.8.3，父hash：`1270ce6dfcae3c01d050c09ecfd60337`；兼容声明保留0.20.0。
全部模板实际内容、父快照、父指针及当前编辑记录校验通过：0错误、0警告、0差异。
见[正式保存与Bump](evidence/p06-launch-tail/release-template-save.json)和[正式模板校验](evidence/p06-launch-tail/release-template-validation.json)。

## 契约

- 新增 `Game.Entity.LaunchTailSpec2D`，默认关闭。`LaunchRequest2D` 末尾增加可选 `tail`，只支持 `ConstantRise + ContinueVelocity`；`PlayerExternalMotionRequest` 末尾增加 `launchTail`，只支持未配置结束停速的 `ForcedRise`。
- 请求接受时按值冻结，保持到期或碰顶由玩家唯一固定步交接。没有速度重发、项目轮询补发或共享重力空档；风和支撑贡献继续统一合成一次。
- 可配置上升/下降加速度、闭区间低速段和自身下降终速。可选慢速许可闩锁首次观察到自身速度严格低于下边界后解除，再跟随当前JumpHeld；落地、新动作、owner失效及生命/会话清理结束本次所有权。
- 尾段不持有输入锁、水平通道，也不再次刷新墙跳资格。保持P02和P03语义。活跃尾段拒绝瞬时快照；旧凭证不能清理新代次。
- 旧调用源码、默认Preserve、ContinueVelocity/StopVertical和现有旗跳PlayerFlightSpec兼容。载具收到尾段策略明确返回UnsupportedPolicy。没有修改共享手感、全局重力、玩家方案或Mountain数值默认值。

准确签名、配置示例和生命周期优先级见[尾段公共契约](../../Packages/com.ember/Templates~/platformer-2d/Assets/Game/Documentation/platformer-2d/launch-tail-contract.md)。源证据、首次诊断和完整验证边界见 [P06维护记录](p06-launch-tail-20261020.md)。

## 验证

最终相关复测 **79/79**、Editor **76/76**通过，均无跳过。
首次PlayerStateTests全回归 **572/573**，唯一失败源于Package Manager的EMFILE错误日志，
该owner用例在最终复测通过。随后补充的顶棚/风组合首次 **4/6**，两项反向风用例失败；
修复启用策略分支的真实顶面接触检查后，6项均在最终79项中通过。
最终79项覆盖49项尾段/兼容/碰顶、1项正式会话、27项P02/P03和2项owner回归，
不冒称修复后重新运行整个Player全集。

- [首次全回归与环境失败](evidence/p06-launch-tail/regression-player-01.xml)
- [首次反向风碰顶失败](evidence/p06-launch-tail/collision-first-01.xml)
- [修复后79项及原始轨迹](evidence/p06-launch-tail/final-related-01.xml)
- [Editor 76项](evidence/p06-launch-tail/editor-01.xml)
- [最终3000对轨迹对照](evidence/p06-launch-tail/protected-comparison-final.json)
- [各轮原始结果摘要、实际FPS和相邻固定步](evidence/p06-launch-tail/final-evidence-summary.json)
- [最终既有源码与配置保护哈希](evidence/p06-launch-tail/protected-source-hashes-final.json)

Unity 6000.5.4f1。修改前PlayerStateTests **530/530**；只使用旧API的保护轨迹 **15/15**；首次实现专项 **44/44**，包含15组兼容轨迹及正式会话恢复。

升级前后15组、3000对固定步样本的位置、速度、重力数值差均为0，状态、资格和来源一致。保护地空惯性、长短跳、墙滑/墙跳各阶段、独立窗口地面旗跳、地面自动回弹、空中准备→自动回弹→风，以及未启用策略的旧弹射。它们是维护侧契约夹具，不能替代Windward认可方案的完整产品轨迹。

专项覆盖左右上/侧弹、按住/松开、从普通/墙跳进入、保持边界、±5及首次低于−5、风/支撑离台、碰顶、拒绝/替换、owner、暂停、生命、恢复及快照边界。无风上弹在dt=0.02的相邻步Y20→17.6，直接使用120加速度；低速60和终速20由实例显式配置。这些目标来自旧Mountain源码/参数推导，本轮没有重新运行Mountain。

输入为SYNTHETIC。目标30/120/240FPS各运行dt=0.02及约1/60；首次专项实际平均分别为30.0001/29.9999、119.3130/109.2692、217.1226/222.5401，不宣称稳定目标帧率。

- [旧行为原始轨迹](evidence/p06-launch-tail/baseline-protected-01.xml)
- [首次专项原始轨迹](evidence/p06-launch-tail/implementation-first-01.xml)
- [3000对样本对照](evidence/p06-launch-tail/protected-comparison-01.json)
- [测试摘要及实际FPS](evidence/p06-launch-tail/first-evidence-summary.json)
- [既有源码与配置保护哈希](evidence/p06-launch-tail/protected-source-hashes.json)

首次脚本刷新未导入新资产造成的CS0246及完整复测结果单独保存。全资产导入后编译错误清零，专项通过。共享二进制和字体配置检查通过；bundle标准检查因既有无关技能工作区改动拒绝，保留诊断；不变的公共技能bundle另核验32个原始哈希、真实来源提交与公共维护源，仅UTF-8文本换行归一化。

版本元数据更新后的限制：`package.json`已通过正式脚本更新为0.20.5；正式模板校验时PackageInfo缓存仍为0.20.4。
一次Resolve后查询失败，用户手动编译并确认恢复后，首次查询仍返回`success=false`且无详情；
按仓库规则停止自动Unity查询，见[最后诊断](evidence/p06-launch-tail/package-version-query-02.json)。
因此不宣称Resolve后的编辑器包缓存或编译状态已由MCP确认。最终源码测试和模板实际校验均在此前完成；
之后只更新发布元数据/文档，没有修改已验证的C#、资源或父谱系，也没有已知编译错误或未封存内容。
据此继续Git发布；消费项目仍须通过正式升级器确认实际版本和编译。

未验证：Windward消费、认可方案/自有风场/原房间路线和手感、真实键盘/手柄设备、声音/镜头、发行构建/IL2CPP/其他平台。P01/P04独立保留，本版不关闭Windward P06或签收S3。

## 消费迁移

先保护定制、GUID、部署基线、完整认可方案和输入。通过 **Ember/UPM Manager** 消费框架v0.20.5，再处理platformer-2d 0.9.0→0.10.0的跨minor模板迁移；不能使用同minor自动patch替代合并，也不手改manifest/lock或PackageCache。

在独立候选中仅对目标弹射来源显式传入tail，复验蘑菇完整轨迹、原路线及所有现有动作后，再由Windward判断P06关闭。框架测试只证明契约。本轮没有修改或升级Windward，也没有修改Mountain。

包地址：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.20.5`。依赖及模板声明见 [发布声明](../../Packages/com.ember/Dependencies~/release-0.20.5.json)。
