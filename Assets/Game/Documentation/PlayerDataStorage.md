# PlayerDataModule 接入、存储服务与文件后端

归属：`base`。模块与配置位于 `Game/Module/PlayerData/`（`Game.PlayerData.Runtime`），
纯存储实现位于其 `Storage/` 子目录（`Game.PlayerData.Storage`）。
阶段 2.1—2.3 已完成通用契约、可靠后端、队列/Drain、模块生命周期及基础示例；
base 中仍显式 `Enabled=false`，由具体项目按需启用。平台跳跃已有独立玩家模型与流程适配，通用模块不引用它。

## 配置和模块生命周期

默认资产为 `Assets/GameResource/Resources/Config/PlayerData/PlayerDataConfig.asset`，
OnInit 自行通过 Resources 路径 `Config/PlayerData/PlayerDataConfig` 加载，先冻结配置再异步准备存储。
默认目录为 `Application.persistentDataPath/EmberProgress/BaseProgress`，文档族为 `ember.base.progress`，
槽位 0、1、2，队列容量 16，负载与索引各限 4 MiB。目录与旧 `player_data*.json` 独立，不自动导入旧文件。

项目应设置自己的目录名、DataTypeId、槽位和限额，并将 PlayerDataModule 特性的 Enabled 显式设为 true。
配置中没有玩法数据、Debug 模式或共享 defaultData。目录名限定为英文字母、数字、横线、下划线，
不接受绝对路径或 `..`；模块负责将其放在持久化目录的 EmberProgress 下。
测试/特殊启动可以在 OnInit 前调用 `SetConfigurationForNextInit(config, persistentRoot)`，
或 `UseDefaultConfigurationForNextInit(persistentRoot)` 保持自动加载配置而覆盖持久化基目录。
运行期间不能热换配置；注入设置跨 Reset 保留，OnInit 冻结后的设置不受 Inspector 修改影响。

登记、激活、服务 Ready 是不同状态。Collector 的 TryGetModule 只返回已登记模块，不能证明服务可用。
OnInit 返回时可能仍为 Preparing；消费方要等待当次 WhenReady 并检查结果和世代。
OnInit 前的 WhenReady 为 NotInitialized，读写明确返回 NotReady，不因属性访问初始化。
准备只检查目录/索引，不读取进度槽、不创建默认进度、不启动游戏会话。配置/索引失败返回明确结果。

Reset/OnDestroy 关闭入口、递增世代、结算旧准备为 Stopped，异步停止旧服务；不会发起保存。
新 OnInit 即使紧接 Reset 也会等待旧服务结束后才获取文件所有权。
旧准备迟到不能发布到新世代。正常主菜单/Gameplay 转场不应 Reset Global 存档模块。
WhenStopped 表示本次已请求停止的 I/O 收束任务；State=Stopped 不代表该任务已经结束。
框架同步 OnDestroy 无法等待它，所以正常退出必须在场景/框架销毁前显式等待 Drain 与 StopAsync。

模块保留旧世代未确认的保存结果，新世代 Drain 一并纳入；GetUnacknowledgedSaveResults 可查看已完成记录。
重试成功不会抹去原失败，必须显式确认对应请求 ID。确认不会改写旧句柄或已捕获的 Drain 报告。
模块和所有 codec/业务入口均应在 Unity 主线程使用，不依赖同 Phase 模块的发现顺序。

## 基础示例和旧接口替换

`Game.Module.PlayerData` 是 base 的完整示例类型（version/level/coins），`PlayerDataCodec` 是显式适配器。
它校验完整字段、字段类型、业务版本和内外版本一致性；未知版本或缺失字段不会变成默认进度。
平台跳跃模板使用自己的完整类型和 codec，通用模块不引用示例或玩家类型。

在业务异步入口中先取得已登记模块并等待准备结果；以下语句展示调用关系，错误展示和会话提交由业务处理：

```csharp
if (!EmberModuleCollector.Instance.TryGetModule<PlayerDataModule>(out var module))
    return; // 业务报告模块未装配，禁止启动依赖它的会话。
long epoch = module.ServiceEpoch;
var ready = await module.WhenReady;
if (!ready.Succeeded || epoch != module.ServiceEpoch)
    return; // 业务处理 ready.Status/Error；不能继续保存或恢复。

var codec = new PlayerDataCodec();
var newGame = new Game.Module.PlayerData(); // 独立默认对象，不产生写盘。
var read = await module.ReadAsync(0, codec, cancellation).Completion;
// Loaded 后仍需校验业务/资源并交接候选；NotFound、损坏及不支持分别处理，不在此默认化。

var snapshot = new Game.Module.PlayerData
{
    version = 1,
    level = runtimeLevel,
    coins = runtimeCoins
};
var save = module.SaveAsync(0, snapshot, new SaveSummary("检查点"), codec, cancellation);
var saved = await save.Completion;
// 根据 saved.Status 处理成功/失败；这是本次完整快照，不是上次读取的缓存。
```

正常退出先停止生产请求并提交全部已采集快照，再执行以下等待；不能直接从同步退出钩子调用后就卸载场景：

```csharp
var ticket = module.BeginDrain();
var drained = await module.DrainAsync(ticket, waitCancellation);
if (drained.Status == DrainStatus.Drained)
{
    module.AcknowledgeResults(drained.Operations.Select(item => item.Context.RequestId).ToArray());
    await module.StopAsync(); // 整体关闭 Global 模块/游戏时才停止服务。
    // 此后再执行已授权的对象释放和框架退出。
}
```

返回菜单时通常保持 Global 存储服务，处理成功结果后 ResumeRequests(ticket)，由玩法流程释放玩家和场景。
Failed 时列出 FailedWrites 并决定重试、取消退出或明确放弃；WaitCancelled 只结束等待。
重试需显式恢复入口，重新采集/提交快照，旧失败另行确认。Read 成功后的业务候选也需检查请求/会话世代，
不能仅靠服务世代判断同一服务内的菜单/角色会话是否仍然有效。

旧 `Data`、`HasLoaded`、`CurrentSlot`、`Save()`、`Save(slot)`、`Load`、`SwitchSlot`、`ResetData`、
`DeleteSave`、`HasSave` 已移除。查询改为 GetSlots；新游戏显式创建项目默认对象；
不提供默认覆盖或通配符删档。编辑器原“清除本地玩家存档”入口已替换为“选择存储配置/查看存储目录”，
两者服从模块 Enabled 开关。旧格式导入和新库删除工具都不属于本次实现。

## 类型和所有权

- 项目使用一个完整进度类型，并显式提供 `IProgressCodec<TProgress>`；模块和文件协议不引用该业务类型。
- `DataTypeId` 表示文档族，`EncodedProgress.DataVersion` 表示业务版本。codec 负责旧业务版本兼容、
  负载内外版本一致性检查，并在每次成功解码时创建独立对象；业务随后负责语义检查与场景恢复。
- `ProgressSnapshot.TryFreeze` 在调用线程执行 codec，检查槽位、文档族、负载大小，复制负载字节和摘要。
  后台不持有运行对象、原快照或 codec 输出数组。`SaveSummary` 构造时也复制元数据。
- `ProgressRequestContext` 关联请求 ID、显式槽位和服务世代。结果有独立状态码与诊断信息；
  `SaveOperation<T>` 提供可多次等待的终态。`ProgressStorageService` 分配请求 ID/服务世代并生产句柄；
  被拒绝的请求同样有已完成的 Completion 和关联信息。
- `ReadResult.Loaded` 只表示读取、完整性检查及解码成功，不代表角色已经恢复。
- `SlotCatalog` 是不可变的已提交视图。读取任一槽位不会改变最近成功保存槽位，空槽也列在视图中。

`ProgressStoreOptions` 是纯值设置：绝对根目录、文档族、显式槽位集合、负载和索引大小上限。
默认负载/索引上限均为 4 MiB；配置最多 128 个非负且不重复的槽位。摘要限制为标题 256 字符、
描述 2048 字符、16 项字符串元数据（键 64、值 256 字符）。这些限制与校验错误是可见的，不截断数据。

## 文件协议 v1

新格式必须配置独立目录，不扫描或导入旧档。目录结构：

```text
<root>/
  .writer.lock                存储实例持有的排他目录所有权
  index.json                  当前已提交索引
  index.json.bak              上一份索引（首次保存时尚不存在）
  <commit-id>.progress        不可变负载文件
  <commit-id>.progress.tmp    本次负载临时文件
  <commit-id>.index.tmp       本次索引临时文件
```

负载使用二进制封装：8 字节 ASCII `EMBERPRG`，随后依次为存储版本、业务版本、UTF-8 文档族长度、
文档族字节、负载长度、负载字节；整数采用 BinaryWriter 的 32 位小端格式。
负载实际内容由项目 codec 定义。存储版本与业务版本分别检查，不共用一个版本号。

索引是 UTF-8 JSON 外壳，包含 `formatVersion`、字符串 `body` 和 `bodySha256`。
body 保存文档族、全库提交修订号、提交 ID、最近成功保存槽位和各槽位条目。每条包含文件名、
长度、SHA-256、业务版本、保存时间、提交信息和业务提供的摘要。
body 校验同时覆盖展示摘要；SHA-256 用于损坏检测，不是防篡改认证。

读取只接受与提交 GUID 精确匹配的文件名，校验长度、SHA-256、格式和业务版本。
路径各层拒绝符号链接/重解析点。大小在分配读取缓冲前检查；错误不转为默认进度、不改写文件。
主索引损坏或主索引缺失但存在备份时准备失败，备份不会自动冒充当前成功结果。

## 提交、取消与故障

`ProgressFileStore` 为模块内部同步字节后端，不自行开线程，不调用 codec，也不驱动游戏会话。
内部锁保护文件所有权与提交视图；整个库的有界 FIFO 由 `ProgressStorageService` 负责。

每次保存执行以下步骤：

1. 写入新负载临时文件并 `Flush(true)`，移动到唯一的不可变文件名。
2. 从执行时的最新已提交索引构造候选，保留其他槽位更新。
3. 写入新索引临时文件并刷写。
4. 原子衔接最后一次取消检查与进入提交区。首次索引用 `File.Move`，后续用 `File.Replace` 并保留备份。
5. 成功后发布内存视图和 `Committed`，此时才改变最近成功保存槽位。

提交区前取消返回 `Cancelled`；提交区内取消不改写实际落盘结果。
提交操作抛异常时回读主索引：等于候选返回 `Committed`，等于旧索引返回 `WriteFailed`，
无法确认则返回 `CommitUncertain` 并进入 `Faulted`，拒绝后续存储操作。
需要释放原实例、重新准备并核对磁盘，不能在未知状态上盲目重试。

目录排他所有权保持到 Dispose；Dispose 等待当前同步 I/O 离开临界区，只释放资源，不保存数据。
临时文件按请求清理；已落成的负载、备份及备份引用的负载保留。孤儿回收和备份恢复 UI 尚未实现。
后端不会用“先删除旧索引再移动”降级替代原子替换。

当前验证环境是 Windows Editor 的本地临时目录。其他平台、文件系统、设备故障及断电后的恢复未验证；
不能将本次文件替换测试解释成跨平台或断电持久性保障。

## 验证与后续接线

### 异步服务、线程与队列

`ProgressStorageService` 是 PlayerDataModule 持有的普通对象，不自行登记为 Module 或 Manager。
构造不读写磁盘；显式 `PrepareAsync` 准备目录和索引，`WhenReady` 返回同一世代的准备结果。
准备失败必须停止该实例后重新创建，访问属性或调用保存不会隐式启动准备。

入口在创建线程调用，要求能够回到该线程的 SynchronizationContext；Unity 中应由主线程创建和使用。
Encode 在 SaveAsync 返回前同步完成，Decode 在后台读取结束后回到主线程执行。文件后端在 Task.Run 中执行。
codec 期间禁止重入服务控制入口；不同线程调用是编程错误。不能在主线程使用 Wait/Result 阻塞未完成操作。

整个库共用一个有界 FIFO，默认容量 16，可设为 1—1024。容量统计包括活跃请求和等待请求；
满时在执行 codec 前返回 QueueFull，不进行编码和写盘。每次保存冻结各自数据，不合并或覆盖请求。
读请求等待此前写请求结束；写失败后读取仍得到旧已提交版本。GetSlots 无文件 I/O，不等待队列。

排队请求取消后，在轮到该请求时产生 Cancelled 终态并释放容量，期间不会写入。
活跃读取在后台结束与主线程解码前再次检查取消，丢弃迟到字节；解码期间发生取消也不交付成功对象。
活跃保存沿用文件后端的提交边界，进入提交区后保持真实落盘结果。

### Drain、失败确认与停止

1. 业务先停止生成新保存请求，把已采集快照提交完毕，再调用 `BeginDrain()`。
2. 入口同步关闭，捕获固定请求水位及结果集合；重复 BeginDrain 返回同一 ticket。
3. `DrainAsync(ticket, waitCancellation)` 等待水位内已接受的读写，并汇总尚未确认的保存历史。
4. 保存均已提交时返回 Drained；存在未确认的非 Committed 保存时返回 Failed，FailedWrites 明列请求。
5. 业务处理/明确放弃结果后调用 `AcknowledgeResults(ids)`。仅移除已完成记录，不预先确认未完成工作，
   不改变旧 Completion 或已经捕获的 Drain 报告。

为避免保存入口拒绝被退出流程漏报，报告也保留水位之前的 QueueFull、EncodeFailed、InvalidSlot 等保存拒绝。
ticket 捕获之后的新请求不加入既有报告；恢复后下一次 Drain 会看到尚未确认的这些结果。
历史读请求不长期保留，捕获时仍在执行/排队的读请求包含在 Operations 中；读失败不代表写盘失败，
Drain 的 Failed 判定依据保存终态，读取状态仍可从 Operations 查看。
未确认的保存结果不自动淘汰，业务应及时确认；有界的是活跃请求队列，不是静默丢弃失败的结果缓存。

WaitCancelled 仅取消此次等待，提供当时的已完成结果及 PendingRequestIds；不开放入口、不取消保存。
取消退出时显式 `ResumeRequests(ticket)`，原 ticket 随即失效，等待它的调用返回 InvalidTicket；
旧世代或其他实例的 ticket 无法恢复当前服务。失败后重试具有新的请求 ID，旧失败仍需单独确认。
CommitUncertain 使服务 Faulted，后续排队写返回 NotReady，不允许 ResumeRequests 绕过故障。

`StopAsync()` 关闭入口，取消仍可取消的请求并等待文件所有权释放，不生成新保存。
State=Stopped 表示入口已关闭，**只有 StopAsync 完成才表示旧 I/O 已结束且目录所有权已释放**。
该调用幂等；保存已提交但主线程结果尚未交付时停止，仍保留 Committed。
正常退出先处理 Drain 结果，再 StopAsync；异常销毁的停止不能替代这一步。模块生命周期规则见本文开头。

### 已执行验证

阶段 2.1 通过 Unity MCP 在 Unity 6000.5.4f1 执行 `Game.PlayerData.Storage.Tests`：
**28 通过、0 失败、0 跳过**，耗时 1.310279 秒；编译错误 0。
任务 ID：`5f9571ff1d14499e95e0db8c4694ba99`。

测试使用每次独立的临时目录，覆盖快照与元数据复制、独立解码、跨槽顺序提交、重开索引、
不可变目录视图、备份引用保留、索引写失败、提交异常回读、无法确认后停写、提交前后取消、
损坏/缺失/超限负载、摘要损坏、路径穿越、文档族/配置不匹配、版本区分及排他文件所有权。
测试中的完整示例类型和 codec 仅用于验证存储协议，不规定平台跳跃模型字段。

阶段 2.2 复跑同程序集：**47 通过、0 失败、0 跳过**（原 28 项后端测试＋19 项服务测试），
耗时 2.8874389 秒；任务 ID `3c878c3489754a1fb579961e17b9d564`。
Unity 编译和域重载完成，编译错误查询为 0。首轮编译发现测试使用 NUnit 字符串断言重载，
改为集合成员断言后重新编译通过；未修改业务逻辑迁就断言。

新增测试使用受控的主线程 SynchronizationContext 和文件阻塞点，验证 FIFO、满队列拒绝、
codec/I/O 线程边界、取消和迟到读取、Drain 水位与历史失败、确认/恢复/凭证隔离、
未知提交后停写及停止时文件所有权释放。这不是实际 Module 激活、场景退出或 PlayerModule 接线验收。

阶段 2.3 通过 Unity MCP 执行两个存档测试程序集：**63 通过、0 失败、0 跳过**，
包含原有 47 项和新增 16 项模块/配置/codec/Unity 上下文测试，耗时 4.7480724 秒。
任务 ID `a45eacadea1f42c283c3c746f959e1ef`。Unity 编译与域重载完成，编译错误为 0。
初次编译发现旧编辑器程序集缺少新入口需要的引用，补齐后重新编译及测试通过。

新增验证覆盖真实 Resources 配置加载（使用独立测试目录）、配置失败、索引损坏、
不自动读档/保存、快照/读取独立性、准备中停止、等待旧 I/O 后重入、配置冻结、
跨世代失败保留、旧凭证失效，以及 Unity 自身 SynchronizationContext 上的完整异步链路。
测试不访问真实玩家档案；尚未执行正式 GameLauncher 全流程或平台跳跃场景的 PlayMode 验收。

后续接线状态：玩家快照采集、候选恢复、保存退出及平台场景验收已在对应模板完成；公共内容已通过正式 base 保存与派生同步。本段63项是早期存储模块验收，不代表后续玩家测试或新发布。实际消费部署和发行准备仍独立核对。

视觉小说及旧档导入器不在此次平台跳跃实施范围。通用实现不引用小说模型、账户或语言数据。
