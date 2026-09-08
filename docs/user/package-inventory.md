# Package 清单

> 本文列出 ember-unity-framework 已集成的所有包。使用本框架前请先确认清单，**避免重复导入已有依赖**。

最后核对：2026-09-08；按当前 `manifest.json`、`packages-lock.json` 和 embedded `package.json` 统计，不代表远程最新版本。

---

## 一、第三方 Package

### 1.1 Embedded Package（内置在 Packages/ 中）

| 包名 | 版本 | 用途 | 来源 |
|------|------|------|------|
| com.borodar.rainbow-folders | 2.4.5 | Project 窗口文件夹自定义图标和背景色 | [GitHub](https://github.com/Borod4r/Rainbow-Folders-2) → 源码移入 |
| com.borodar.rainbow-hierarchy | 2.6.5 | Hierarchy 窗口 GameObject 自定义图标和背景色 | [GitHub](https://github.com/Borod4r/Rainbow-Hierarchy-2) → 源码移入 |
| com.demigiant.dotween | 1.2.815 | 动画引擎，补间动画 | [官网](https://dotween.demigiant.com/) → 手动移入 |
| com.flyingworm.consolepro | 3.9.81 | 编辑器控制台增强：过滤、搜索、远程日志 | [Asset Store](https://assetstore.unity.com/packages/tools/utilities/console-pro-3) → 手动移入 |
| com.ryanindiedev.inputdevicedetector | 1.0.0 | 输入设备检测：自动识别鼠标/键盘/手柄切换并触发事件 | [YouTube](https://www.youtube.com/channel/UCSRCf2y6LV8vpKSoXDoU2VQ) → 手动移入 |
| com.ember | 0.11.5 | Ember 框架（单包合一）：事件/资源/UI/场景/音频/相机/输入 + 状态机 + 编辑器工具 + UI 绑定代码生成；**内置 UniTask（Unity 6000.5 TreeView 泛型修复版，MIT 随包分发）** | 框架自带 → embedded |
| com.neuecc.unirx | 7.1.0 | 响应式编程框架，用于事件流和异步操作 | manifest 声明 OpenUPM 版本，当前 lock 实际解析为 embedded 副本 |

> 以上包均放在 `Packages/` 下作为 embedded package，随 git 提交，无需额外下载。
> 📌 2026-08-26 起 11 个 `com.ember.*` 包合并为单一 `com.ember`（模块边界由包内 asmdef 保证），详见 [UPM 交付维护](../dev/upm-migration-plan.md)。

### 1.2 UPM 第三方（通过 OpenUPM Registry）

| 包名 | 版本 | 用途 |
|------|------|------|
| com.github-glitchenzo.nugetforunity | 4.5.0 | NuGet 包管理器，在 Unity 中安装 .NET 第三方库 |

UniRx 在 manifest 中仍声明 `7.1.0`，但本工程存在同名 embedded 包，实际来源见 1.1；消费工程没有该副本时需使用以下 OpenUPM 配置解析。

> 需要配置 OpenUPM scoped registry（已在 `manifest.json` 中配置）：
> ```json
> { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": ["com.github-glitchenzo", "com.neuecc"] }
> ```

### 1.3 Git URL 包（本项目直接依赖）

| 包名 | 版本/tag | 用途 | 来源 |
|-------|------|------|------|
| com.coplaydev.unity-mcp | main（解析提交见 packages-lock） | Unity MCP 编辑器集成 | [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) `?path=/MCPForUnity#main` |
| com.sirenix.odin-inspector | odin-v4.0.2 | 编辑器扩展，增强 Inspector 面板 | 私有仓 [ember-thirdparty-upm](https://github.com/wsydet/ember-thirdparty-upm) `?path=/com.sirenix.odin-inspector#odin-v4.0.2`（付费，需 git 凭据 + 正版授权） |

> Odin Inspector 是付费插件，已从 `Assets/Plugins/Sirenix/` 迁出（2026-08），改由私有仓库 git URL 安装。
> 同仓托管的还有：DOTween（`dotween-v1.2.815`）、Console Pro（`consolepro-v3.9.81`）、RainbowFolders（`rainbow-folders-v2.4.5`）、RainbowHierarchy（`rainbow-hierarchy-v2.6.5`）、InputDeviceDetector（`inputdevicedetector-v1.0.0`）。本项目仍使用上表中的 embedded 副本；消费端安装方式按其 manifest 确认，本清单不推断远程 tag 的发布状态。

### 1.4 已确定的第三方交付范围

Rainbow Folders、Rainbow Hierarchy、Console Pro、InputDeviceDetector 和 Feel 均纳入交付，统一放入私有 `ember-thirdparty-upm`，不内嵌到公开 Ember 主包。
2026-09-07 已逐文件核对前四个包与本工程 embedded 副本一致（172 / 109 / 22 / 11 个文件）。已有本地 tag 不等于远程已发布。

Feel 的当前安装位置仍为 `Assets/ThirdParty/Feel`，供应商版本 **5.4**。第三方仓库已新增 `com.moremountains.feel 5.4.0` 的本地 UPM 封装，4901 个原始文件及根目录 `.meta` 原样保留，未改变当前工程安装方式。
该封装面向当前 Unity 6000.5 / URP 17.5.0 基线；尚未完成 Unity 安装、编译、Inspector/运行时和原生插件验收，也未创建或推送 Feel tag。

随框架分发的 [0.11.5 消费端依赖声明](../../Packages/com.ember/Dependencies~/README.md)包含当前 55 项直接依赖加 Feel，共 56 项；第三方内容未变化，7 个私有包复用已发布的 `ember-v0.11.1`，MCP 固定到当前 lock 的 commit。该清单独立于本工程 manifest，不改变这里记录的实际安装来源。
声明清单不代表升级 `com.ember` 会自动安装或同步这些包，消费端仍须确认后合并清单并配置私有仓库权限；迁移 Feel 时不得同时导入源插件和新包。

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

以下模块由当前 manifest 声明，均为 1.0.0；分组展示，具体启用项以 manifest 为准：

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
| unitywebrequest* | 网络请求（当前 5 个模块） |
| imageconversion | 图像转换 |

---

## 四、如果未来要新增依赖

### 怎么加

| 来源 | 方式 | 放在哪 |
|------|------|--------|
| Unity Registry | `Window → Package Manager` 搜索安装 | `manifest.json` 自动更新 |
| OpenUPM | 确认 scope 已在 `manifest.json` 中注册 | `manifest.json` 自动更新 |
| Asset Store / 官网 .unitypackage | 先检查授权、路径约束、GUID 和程序集边界，再按插件迁移方案处理 | 手动维护 |
| Git URL | 直接在 `manifest.json` 加 Git URL | `manifest.json` |

### 加完之后

- 更新本文档，在对应章节新增一行
- 提交时注明 `chore: 添加 <包名>（<用途>）`

---

## 五、导入框架后的检查步骤

如果你是新接手的开发者，拿到本项目后：

1. 用 Unity Hub 打开项目，Unity 会自动根据 `manifest.json` 和 `packages-lock.json` 还原所有 UPM 包
2. `Packages/com.demigiant.dotween/` 是 embedded package，已随 git 提交，无需额外下载
3. 打开本文档确认所有包已正确加载
4. 不要重复导入 Dotween、UniRx、Cinemachine 等已有包
