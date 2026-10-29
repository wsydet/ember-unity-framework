# 制作与消费部署

关卡配置位于 `Assets/GameResource/Resources/Config/LaneDefense/`：

- TrainingYard：5 行 × 9 列，2 波；默认启动。
- CompactYard：3 行 × 6 列，3 波，其中一波为空波。
- LaneDefenseActions：Gameplay/Point（指针位置）、Place（指针按下）、Cancel（Esc/右键）。

单位、敌人和波次是 LaneDefenseLevel 内的序列化定义。修改关卡源资产后启动新会话才生效，运行中不会修改已冻结会话。wave spawns 按时间升序；同一时间允许多个条目。地形索引为 row * columns + column。Inspector/工作台提供校验。

示例实体 Prefab：`Assets/GameResource/Prefabs/LaneDefense/`。四个 EUI Prefab：`Assets/GameResource/Resources/UI/Module/LaneDefense/Prefabs/`；用户代码和中心生成 Binding 位于 `Assets/Game/UI/Runtime/Module/LaneDefense/`。需要调整绑定或再生时使用 EUI 开发中心，不修改 Binding 文件或 GamePages 注册生成区。

正式场景为 `Assets/Game/Scenes/FrameworkScene.unity`、MainScene、GameplayScene，沿 base 场景映射运行；不要直接 Play GameplayScene 替代正式启动验收。GameplayScene 的 LaneDefenseSceneParticipant 显式登记在 GameSceneScope。

工作台入口为 `Ember/LaneDefense/塔防开发工具`，仅在正式所属模板体系且模块启用时出现。可选择配置、预览格子/基地/出生方向、检查波次及错误、定位 Inspector，并从 Framework 正式启动所选关卡。试跑先进入 Main，再由开始按钮进入所选关卡；选择通过本机 SessionState 跨 Play 域重载交接，不改写关卡场景。身份变化后旧窗口停止操作并保留所选草稿。

消费部署必须使用含本模板的实际已保存/已发布包。当前示例运行程序集显式依赖 `Unity.RenderPipelines.Universal.Runtime`；开发核对环境为 URP 17.5.0、Input System 1.19.0、Cinemachine 3.1.7。不要假定仅安装 com.ember 就已在任意 Built-in 工程安装 URP：

1. 在 Unity 6000.5 URP 工程通过 UPM 安装该包的已确认版本；确认 URP 已安装并配置渲染管线，记录包版本、commit/tag。
2. 打开 Ember 项目中心，选择 lane-defense-2d，按正式部署流程安装业务 Assets 与活动部署身份，人工处理确认弹窗。
3. 等待导入，主动请求 Unity 编译，确认域重载完成后读取 Console；检查输入、场景映射、UI Binding 和必要资源。
4. 从 FrameworkScene 启动，进入 Main 再开始，完成选卡、放置、波次、胜败、暂停、重开和返回。
5. 在消费程序集提供策略组件及产品资源，不直接改 Library/PackageCache；保存修改属于消费工程。

当前开发候选不等于已发布包，独立消费工程验证状态见 [Validation](Validation.md)。普通保存与 Bump、Git 提交和包发布分别操作；本次未授权新模板 Bump、提交、推送或发布。
