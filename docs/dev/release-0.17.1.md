# Ember Framework 0.17.1

2026-09-29，补丁版本。新增通用字体接入技能，公共技能共 11 项。

## 范围

`ember-add-font` 为任意 Ember 模板项目导入或复用 TMP 字体，登记字体皮肤，并批量补齐 Prefab、场景和运行时 UI 的使用映射。保留固定字体、原有布局和无关配置；避免只登记资源却未接入使用处。

本次不改变 Runtime/Editor、字体资产、模板内容或第三方依赖。模板沿用 base 0.7.0、source3d-2p5d 0.4.0、visual-novel 0.17.3，兼容声明仍为 0.17.0。

## 使用

已有 0.17.0 项目可直接在 `Ember/UPM Manager → AI Skill` 选择 `v0.17.1`，检查并安装 Ember 添加字体，重新加载 AI 会话后调用 `$ember-add-font`。也可通过 UPM Manager 升级框架；缺失技能由随包机制补入，已有本地副本不会静默覆盖。无需为本次技能更新重新部署模板。

技能最低框架版本 0.17.0；从更旧版本升级仍须遵循 0.17.0 的迁移要求。

## 验证

- 技能格式、公共清单与 bundle 的 11 项技能 / 32 个文件校验通过。
- 共享二进制及字体静态检查通过；模板相对 v0.17.0 无改动。
- 本次仅技能和发布资料更新，未启动 Unity 编译。真实字体导入、批量接线、消费端安装和视觉效果尚未验收。

发布声明见 [release-0.17.1.json](../../Packages/com.ember/Dependencies~/release-0.17.1.json)，完整依赖见 [manifest-0.17.1.json](../../Packages/com.ember/Dependencies~/manifest-0.17.1.json)。
