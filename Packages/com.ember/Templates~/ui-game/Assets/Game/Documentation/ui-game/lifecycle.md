# 生命周期与扩展

## 所有权

| 对象 | 拥有者与释放点 |
| --- | --- |
| UiGameSession | UiGameModule 独占；Gameplay 初始化、重开建立新代次，退出/销毁释放旧会话 |
| GameSceneSession | GameGameplayState 持有；正式 SceneFlow 准备，进入 Start，退出或失败 Dispose |
| 页面 | EUI 引擎管理；状态退出明确关闭其 Play/Result 页面，显示另一页不代替关闭旧页 |
| Changed 订阅 | Play 页 OnOpen 订阅；OnClose/OnDispose 先解绑，避免旧会话保留页面 |
| 动态行 | Play 页持有 EUIItemList；宿主负责实例与 lease，页面释放列表，不自行 Destroy 行 |
| 公共图片、字体、Prefab | 项目资产；页面只引用，不在会话释放时销毁共享资产 |

步骤数据 key 为 `step-0` 到 `step-2`，index 只表示显示顺序。刷新用 Replace；旧 lease 失效，异步回调必须先检查 IsCurrent，不捕获可复用行实例后跨代写入。行展示只读状态，推进由页面主按钮提交意图；禁用行不等于隐藏行。

## 输入与暂停

会话分别保留 IsPaused、IsNavigating、IsComplete 和 IsDisposed。解除暂停不解除导航或完成锁。TryAdvance 接收期望步骤，拒绝已完成、重复步骤及失效会话。页面还检查当前 Gameplay、转场状态及同帧重复点击。

GameGameplayState 的暂停/恢复钩子明确调用模块 SetPaused。设置弹窗遮罩拦截底层射线；会话也拒绝暂停输入，不能只靠遮罩、timeScale 或 GameInput 推断业务已暂停。恢复保留当前代次和进度。

重开只接受当前已完成会话并先获得导航锁；旧会话 Dispose 后建立新会话。返回首页等待正式状态请求 Completion，失败才撤销当前会话导航锁。ShowMainPage 的 isCurrent 检查捕获的会话仍有效，迟到页面请求不能打开旧局。新增异步动画、下载和存档也应捕获代次/会话，完成时检查有效性，并在关闭时取消自己的任务。

## 替换示例

产品规则放消费项目的 Module，不进入 base 或必备 Manager。先替换 UiGameSession 的规则和页面数据，再保持上述生命周期契约。结果仍可属于同一局阶段；不必为每个产品阶段新增框架状态。通过 EUI 中心创建 Business Page/Item，ClassPath 保留 `Module/<业务模块>`，生成用户代码后再接线。

## 持久化接入

三步示例不保存成绩。需要正式进度时，显式启用继承的 PlayerDataModule 并检查 WhenReady 的 PrepareResult 成功；初始化不自动读档，StopAsync 不自动保存。

使用项目中实际的 SaveAsync / ReadAsync 返回操作，等待 `.Completion` 并检查 Succeeded/Status。产品定义自己的 `IProgressCodec<T>`、DataTypeId、格式版本和独立存储目录/数据族，不把 base 的 version/level/coins 示例当产品协议。退出使用已有 BeginDrain/DrainAsync 契约；强杀恢复另行验证。PlayerPrefs 只存偏好，DataSaver 同步 JSON 不替代正式进度事务。

源码入口为 `Assets/Game/Module/PlayerData/`；按实际安装版本核对签名。音频可复用 EmberAudioManager；需要设备输入或数据表时再显式启用对应可选模块。
