# 项目初始化布局与模板身份

2026-10-27。修改范围为 EmberProjectSetupPanel 和 EmberSetupWindow；备份位于 `.utmp/project-setup-layout-20261027/before`。

- 顶部固定显示正式编辑身份或消费部署身份、模板 ID、当前版本和包内版本。
- 左侧可用模板列表，右侧独立滚动详情，当前模板明确标记；开发仓库操作转到模板开发页，消费端保留现有部署/补丁/完整更新流程。
- 状态兼顾内容 hash、未封存内容、父模板变化与缺失基线，避免只比较版本号就显示最新。项目未保存内容仍由正式校验判断。
- 5 项内存状态检查通过，见 status-checks.json；未写入模板记录。最后 MCP 主动编译 started1793076506207 / finished1793076510688 / reload1793076518530，随后 Console 错误和警告均0。已打开项目中心初始化页。
- 桌面自动化未枚举到框架 Unity 窗口，屏幕采集未捕获面板，错误截图已移除；本轮未完成视觉验收或消费项目实机验收，不将编译视为视觉通过。
- 本轮未切换模板、部署、Bump、提交或发布；仅框架编辑器源码与开发记录变动，不需要重新保存业务模板。

## 弹窗调查

技能确认来自 EmberProjectSetup.AISkills.cs 的 ConfirmLifecycleSkills：即使通过公共 LoadTemplate API 调用，也会执行 EditorUtility.DisplayDialog。同步模态窗口可阻塞 Unity 主线程，不能依赖另一次同通道 execute_code 来关闭它。

普通 MCP 场景操作支持 load/close，但“场景已在磁盘改变”的 Unity 原生提示与未保存场景确认不等于自动授权放弃。独立桌面自动化能处理可定位的窗口，但本次没有定位到目标窗口，不能声称已有可靠自动点击路径。

用户确认保留现有弹窗机制，不新增无弹窗或自动点击接口。切换前说明目标模板；操作等待、超时或结果不明时，及时请用户检查并确认弹窗，不持续等待或重复提交。用户处理后先核对真实模板身份与操作结果，再继续验证。统一执行[模板切换弹窗与人工确认规则](../../../Packages/com.ember/Documentation~/maintenance/unity-validation.md#模板切换弹窗与人工确认)。
