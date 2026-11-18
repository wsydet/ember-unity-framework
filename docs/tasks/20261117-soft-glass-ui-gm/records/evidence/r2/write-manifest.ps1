$ErrorActionPreference='Stop'
$taskRoot='.utmp/ember-workflow/20261117-soft-glass-ui-gm'
$projectRoot=(Get-Location).Path
$paths=@()
foreach($dir in @('Packages/com.ember/UI','Packages/com.ember/UIExtension','Packages/com.ember/UIRendering','Assets/Game/UI/Runtime/Module/UIFoundation','Assets/Game/UI/Editor/UIFoundation','Assets/GameResource/Resources/Config/UIFoundation','Assets/GameResource/Settings/UIFoundation')) {
  $paths+=Get-ChildItem -LiteralPath $dir -Recurse -File
}
$paths+=Get-ChildItem Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUISoftGlass*
$paths+=Get-ChildItem Assets/Ember/Editor/UILibrary/SoftGlassLibrary*
$paths+=Get-ChildItem Packages/com.ember/Tests/EditMode/EUIBackdrop*,Packages/com.ember/Tests/EditMode/EUIGlassGeometry*,Packages/com.ember/Tests/EditMode/EUIFullscreenBackdrop*
foreach($path in @('Assets/Game/UI/GamePages.User.cs','Assets/Game/State/GameInitState.cs','Assets/Settings/PC_RPAsset.asset','Assets/Settings/Mobile_RPAsset.asset','ProjectSettings/TagManager.asset','Assets/GameResource/Resources/UI/Common/Prefabs/GMPage.prefab','Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset','Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset','Packages/com.ember/Documentation~/ui/popup-backdrop.md','Packages/com.ember/Documentation~/ui/soft-glass.md','Assets/Game/Documentation/base/soft-glass-ui.md')) { $paths+=Get-Item -LiteralPath $path }
$files=@($paths|Where-Object{!$_.PSIsContainer}|Sort-Object -Property FullName -Unique|ForEach-Object{[PSCustomObject]@{path=$_.FullName.Substring($projectRoot.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
$editor=Get-Content -LiteralPath "$taskRoot/evidence/r2/editor-final.json" -Raw|ConvertFrom-Json
$pictures=@(Get-ChildItem -LiteralPath "$taskRoot/evidence/previews/r2" -Filter '*.png'|Sort-Object Name|ForEach-Object{$file=$_;$dimensions=$editor.images|Where-Object{$_.file-eq$file.Name};[PSCustomObject]@{file=$file.Name;width=$dimensions.width;height=$dimensions.height;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}})
$manifest=[PSCustomObject]@{task='20261117-soft-glass-ui-gm';revision='R2';date=(Get-Date).ToString('o');source='Actual Unity GameView through Unity MCP ScreenCapture; no image generation/compositing';requirements='R4';design='D1 with implementation deviations';runtimeMvid=$editor.runtimeMvid;editorMvid=$editor.editorMvid;environment='Unity6000.5.4f1 / URP17.5 / WindowsEditor D3D12 Linear HDR / RTX5060Ti';scope='Current shared UI code/assets and related configuration; includes preexisting dependent UI files to make baseline auditable, not an ownership claim';userConfirmation='Pending explicit user reply';template='base draft in active Assets; not saved/distributed';images=$pictures;productFiles=$files}
$manifest|ConvertTo-Json -Depth 7|Set-Content -LiteralPath "$taskRoot/evidence/previews/r2/manifest.json"
[PSCustomObject]@{images=$pictures.Count;productFiles=$files.Count;manifestSHA256=(Get-FileHash -LiteralPath "$taskRoot/evidence/previews/r2/manifest.json").Hash}|ConvertTo-Json

