# 俯视关卡制作

打开 `Ember/Level/俯视关卡工作台`。模板适配 `topdown.level` 复用公共工作台；实际场景、Prefab、Collider 和资源仍为权威。

默认两个关卡定义位于 `Resources/Config/Level`：Starter 训练场，以及不同关卡 ID/GUID、出生和障碍布局的 TopdownArena。竞技场装配独立 Focus 示例方案。关卡包含唯一 PlayerController、启用的 GameSceneScope 与 TopdownLevelIdentity；Scope 显式接入相机和玩家准备参与者。

“复制当前关”或“从模板新建”生成新场景、配置和稳定关卡 ID，登记 Build Settings。目标必须为新路径，不能覆盖原场景/配置。先保存所有编辑场景，才能恢复原编辑现场。角色方案默认共享源资产，需要独立数值时先用玩家工具复制方案，再指定关卡定义的 Scheme。

工作台提供实体障碍、追击敌人、沙袋与检查点素材。物理用途有显式声明；本版不默认装配平台的单向地面、弹射或出生检测。对象放置/复制会生成唯一 StableId，删除/Undo/Redo 保持本关身份。调整位置使用 Unity Transform/Undo；检查点圆圈显示交互半径，敌人圆圈显示发现范围。

“检查”校验场景/关卡身份、唯一 Scope/玩家/身份组件、重复对象身份、必要引用与角色方案。检查点试玩必须填写场景已有的检查点 ID。位置试玩只覆盖本次出生点，不写回场景；与实体墙重叠由正式玩家准备拒绝。

试玩经 Framework 正式启动，等待 Main 开屏完成后进入选择的关卡。适配器以配置 GUID/依赖 hash 检查冻结请求，变化后需要重新发起。这里的依赖 hash 是本次试玩校验值，不是模板版本或封存 hash。

每次试玩使用 `Library/TopdownTrials/<requestId>/Progress`，与正式 `TopdownProgress` 隔离。停止先等待框架停止/存储 I/O，再结束 Play；公共 EmberLevelTrialSession 恢复打开场景、选择和 SceneView。试玩目录保留诊断数据，不自动删除用户资源。重试沿用原冻结请求；资产变更后不静默重新捕获。

不要直接运行 GameplayScene，或用私有 Tick 的编辑器控制器替代正式试玩。单场景 Scope 原位恢复只替换玩家参与者的结果，其他世界参与者需要自己的明确恢复协议。
