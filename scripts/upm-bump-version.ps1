# ============================================================
# Ember 框架 lockstep 版本 bump 脚本
# 用法：
#   pwsh scripts/upm-bump-version.ps1 -Version 0.2.0
#   pwsh scripts/upm-bump-version.ps1 -Version 0.3.0 -Check   # 只预览不写入
# 说明：同时更新 package.json 的 version 字段与
#       dependencies 中 ember 框架 git URL 的 #vX.Y.Z tag（lockstep）。
# ============================================================
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [switch]$Check
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "版本号格式错误，应为 x.y.z（如 0.2.0）"
    exit 1
}

# 统计 dependencies 中需要同步 tag 的数量（仅 ember 框架 URL：#vX.Y.Z）
function Get-DepTagSyncCount($json, $oldVersion) {
    $count = 0
    if ($null -ne $json.dependencies) {
        foreach ($prop in $json.dependencies.PSObject.Properties) {
            if ($prop.Value -is [string] -and $prop.Value -match ("#v" + [regex]::Escape($oldVersion))) {
                $count++
            }
        }
    }
    return $count
}

# 同步 dependencies 中 ember 框架 URL 的 #vX.Y.Z -> #vNewVersion
function Sync-DepTags($json, $oldVersion, $newVersion) {
    if ($null -eq $json.dependencies) { return }
    foreach ($prop in $json.dependencies.PSObject.Properties) {
        if ($prop.Value -is [string] -and $prop.Value -match ("#v" + [regex]::Escape($oldVersion))) {
            $prop.Value = $prop.Value -replace ("#v" + [regex]::Escape($oldVersion)), ("#v" + $newVersion)
        }
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$packagesDir = Join-Path $repoRoot "Packages"

if (-not (Test-Path $packagesDir)) {
    Write-Error "未找到 Packages 目录：$packagesDir"
    exit 1
}

$packages = Get-ChildItem -Path $packagesDir -Directory -Filter "com.ember.*" | Sort-Object Name
if ($packages.Count -eq 0) {
    Write-Error "Packages 下没有 com.ember.* 包"
    exit 1
}

$mode = if ($Check) { '预览' } else { '写入' }
Write-Host "== lockstep bump -> $Version（共 $($packages.Count) 个包，模式：$mode）=="

$changed = @()
foreach ($pkg in $packages) {
    $jsonPath = Join-Path $pkg.FullName "package.json"
    if (-not (Test-Path $jsonPath)) {
        Write-Warning "跳过 $($pkg.Name)：无 package.json"
        continue
    }

    $json = Get-Content $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $old = $json.version
    if ($old -eq $Version) {
        Write-Host "  $($pkg.Name)  已是 $Version（跳过）"
        continue
    }

    if ($Check) {
        Write-Host "  $($pkg.Name)  $old -> $Version"
        $depSync = Get-DepTagSyncCount $json $old
        if ($depSync -gt 0) { Write-Host "    依赖 tag 同步：$depSync 处 #v$old -> #v$Version" }
        $changed += $pkg.Name
        continue
    }

    $json.version = $Version
    Sync-DepTags $json $old $Version
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($jsonPath, ($json | ConvertTo-Json -Depth 10), $utf8NoBom)
    Write-Host "  $($pkg.Name)  $old -> $Version  [已写入]"
    $changed += $pkg.Name
}

if ($Check) {
    Write-Host ""
    Write-Host "预览模式：未写入任何文件。"
    exit 0
}

Write-Host ""
Write-Host "完成：$($changed.Count) 个包已更新。"
Write-Host "下一步（lockstep 发布）："
Write-Host "  git add Packages/com.ember.*/package.json"
Write-Host "  git commit -m ""chore: bump version to $Version"""
Write-Host "  git tag v$Version"
Write-Host "  git push origin main --tags"
