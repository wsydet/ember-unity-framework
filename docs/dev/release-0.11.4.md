# Ember Framework 0.11.4 发布说明

发布日期：2026-09-08

## 修复内容

- GameplayScene 与 2.5D Input Actions 改用 Ember 专用稳定 GUID，并同步更新开发副本、基础模板、派生模板、父快照、场景映射及 Build Settings。新建 URP 项目自带的 SampleScene / InputSystem_Actions 不再与模板资源冲突。
- 首次部署和完整模板部署会在写入前扫描模板 GUID。若 GUID 已被本次不会替换的项目资源占用，操作会列出模板与项目路径并在零写入状态中止，避免 Unity 静默改写 `.meta` 后留下断链 YAML。
- 当前活动模板新增“完整重新部署”。它会在明确覆盖警告后事务替换 `Game`、`Resources`、`Ember/Editor`、`Settings`、`GameResource` 五个模板管理目录；`Art`、`ThirdParty` 等非模板目录不受影响。
- `base` 模板封存为 `0.5.6`，`source3d-2p5d` 模板封存为 `0.2.7`，共同声明兼容框架 `0.11.4`。

## 受影响项目的处理

0.11.3 或更早版本部署过 2.5D 模板，且项目根目录保留 Unity 默认 Input Actions / SampleScene 的项目，应在升级框架后：

1. 备份五个模板管理目录中的业务修改。
2. 打开 `Ember/项目中心 → 项目初始化`，对当前 `source3d-2p5d` 选择“完整重新部署”。
3. 检查 GameplayScene 的 InputActionAsset、场景映射和 Build Settings，再进行 Play 验收。

“补齐缺失”不会覆盖已存在的错误场景引用，因此不能用于应用本次修复。若预检报告其他 GUID 冲突，应先重置冲突项目资源的 GUID 或移除重复资源，再重试；不要手工删除模板 `.meta`。

## 发布与依赖

- 框架 Git tag：`v0.11.4`
- 安装 URL：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.11.4`
- 完整依赖基线：`Packages/com.ember/Dependencies~/manifest-0.11.4.json`
- 第三方包内容未变化，继续固定到已发布的 `ember-thirdparty-upm` tag `ember-v0.11.1`。

## 验证边界

发布前已复核模板 metadata/hash/父快照、稳定 GUID 引用、部署预检回归用例、版本文件、JSON 语法和静态差异。当前 Unity MCP 未连接或不可用，因此未完成 Unity 编译、EditMode 执行或 Unity Farm 升级后的 Play 验收；这些项目不能由 dotnet 静态构建替代。
