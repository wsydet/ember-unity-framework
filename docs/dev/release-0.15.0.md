# 0.15.0：通用多语言中心与小说立绘管理

发布日期：2026-09-28。框架 tag：`v0.15.0`。本次属于框架 + 模板升级，不增加第三方依赖。

## 交付内容

- `Ember/多语言中心` 面向所有模板：选择注册文案表、搜索/新增 Key、编辑各语言、缺译与待复核筛选，以及保存后更新预览。基础模板提供公共 UI 文案示例。
- 辅助翻译支持配置 Chat Completions 兼容服务，选择源语言和目标语言，先预览再应用。校验占位符、富文本标签及换行，拒绝过期请求结果；密钥仅保留在窗口内存。译文仍需人工复核。
- 通用运行解析、语言偏好与语言变化通知进入框架；小说保留旧接口适配，小说表注册与立绘工具留在小说模板。
- `Ember/视觉小说/立绘管理` 随小说模块启用显示，提供 Key 搜索/新增、角色表情编辑、图片绑定、缺图筛选和缩放预览。
- 两个窗口共用安全源表草稿与分栏列表。修复双行 Key 列表文字越出按钮的问题，按字体与行数预留高度，长文本裁切并可通过提示查看。
- 保存检测外部文件冲突，烘焙失败回退源文件；构建前更新注册文案表产物，校验失败阻止构建。

使用说明：[多语言中心](../../Packages/com.ember/Documentation~/manual/localization-center.md)、[小说内容工具](../../Assets/Game/Documentation/visual-novel/ContentTools.md)。

## 模板版本与继承

| 模板 | 版本 | 父模板 | 内容及封存 Hash |
|---|---|---|---|
| base | 0.7.0 / stable | 无 | `274ae24260525ab08b7264fc66625b57` |
| source3d-2p5d | 0.4.0 / preview | base 0.7.0 | `0ad733aaf3f71e7f77625ba7f814ddb9` |
| visual-novel | 0.15.0 / preview | base 0.7.0 | `cd932a1033756ecd35136ee34bc2b51f` |

三者框架兼容声明均为 **0.15.0**。保存、版本封存、父级同步及兼容声明通过正式模板服务完成。两个子模板的父快照与 base 的 270 个文件逐字节一致；公共多语言资产继承一致，base 与 2.5D 模板不包含小说表或小说立绘工具。

## 消费端升级

1. 通过 `Ember/UPM Manager` 升级框架到 **0.15.0**，等待包解析与编译完成；不要手改消费项目 manifest 或 lock。
2. 框架升级不会自动覆盖已部署 Assets。三个模板此次均跨 minor，采用新模板前备份本地业务和资源定制，再通过项目中心完整部署并恢复/合并修改；完整部署会替换五个受管目录。不要用同 minor 的 patch 增量或“补齐缺失”代替此次迁移。
3. 核对公共表、小说表、语言切换和图片绑定，再验证项目自己的场景、存档和构建。翻译服务需自行配置并复核结果。

完整依赖基线包含 56 项直接依赖，按需选用；第三方来源保持既有版本。详见 [依赖声明](../../Packages/com.ember/Dependencies~/README.md)、[发布 JSON](../../Packages/com.ember/Dependencies~/release-0.15.0.json) 与 [manifest](../../Packages/com.ember/Dependencies~/manifest-0.15.0.json)。

## 验证与边界

- 列表布局修复后 Unity 编译通过，无编译错误。
- 本轮 **196/196 EditMode** 回归通过：`EmberLocalizationEditTests`、`EmberLocalizationSourceEditTests`、`EmberSourceDocumentEditTests`、`NovelSessionTests`，覆盖通用解析、源表保存回退、草稿恢复和小说会话。
- 模板封存 Hash、继承与父快照一致性已核对。
- 暂存发布的共享二进制完整性（含两种换行配置的干净检出）及字体配置检查通过；发布范围 292 份文档的本地链接审计无问题。
- 完整 PlayMode、外部翻译服务请求、真实消费项目全新安装和版本升级尚未验证；不以本轮局部回归替代这些验收。
- 文档审计保留一处原有本地技能发现副本坏链接：`.agents/skills/ember-vn-custom-node/references/extension-contract.md`。该未跟踪副本不属于本次发布内容。
