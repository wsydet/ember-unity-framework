# Changelog

## [0.4.0] - 2026-08-26

- EmberUPMManager 一键升级（检查更新 + 改 manifest tag + Client.Resolve），并按版本语义标注强制/可选更新
- 版本语义定稿：开发期 major=0；第二位=框架变化（强制更新）；第三位=小修补（可选）
- 累计 0.3.x 变更：UniTask 内置、Setup 向导 4 场景、场景映射 SO 修复

Ember 框架单一包，lockstep 版本。

## [0.3.0] - 2026-08-26

- 11 个包合并为单一 `com.ember`（basic/extensions/uiextension/core/resource/scene/audio/camera/input/ui/editor 全部并入）
- 程序集边界不变（原 11 个 asmdef 原样保留），模块化由 asmdef 保证
- 新增 EmberUPMManager 面板：Odin/DOTween 检测 + 一键安装 + 手动安装指引
- 消费端安装简化为一行 git URL；升级只改一个 tag

## [0.2.0] - 2026-08-24

- 框架转 UPM 包首个发布版（11 包结构，已被 0.3.0 取代）
