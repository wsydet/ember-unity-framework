# Changelog

Ember 框架单一包，lockstep 版本。

## [0.3.0] - 2026-08-26

- 11 个包合并为单一 `com.ember`（basic/extensions/uiextension/core/resource/scene/audio/camera/input/ui/editor 全部并入）
- 程序集边界不变（原 11 个 asmdef 原样保留），模块化由 asmdef 保证
- 新增 EmberUPMManager 面板：Odin/DOTween 检测 + 一键安装 + 手动安装指引
- 消费端安装简化为一行 git URL；升级只改一个 tag

## [0.2.0] - 2026-08-24

- 框架转 UPM 包首个发布版（11 包结构，已被 0.3.0 取代）
