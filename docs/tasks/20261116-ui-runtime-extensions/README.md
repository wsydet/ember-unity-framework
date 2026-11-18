# UI 通用运行时能力补齐：任务归档

- 任务：20261116-ui-runtime-extensions。
- 日期/时区：2026-11-17，Asia/Shanghai。
- 状态：已完成；归档核验通过，本任务临时目录已清理。实际清理结果见 closure.json。
- 范围：滚动定位与吸附、TMP/TMPEx 跑马灯、Image/RawImage 地址异步加载、既有多图索引与 Simple 圆角的必要补齐及组合验证。
- 本页是当前收束状态入口；records 中的阶段记录保留当时状态，不改写历史失败或待决结论。

## 实际交付与使用入口

| 能力 | 实际入口 | 使用说明 |
| --- | --- | --- |
| 滚动定位/吸附 | EUIScrollController、EUIScrollContracts；定位、平滑滚动、中断、可见性判断和邻近项吸附 | [随包使用说明](../../../Packages/com.ember/Documentation~/ui/runtime-extensions.md#滚动定位和吸附) |
| TMP/TMPEx 跑马灯 | TMPMarquee；公开网格 API、多材质共同位移、溢出判断及复位 | [跑马灯](../../../Packages/com.ember/Documentation~/ui/runtime-extensions.md#tmp--tmpex-跑马灯) |
| 地址图片加载 | EUIImageLoader、EUIRawImageLoader、EUIAssetImageSlot；复用 Ember 资源 Handle/Slot | [图片加载](../../../Packages/com.ember/Documentation~/ui/runtime-extensions.md#image--rawimage-地址加载)、[资源合同](../../../Packages/com.ember/Documentation~/resource/README.md) |
| 多图及圆角 | 复用 EUIImageEx.SpriteArray/SpriteIndex、EUIRoundedImageModifier | [多图/圆角](../../../Packages/com.ember/Documentation~/ui/runtime-extensions.md#euiimageex-多图与独立圆角) |
| 正式 EUI 接入 | companion 的精确 Extension 选择、中心生成的强类型 Binding；Runtime/Editor 程序集隔离 | [绑定入口](../../../Packages/com.ember/Documentation~/ui/runtime-extensions.md#正式页面绑定) |

长期接口/用法以随包 UI 文档为唯一权威正文。本归档不复制完整手册，也不需要任务临时目录才能使用上述能力。

## 验收与问题闭环

独立验收来源为 [A1](records/acceptance.md) 和 [A2 定向复验](records/acceptance-recheck.md)。[字体决定 D02](decisions.md) 是用户对资源差异的明确处置，不是新的独立技术验收或写入归属证明。

| AC | 最终收束结论及证据边界 |
| --- | --- |
| AC01 | 维持 A1 滚动几何范围通过；产品及相关测试指纹保持，A2 未机械重跑。 |
| AC02 | 维持 A1 动画/输入接管/惯性/吸附自动事件范围通过；不等于人工设备签收。 |
| AC03 | A1 行为证据及 A2 隔离 EditMode/真实渲染通过；无新增共享源写回。 |
| AC04 | 保留 A1 声明支持范围及历史字体皮肤结果；A2 补充独立字体/fallback/sprite/裁剪，未新增全局皮肤集成覆盖。 |
| AC05 | A1 正式 Binding 及 A2 两类型图片 14 项通过。 |
| AC06 | A2 独立复验通过，UIRT-A01 关闭；原失败探针的 Sprite/Texture 均在释放前卸下实际 Graphic 引用，释放后无残留，恰好释放一次；长期销毁/借入/目标已毁回归通过。 |
| AC07 | 维持 A1 资源合同通过，并有 A2 实际 ResourcesProvider/SceneUI adapter 及一次释放补证；未交付第三方 Provider。 |
| AC08 | 维持 A1 多图/圆角及 PNG 证据通过，仍仅 Simple；A2 未重渲染圆角。 |
| AC09 | 维持 A1 Binding/依赖及直接回归范围通过，受保护代码未变。 |
| AC10 | 主动编译、域重载、Console、独立测试与文档技术证据由 A1/A2 承接；资源处置条件经 D02 明确接受而满足。UIRT-A02 按用户决定关闭，保留不可精确恢复及归属未知的限制。 |

A1 独立验收实际执行 37 个 EditMode 情景通过、3 个短 PlayMode 方法中 1 个失败；该失败历史不改写。返工后的 A2 为 **19/19 EditMode、4/4 短 PlayMode 通过，0 失败、0 跳过**。S1/S2 自检数量分别保留在原记录，不将多次重跑及方法内部组合相加为新覆盖，也不称全量通过。

A2 最终探针移除后，MCP 主动编译 started=1794847201661、finished=1794847208337、domainReloadAfter=1794847215933，ProbeLoaded=false，Console 错误 0；最终 FrameworkScene、非 Play/编译/更新、timeScale=1，无测试 Manager/Provider。原始请求、完成事件、程序集、Console 与最终状态均在证据包。入口本轮只有记录/归档操作，没有修改产品代码/字体，也未重跑 Unity 验证。

## 基线与限制

- Git：codex/local-snapshot-20261111-162819；HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`；实际交付包含工作区未提交内容。
- Unity 6000.5.4f1、uGUI 2.5.0、Ember embedded 工作区 0.25.5；不表示这些改动已发版或消费项目已升级。
- 正式编辑身份 topdown-2d 0.1.2 / hash `d83863b16bda8c2655f21d6aa3d55693`；本任务属于框架公共能力，没有模板保存/Bump/部署。
- 收束前 61 个 owned、13 个 protected 与 A2 基线匹配；34 个共享字体源文件及完整字节备份匹配；A1/A2 原文指纹保持。清理前再次核对，结果记入 closure.json。
- 跑马灯仍不支持 3D/RTL/竖排/多行及多个顶点位置效果叠加；滚动不承诺相对旋转/斜切；圆角仅 Simple，非子节点 Mask。
- 长列表虚拟化、Tab/Attachment、通用 Tween、MergedImage、软遮罩/模糊及第三方资源后端不在交付范围。
- 没有人工、真机、构建、全工程或跨所有管线签收。本轮没有新增全局字体皮肤集成测试；历史 AC04 证据及隔离夹具限制继续保留。后续该方向回归应先隔离全局活目标和动态字体源。
- 当前钉钉字体按用户决定保留，早期精确字节不可恢复、旧写入归属未确证；不以动态延后序列化假说替代事实。

## 正式记录与完整证据

- [有效需求 R1 原始快照](records/requirements.md)
- [方案 D1 原始快照](records/design.md)
- [实施/返工 S2（含 S1 历史）](records/implementation.md)
- [独立验收 A1 原失败历史](records/acceptance.md)
- [独立定向复验 A2](records/acceptance-recheck.md)
- [用户字体处置 D02](decisions.md)
- [完整证据包的逐文件指纹清单](archive-manifest.json)。ZIP 原件由维护者在本地留存，Git 仓库保留本页及上述验收记录。
- [收束及清理结果](closure.json)

`task-evidence.zip` 的 `source-task/` 目录保存本任务全部阶段记录、原始测试/编译 JSON、PNG、原探针文本、交接和提示词、代码备份，以及字体完整字节备份。包内每个文件均与归档前源逐字节核对，archive-manifest.json 给出路径/大小/SHA256，闭包检查与压缩包校验结果见 closure.json。

如需复核阶段记录中的 `evidence/...`，向维护者获取备份 ZIP，在 `source-task/evidence/` 下读取，并按指纹清单核对。原文保留的旧任务路径和机器路径仅作历史来源，不是当前运行依赖；无需恢复原临时目录。A1/A2 的字节不因路径迁移而改写，当前决定及支持边界以本页和 decisions.md 为准。

本次授权覆盖归档和本任务临时目录清理；没有执行 Git 提交/推送、版本修改、发布、模板操作或消费部署。
