# S3 P01/P04 载具契约维护交付（Ember 0.21.1 发布交付）

2026-10-21。本轮维护侧工作已完成当前版本P04正式复现、唯一PlayerModule流程采样、新增交叉回归，以及项目中心保存/Bump。**不关闭Windward P01/P04或S3，不代表消费项目验收。Windward、Mountain均保持只读。本次通过 Ember 0.21.1 / tag v0.21.1 交付；远端发布状态以发布回执为准。**

## 实际版本与封存

| 项目 | 实际结果 |
| --- | --- |
| 开发包声明 | 测试与封存时 com.ember **0.21.0**；发布包 **0.21.1** |
| Unity | **6000.5.4f1** |
| 模板 | platformer-2d **0.10.0 → 0.11.0**，preview |
| contentHash / versionedContentHash | **48deb2fc08b00e1c7ee279890b289dad**，中心重算一致 |
| 父模板 | base **0.8.3** |
| 父hash与ParentSnapshot~实际hash | **1270ce6dfcae3c01d050c09ecfd60337** |
| 模板关系图 | 0问题，父同步状态 **Synced** |
| 中心操作 | SaveTemplate成功，1472文件；随后BumpTemplateVersion(field=1)成功 |
| 编辑记录 | 0.11.0及新hash，中心同步更新 |
| Git | 发布提交由 annotated tag `v0.21.1` 固定；测试基线 `cafc3ea0e459c7ef98eb0975f7a0504703055950` |

模板快照仅由项目中心生成；没有手改Templates~、ParentSnapshot~、版本或hash。模板保存与Bump已完成；用户随后明确授权发布0.21.1。现有工作区的大量改动均保留；封存前对比当前受管目录与旧模板，确认待保存差异属于本任务。

归属为**模板升级**：Entity/Player能力由玩法模板部署到Assets，不提升为所有产品必需的Manager。新的可选能力按minor封存。模板通过新包0.21.1交付；兼容声明0.21.0在同minor补丁内保持有效。

## 当前版本复现与根因

原0.20.4/0.9.0报告、来源规格与Windward后续已消费版本分开处理，不拿旧报告代替当前实测。来源仍为Windward两份交接、生成支撑规格、Mountain载具计划及原始报告/TSV；产品风公式、不对称参数和认可玩家方案均未更改。

P01当前基线在独立PhysicsScene2D复现：41×.02秒，目标(4,-4)，SetVelocity直接移动(3.28,-3.28)穿过地面；现有Motor整向量扫掠停在约(1.22732,-1.22732)，切向也停止。记录见 [当前基线](evidence/carrier-contract/baseline-current.txt)。该诊断是同步隔离物理，不是实际FPS证据。

P04当前基线在**0.21.0包/封存0.10.0原运行源/6000.5.4f1**正式流程中复现。三个原运行源在项目Assets临时恢复，与只读模板逐文件SHA一致，见 [来源核对](evidence/carrier-contract/p04-baseline-source-verification.json)；诊断后恢复修复稿。FrameworkScene→独立验收库→Main→LevelA，合成键盘真实跳上载具，唯一PlayerModule驱动物理，无手动Simulate、额外FixedTick或私有计时反射。

| 基线方向 | 第11步平台最终Y | 玩家Support.Y | Motor.Command.Y | 目标/实际FPS |
| --- | ---: | ---: | ---: | --- |
| 左 | 0 | 20 | 20 | 120 / 109.94 |
| 右 | 0 | 20 | 20 | 120 / 103.29 |

基线报告见 [p04-current-baseline-report.txt](evidence/carrier-contract/p04-current-baseline-report.txt)，同前缀左右TSV保留逐步轨迹。报告的“PASSED baseline / REPRODUCED”只表示诊断成功观察到缺口，**不是行为验收通过**。

根因是载具弹射组件独立FixedUpdate计时，与玩家观察支撑没有明确顺序。宿主-1000只写X无法保证到期StopVertical先于玩家提交。本轮还修正了骤停时将上一承载Y误当玩家主动上跳的问题。

## 修改范围与默认兼容

- **EntityTranslationSupport2D**：默认关闭的自身运动约束；ConfigureMotion、PrepareStep、TargetVelocity、乘员登记释放；自动FixedUpdate -500统一推进弹射和自身扫掠；瞬移/停用清理。
- **TranslationSupportSweep2D**：逐个有效附着实体形状Cast，按阻挡法向收窄并保留切向，每次改向后重扫；过滤Trigger、Layer、碰撞对和单向表面。12轮未收敛保守停止。
- **TranslationSupportLaunchTarget2D**：移除独立计时FixedUpdate，内部AdvanceStep由支撑调用；新增CancelLaunch；水平API保留当前已约束Y。
- **PlayerSupportMotion**：连续支撑按上一相对速度识别主动离台；登记与释放乘员，不增加玩家驱动。
- **PlayerExternalMotion**：扣除上一环境/支撑贡献及尾段碰顶判断跳过disabled接触，避免单向上穿产生假阻挡和双重承载速度。
- 新增专项/交叉/到期组合测试、仅Editor编译的正式采样夹具、模板消费文档及索引。

**EntityMotor2D的整向量扫掠完全未改。** 自身扫掠默认关闭；旧直接速度支撑保留。计时顺序、Teleport和停用取消弹射是有意的生命周期修正。没有修改P03资格、P06参数、普通跳/旗跳/墙跳数值、风公式或PlayerModule唯一驱动入口。

新增来源交叉实际发现两层问题并保留失败：乘员净空把单向底面当顶棚，先将Y20收窄到18.02323；复用单向过滤后，disabled接触又使玩家承载贡献错误扣除，Y变39.41140并丢台。最终过滤disabled接触后，平台/玩家均维持Y20且支撑引用稳定。见 [首次失败](evidence/carrier-contract/cross-first-completed.json)、[中间失败](evidence/carrier-contract/targeted-first.json)、[修复前轨迹](evidence/carrier-contract/oneway-rider-before-support-fix.txt)、[修复后轨迹](evidence/carrier-contract/oneway-rider-after-contact-fix.txt)。这些是本轮新增测试发现，不冒充原交接已有实测。

## 正式接口、执行顺序与消费说明

随模板分发的完整文档为 [载具自身碰撞与本步承载速度](../../Assets/Game/Documentation/platformer-2d/carrier-motion-contract.md)。关键顺序：

1. 产品宿主 **-1000**：用原公式一次计算产品水平与风；常规SetVelocity；弹射中仅TrySetHorizontalVelocity；新请求/替换在支撑阶段前提交。
2. 支撑 **-500**：校验owner、按秒计时、结束策略，再做自身碰撞收窄。
3. GameLauncher默认 **0**→唯一PlayerModule：Sensors→Support.Observe→乘员净空与再次自身扫掠→读取最终Velocity→原Compose→Motor提交。
4. Unity正常物理积分。产品不能在玩家观察之后回写原目标Y。

```csharp
// 初始化显式启用；宿主类标记 [DefaultExecutionOrder(-1000)]。
support.ConfigureMotion(true, obstacleLayers, .025f);

private void FixedUpdate()
{
    float x = ComputeProductHorizontalOnce();
    if (launch.IsLaunching)
        launch.TrySetHorizontalVelocity(x);
    else
        support.SetVelocity(new Vector2(x, ComputeProductVertical()));
}
```

TargetVelocity是到期处理后的自身扫掠输入诊断，不是承载输出；产品最初目标自行记录。正式玩家使用Velocity。PrepareStep是自动阶段，正式宿主不可再手动调用；公开方法供隔离物理测试/完全显式模拟使用，无步ID去重。

dt=.02、13/60保持在第11次推进到期。StopVertical本步清Y；ContinueVelocity保留允许速度。撞顶/净空后的Y不会被水平API恢复。旧owner清理不影响替换的新请求；新owner失效在支撑阶段按结束策略处理。None/Horizontal/Full使用最后有效允许速度，瞬移/失效不继承旧惯性。

## 验证结果

各批存在重叠，**不相加宣称唯一测试数**。

| 批次 | 结果与证据 |
| --- | --- |
| 原现有Entity/Player完整回归，未重启 | **821/821通过**，0失败/跳过；[完整结果](evidence/carrier-contract/regression-821.json)。这是新增单向接触修正前的基线回归，保留原含义 |
| 初始载具专项 | **25/25通过**；[结果](evidence/carrier-contract/tests-first.json)、[逐步轨迹](evidence/carrier-contract/retest-traces.txt) |
| 新增交叉首次执行 | 16项报告单向乘员上穿失败；原始失败不删除 |
| 最终载具/交叉/支撑/P02/P03/P06/外部运动定向回归 | **114/114通过**，0失败/跳过，99.600秒；[完整结果](evidence/carrier-contract/targeted-114.json) |
| 到期组合/普通跳节奏/Entity单向回归 | **49/49通过**，0失败/跳过，46.629秒；[完整结果](evidence/carrier-contract/expiry-ordinary-oneway-49.json) |
| 修复后正式PlayerModule流程 | **8/8组通过**；[正式报告](evidence/carrier-contract/p04-repaired-formal-report.txt)，8份同前缀TSV |
| 最终Unity Console | 0错误，封存前已确认 |

正式8组为左右×StopVertical/ContinueVelocity×有无顶棚，真实键盘登台与唯一Module驱动。每组22个固定步采样均无平台/Support.Y不一致；第11步StopVertical为0/0/0，Continue无顶棚为20/20/20，有顶棚为净空收窄后的0/0/0。清理验证正式退Main、卸载场景、释放支撑贡献并恢复输入设备过滤和渲染设置。

正式采样目标120，实际平均 **90.82～118.82 FPS**，dt约.02；不宣称稳定120。TSV同时记录宿主目标、平台允许、上一物理步位置差得到的实际速度、本步位置、玩家Support与Motor提交。同步测试明确标记FPS=N/A，不能替代真实帧率采样。

交叉覆盖空载地面/左右墙/顶棚/斜向切向、附着形状、薄障碍/角落/高速，载具自身与乘员净空同时约束；原Box(1,.3)/offset(0,.35)/179.5/Grouping=false的上穿、下落、受风横移及独立盒接缝；正反向多来源风一次合成；不对称速度向量；两结束策略、到期同一步顶棚、替换及owner失效；暂停、Teleport、停用、复位、离台模式。普通跳完整轨迹、移动惯性、墙跳、外部回弹及P02/P03/P06相关行为通过相应批次。

## 尚未验证与验收边界

- 正式8组采样限Y20/X±2，其他侧弹/蒲公英不对称向量与风组合在隔离物理测试覆盖，未全部交叉为正式房间流程。
- 多乘员同一步全局净空一致性、任意旋转/缩放Effector、复杂动态障碍与所有Collider拓扑组合未穷举；本能力针对冻结旋转平移载具。
- 没有在Windward运行载具接线、原房间路线、产品输入门禁/快照、完整旗跳→挥旗准备→自动回弹→自有风流程；不以模板替身代签产品行为。人工听感、手柄、发行构建及消费端迁移未执行。
- 历次MCP中断与暂存草稿保留在evidence；历史记录见 [封存前维护历史](evidence/carrier-contract/maintenance-history-before-final.md)。其中“未知/未执行”是当时状态，以本记录最终结果为准。

## Windward未来消费步骤（本轮未执行）

1. 核对正式发布 tag `v0.21.1` 后消费；封存历史记录保留当时未发布状态，以本次发布声明及远端回执为准。
2. 保护本地定制、原部署基线及认可方案。未来通过 **Ember/UPM Manager** 升级包，不改manifest/lock/PackageCache。
3. 包升级不会更新已部署Assets。0.10→0.11属于minor变化，不能误用同minor补丁增量；对照新模板和本地基线人工迁移上述5个运行源（含PlayerExternalMotion）及新增Sweep/必要接线，保留既有GUID，禁止全量覆盖定制。
4. 显式开启ConfigureMotion，宿主在-1000一次提交原风驱，弹射期间只发水平；核对support-500、GameLauncher0及项目执行顺序覆盖。
5. 在Windward独立复跑原白盒、所有原速度/风组合、到期/碰顶/替换步、单向平台/接缝、生命周期及原路线，记录目标/允许/实际、Support/Motor、位置、输入、物理步及实际FPS。由消费侧决定P01/P04和S3是否关闭。

本次修复由 `v0.21.1` 固定，发布范围与验证见[0.21.1 发布说明](release-0.21.1.md)；不以测试基线HEAD冒充修复提交。
