# ============================================================
# 剥离模板场景中的 dev 测试对象（不可进模板的第三方/测试内容）
# 用法：pwsh scripts/strip-template-scene-objects.ps1
# ============================================================
$ErrorActionPreference = "Stop"

$targets = @(
    @{ scene = "Packages\com.ember\Templates~\base\Assets\Game\Scenes\FrameworkScene.unity"; names = @("RainbowHierarchyRuleset") },
    @{ scene = "Packages\com.ember\Templates~\base\Assets\Game\Scenes\MainScene.unity";         names = @("UnitaskDeme", "OdinDemo", "FeelDemo") }
)

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Remove-ObjectsFromScene($scenePath, $names) {
    $lines = [System.Collections.Generic.List[string]]([System.IO.File]::ReadAllLines($scenePath, [System.Text.Encoding]::UTF8))

    # 1) 找目标 GameObject 块：解析块边界
    $blockStart = @()   # 行索引列表（0 基）
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].StartsWith("--- !u!")) { $blockStart += $i }
    }

    $goIds = @{}
    $componentIds = @()   # HashSet 行为：用数组 + -contains（小规模）
    foreach ($name in $names) {
        for ($b = 0; $b -lt $blockStart.Count; $b++) {
            $s = $blockStart[$b]
            $e = if ($b + 1 -lt $blockStart.Count) { $blockStart[$b + 1] } else { $lines.Count }
            $header = $lines[$s]
            if ($header -match '^--- !u!1 &') {
                $goId = $header.Substring($header.LastIndexOf('&') + 1).Trim()
                $inBlock = $lines[$s..($e - 1)]
                $hasName = ($inBlock | Where-Object { $_ -match "m_Name: $([regex]::Escape($name))$" }).Count -gt 0
                if ($hasName) {
                    $goIds[$goId] = $true
                    # 收集组件 fileID
                    foreach ($l in $inBlock) {
                        if ($l -match 'component: \{fileID: (\d+)\}') { $componentIds += $matches[1] }
                    }
                }
            }
        }
    }

    if ($goIds.Count -eq 0) {
        Write-Host "  （$scenePath 无匹配对象）"
        return
    }

    # 2) 删除块：GameObject 块 + m_GameObject 指向它们的块
    $toDelete = @{}
    for ($b = 0; $b -lt $blockStart.Count; $b++) {
        $s = $blockStart[$b]
        $e = if ($b + 1 -lt $blockStart.Count) { $blockStart[$b + 1] } else { $lines.Count }
        $header = $lines[$s]
        if ($header -match '^--- !u!1 &') {
            $id = $header.Substring($header.LastIndexOf('&') + 1).Trim()
            if ($goIds.ContainsKey($id)) { for ($i = $s; $i -lt $e; $i++) { $toDelete[$i] = $true } }
        } else {
            $inBlock = $lines[$s..($e - 1)]
            $isComp = ($inBlock | Where-Object { $_ -match "m_GameObject: \{fileID: ($($goIds.Keys -join '|'))\}" }).Count -gt 0
            if ($isComp) { for ($i = $s; $i -lt $e; $i++) { $toDelete[$i] = $true } }
        }
    }

    # 3) 重建：跳过删除行；对保留行清理 m_Children 中被删组件引用
    $out = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($toDelete.ContainsKey($i)) { continue }
        $line = $lines[$i]
        if ($line -match '- \{fileID: (\d+)\}') {
            if ($componentIds -contains $matches[1]) { continue }
        }
        $out.Add($line)
    }

    [System.IO.File]::WriteAllLines($scenePath, $out, $utf8NoBom)
    Write-Host "  已剥离 $($goIds.Count) 个对象（$($names -join ', ')）from $scenePath"
}

foreach ($t in $targets) {
    Remove-ObjectsFromScene $t.scene $t.names
}
Write-Host "完成"
