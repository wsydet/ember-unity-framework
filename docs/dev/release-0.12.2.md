# Ember Framework 0.12.2 发布说明

发布日期：2026-09-10。发布 tag：`v0.12.2`。用户已确认修复后的框架项目测试通过；UnityFarm 消费回归单独验收。

## 问题与归属

UnityFarm 用户的 Unity Test Runner 报告实际执行 74 项，73 通过、1 失败。唯一失败为 `GeneratedCatalogBindingManifestAndBytes_AreCurrent`：Crop Binding 期望 1170 字节、实际 1173 字节，文件开头为 UTF-8 BOM（EF BB BF）。核心 58 项全部通过。

Ember.Table 0.12.1 生成无 BOM 的 C#，`ScriptEncodingPostprocessor` 导入时通过 `FileEncodingUtility` 增加 BOM；`EmberTableArtifactBatch` 对磁盘与期望产物逐字节比较，于是导入后会误判过期。这也影响单表导出的前置检查和重复全量导出的幂等性。

归属：**框架升级**。用户已授权回流修复；UnityFarm 业务、测试断言、PackageCache、框架模板快照均未修改。

## 修复范围

- `EmberTableArtifactContent.Text` 对 `.cs` 显式加入 UTF-8 BOM，已有前导时不重复添加；其他文本保留原编码字节。
- Table ProjectScaffold 复用相同策略新建 Module/User 脚本；默认 Module 同时接受旧无 BOM 与带 BOM 形式，正文必须完全匹配。用户启用 Module 等业务改动仍受保护。
- 保留生成物严格字节比较、Manifest 所有权和事务回滚约束。
- JSON Manifest、CSV 处理及 ETBL V1 二进制格式不变；不增加第三方依赖。
- `base 0.6.0`、`source3d-2p5d 0.3.1` 的内容、版本、Hash 和 ParentSnapshot 不变。0.12.0 模板兼容声明继续适配 0.12.2，无需模板重新部署。

## 回归与验证边界

新增 `Ember.Table.Editor.Tests` 10 个案例：

| 方法 | 案例数 | 验证内容 |
|---|---:|---|
| ScriptArtifactsIncludeExactlyOneBom | 4 | 普通/已有 BOM/空内容/大写扩展名，中文和换行字节保持不变 |
| NonScriptTextArtifactsKeepTheirOriginalUtf8Bytes | 2 | JSON/Markdown 不额外增加 BOM |
| ScaffoldAcceptsBothBomFormsButProtectsUserChanges | 2 | 默认 Module 两种编码兼容，启用 Module 的业务修改仍拒绝 |
| GeneratedArtifactsRemainCurrentAfterScriptEncodingConversion | 2 | 真实表生成、正式编码工具转换、旧无 BOM 迁移、全量预览与提交无变化、单表提交前置检查、Manifest/ETBL 不变、真实代码修改仍报过期 |

测试在独立临时目录使用正式编码工具和 Table 事务，不向项目 Assets 写测试脚本。真实 AssetPostprocessor 回调、域重载和配置表窗口仍需 Unity 内验收，不能用这些测试源码替代实际结果。

- [x] 用户原始 XML 与框架源码相互印证根因。
- [x] 静态检查：git diff --check 无错误；JSON 可解析，包版本与发布候选声明一致；manifest 仅 com.ember 目标变化，registry 不变；新增测试引用可解析；Assets 与模板快照无差异。这些检查不代表 Unity 编译或测试通过。
- [x] 用户在本对话确认“框架项目测试通过”，据此继续 0.12.2 发布。该结论来自用户手动验收，不是本会话 MCP 自动验证；本轮未提供 XML、精确案例数或单独的编译记录，不伪造数量与输出。
- [ ] 全量导表 → Unity 导入 → 再次预览无替换；修改一张表数据后单表导出成功。
- 发布使用新的不可变 `v0.12.2`，不移动 0.12.0 或 0.12.1 tag。
- [ ] UnityFarm 升级后完成下面的消费回归；P2 Gate 在此前保持未通过。

当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

## 发布后的 UnityFarm 升级步骤

1. 通过 `Ember/UPM Manager` 升级 `com.ember` 到 `v0.12.2`，确认 UPM 实际解析为 0.12.2。无需重新部署模板。
2. 配置表中心执行“校验全部 → 预览全部变更 → 导出全部表”，手动触发编译。旧文件若已被导入器加 BOM，预览可能直接无变化，这是正常结果。
3. 再次预览确认无意外替换，并执行项目中心“项目校验”。
4. 重跑 `Game.Farm.Tests.EditMode` 与 `Game.Farm.Table.Tests.EditMode` 共 74 项；原失败项应通过，以新 XML 为最终证据。
5. 确认单表导出正常。不要手改 Binding、Catalog、Manifest 或 PackageCache；保留 UnityFarm 自有 Module 启用与 Adapter 接线。

安装地址：

```text
https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.12.2
```
