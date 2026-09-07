# `com.ember.updater` 独立升级器演进方案

> 状态：未来演进预案，当前不实施
>
> 记录日期：2026-09-02
>
> 核对基线：2026-09-07 工作区，正在准备 0.11.0 发布，`package.json` 仍为 0.10.0；升级追踪器内置于 `com.ember`，独立包尚不存在

## 一、背景

当前 `Ember/UPM Manager` 位于 `com.ember` 包内部，通过替换项目
`Packages/manifest.json` 中 `com.ember` Git URL 的 tag，再调用
`UnityEditor.PackageManager.Client.Add` 完成升级。

当前工作区的 `EmberUPMUpgradeTracker` 已包含以下实现：

- 四阶段进度：校验、下载与依赖解析、注册与编译、版本验证。
- 使用 `SessionState` 保存升级目标和阶段，支持脚本域重载后续接。
- 监听 Package Manager 的 `registeringPackages` / `registeredPackages` 事件。
- 同步 Unity 后台 Progress，并显示耗时、慢任务警告和诊断信息。
- 不伪造下载百分比，最终以实际安装的包版本确认成功。

这套方案适合当前单包、体量较小的框架，但它仍有一个先天限制：
**负责升级的代码与被升级的包是同一个包。** 升级 `com.ember` 时，升级器自己的程序集也会被替换。

## 二、何时需要拆包

不要仅因为“未来可能变大”立即增加一个包。满足下列任意两项时，再正式启动拆分：

1. `com.ember` 的 Git 下载、导入或脚本编译时间持续增长，消费项目升级经常超过 2 分钟。
2. 框架开始拆为多个可选包，需要统一处理兼容矩阵、升级顺序或批量升级。
3. 需要稳定支持回滚、预览通道、升级前检查、升级日志导出等能力。
4. 框架包发生编译错误时，仍要求升级/修复面板保持可用。
5. 升级协议需要独立迭代，不希望升级器版本与框架版本锁步发布。
6. Farm 或其他消费项目需要无人值守升级、CI 检查或统一版本策略。

建议在决定拆分前记录一段时间的真实数据：升级总耗时、Package Manager 解析耗时、失败率、
域重载次数和主要失败原因。是否拆分应由这些数据决定。

## 三、目标架构

```text
消费项目 Packages/manifest.json
├── com.ember          # 业务框架，可频繁升级
└── com.ember.updater  # 纯 Editor 升级器，稳定、低频升级

com.ember.updater
    ├── 读取 manifest 与 PackageInfo
    ├── 查询远程版本
    ├── 执行升级、验证、诊断与回滚
    └── 只依赖 UnityEditor / UnityEditor.PackageManager

com.ember
    └── 不引用 com.ember.updater，也不再承载升级状态机
```

核心原则是：`com.ember.updater` 必须能在 `com.ember` 缺失、正在替换或编译失败时独立工作。

### 3.1 依赖边界

- 升级器为纯 Editor 包，不包含 Runtime 程序集。
- 升级器不得引用 `Ember.Core`、`Ember.Basic`、Odin、DOTween 或其他框架程序集。
- 升级器只使用 Unity 自带 API；日志、窗口和配置都由升级器自己实现。
- `com.ember` 也不应反向引用升级器，避免循环依赖和“框架不编译导致升级器失效”。
- 两个 Git 包都由消费项目的 `manifest.json` 直接声明；不要依赖 Git 包传递 Git URL 依赖。
- 升级器采用独立版本，例如 `1.0.0`，不与框架的 `0.x` 版本锁步。

### 3.2 建议目录

```text
Packages/com.ember.updater/
├── package.json
├── CHANGELOG.md
├── Editor/
│   ├── Ember.Updater.Editor.asmdef
│   ├── EmberUpdaterWindow.cs
│   ├── EmberUpdateCoordinator.cs
│   ├── EmberUpdateStateStore.cs
│   ├── EmberManifestRepository.cs
│   ├── EmberRemoteVersionProvider.cs
│   └── EmberUpdateDiagnostics.cs
└── Tests/
    └── Editor/
        ├── Ember.Updater.Editor.Tests.asmdef
        └── ...
```

## 四、职责划分

### `EmberUpdaterWindow`

- 展示已安装版本、远程版本、更新通道和兼容性提示。
- 展示阶段进度、耗时、慢任务警告和最终结果。
- 提供检查更新、开始升级、复制诊断和安全回滚入口。
- 不直接持有 `AddRequest`；窗口关闭或重建不影响升级任务。

### `EmberUpdateCoordinator`

- 升级任务的唯一状态机。
- 调用 `Client.Add`，监听 Package Manager 注册事件并验证结果。
- 处理脚本域重载、窗口关闭和 Unity 重启后的恢复。
- 同一时间只允许一个变更 manifest 的操作。

### `EmberUpdateStateStore`

- 保存状态结构版本、操作 ID、源 URL、目标 URL、源版本、目标版本、阶段、时间戳和错误。
- 短期状态可放 `SessionState`；若要支持 Unity 重启恢复，则将任务状态写入项目 `Library/EmberUpdater/`。
- 不保存 Git 用户名、令牌或其他凭据。

### `EmberManifestRepository`

- 负责读取、校验和修改 `Packages/manifest.json`。
- 修改前记录原始 `com.ember` URL，用于诊断和显式回滚。
- 只允许修改白名单包名与预期仓库 URL，拒绝任意包或任意命令输入。

### `EmberRemoteVersionProvider`

- 查询并解析稳定版、预览版 tag。
- 将网络查询与 UI 分离，提供超时、缓存和清晰错误分类。
- 后续若迁移到 registry，只替换版本提供器和安装策略，不重写窗口状态机。

## 五、升级状态机

```text
Idle
  ↓
Preflight          校验当前版本、目标版本、manifest、网络与 Git URL
  ↓
Resolving          Package Manager 下载并解析包
  ↓
Registering        注册包、刷新 AssetDatabase、编译脚本
  ↓
Verifying          核对 PackageInfo、manifest URL 和目标版本
  ├── Succeeded
  └── Failed        保留诊断；不自动重复执行
```

设计要求：

- Package Manager 未提供真实下载百分比时，使用阶段进度和活动动画，不显示伪精确百分比。
- 超时只改变提示级别，不擅自认定失败，也不自动发起第二个 `Client.Add`。
- 成功必须以 `PackageInfo.FindForPackageName("com.ember")` 返回目标版本为准。
- 回滚必须是用户显式操作，并且只能在没有活动 UPM 请求、Unity 未编译且 AssetDatabase 未刷新时启动。

## 六、两版本迁移策略

独立升级器不能在一个版本中直接“无缝替换”旧升级器。旧消费项目发起升级时，运行的仍是旧版
`com.ember` 代码，因此应采用两个框架版本完成迁移。

### 迁移版本 N

1. 创建并发布 `com.ember.updater`，例如 tag `updater-v1.0.0`。
2. `com.ember` 暂时保留现有内置 UPM Manager。
3. 内置面板检测消费项目是否直接安装了 `com.ember.updater`。
4. 未安装时提供明确的“一键安装独立升级器”入口，并说明会修改项目 manifest。
5. 安装完成后由独立升级器接管菜单和后续升级；旧面板只显示迁移状态。
6. Farm、模板项目和新项目清单从这一版本开始直接声明两个包。

### 清理版本 N+1

1. 移除 `com.ember` 内部的升级状态机和升级窗口。
2. 可保留一个极小的兼容提示：未检测到独立升级器时给出安装 URL，不承担升级操作。
3. 所有正式升级测试从独立升级器发起。

至少保留一个完整发布周期的过渡期，避免老项目升级到新框架后突然失去管理入口。

## 七、版本与发布建议

- 框架 tag 继续使用 `v0.12.0`、`v0.13.0` 等格式。
- 升级器 tag 使用独立前缀，例如 `updater-v1.0.0`。
- 即使两个包暂时位于同一 Git 仓库，也必须使用不同 tag 和独立 `package.json` 版本。
- 升级器优先保持向后兼容：新升级器应能管理多个旧框架版本。
- 升级器自身升级应低频、小体积，并在升级前确认没有正在进行的框架升级。

消费项目示意：

```json
{
  "dependencies": {
    "com.ember": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.12.0",
    "com.ember.updater": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.updater#updater-v1.0.0"
  }
}
```

## 八、验收清单

- 在 `com.ember` 编译失败时，升级器窗口仍可打开并正常显示诊断。
- 升级过程中关闭并重新打开窗口，状态不丢失。
- 升级触发脚本域重载后，任务能继续到成功或失败终态。
- Unity 中途退出后再次打开项目，能够恢复或明确识别遗留任务。
- 网络失败、Git 凭据失败、目标 tag 不存在、版本不匹配均有独立错误信息。
- 连续点击不会产生并发 UPM 请求。
- 成功、失败和显式回滚都保留可复制的诊断记录。
- 从迁移版本 N 升到 N+1 后，内置升级器移除且独立升级器继续可用。
- Farm 至少完成一次稳定版升级、一次失败恢复和一次域重载续接实测。

## 九、当前决策

当前继续采用内置跨域重载升级追踪器，不立即增加第二个包。源码存在不代表当前工作区已通过 Unity 回归，验收见 [框架测试清单](framework-test-checklist.md)。

当第二节的拆分条件成立时，以本文为基线启动 `com.ember.updater`；优先复用当前追踪器已有的
阶段模型、`SessionState` 续接、Package Manager 事件监听和最终版本验证逻辑，再增加持久化恢复、
独立发布和安全回滚能力。
