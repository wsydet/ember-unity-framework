# Entity 效果生命周期

归属 platformer-2d 的 Entity 层。EntityEffectContainer 是可选宿主能力，不依赖 Player、输入、FSM 或存档模块，也不注册自己的 Update。玩家接入见[玩家架构](../../../Documentation/platformer-2d/PlayerArchitecture.md)，动作效果配置唯一来源是完整方案 ActionEffects。

## 逐次身份与寿命

- ApplyIndependent 显式独立施加，每次取得新的 ApplicationId 和句柄；SourceKey 只标记来源，不代替撤销身份。是否刷新已有具体效果由业务适配决定，容器不替业务选择叠加策略。
- Timed 使用宿主传入的游戏秒数计时。Owned 不自动到期，由句柄 Dispose 或 EndAction 撤销。OwnedThenTimed 只在 EndAction 时进入配置的尾随，重复 EndAction 不重启，Dispose 始终立即撤销。
- OwnedThenTimed 的尾随仍不是独立 Timed。动作打断是否留下效果必须明确，不能从容器原语猜测；玩家冲刺已采用释放 Owned、正常结束另建 Timed 的独立策略。
- 零秒 Timed 立即无效，零秒尾随在真实 EndAction 时移除。Tick(0)不推进；暂停时宿主不传入游戏时间。
- 到期、释放、RemoveAll 和容器 Dispose 共用移除路径。配置在创建时捕获，编辑 SO 不改变已有实例。句柄仅管理本次施加，不管理另一宿主资源。

## 规则组合与输出所有权

EntityEffectDefinition 显式声明伤害类别字符串集合、硬直保护、排除 LayerMask 及可选外观。未配置项不保护；不内置怪物、投射物、陷阱分类，不改变项目 Layer。EntityEffectRules 持有捕获的普通不可变参数；保护和碰撞排除组合全部有效来源。

EntityEffectBinding 显式绑定本角色 Collider2D/SpriteRenderer，保留基础 excludeLayers 并 OR 当前效果排除层。绑定存续期间独占这些碰撞体的 excludeLayers 与 Sprite alpha 输出；其他效果通过同容器合成，不能另写后缓存恢复。不修改 RGB、全局碰撞矩阵或嵌套 Entity 的组件。

外观选择最高优先级，同级取当前帧较低 alpha；这不取消其他来源的保护。alpha 是基础透明度的倍率，blinkHz=0为恒定值，否则按游戏秒驱动从明到暗的余弦周期，不增加 Tween/协程/独立驱动。无外观时恢复基础 alpha。

到期、撤销、驱散和销毁发布最终结果；绑定释放退订并恢复基础输出。Revision 供观察，读取/采集不修改它。Changed 在修改完成后通知本宿主，只读订阅者不能在通知中重入修改；单个订阅异常被隔离记录，不阻断其他输出清理。订阅数组仅在增删时重建，正常逐帧通知不分配该数组。

## 快照与恢复

CaptureActive 返回当前只读记录。CaptureIndependentTimed 只采集原生命周期为 Timed 的效果，逐条保留 EffectId、SourceKey、剩余秒数及规则/外观；不合并来源，后续 Tick 不修改已采集内容。它不是“完整宿主可保存”的判断。

CaptureRestorableTimed 会检查全部活动效果：发现 Owned 或 OwnedThenTimed（包括已计时尾随）即拒绝并说明来源，不删除或静默漏掉。RestoreTimed 按每条剩余时间与普通规则参数重建独立实例；运行 Guid 和表现相位重新建立。Owned 的来源恢复由具体业务负责，不能因它不在 Timed 列表便丢弃来源状态。

玩家已在完整 v2 快照中接通这些接口。空容器或纯可恢复 Timed 可以保存；无恢复适配效果在接受保存前拒绝，拒绝不清效果、不改恢复目标、不自动等待重试。普通保存不暂停，保存退出失败不继续转场。旧 v1 明确拒绝并保留文件。Entity 层不承担玩家的版本、保存退出或死亡恢复语义。

## 玩家驱动与历史验证

当前完整方案捕获效果装配，由准备链创建容器，Context.Effects 暴露该宿主实例，PlayerController.Tick 唯一推进，FixedTick 不重复计时；停止先退出 FSM 的动作资源，再释放容器和绑定。

历史 R03.1 的135/135覆盖容器逐次身份、查询、三种寿命、快照及多宿主；R03.2的153/153增加保护规则、实际物理过滤、外观仲裁、玩家驱动/暂停/清理及SO重载；R04.1的162/162增加完整快照/恢复；R04正式链213/213后删除“按是否选装效果禁止v1保存”的临时限制。上述数字是不同批次且有重叠，不相加，也不是本轮文档维护新跑的测试。
