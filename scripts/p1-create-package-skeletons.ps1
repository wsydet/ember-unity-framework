# ============================================================
# P1 包骨架生成脚本（可重复运行，已存在文件跳过）
# 用法：pwsh scripts/p1-create-package-skeletons.ps1
# ============================================================
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$packagesDir = Join-Path $repoRoot "Packages"

$MIT = @"
MIT License

Copyright (c) 2026 wsydet

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"@

$GitFramework = "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/"
$GitThirdparty = "https://github.com/wsydet/ember-thirdparty-upm.git?path=/"

$packages = @(
    @{
        name = "core"
        displayName = "Ember Core"
        description = "Ember 框架核心：事件总线、服务定位、单例/对象池、GameState 状态机、Update 循环、Manager 自动发现、定时器。"
        deps = [ordered]@{
            "com.ember.basic" = $GitFramework + "com.ember.basic#v0.2.0"
            "com.cysharp.unitask" = "2.5.10"
            "com.sirenix.odin-inspector" = $GitThirdparty + "com.sirenix.odin-inspector#odin-v4.0.2"
        }
    },
    @{
        name = "resource"
        displayName = "Ember Resource"
        description = "Ember 框架资源管理：IResourceProvider 接口 + EmberResourceManager 门面，可挂 Resources/Addressables/YooAsset 后端。"
        deps = [ordered]@{
            "com.ember.core" = $GitFramework + "com.ember.core#v0.2.0"
        }
    },
    @{
        name = "scene"
        displayName = "Ember Scene"
        description = "Ember 框架场景管理：异步加载/卸载、过渡切换、激活前回调、状态机桥接。"
        deps = [ordered]@{
            "com.ember.core" = $GitFramework + "com.ember.core#v0.2.0"
            "com.ember.resource" = $GitFramework + "com.ember.resource#v0.2.0"
            "com.cysharp.unitask" = "2.5.10"
        }
    },
    @{
        name = "audio"
        displayName = "Ember Audio"
        description = "Ember 框架音频管理：BGM/SFX 分离播放与 Mixer 音量控制。"
        deps = [ordered]@{
            "com.ember.core" = $GitFramework + "com.ember.core#v0.2.0"
        }
    },
    @{
        name = "camera"
        displayName = "Ember Camera"
        description = "Ember 框架相机管理：Cinemachine 虚拟相机注册/切换与强制霸占堆栈。"
        deps = [ordered]@{
            "com.ember.core" = $GitFramework + "com.ember.core#v0.2.0"
            "com.unity.cinemachine" = "3.1.7"
        }
    },
    @{
        name = "input"
        displayName = "Ember Input"
        description = "Ember 框架输入抽象：基于 Unity Input System 的 Action Map 切换与轴/按键读取。"
        deps = [ordered]@{
            "com.ember.core" = $GitFramework + "com.ember.core#v0.2.0"
            "com.unity.inputsystem" = "1.19.0"
        }
    },
    @{
        name = "ui"
        displayName = "Ember UI"
        description = "Ember 框架 UI 管理：四层 Canvas 界面栈、EUIViewEngine 视图引擎、页面生命周期与过渡管道。"
        deps = [ordered]@{
            "com.ember.basic" = $GitFramework + "com.ember.basic#v0.2.0"
            "com.ember.core" = $GitFramework + "com.ember.core#v0.2.0"
            "com.ember.resource" = $GitFramework + "com.ember.resource#v0.2.0"
            "com.ember.scene" = $GitFramework + "com.ember.scene#v0.2.0"
            "com.neuecc.unirx" = "7.1.0"
            "com.cysharp.unitask" = "2.5.10"
            "com.unity.ugui" = "2.5.0"
            "com.demigiant.dotween" = $GitThirdparty + "com.demigiant.dotween#dotween-v1.2.815"
        }
    },
    @{
        name = "editor"
        displayName = "Ember Editor"
        description = "Ember 框架编辑器工具：状态↔场景映射、快速场景打开、Toolbar 按钮等框架级工具。"
        deps = [ordered]@{
            "com.ember.basic" = $GitFramework + "com.ember.basic#v0.2.0"
            "com.ember.core" = $GitFramework + "com.ember.core#v0.2.0"
        }
    }
)

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$created = 0
$skipped = 0

foreach ($pkg in $packages) {
    $pkgDir = Join-Path $packagesDir ("com.ember." + $pkg.name)
    if (-not (Test-Path $pkgDir)) { New-Item -ItemType Directory -Path $pkgDir | Out-Null }

    # ---- package.json ----
    $pkgJson = Join-Path $pkgDir "package.json"
    if (-not (Test-Path $pkgJson)) {
        $json = [ordered]@{
            name = "com.ember." + $pkg.name
            displayName = $pkg.displayName
            version = "0.1.0"
            description = $pkg.description
            unity = "6000.0"
            dependencies = $pkg.deps
        }
        [System.IO.File]::WriteAllText($pkgJson, ($json | ConvertTo-Json -Depth 10), $utf8NoBom)
        $created++
    } else { $skipped++ }

    # ---- README.md ----
    $readme = Join-Path $pkgDir "README.md"
    if (-not (Test-Path $readme)) {
        $depRows = ""
        foreach ($k in $pkg.deps.Keys) {
            $depRows += "| $k | $($pkg.deps[$k]) |`n"
        }
        $readmeText = @"
# $($pkg.displayName)（com.ember.$($pkg.name)）

$($pkg.description)

## 依赖

| 包 | 版本/来源 |
|----|----------|
$depRows
> 第三方依赖说明：`com.cysharp.unitask` / `com.neuecc.unirx` 需在项目 manifest.json 配置 OpenUPM scoped registry；`com.sirenix.odin-inspector` / `com.demigiant.dotween` 来自私有仓库，需 git 访问凭据（见框架文档 §3.4）。

## 安装

```json
{
  "dependencies": {
    "com.ember.$($pkg.name)": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.$($pkg.name)#v0.2.0"
  }
}
```

本包与 Ember 框架其他包 lockstep 统一版本。详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。
"@
        [System.IO.File]::WriteAllText($readme, $readmeText, $utf8NoBom)
        $created++
    } else { $skipped++ }

    # ---- CHANGELOG.md ----
    $changelog = Join-Path $pkgDir "CHANGELOG.md"
    if (-not (Test-Path $changelog)) {
        $changelogText = @"
# Changelog

本包与 Ember 框架其他包 lockstep 统一版本。

## [0.1.0] - 2026-08-22

- 包骨架创建（P1）。代码于 P2 迁入；首个对外发布版本为 v0.2.0。
"@
        [System.IO.File]::WriteAllText($changelog, $changelogText, $utf8NoBom)
        $created++
    } else { $skipped++ }

    # ---- LICENSE.md ----
    $license = Join-Path $pkgDir "LICENSE.md"
    if (-not (Test-Path $license)) {
        [System.IO.File]::WriteAllText($license, $MIT, $utf8NoBom)
        $created++
    } else { $skipped++ }
}

Write-Host "P1 骨架生成完成：新建 $created 个文件，跳过 $skipped 个已存在文件。"
