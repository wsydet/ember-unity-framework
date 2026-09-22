param([switch]$Check)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$sourceRoot = Join-Path $repoRoot '.agents/skills'
$outputRoot = Join-Path $repoRoot 'Packages/com.ember/AISkills~'
$catalog = Get-Content -LiteralPath (Join-Path $sourceRoot 'catalog.json') -Raw | ConvertFrom-Json
if ($catalog.schemaVersion -ne 1) { throw 'Framework bundle requires the generic schema v1 catalog.' }
$sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '^[0-9a-f]{40,64}$') { throw 'Cannot resolve source commit.' }
$dirty = & git -C $repoRoot status --porcelain -- .agents/skills
if ($dirty) { throw 'Commit generic skill source changes before generating a traceable release bundle.' }

function Get-SafeFiles([string]$Directory) {
    $entry = Get-Item -LiteralPath $Directory -Force
    if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Linked source is unsupported: $Directory" }
    foreach ($item in Get-ChildItem -LiteralPath $Directory -Force) {
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Linked source is unsupported: $($item.FullName)" }
        if ($item.PSIsContainer) { Get-SafeFiles $item.FullName } else { $item.FullName }
    }
}
$paths = [Collections.Generic.List[string]]::new()
$paths.Add('catalog.json')
$ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($skill in $catalog.skills) {
    if ($skill.id -notmatch '^[a-z0-9][a-z0-9-]{0,63}$' -or !$ids.Add($skill.id) -or $skill.templateId) { throw 'Invalid or duplicate generic skill ID.' }
    $directory = Join-Path $sourceRoot $skill.id
    foreach ($file in Get-SafeFiles $directory) { $paths.Add($file.Substring($sourceRoot.Length + 1).Replace('\', '/')) }
}
$fingerprints = @($paths | Sort-Object -CaseSensitive | ForEach-Object {
    [ordered]@{ path = $_; sha256 = (Get-FileHash -LiteralPath (Join-Path $sourceRoot $_) -Algorithm SHA256).Hash.ToLowerInvariant() }
})
if ($Check) {
    $manifest = Get-Content -LiteralPath (Join-Path $outputRoot 'bundle.json') -Raw | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1 -or $manifest.generatedBy -ne 'Ember AI Skill Bundle') { throw 'Invalid bundle manifest.' }
    # The recorded commit is the source baseline, not necessarily the later packaging commit.
    $expected = $fingerprints | ConvertTo-Json -Depth 5 -Compress
    $actual = @($manifest.files | Sort-Object path -CaseSensitive) | ConvertTo-Json -Depth 5 -Compress
    if ($expected -ne $actual) { throw 'Bundle source differs. Regenerate before publishing.' }
    $bundleFiles = @(Get-SafeFiles (Join-Path $outputRoot 'skills'))
    if ($bundleFiles.Count -ne $fingerprints.Count) { throw 'Bundle contains missing or extra files.' }
    foreach ($file in $fingerprints) {
        $hash = (Get-FileHash -LiteralPath (Join-Path $outputRoot ('skills/' + $file.path)) -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($hash -ne $file.sha256) { throw "Bundle content changed: $($file.path)" }
    }
    Write-Output "Verified $($catalog.skills.Count) bundled framework skills, $($fingerprints.Count) files."
    exit 0
}
$stage = Join-Path $repoRoot ('.utmp/ai-skill-bundle-' + [Guid]::NewGuid().ToString('N'))
foreach ($file in $fingerprints) {
    $target = Join-Path $stage ('skills/' + $file.path)
    [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
    [IO.File]::Copy((Join-Path $sourceRoot $file.path), $target)
}
$manifest = [ordered]@{ schemaVersion = 1; generatedBy = 'Ember AI Skill Bundle'; sourceCommit = $sourceCommit; files = $fingerprints }
[IO.File]::WriteAllText((Join-Path $stage 'bundle.json'), ($manifest | ConvertTo-Json -Depth 6) + "`n", [Text.UTF8Encoding]::new($false))
if (Test-Path -LiteralPath $outputRoot) {
    $existing = Get-Content -LiteralPath (Join-Path $outputRoot 'bundle.json') -Raw | ConvertFrom-Json
    if ($existing.generatedBy -ne 'Ember AI Skill Bundle') { throw 'Refusing to replace an unmanaged output directory.' }
    # Keep previous generated output in the repository temporary area; never recursively delete source.
    $resolvedOutput = [IO.Path]::GetFullPath($outputRoot)
    if ($resolvedOutput -ne [IO.Path]::GetFullPath((Join-Path $repoRoot 'Packages/com.ember/AISkills~'))) { throw 'Output escaped package directory.' }
    Move-Item -LiteralPath $resolvedOutput -Destination ($stage + '-previous')
}
Move-Item -LiteralPath $stage -Destination $outputRoot
Write-Output "Generated $($catalog.skills.Count) framework skills from $sourceCommit into $outputRoot"
