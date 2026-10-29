# topdown-2d 术语

先读框架通用术语字典，再使用本表。

| 简称 | 明确含义 |
|---|---|
| 俯视玩家 | 当前活动 topdown-2d 的 PlayerController/PlayerModule，不是平台玩家 |
| 角色方案 | PlayerScheme 及角色图、Definition、角色能力、武器装配和动作方向映射 |
| 基础图 | 玩家 Alive(Locomotion[Idle/Move],Action[Dash],Hit) 与 Dead，不含攻击节点 |
| 武器图 | 每把武器独立拥有的状态图，默认 ready/windup/active/recovery |
| 武器会话 | WeaponRuntime/WeaponContext；拥有自身fire资源、攻击方向、窗口与去重，不共享角色Powers |
| 移动射击 | 当前手枪不锁角色移动；首版使用墙体遮挡的即时射线，不表示已实现实体弹丸 |
| Focus | 竞技场独立方案的扩展示例动作，不是模板必需状态 |
| 世界样例 | 稳定战斗对象生命/位置与检查点交互值，和玩家属于同一次快照 |
| 检查点重试 | 使用当前检查点基线世界/能力、回到检查点并恢复生命；区别于所选槽位读档 |
| 俯视试玩 | 关卡工作台经 Framework 启动、使用独立开发存档并恢复编辑现场的流程 |

词条不授权保存模板、Bump、提交或发布。当前交付版本由项目中心和发行记录确认。
