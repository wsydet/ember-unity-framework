# Package 清单

> 本文列出 ember-unity-framework 已集成的所有包。使用本框架前请先确认清单，**避免重复导入已有依赖**。

---

## 一、第三方 Package

### 1.1 Embedded Package（内置在 Packages/ 中）

| 包名 | 版本 | 用途 | 来源 |
|------|------|------|------|
| com.demigiant.dotween | 1.2.815 | 动画引擎，补间动画 | [官网](https://dotween.demigiant.com/) → 手动移入 |

> DOTween 作为 embedded package 放在 `Packages/com.demigiant.dotween/`，不需要额外安装。

### 1.2 UPM 第三方（通过 OpenUPM Registry）

| 包名 | 版本 | 用途 |
|------|------|------|
| com.neuecc.unirx | 7.1.0 | 响应式编程框架，用于事件流和异步操作 |

> 需要配置 OpenUPM scoped registry（已在 `manifest.json` 中配置）：
> ```json
> { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": ["com.neuecc"] }
> ```

### 1.3 传统 Plugins（`Assets/Plugins/`）

| 插件名 | 版本 | 用途 | 来源 |
|-------|------|------|------|
| Sirenix (Odin Inspector) | [TODO] | 编辑器扩展，增强 Inspector 面板 | [Asset Store](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041) → 手动安装 |

> Odin Inspector 是付费 Asset Store 产品，以预编译 DLL 形式放在 `Assets/Plugins/Sirenix/`。
> 因 Odin 依赖结构复杂无法迁移为 UPM embedded package，保留在 Plugins 目录中。

---

## 二、Unity 官方 Package

### 2.1 核心框架

| 包名 | 版本 | 用途 |
|------|------|------|
| com.unity.render-pipelines.universal | 17.5.0 | URP 渲染管线 |
| com.unity.inputsystem | 1.19.0 | 新输入系统 |
| com.unity.ugui | 2.5.0 | Unity UI（uGUI） |
| com.unity.visualscripting | 1.9.11 | 可视化脚本（远期蓝图基础） |
| com.unity.cinemachine | 3.1.7 | 虚拟摄像机系统 |

### 2.2 辅助工具

| 包名 | 版本 | 用途 |
|------|------|------|
| com.unity.timeline | 1.8.12 | 时间线编辑 |
| com.unity.ai.navigation | 2.0.13 | AI 导航（NavMesh） |
| com.unity.test-framework | 1.7.0 | 单元测试与集成测试 |
| com.unity.multiplayer.center | 1.0.1 | 多人游戏中心 |

### 2.3 IDE 支持

| 包名 | 版本 | 用途 |
|------|------|------|
| com.unity.ide.rider | 3.0.38 | JetBrains Rider 支持 |
| com.unity.ide.visualstudio | 2.0.26 | Visual Studio 支持 |

---

## 三、Unity 内置模块

以下 35 个模块是 Unity 引擎内置的，无需额外安装，默认启用：

| 模块 | 说明 |
|------|------|
| animation | 动画系统 |
| audio | 音频系统 |
| physics / physics2d / physicscore2d | 物理引擎 |
| particlesystem | 粒子系统 |
| terrain / terrainphysics | 地形系统 |
| tilemap | 瓦片地图 |
| ui / uielements | UI 系统（UGUI + UI Toolkit） |
| umbra | 遮挡剔除 |
| imgui | 即时模式 GUI（Editor 用） |
| jsonserialize | JSON 序列化 |
| assetbundle | 资源包 |
| video | 视频播放 |
| director | Playable Director |
| androidjni | Android JNI |
| xr | XR（AR/VR） |
| accessibility / ai / adaptiveperformance | 无障碍 / AI / 自适应性能 |
| cloth | 布料系统 |
| screencapture | 截屏 |
| vectorgraphics | 矢量图形 |
| vehicles | 载具系统 |
| wind | 风力系统 |
| unityanalytics | 分析 |
| unitywebrequest* | 网络请求（4 个模块） |
| imageconversion | 图像转换 |

---

## 四、如果未来要新增依赖

### 怎么加

| 来源 | 方式 | 放在哪 |
|------|------|--------|
| Unity Registry | `Window → Package Manager` 搜索安装 | `manifest.json` 自动更新 |
| OpenUPM | 确认 scope 已在 `manifest.json` 中注册 | `manifest.json` 自动更新 |
| Asset Store / 官网 .unitypackage | 安装后**移动到 `Packages/` 下** + 补 `package.json` | 手动维护 |
| Git URL | 直接在 `manifest.json` 加 Git URL | `manifest.json` |

### 加完之后

- 更新本文档，在对应章节新增一行
- 提交时注明 `chore: add <包名> (<用途>)`

---

## 五、导入框架后的检查步骤

如果你是新接手的开发者，拿到本项目后：

1. 用 Unity Hub 打开项目，Unity 会自动根据 `manifest.json` 和 `packages-lock.json` 还原所有 UPM 包
2. `Packages/com.demigiant.dotween/` 是 embedded package，已随 git 提交，无需额外下载
3. 打开本文档确认所有包已正确加载
4. 不要重复导入 Dotween、UniRx、Cinemachine 等已有包
