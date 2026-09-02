# ============================================================
# 同步脚手架模板：把 dev 业务层镜像进包内 Templates~/base/Assets
# （黄金基准机制：改 dev 的演示代码 → 跑本脚本 → 提交发版）
#
# 用法：pwsh scripts/sync-scaffold.ps1
# 之后自动执行 strip-template-scene-objects.ps1 剥离 dev 测试对象
# ============================================================
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dst = Join-Path $repoRoot "Packages\com.ember\Templates~\base\Assets"

Write-Host "== 同步 dev 业务层 → 模板 =="
$pairs = @(
    @{ src = "Assets\Game";            sub = "Game" },
    @{ src = "Assets\GameResource";    sub = "GameResource" },
    @{ src = "Assets\Resources";       sub = "Resources" },
    @{ src = "Assets\Ember\Editor";    sub = "Ember\Editor" },
    @{ src = "Assets\Settings";        sub = "Settings" }
)
foreach ($p in $pairs) {
    # 先清空旧镜像再复制（模板 = 当前 dev 的精确快照）
    $target = Join-Path $dst $p.sub
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    robocopy (Join-Path $repoRoot $p.src) $target /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy $($p.src) failed: $LASTEXITCODE" }
}
Write-Host "镜像完成：$((Get-ChildItem $dst -Recurse -File | Measure-Object).Count) 文件"

Write-Host "== 剥离 dev 测试对象 =="
& (Join-Path $PSScriptRoot "strip-template-scene-objects.ps1")

Write-Host "同步完成。检查 git diff 后提交发版。"
