# SceneFlow：显式场景准备

GameSceneScope 是 base 的场景装配组件，不是 Manager 或 IEmberModule，不加载场景、不读取存档，也没有 Update/FixedUpdate。
GameplayScene 必须有且只有一个启用的 Scope；纯静态场景使用空参与者列表。

将必要资源准备器或玩法适配组件继承 GameSceneParticipant，显式填入 Scope 的参与者列表。
Awake/OnEnable 不启动业务。PrepareAsync 必须等待所有必要子任务及数据应用，并返回本次独立的 IGameScenePreparedParticipant。
尚未返回结果就失败时，由参与者释放自己的部分资源；返回后由 Scope 会话统一拥有。
结果的 Start 只交接已准备资源，不隐藏额外异步加载；Dispose 负责未启动/已启动资源的清理。
参与者不得另外声明 OnDisable/OnDestroy 屏蔽基类的失效处理；业务清理放在准备结果中。

组组件通过 OwnedParticipants 声明子对象，并负责子对象的准备和结果聚合；Scope 只驱动顶层条目。
组内子对象再次出现在外层列表、循环所有权、跨 Scope 同时占用都拒绝。所有权声明在一次会话内应保持不变。
静态地面、SpriteRenderer 等不需要实现参与者。

GameGameplayState 在 PrepareEnterAsync 中找到候选场景的 Scope，等待准备后把 GameSceneSession 放入 request.PreparedArgs。
父 GameplayState 先激活 Gameplay Modules，再由 OnGameplayEnter 调用 session.Start。
候选不存于 Module，ResetModuleData 不会抹除它。玩家由 platformer 自己的适配交给 PlayerModule 唯一驱动；base 不依赖玩家模块。
覆盖/Pop 不重新准备或启动；退出释放会话。提交失败（包括 Module 激活失败）由请求终态观察者回收候选。

失败与取消会反序释放已返回结果；清理异常会聚合，仍继续清理其他结果。
Scope 或参与者禁用/销毁会结束所属会话。准备挂起时保留占用直到迟到任务返回并清理，避免新旧准备争用同一对象。
参与者必须遵守取消令牌，不能依靠遗忘后台任务让请求假装成功。

Loading 与输入接线已完成；历史 R01.2.2 验证包含 Unity 编译、88 项相关测试及实际默认/快速/外层过渡验收。本次文档修订不代表重新执行这些验收。
Loading 使用当前 TransitionOperation.IsPrepared，通用无请求工作使用 LoadingWorkCompleted；计时器只平滑展示。
GameInitState 在 Global 激活之后检查可选 GameInputModule 的登记，由 BindTransitions 验证 Ready 并接管本 FSM。
base 未装配输入模块时跳过接线；不访问 GameInputModule.Instance 强行创建它。
GameTransitionInputGate 对 gameplay 通道使用本请求的最高优先级 Deny，完整 WhenInteractive 成功才释放自有句柄。
准备失败释放本请求限制并保留旧会话；提交/显露失败保持禁止至后续完整成功进入或服务停止。
旧请求、服务世代和其他拥有者互不释放；服务停止先关闭输入，再拆接线。恢复触发键仍由 GameInputButton 处理。

## 运行场景内替换一个参与者结果

实现可选 IGameSceneReplaceableParticipant 的顶层参与者可由活动 GameSceneSession 的
PrepareReplacementAsync 准备新结果，必须返回独立候选，不能返回本会话已拥有的任何结果。
它不改变参与者列表/场景所有权，不重新准备或启动其他世界参与者；组内对象通过其顶层组聚合替换。
调用者通过公共原位请求等待准备成功，再调用 GameSceneReplacement.Commit；准备失败或取消只释放候选。

同一会话只允许一个在途替换，等待中的取消/Scope 禁用仍保留占用直到迟到结果完成清理。
提交时先 Start 新结果，成功后登记新所有权再 Dispose 旧结果；新结果 Start 须在破坏旧资源前完成可能失败的检查。
框架不能回滚 Start 内任意业务副作用。旧清理抛错会报告失败，但新结果仍由场景拥有，不会因调用方 Dispose 替换票据而误释放。
Scope 结束会同时取消在途候选并释放当前结果；票据提交/释放均防止重复和陈旧操作。

本扩展不负责玩家数据、暂停、输入或加载场景；归属 base。platformer 的玩家适配已接入公共替换协议，具体冻结配置与快照语义由玩家层负责；其他模板不因此获得玩家业务。

本次替换扩展已在 platformer 编辑副本续验：2026-10-09 Unity 编译完成、Console error/warning 0，
190/190 PlayMode 通过，包含新增 10 项替换测试及原有业务/遮罩回归。此前 MCP 中断后恢复验证，该结果不代表后续文档维护重新运行测试。
以上为当时扩展验收。随后公共扩展已通过 base 0.8.2 保存和父级同步，平台玩家正式恢复接线也已完成；不能把当时待办作为当前状态。本次文档修订不代表重跑190项测试。
