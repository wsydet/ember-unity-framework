# 模板技能编码修复（2026-09-29，未发布）

> 发布衔接（2026-09-30）：本记录的实现与封存成果随 [框架 0.17.2](release-0.17.2.md) 交付；下文“未发布/不发布”描述各实施阶段当时的范围。消费项目尚未升级。

归属：框架 + 模板升级。没有修改剧情导入业务逻辑或消费项目。

## 来源与发现原因

- Git 原始字节表明，Assets 剧情导入技能源在 `82d2582`（0.14.11）首次带上
  `EF BB BF`；前一修改提交 `3b5b3e4` 无 BOM。提交只能定位引入版本，不能确定当时的编辑器/写入命令。
- 模板保存、部署、独立技能同步及发现副本使用字节复制，传播了维护源已有的 BOM。
  安装记录使用 `new UTF8Encoding(false)`；现有 `.cs` 编码后处理不处理 Markdown。
- 旧 `ReadCatalog` 使用 `ReadAllText` 并 `TrimStart('\uFEFF')`，掩盖了源文件头问题。
- 本机 Codex 0.148.0 的真实 `app-server` / `skills/list` 对照：同步前返回
  `missing YAML frontmatter delimited by ---` 且未列出剧情导入；正式同步后错误消失，
  返回 `ember-vn-import-story`，`scope=repo`、`enabled=true`。
  因此已在本机加载器确认 BOM 导致发现失败；尚未直接验收桌面 `/` 菜单，也未在 Call Me Heartless 完成同步后验收。

## 修复

- Assets 维护源仅删除前三字节，与旧模板去掉 BOM 后逐字节一致；正文、元数据和 CRLF 均保留。
- 通过框架 Unity 实例调用正式 `SaveTemplate` → `BumpTemplateVersion(..., 2)`，
  visual-novel 从 0.17.3 封存为 0.17.4，hash 为 `79ec18375d7d3389ac332587165bd376`。
  模板快照未手改，父基线不变。保存时隔离并恢复原有 EmberDebugConfig 本地修改。
- 通过 `SyncCurrentTemplateSkills(preview, false)` 同步开发发现副本；无本地冲突，
  再预览无差异，剧情导入执行前置检查通过。
- 框架校验拒绝 BOM、非法 UTF-8、错误文件头分隔符、重复/缺失必需字段。
  不在复制阶段隐式修改字节，不改现有备份、所有权、指纹和事务逻辑。
- 新增只读全技能审计脚本及回归测试，维护文档登记保存/发布前检查入口。

## 验证

- Unity MCP 刷新编译后控制台无错误；两个相关 EditMode 测试类 66/66 通过。
- 新增 LF/CRLF 首次部署与独立更新字节一致测试，以及带 BOM 本地修改拒绝静默覆盖、
  确认后原字节备份测试；原有事务回滚、本地修改和独立业务基线测试一起通过。
- 全目录扫描 42 份 SKILL.md（含 6 份 Assets 源、6 份模板源及发现副本）：0 错误。
- Python 文件头测试通过；Git diff 空白检查通过。
- 本机 Codex 加载器前后对照通过；桌面菜单与消费项目同步后结果仍待验收。

## 消费端接收

本次尚未发布新框架 tag。发布包含上述框架及模板修复的版本后，Call Me Heartless 通过
`Ember/UPM Manager` 升级，再在项目中心“模板专属 AI Skill”预览并更新。
本地技能源/发现副本有改动时按原机制明确备份后更新；业务目录及业务部署基线不推进。
最后重新加载 Codex 会话并检查 `/` 菜单。不要完整重部署业务、修改 PackageCache 或手改安装记录。
