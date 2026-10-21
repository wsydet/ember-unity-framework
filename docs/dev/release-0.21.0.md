# Ember 0.21.0 发布说明

发布日期：2026-10-21。交付 Unity 工程外唯一 CSV/TSV 源表支持，以及 visual-novel **0.21.2** 的模板接线与技能适配。
消费项目可以把源表集中在 Unity 根目录外，继续把 .bytes、生成代码和多语言 .asset 输出到原 Assets 路径。
无需符号链接、目录联接或手动复制，也无需放宽策划对 Program 目录的写权限。

## 框架与模板

- 公共 `EmberTableSourceFiles` 读取 `ProjectSettings/EmberTableSources.json`：allowedRoots、defaultRoot 和按 Table ID 的 bindings。
  路径相对 Unity 工程根目录；示例 `../Design/TableSources` 仅为消费配置，不是框架硬编码。
- 保留旧 TextAsset 字段及已序列化资产兼容，新增 `SourcePath` 和资产级 ExternalSourcePath。
  共享映射优先；与非空资产外部路径冲突会报错，目标缺失不回退旧 CSV。
- 配表校验、预览、当前/全量导出、项目校验、构建前陈旧检测、多语言与图片库共用实际磁盘源。
  外部文件不调用 AssetDatabase.ImportAsset。运行时 Table ID、Resources 键、ETBL V1、Schema 与加载 API 不变。
- 保留草稿和原始字节并发检查，使用同目录原子替换与失败恢复；提交生成物前复核源快照。
  规范化后限制 CSV/TSV 与允许根目录，拒绝越界、绝对绑定和链接逃逸。
- visual-novel 的文案注册、示例导入/初始化、旧图迁移以及图片导入/同步、剧情导入技能接入统一源。
  Python 工具共用项目配置，支持外部 TSV，配置变化会使旧确认指纹失效。

| 模板 | 版本 | 兼容框架 | 内容状态 |
| --- | --- | --- | --- |
| base | 0.8.3 | 0.21.0 | 内容不变 |
| platformer-2d | 0.10.0 | 0.21.0 | 内容不变 |
| source3d-2p5d | 0.5.0 | 0.21.0 | 内容不变 |
| visual-novel | 0.21.2 | 0.21.0 | 正式 SaveTemplate/Bump；hash `56cba1d73472bba8f1fa4baa293466f0` |

base 通过项目中心正式声明兼容框架版本，派生模板通过正式父子同步继承；不因兼容声明变化伪造内容 Bump。
全部模板实际 hash、封存 hash、父快照、父指针及当前编辑记录已核对，无谱系问题。
当前小说五个受管目录 1,301 文件与正式快照逐字节一致。没有直接修改模板快照或部署基线。

第三方依赖沿用 0.20.5 发布基线，未引入额外包；公共 11 项技能 bundle 内容及 sourceCommit 不变。
模板自有技能随小说模板封存，并已通过正式技能同步检查。

## 实际验证与边界

Unity 6000.5.4f1，通过框架项目 MCP 执行；PackageInfo 实际解析为 0.21.0。

- 配表 Editor/Runtime/Integration **65/65**；多语言源表 **5/5**，均无跳过。
- Python 图片导入 **17/17**、图片同步 **8/8**；含中文、空格、Design/Art/Program 布局夹具及外部 TSV。
- 框架实例中把 11 个 Definition、3 个文案注册临时映射到 Assets 外的 12 份源表，实际跑通校验、预览、逐表烘焙、
  全量生成、小说文案注册、公共文案初始化及两个构建前处理器；原有 .bytes/.asset 逐字节一致，注册及审核状态一致。
- Windows 目录联接逃逸实测被拒绝；测试联接和临时配置已清理/恢复。
- 发布候选的共享二进制、字体静态策略和公共技能 bundle 检查通过；既存本地字体、依赖和 Unity 设置改动不进入发布。

早期测试的 Windows File.Replace 短暂占用失败，在加入有界原子替换重试后通过专项及最终回归；
原始失败与修复边界保留在[交付记录](external-table-sources-delivery.md)。

**未验证**：真实消费项目编译与业务回归、用户 22 份源表迁移、完整交互导入、全新克隆/换目录、真实目录权限流程、
Player Build/IL2CPP/其他系统。构建前回调已执行不等同于实际发行构建通过。
按用户要求，消费项目验收由用户执行；没有继续连接、升级或部署消费项目。

## 消费升级与迁移

1. 通过 **Ember/UPM Manager** 升级框架至 `0.21.0`，确认实际解析版本并在 Unity 编译。不要手改 manifest/lock 或 PackageCache。
2. 框架升级不会自动更新项目 Assets。小说模板跨 minor，先预览并保护业务定制，按项目中心流程吸收 0.21.2 接线；
   不用同 minor 的自动 patch 替代跨 minor 合并，也不为更新工具而盲目完整覆盖产品目录。
3. 盘点源、Definition 和多语言注册，备份 GUID/.meta、源文件指纹与现有产物。把唯一源移到 Design，提交 Program 的共享映射。
   目标已有不同内容时先人工合并，不覆盖；失败按清单恢复。
4. 调整产品自定义的 Source/Source.bytes/text、旧 TableSources 目录和 ImportAsset 调用。
   保留 Definition、Row、注册和生成资产，不删除重建，不改 Table ID 或输出资源键。
5. 配表中心依次校验、预览、当前导出、全量导出；多语言中心逐表保存，比较产物 SHA-256 和 Reviews，再验收图片/剧情导入及实际构建。

配置、API 示例、五处模板脚本清单、迁移预览清单与恢复步骤见
[外部源表手册](../../Packages/com.ember/Table/Documentation~/table/external-sources.md)。
依赖与模板的固定版本见[发布声明](../../Packages/com.ember/Dependencies~/release-0.21.0.json)。
UPM 固定地址：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.21.0`。
