# Windward 消费端接续说明

本说明承接已采用的方案 B。框架已完成实现和对应 Unity 定向验证，0.25.4 的正式发布状态以远端 tag 为准；本任务未执行 Windward 升级，消费接续前先查询其实际版本，不能通过框架工作区状态推断已接入修复。[框架实施与证据](README.md)、[任务独有差异](evidence/task-only.diff)。

## 授权边界

1. 先审查本任务差异与既有工作区改动，分别授权提交、推送和框架发版。版本、tag 和模板封存按正式发版流程处理；不要为本修复手改模板快照/hash。
2. 正式框架版本可用后，再明确授权 Windward 通过 **Ember/UPM Manager** 升级。保护当前工作区、正式活动模板记录、扩展组合、发现副本与产品存档。禁止修改 PackageCache、手改 manifest/lock 或复制一套产品侧缓存。
3. 升级完成后，使用 Windward 对应 Unity MCP 主动请求编译，确认该轮编译/域重载及新程序集加载，再读取 Console。不要把框架工程的 MVID 或测试结果套到 Windward。
4. 框架 Editor 源码内容身份已变化，旧配置可能按设计拒绝扩展。先从项目中心展示必要的扩展组合/内容 hash/Skill 发现预览差异，再单独授权重新绑定；保留旧记录及可回退资料。不因窗口仍打开而绕过资格，也不自动签收新 hash。框架当前 topdown 绑定预览仅供框架诊断，不是 Windward 的绑定计划。

## 消费端复验

按[原方案](evidence/consumer-reference/design.md.txt)和[有限基线回执](evidence/consumer-reference/implementation.md.txt)恢复同硬件、Game 分辨率、焦点、Profiler 设置与正式序章起点。普通入口确认 trialActive=false；试玩使用现行关卡窗口的正式 Start 路径，确认 trialActive=true。两入口使用隔离存档，不读取/覆盖用户正式进度。

对照项目中心关闭/打开，分别保留未录制帧率和录制 CPU 数据。读取 EditorLoop、PlayerLoop、Trial Tick、ConditionalMenu、项目中心、Physics2D、GC 和可用的渲染/GPU 信息；Deep Profile 关闭。标记焦点、后台样本、MCP dispatcher 开销和有效帧数，inclusive 时间不叠加。不得用关闭项目中心作为最终修复，也不能用本轮 API 数字替代消费端数据。

范围保持原直接相关功能：正式进入、一个已允许的邻房往返、原位死亡恢复、返回菜单、同隔离库继续，以及试玩 Start/Retry/失效 Stop 与 owner 清理/现场恢复。玩家手感、机关时序、能力资格、一跳集合、房间坐标和正式存档仍受保护。无新采样证据不实施 P2 碰撞层或驻留候选；全量或长时间测试另行授权。

## 已知成本与回退

稳定查询与周期主线程检测已避开模板枚举、源码读取和重复 hash。实际写入/Start/Retry/受控文档执行边界仍同步复查输入；完整源码 stat 在框架微基准有约 78ms p95 的一次性成本，环境波动时可能更高。冷启动和合法失效重建单列统计。

实际模板切换、包来源切换和产品启用态 Play 本轮没有执行。失败时保护配置和发现副本，通过 UPM Manager 回退正式已发布包，并核对对应的扩展绑定；不要在 PackageCache 修补或把版本号改回去就宣称恢复。消费端四条件及相关功能复验通过前，原低帧率问题继续保留为待验证。
