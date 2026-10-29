# 分路塔防术语

先读安装包 `Documentation~/maintenance/glossary.md`。

| 简称 | 明确含义 |
| --- | --- |
| Cell | row 从上向下，column 从左向右；格中心逻辑位置 `(column+.5, rows-row-.5)` |
| lane | 敌人的固定逻辑分路，不通过 Sprite 大小、排序或 Z 推断 |
| 同路 | 默认攻击策略的索敌条件，不是全部投射物的运动限制 |
| 会话 | BattleSession；唯一持有权威实体、时间和终局，Generation 区分重开 |
| 预览 | Preview 的无副作用检查及表现提示；不扣费、不占格、不发射 |
| 放置 | Place 以最终规则重新检查并提交纯模型；表现根据快照跟随 |
| 投射物策略 | IProjectilePolicy；由会话唯一调度，替换后默认策略完全退出 |
| 命中能力 | ProjectileContext；只在当前同步 Step 调用期间有效 |
| 重开 | 新会话代次、初始资源和配置，不是中途读档 |
| 已生成 | 所有 spawn entries 已处理；不等于存活敌人已经清空 |
| 普通保存 | 更新实时模板 hash；不等于 Bump 封存或发布 |
