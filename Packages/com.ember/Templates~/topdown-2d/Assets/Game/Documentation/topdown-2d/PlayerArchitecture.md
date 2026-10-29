# 俯视玩家与进度契约

`Game.Player.Runtime` 拥有玩家运行会话；`Game.Entity.Runtime` 提供独立生命、能力、效果、平面运动和命中原语；`Game.PlayerCamera.Runtime` 订阅玩家交接，隔离 Cinemachine。Entity 不依赖 Player。平台的左右朝向、重力运动、地空恢复及整个 Player 目录均未直接复制。

`PlayerSceneParticipant` 是 SceneScope 明确参与者。Prepare 等待输入/存储 Ready，冻结方案、图、Definition 与能力定义，返回独立 `PreparedPlayerSession`。准备不开放输入、不模拟身体；Module Reset 不抹掉候选。`PlayerModule` 是唯一 Update/FixedUpdate 所有者，场景声明和蓝图不额外 Tick。

基础玩家图为 Alive 下的 Locomotion(Idle/Move)、Action(Dash)、Hit，以及独立 Dead，共8节点；竞技场额外装配Focus为9节点。玩家图没有Attack节点。攻击意图交给当前武器的独立状态机，武器拥有攻击参数、fire资源、方向、前摇/命中/后摇及去重记录。角色Powers只管理冲刺及角色扩展动作。生命/冲刺先处理，再允许当前武器攻击；预检只查询，武器进入前摇才提交次数/CD，打断不退款。Owned 冲刺无敌在退出/打断/释放时只撤回自己的施加。详见[武器状态机](Weapons.md)。

连续 Move 限制在单位圆并保留摇杆力度。瞄准与移动解耦，方向映射只影响表现。平面 Box Cast 最多四次滑动，每步提交一次 MovePosition；零重力，无平台落地补次数等行为。运行查询、刀光和制作预览共用 `EntityPlaneFrame` 的前/侧向变换。

战斗会话显式发现本场景的 `TopdownCombatActor`，按稳定 ID 排序、推进、收集及统一结算。每个攻击实例/窗口/实体去重，多 Hurtbox 不重复扣血。只承诺锁向 Box 在平移中的扫掠及跨短窗口；任意旋转/形变的连续碰撞需另行适配。敌人恢复从 idle 开始，不保存任意攻击相位。

`PlayerScheme.Capture` 拒绝缺少必要映射；只有明确 `AllowPrimitivePresentation` 才回退占位。运行源图与冻结图通过 ObservationOrigin 关联，蓝图只读取已采集事实。修改源方案只影响后续会话。复制工具给方案、玩家/武器图、Definition、Powers 和技能创建独立资源及武器ID，Sprite 美术继续共享；扩展 Definition 若引用额外可变 SO，项目须补充捕获/复制协议。

`PlayerModule.Pause(owner)` 返回独立租约。首次暂停冻结 FSM/输入/身体和游戏时间，最后释放恢复；旧租约不能解除新会话或其他拥有者的暂停。Esc、设置覆盖和 HUD 使用此协议。输入禁止立即清除旧请求；恢复后按住键不重放旧动作。

`TopdownSnapshot` v2 与平台快照无关联，codec 类型为 `ember.topdown2d.progress`。一份不可变记录同时含场景/关卡/出生/方案身份、位置、连续朝向、HP、角色资源、当前武器ID、每把武器的开放/次数/CD、可恢复 Timed 效果、敌人/沙袋位置与HP、持久交互值及检查点。只接受角色稳定移动且全部武器就绪的快照；恢复角色初速为零、武器从ready开始，不保存攻击相位。此次尚未发布的v1开发存档明确不兼容，不静默迁移或当新游戏。

普通保存由 `TryCaptureSnapshot` 同步冻结全部片段，再 `SaveProgressAsync` 异步写盘。作用域中销毁/重复/缺失对象、装配差异、无效 HP/方向、坏数据、与实体墙重叠的恢复位置都明确失败。独立 Typed Skill/Effect 数据遵循其原语校验，不静默补齐资源。

读取通过 `TopdownEntryRequest.ReadAsync` 完成存储校验，再由 `GameGameplayState.EnterLevel` 选择同场景 `RequestInPlace` 或跨场景正式请求。原位准备保留旧 Runtime 和世界，不写颜色、碰撞豁免或身体位置。候选在 Start 前预检，提交后替换所有权，再释放旧结果。不可预期的提交异常是故障退出，不宣称回滚任意副作用。

`CreateRetryRequest` 使用当前检查点基线的世界/能力，回到检查点、恢复最大 HP、清掉临时计时效果；读档使用所选存档完整内容。二者不默认采用同一个世界复位策略。新的产品恢复原因应显式扩展策略。

`ExitAsync` 关闭存储请求入口并 Drain 已接受操作；等待取消/写入失败留在当前会话并恢复入口。已启动玩家允许同世代 Draining，准备新候选仍要求 Ready。失败写入不会被静默清除；`AcknowledgeFailedSaves` 是显示结果后的显式确认入口。

HUD 读档先取得 `BeginProgressRead(owner)` 租约，冻结玩家并阻止竞争退出；读完后同步释放，由正式请求接管。界面代次和 SessionVersion 在读取后再次核对，关闭或替换会话后的旧回调不能重新进入。旧读租约释放不能解除新租约。受击运动使用本次 EntityHit 的平面速度和 Braking，硬直结束清掉反应速度；已退休的接收器明确拒绝新伤害。
