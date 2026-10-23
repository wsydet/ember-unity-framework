# Ember 0.21.4 发布说明

2026-10-23。框架补丁版本，兼容既有自动场景 Loading，全部模板版本保持不变。

## 本版交付

- 过渡块只负责动画，`EUICurtainSession` 管理独立覆盖/保持/揭幕会话，场景 Loading 作为调用方编排业务阶段。无需切换场景即可使用正式 Loading 页面。
- `BeginCurtain` / `CoverAsync`、`Covered`、`RevealAsync` / `RevealFinished`、`SetPaused` 和有界释放：覆盖跨过实际渲染屏障；业务显式揭幕；独立进入/退出时长与用户暂停不受恢复事务内部 timeScale=0 阻塞。
- 修复取消、异常、销毁、重复与迟到资源回调的清理；区分准备、提交、整体请求与可交互阶段，外层遮幕保持时内层原位恢复可完成并释放暂停，避免循环等待。无进度展示不运行假进度等待。
- 修复 `EditorPlayEnterPreview` 在 `PrefabUtility.LoadPrefabContents` 路径的初始化及重复预览清理，保留实际横向条带参数与帧证据，不改变所有项目的默认外观。
- UPM Manager 检查 MCP 实际版本和框架推荐10.3.0，旧官方稳定版可一键升级；相同版本不重装、较新版本不降级，本地/内嵌/自定义/固定提交/间接依赖保留，点击前重查 manifest。升级框架不会静默升级 MCP 或改动 Python 服务端配置。
- 公共规则补充“修改代码后主动请求重编译并确认编译与域重载”；公共术语字典随框架交付，各模板通过统一入口读取。

正式契约与 Windward 接入代码见 [Loading 会话](../../Packages/com.ember/Documentation~/ui/loading-sessions.md)。MCP 使用说明见[包 README](../../Packages/com.ember/README.md#unity-mcp-的安装与更新)，用语见[通用字典](../../Packages/com.ember/Documentation~/maintenance/glossary.md)。

## 消费与模板边界

通过 `Ember > UPM Manager` 升级框架至0.21.4，完成编译/域重载。旧0.21.3不含新遮幕API。普通场景自动Loading继续使用默认流程；需要手动恢复时序的项目按公开契约接线，不修改私有字段、猜延迟或重载同一关卡。

Windward 原死亡/复生动作、音频、一次死亡计数和镜头冲量、0.5秒隐藏+0.53333336秒动作及挑战/安全区计时仍由产品控制。框架发布不等于 Windward 已升级或签收；本轮未修改消费项目，后续需要独立实施与人工验收后才能移除项目重复遮幕。

| 模板 | 版本 | 状态 |
|---|---|---|
| base | 0.8.3 | 保持不变 |
| platformer-2d | 0.11.1 | 保持不变 |
| source3d-2p5d | 0.5.0 | 保持不变 |
| visual-novel | 0.21.2 | 保持不变 |

兼容声明均保持0.21.0；本轮没有SaveTemplate/Bump或父级同步。2D平台专属字典仍在工作区Assets维护源，尚未通过整份模板保存封存，因此不在本版模板中。其余模板编辑、字体动态内容与设置变化保留在工作区，不混入发布。

新[发布声明](../../Packages/com.ember/Dependencies~/release-0.21.4.json)与[完整依赖基线](../../Packages/com.ember/Dependencies~/manifest-0.21.4.json)将可选MCP地址更新为官方`#v10.3.0`，其余第三方依赖不变。公共技能及bundle内容不变，仍为11项；历史release/manifest不改写。

## 验证证据与限制

- 复用本会话最终代码对应的增量证据：遮幕/场景/暂停/进度32/32、Inspector与Prefab contents预览4/4、公共规则同步18/18、可选包42/42，共96个不同用例。没有全量重测；测试重复批次不重复计数。
- 发布前主动Unity编译取得本轮结束与域重载时间，确认非编译/非更新状态；UI与UPM程序集MVID和上述测试版本一致，Console无错误/警告。见[编译证据](evidence/release-0.21.4/compile.json)。
- 四模板实际hash、封存hash、父快照全部一致，谱系0问题；活动模板编辑记录保持platformer-2d0.11.1。见[模板证据](evidence/release-0.21.4/templates.json)。
- 共享二进制、字体和技能bundle发布检查结果见[候选检查](evidence/release-0.21.4/candidate-checks.txt)。
- 初始失败与修复不隐藏：见[Loading首次交付](loading-curtain-delivery-20261023.md)、[复审记录](loading-curtain-review-20261023.md)和[MCP升级入口记录](upm-mcp-upgrade-20261023.md)。条带[实帧预览](evidence/loading-curtain-20261023/preview.html)来自真实Unity渲染，裁切条带不等高，未作Windward逐像素签收。
- 真实旧项目MCP下载升级/重连、本版Windward业务接入、真实设备与发行构建未验证。实际UPM窗口状态已核对，未将无效截图当作视觉验收。

版本脚本初次使用Windows PowerShell启动时受本机执行策略阻止，未写入版本；改用PowerShell7进程级ExecutionPolicy后按原脚本先Check再正式写入0.21.4，没有修改全局执行策略或手改版本绕过脚本。
