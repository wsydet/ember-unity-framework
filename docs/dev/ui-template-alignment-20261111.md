# UI 模板规范对齐（2026-11-11）

本轮将七个模板按[统一 UI 规范](../../Packages/com.ember/Documentation~/ui/naming-and-paths.md)整理，并提供消费端通用技能 [ember-ui-normalize](../../.agents/skills/ember-ui-normalize/SKILL.md)。用户授权受影响模板各递增一次补丁版本并正式同步；本轮不提交、不发布。

## 模板结果

| 模板 | 原版本 | 本轮封存版本 | 父级基线 |
| --- | --- | --- | --- |
| base | 0.10.1 | 0.10.2 | 根模板 |
| lane-defense-2d | 0.1.1 | 0.1.2 | base 0.10.2 |
| platformer-2d | 0.13.1 | 0.13.2 | base 0.10.2 |
| source3d-2p5d | 0.6.1 | 0.6.2 | base 0.10.2 |
| topdown-2d | 0.1.1 | 0.1.2 | base 0.10.2 |
| ui-game | 0.2.1 | 0.2.2 | base 0.10.2 |
| visual-novel | 0.22.2 | 0.22.3 | base 0.10.2 |

base 经正式 LoadTemplate → Assets 编辑 → SaveTemplate → Bump 完成。六个派生模板通过正式 ComputeParentSyncPlan / ApplyParentSync 更新父级基线及补丁版本。没有直接编辑 Templates~ 或 ParentSnapshot~。同步中的历史 GUID 差异保留派生身份；小说旧 Ownership Binding 的唯一内容差异是生成时间戳，旧路径随正式同步移除。

## 资源与制作行为

- 公共固定大图迁入 Common/StaticTexture，保留小型九宫图；宽条素材进入 UIFoundation/StaticTexture。保持 PNG 内容、GUID、原导入参数及 Prefab 引用，不缩图、不重建美术。
- 两个 Ownership 示例的脚本迁入 Runtime/Module/UIFoundation，更新真实 Binding 的 ClassPath，并调用公开生成 API；用户逻辑及嵌套所有权保留。
- 模块目录模板通过中心服务加入 StaticTexture、DynamicTexture；公共制作工具和当前库说明同步路径。
- 小说背景、立绘迁入 Narrative/DynamicTexture/LastLight；小说专用固定大图进入 Narrative/StaticTexture/Novel；皮肤表选择的 return/menu_evening 及未来皮肤编辑输出归 Narrative/DynamicTexture/Skins。
- 更新小说图片/皮肤编辑器、示例资产生成、相关测试及当前文档。三张源表通过正式 BakeCurrent 烘焙；不手改运行 bytes。既有页面名称、皮肤旧键和剧情稳定 ID 保持兼容。
- 图片导入技能的立绘/背景新计划使用 DynamicTexture，相关断言覆盖新地址；未修改其确认、指纹及回滚协议。其他用途仍需按真实消费者核查，不因迁移默认值认定为动态内容。

逐模板资产迁移和 GUID 对照见[验证清单](evidence/ui-template-alignment-20261111/template-verification.json)。六份公共调整各 22 项路径变化，小说共 40 项；七份模板原 GUID 均无丢失，移动图片字节与调整前一致。目录也计入路径变化，不将该数值当作图片数。

## 技能分发

ember-ui-normalize 位于框架通用技能源并登记 schema v1 catalog，覆盖消费项目审查、分类、保留 GUID 的迁移、动态地址、生成及验证。消费端只整理自有 Assets，不写依赖缓存，不为整理目录重新部署模板。技能源基础校验通过；本轮未提交源、未构建带 sourceCommit 的发布 bundle。正式交付仍须按既有技能打包、框架发版及消费端安装/更新流程完成。

## 验证与边界

- Unity 6000.5.4f1，工作区 com.ember 0.25.1；分别对公共调整和小说 C# 修改主动请求编译并确认本轮编译/域重载完成，Console 无编译错误。
- base 回读 70 个 EUI Prefab、25 个纹理，未发现名称、脚本、图片引用或地址问题；小说 39 个 UI 纹理可按新地址加载。
- 三张表 novel_backgrounds / novel_portraits / novel_skin_sprites 的 BakeCurrent 全部成功，诊断为空。
- Unity EditMode 增量 6/6 通过：两项 Item 所有权、Page 命名、显式 Module 路由，以及小说图片交叉切换/清理的 1920×1080、1440×1080 两组用例。job_id=f4aa519763064f24a9eb427598ce8368。
- 图片导入技能 Python 回归 17/17 通过；首次因沙箱系统临时目录权限失败，改用本任务工作区临时目录后通过。测试生成的 Python 缓存已清理，未进入模板。

最终已恢复 topdown-2d 0.1.2 和原 FrameworkScene（无未保存场景修改）。原本地 DefaultLightSans.asset 按备份逐字节恢复，SHA256 为 `14c9718d55d832593c689643cd752a277e0915fe418bb71f8f85c883653c0e4d`；当前维护源相对新 topdown 模板唯一差异就是该字体，没有额外新增或缺失文件。

恢复后再次主动请求编译：started=1794383651396、finished=1794383659689、domain_reload_after=1794383666907，业务程序集 MVID=`aa95ba7b-096b-4a7d-9f83-a892866c0c96`，Console 0 error。回读 topdown 的 71 个 EUI Prefab、25 个纹理，issues 为空。正式模板图校验无问题，七模板实际磁盘 hash 均匹配 contentHash，且均等于 versionedContentHash。最终文档审计仍为原有 67 个链接问题，本轮未新增；最终技能文件头检查 42 项、0 错误。

未运行全部玩法回归、Player 构建或图集打包；本对话结果为实施自检，不作为独立验收或人工美术签收。任务备份继续保留，未执行清理或发布。
