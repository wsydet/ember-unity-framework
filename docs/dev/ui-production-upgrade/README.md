# Ember UI 组件化与美术替换分阶段实施

2026-10-27 追加授权：[S8：base 全量 UI 组件化迁移](08-base-full-component-migration.md)，包含 GM，保留功能和绑定，完成备份、验证与普通保存，暂不发布。S0—S7 为既有阶段；当前进度见末尾交接记录。

本方案用于在新的对话中逐阶段建设 Ember 的 UI 制作能力。目标是让项目先用公共组件完成可运行的占位 UI，再依据规范 PSD 替换美术，保持绑定、交互和生命周期正确，最后将验证过的基础用于 UI 游戏模板。

用户已明确允许后续执行对话通过项目中心切换到 base 模板，并使用 base 的基础 UI 练习组件提取和美术替换。切换前保护当前模板草稿，切换后核对正式编辑身份；这项已明确的选择无需重复询问。本次编制仅写文档，没有执行切换或实现。

编制日期：2026-10-26。执行者应读取本目录文件和真实工程，不依赖原聊天记录。所有验收项目初始均为待执行；文中的拟新增模型、工具与接口不能被当成现有 API。

## 执行入口

每次执行先读本页、目标阶段、[统一验收](08-acceptance.md)及[进度记录](10-progress.md)，再使用[对话提示词](09-prompts.md)中的对应指令。只推进被指定的阶段及其必要前置修复，不同时启动多个对同一批 Prefab 写入的对话。

| 阶段 | 文档 | 核心交付 | 依赖 |
| --- | --- | --- | --- |
| S0 | [现状核对与切换 base](00-baseline-and-base.md) | 可恢复的原工作区、正式 base 编辑身份、行为基线、旧名到规范名清单 | 无 |
| S1 | [组件与绑定契约](01-components-and-binding.md) | Page 命名迁移、高优先级控件、稳定绑定、契约导出和差异分类 | S0 |
| S2 | [公共预制体库](02-prefab-library.md) | Page 骨架、可嵌套组件、macOS 风格初版皮肤、生成图片与正式创建入口 | S1 |
| S3 | [动态 Item 与容器](03-items-and-containers.md) | 唯一所有权、列表刷新和回收、必要页签能力 | S1、S2 |
| S4 | [PSD 规范与 UXP 导出](04-psd-and-export.md) | 美术交付约定、Rename 流程、真实 BUI 和 PNG | S1、S2；列表部分使用 S3 契约 |
| S5 | [BUI 到 Ember 制作工具](05-bui-maker.md) | 分析、预览、创建、更新、重新绑定和差异报告 | S2、S3、S4 的协议 |
| S6 | [base 换肤试点与 AI 工作流](06-base-pilot-and-skills.md) | base 页面真实替换、行为回归、可复用技能与交接产物 | S0 至 S5 |
| S7 | [UI 游戏模板与交付](07-ui-game-template.md) | 正式派生模板、运行示例、模板保存和消费交付记录 | S6；父模板封存状态有效 |

S2 先用生成图片和原生控件完成能直接使用的默认 UI，不等待外部美术。S4 若没有外部 PSD，使用初版素材与 EUI 契约在 Photoshop 制作真实分层试点 PSD，再验证导出。实际环境暂不可用时可以依据明确夹具推进独立工具工作，但不能把合成 BUI 测试写成 Photoshop 导出已通过。

## 先行规范

| 规范 | 面向谁 | 解决什么问题 |
| --- | --- | --- |
| [EUI 信息与交换契约](contracts/eui-contract.md) | 框架、工具、UI 程序和执行 AI | EUI 应保存什么，哪些信息来自资产，哪些来自明确行为记录，怎样稳定关联新旧 UI |
| [PSD 美术交付规范](contracts/psd-art-spec.md) | 美术、TA 和 Rename 执行者 | 画布、分组、组件、状态、文本、切图、九宫格和交付清单 |
| [EUI 命名与旧页面迁移](contracts/ui-naming.md) | 框架、生成器和迁移执行者 | Page 统一命名、Panel 旧名映射、引用迁移与防止重新生成旧名 |
| [初版默认视觉与素材](contracts/default-skin.md) | UI 制作、美术与素材生成执行者 | macOS 风格默认库、图片生成、导入、状态和项目级替换 |

S1 落实命名与 EUI 信息读取；S2 交付有默认视觉的组件库；S4 将 PSD 约定接入真实导出器；S5 消费两边的关系。规范中给出的字段是待实现设计，最终序列化模型由实现者核对现有能力后确定，并同步本文档。

## 当前工程基线

以下保留 2026-10-26 初稿编制时的读取结果。当天本轮修订已读到正式编辑身份为 base 0.8.4，加载时间记录为 16:53:30；不能据此推断 S0 的备份、编译和运行验收已全部完成。进入执行时续接真实状态，不重复加载覆盖现有工作。

| 对象 | 编制时事实 |
| --- | --- |
| 框架工程 | C:/Users/wuyu/My/ember-unity-framework |
| Unity | 6000.5.4f1 |
| embedded com.ember | package.json 声明 0.22.0；不单凭该值推断发布状态 |
| 正式编辑模板 | platformer-2d 0.11.3；尚未切换 base |
| base 快照 | 0.8.4，frameworkVersion 为 0.22.0；当时 contentHash 与 versionedContentHash 相同 |
| 模板列表 | base、platformer-2d、source3d-2p5d、visual-novel；当时未发现 ui-game |
| 参考工程 | C:/Users/wuyu/Project/burner1/burner/client/game |
| Burner 环境 | Unity 2022.3.62f3；Prefab Maker 2.0.0-preview.16；UI Extension 2.0.0-preview.13 |
| Burner 公共库 | schemaVersion 4；目录中 22 个条目，3 个启用自动匹配；页面模板另由设置引用 |

Ember 已有 EUIBinding 嵌套逻辑、Item、基础扩展控件和制作中心。UIContainer、TabLoader、PagePreloader、TMPMarquee 等同名文件仍存在注释参考实现。必须以有效源码和 Unity 验证判断能力，不能按文件是否存在判断迁移完成。

## 归属和路径

| 内容 | 长期维护位置 |
| --- | --- |
| 通用运行时组件与 Binding | Packages/com.ember/UIExtension/Runtime 和已有 UI 子系统 |
| 通用编辑器目录、契约、制作工具 | Packages/com.ember/UIExtension/Editor 下按实际职责组织，保持 Editor 程序集隔离 |
| base 可编辑的默认 Prefab | 当前 base 的 Assets/GameResource/Resources/UI/Common/Prefabs；具体子目录服从创建工具配置 |
| 新增示例 UI 代码 | Assets/Game/UI/Runtime/Module/UiFoundation，由 EUI 中心生成；该模块名为建议名称，开工查重 |
| 新增示例资源 | Assets/GameResource/Resources/UI/Module/UiFoundation；纯测试夹具不放运行资源目录 |
| 基础库配置默认值 | 优先放模板可管理的 Assets/Ember/Editor 下，由正式工具配置实际路径 |
| 正式框架规范 | Packages/com.ember/Documentation~ 中相应 UI 文档 |
| 正式 base 和 ui-game 使用说明 | Assets/Game/Documentation/base 与 Assets/Game/Documentation/ui-game |
| 本轮设计、执行记录和历史证据 | 本目录；消费项目不能将它作为运行时依赖 |
| 产品 PSD、皮肤、业务页面和玩法 | 实际游戏项目；不收进通用包或 base |

ProjectSettings 不在模板受管范围内。如果沿用该目录存放本地工具配置，必须提供部署后的正式初始化或迁移流程；不能只在开发机登记设置就声称模板已具备公共库。

## 必须继承的仓库约束

执行前读取 [AGENTS](../../../AGENTS.md)、[CLAUDE](../../../CLAUDE.md)、[术语](../../../Packages/com.ember/Documentation~/maintenance/glossary.md)、[架构](../../../Packages/com.ember/Documentation~/maintenance/repository-architecture.md)及[目录规范](../../../Packages/com.ember/Documentation~/maintenance/business-directory-layout.md)。正式活动模板有字典时一并读取。

正式页面、弹窗、Item、Binding 与注册通过 EUI 开发中心完成。需要扩展制作能力时扩展中心或其公开服务，不绕开中心另造生成链。生成代码与用户代码分离，原有 Framework 模式页面不强行切成 Business。

模板维护只编辑加载后的 Assets；普通保存和 Bump 通过项目中心。禁止手改 Templates~、ParentSnapshot~、版本或 hash。修改 base 后评估派生影响，不能自动覆盖其他模板的本地差异。

本阶段指令中的实现和必要普通保存按授权执行。Bump、Git 提交、推送、发布、覆盖性消费部署分别按实际授权处理；已明确授权的步骤不重复询问。缺授权时先完成可审阅的改动、验证及差异清单，再处理最后的授权动作。

代码与资源验证遵循[公共 Unity 规范](../../../Packages/com.ember/Documentation~/maintenance/unity-validation.md)。纯文档不要求 Unity 编译。参考 Burner 只读，PackageCache 不作为维护源。

## 与色彩顺序项目的衔接

原有[色彩顺序实施包](../color-order-ui-game/README.md)仍负责已确认的 V2 玩法、三个短关和后续扩展。本包补充它的 UI 基础建设前置工作。完成 S6 后，S7 联合原包的框架模板阶段执行，避免建立两套 ui-game。

不在 base 练习时实现色彩排序、九宫格规则、分数、机关或产品存档；这些留给消费项目。旧方案中的日期、版本和工作区状态是历史事实，不能覆盖本轮现场读取结果。

## 交付状态怎么记录

区分已实现、已生成资产、Unity 已编译、运行已验证、视觉已比对、模板已普通保存、已 Bump、已发布和消费项目已部署。每个状态附实际证据，不能互相替代。统一记录模板见[进度与交接](10-progress.md)。
