# 接入协议与验证

## 状态、Definition与转换

业务代码放Player模块相应职责目录，运行程序集不引用Editor/Odin编辑器API。遵守仓库注释、region和公开静态方法GC标注；写辅助结构前查项目API速查。

状态通常继承`EmberFsmState<PlayerContext>`，Definition继承封闭的`EmberFsmStateDefinition<PlayerContext, TState>`。Definition.CreateState每宿主创建独立对象；GetTransitions/GetRedirectSlots只返回声明，不查输入、建角色或执行Guard。Enter重写时先调用base.Enter(activation)，保证Activation可用；运行计时放状态实例或每角色Context能力。

每条`EmberFsmTransition<PlayerContext,TState>`有稳定转换ID与目标槽；图的`EmberFsmTargetBinding(slot,targetId)`负责绑定实际节点。节点由`EmberFsmNode(id,displayName,definition,parentId,defaultChildId,bindings)`装配，`Graph.Configure`后`Inspect(typeof(PlayerContext))`必须有效。重定向目标另在GetRedirectSlots声明并绑定。声明字符串不是目标已经存在的证据。

用PlayerRule.Fixed或底层EmberFsmRequestSite明确phase、segment、order、site ID、查询条件、continuation和必要提交动作。状态在正确运动/计时前后调用`Activation.AttemptGroup(segment)`并处理ShouldStop。序号在同节点同phase/segment内唯一；改顺序前检查父先子后与本轮短路。多请求位置共用转换ID时，外部尝试需明确site，不猜结束原因。

- 条件每次尝试评估一次；CanEnter独立预检，无消费/创建效果。把完整许可收进规则，不保留请求前重复查询。
- 只有最终目标预检全部通过后，规则专属Commit在退出前执行一次；已有Enter/Begin独占的消费继续留在那里，不重复扣次数/CD或消费输入。
- GuardRejected、EntryRejected、NoChange按该位置continuation处理；协议错误中止本轮并交宿主报告，不当普通条件失败，也不擅自把它变Faulted。
- 进入提交区后的异常由FSM停止并清理，已提交资源不自动回滚。遇Faulted等终止结果不得继续运动。
- Exit和Dispose清理必须幂等；父级打断、停止、准备失败、恢复替换均不能泄漏接管/豁免/效果。

现有E02只开放air的air-jump、wall-slide、ledge-hang三个入口的附加条件，规则资产由方案独占，条件树只查询。新增任意状态或提交积木不是现有编辑器的无代码能力；需要扩大schema/可编辑范围时先确认，不把新条目偷偷塞入三入口数组或在非法配置时回退恒真。

## 参数、能力与捕获

选定唯一参数所有者；运动值放现有Feel分组或有明确归属的新配置，次数/开放/CD按需要使用Powers，动作效果来自方案ActionEffects。不能同时从Definition和方案读取同一效果。补齐依赖校验、工具定位、复制/比较/保存/只读保护，新参数关闭时行为回退明确。

重点核对`PlayerSchemeCapture.CaptureDefinition`：目前仅特殊处理地空冲刺效果，其他Definition按无可变参数的声明对象复用。**新增Definition序列化参数不会自动获得冻结A保证。** 若放入可变配置，必须显式捕获成只读有效值并接入恢复候选、完整复制与生命周期；涉及既有所有权边界先确认，不靠浅克隆或运行中读取源SO应付。

`PlayerRuntimeCandidate`构造独立Context、Powers、Effects、FSM与Animation，准备期间不绑定运行物理/表现；成功交接后才由PlayerModule唯一驱动。新服务清理接入失败准备、释放、停止路径；不能给Controller再加Update/FixedUpdate。

公共水平父状态会先执行。新动作若接管运动，必须让父级与竖直后处理识别该接管，否则叶子写值可能与父级/后处理争用。按实际需要接入Context和Motor规则，不直接写根Transform，不复制第二套物理实现。

## 动画与观察

每个新增节点（包括父节点）补独占Key、播放Clip、完整State路径与方案槽。AnimationProfile当前要求单层无参数基础AnimatorController；增加节点需在自有Controller中增加目标，保留旧状态与Key，不能用Animator自动转换代替FSM。新槽覆盖所有受影响方案，禁用状态也不能缺槽。占位Clip可复用明确选择的视觉，但Key仍独占；提供后续序列帧素材需求。

保留Visual→Animation→Sprite与效果alpha隔离；动画时长、事件不控制进入/退出。FSM蓝图显示新节点/转换/真实活动路径，生命周期预览能定位新状态实现。只安排新节点位置，保护用户已有坐标；图节点数量按实际读取，不硬编码21为上限。

## 保存与恢复

明确本动作是否具有不能按已有位置/速度/能力/效果事实恢复的临时状态。允许保存时说明恢复会重新选择合法状态，不承诺续接计时；需要禁止保存则把实际活动标记纳入现有捕获拒绝链，而非仅在UI隐藏保存按钮。检查PlatformerSnapshot.Capture及PlayerRuntimeCandidate.Restoring、初始节点预检/重定向，避免Load后重新触发动作或扣资源。

有独立计时效果时复用已确认恢复契约；动作Owned效果在动作退出清理，不能直接当Timed效果持久化。新增持久字段、精确状态恢复或旧档迁移需独立确认。不保存状态对象、Animator进度或临时碰撞租约。

## 完成判定

1. 静态装配：真实图Inspect、所有声明/目标/重定向、可达与禁用回退、完整动画、参数/能力依赖、共享影响和来源冻结均通过；缺动画或缺绑定必须明确失败。
2. 行为：正常进入→持续→退出；父级打断；关闭/未开放/资源不足/几何拒绝时不消费；协议错误终止，提交异常不自动返还；Pause冻结，Stop/取消/恢复替换释放。
3. 隔离：两个宿主不共享计时/资源/Override；源方案修改不改变旧会话或其恢复候选；真实资产保存重载后仍合法。
4. 入口：正式与白盒都通过同一PlayerModule/准备链；测试直接构造FSM只能证明内核行为，不能冒充正常入口验收。观察蓝图/动画运行和用户所需视觉证据。
5. 交付：明确新增文件、受影响图/方案、使用方法、动画素材需求、保存恢复和未验证限制。只为Skill测试的状态留隔离测试代码/资产，正式默认图不增加演示机制。

对未涉及的能力/效果/输入/存档字段说明不适用依据，不为了凑检查清单新增系统。缺少关键语义时保留可完成的独立工作并提出具体选项，不能用空生命周期骨架作为已完成结果。
