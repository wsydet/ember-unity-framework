$ErrorActionPreference = 'Stop'
$archiveRoot = 'C:\Users\wuyu\My\ember-unity-framework\docs\tasks\20261111-patch-preview-long-paths'
$manifest = Get-Content -Raw -LiteralPath (Join-Path $archiveRoot 'evidence\archive-manifest.json') | ConvertFrom-Json
$target = [IO.Path]::GetFullPath($manifest.sourceAbsolutePath)
$expected = 'C:\Users\wuyu\My\ember-unity-framework\.utmp\ember-workflow\20261111-patch-preview-long-paths'
if ($target -cne $expected -or $manifest.taskId -cne '20261111-patch-preview-long-paths') { throw 'Task identity mismatch' }
foreach ($ancestor in @($target, (Split-Path $target), (Split-Path (Split-Path $target)))) {
    if ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse parent' }
}
$entries = @(Get-ChildItem -LiteralPath $target -Recurse -Force)
if ($entries | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { throw 'Reparse subtree' }
if (@($entries | Where-Object { -not $_.PSIsContainer }).Count -ne $manifest.files.Count) { throw 'Unknown contents' }
foreach ($entry in $manifest.files) {
    if ((Get-FileHash -LiteralPath (Join-Path $target $entry.path) -Algorithm SHA256).Hash.ToLowerInvariant() -cne $entry.sha256) { throw 'Task file changed' }
    $copy = Join-Path $archiveRoot $entry.path
    if ($entry.path -in @('handoff.md','requirements.md','implementation.md','consumer-validation-prompt.md')) { $copy = Join-Path $archiveRoot ('evidence/archive-inputs/' + $entry.path) }
    if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash.ToLowerInvariant() -cne $entry.sha256) { throw 'Unarchived bytes' }
}
$links = Get-Content -Raw -LiteralPath (Join-Path $archiveRoot 'evidence\archive-link-check.json') | ConvertFrom-Json
if ($links | Where-Object { -not $_.exists -or $_.dependsOnTemporaryDirectory }) { throw 'Invalid formal links' }
Remove-Item -LiteralPath $target -Recurse -Force
if (Test-Path -LiteralPath $target) { throw 'Cleanup incomplete' }
@{ taskId = $manifest.taskId; cleanedAbsolutePath = $target; date = '2026-11-12 Asia/Shanghai'; filesArchived = $manifest.files.Count; status = 'removed exact task directory after byte/link/ownership/reparse checks'; otherTasksAndBackups = 'preserved' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $archiveRoot 'evidence\cleanup.json') -Encoding utf8
Write-Output 'Removed only the fully archived task directory; other tasks and backups preserved.'
