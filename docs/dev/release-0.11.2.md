# Ember Framework 0.11.2 发布说明

发布日期：2026-09-08

## 修复内容

- 项目中心支持从当前完整模板切换到另一完整模板。切换前自动备份 `Assets/Game`、`Assets/Resources`、`Assets/Ember/Editor`、`Assets/Settings`、`Assets/GameResource`，以及部署记录和 Build Settings；目标模板通过文件事务替换，事务失败自动回滚。
- 模板内容 hash 对已知文本格式统一 CRLF/LF，修复 Git URL 安装到 `PackageCache` 后因消费机行尾设置产生的“磁盘内容与 metadata 不一致”。二进制文件仍按原始字节校验。
- 消费端内容比较复用部署器的版本头转换，修复已部署 C# 文件仅因版本头被正常改写而出现的大量伪差异。

## 发布与依赖

- 框架 Git tag：`v0.11.2`
- 安装 URL：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.11.2`
- 完整依赖基线：`Packages/com.ember/Dependencies~/manifest-0.11.2.json`
- 第三方包内容未变化，继续固定到已发布的 `ember-thirdparty-upm` tag `ember-v0.11.1`。

## 验证边界

发布前已检查模板 metadata/hash/父快照、版本文件与静态差异。当前没有可用的 Unity MCP，因此本轮未完成 Unity 编译验证或 0.11.2 的 EditMode 执行。Unity Farm 等消费项目需要在安装 0.11.2 后验证实际 UPM 更新、Base → 2.5D 切换、剩余真实差异与 Play Mode 行为。
