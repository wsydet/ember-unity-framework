# Ember Framework（com.ember）

Ember 通用游戏开发框架——事件系统、资源管理、UI 管理、场景/音频/相机/输入、GameState 状态机、Manager 自动发现 + 编辑器工具 + UI 绑定代码生成，**全模块合一的单一包**（v0.3.0 起由 11 个包合并而来）。

## 安装（一行）

```json
// Packages/manifest.json 的 dependencies
"com.ember": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.3.0"
```

## 前置依赖（付费/受限，需单独安装）

| 依赖 | 用途 | 安装方式 |
|------|------|---------|
| Odin Inspector（付费） | Inspector 增强，框架部分类型使用其属性 | 菜单 `Ember/UPM Manager` 检测 + 一键安装（团队私有仓库）或自行购买导入 |
| DOTween（免费，禁止再分发） | 补间动画（UI 过渡等） | 同面板引导；或从官网 dotween.demigiant.com 自行下载 |

> 未安装 Odin 时框架编译会报 Sirenix 缺失——这是已知的硬前置（开源前将做条件编译解耦，见框架文档 §3.4）。

## 内部模块（程序集，随包全带）

| 程序集 | 模块 |
|--------|------|
| Ember.Basic.Runtime / Editor | 基础库 + 编辑器工具 |
| Ember.Core.Runtime / Editor | 核心：事件/服务/状态机/Update/Manager |
| Ember.Extensions | 扩展方法 |
| Ember.Resource.Runtime | 资源管理 |
| Ember.Scene.Runtime | 场景管理 |
| Ember.Audio.Runtime | 音频 |
| Ember.Camera.Runtime | 相机 |
| Ember.Input.Runtime | 输入 |
| Ember.UI.Runtime / Editor / Tests | UI 框架 |
| Ember.UIExtension.Runtime / Editor | UI 绑定与增强组件 |
| Ember.Editor | 框架编辑器工具 |

## 脚手架

菜单 `Ember/Setup/初始化项目` 一键生成可运行骨架（业务状态类 + GamePages + 启动场景）。详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。

## 升级

改 manifest 中 `#v0.3.0` 为新 tag → 删 `packages-lock.json` → 重新打开项目。
