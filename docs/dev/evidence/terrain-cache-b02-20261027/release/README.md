# Ember 0.24.2 发布门禁

2026-10-28，用户明确授权发布0.24.2。基线 `c83ecb13a4e16f1f070c2937a3bdb1cc157d6a05`；目标origin/main与annotated tag `v0.24.2`。只交付B02缓存恢复、定向测试、验收与必要发布资料。B02保持未关闭，不升级或部署Windward。

## 范围与工作区保护

发布前原暂存区为空。预先记录1,310个已有改动/未跟踪路径；排除本次B02范围后，1,244个无关文件/删除状态保持原样。逐路径暂存，不纳入UI、FSM、共享字体、项目Assets/ProjectSettings、技能源或模板修改。

候选静态门禁使用本次新建的隔离受管工作树，内容来自主工作区已审阅的index，首轮tree `c7c2f5fea4b4b47a261f919673a5529e40e13747`；之后只修正发布README中的旧版历史标注并补门禁回执。代码/测试与8/8及两轮真实重载的最终源码相同，发布准备时Editor MVID保持 `d7b390c0-effe-4916-9994-b480944fc9ba`，未以纯升版为由全量重测。

## Unity与模板

- [主动编译请求](compile-request.json)、[本轮完成状态](compile-state.json)、[Console](console.json)：本轮编译及域重载完成，编辑器就绪，error/warning为0。
- [已加载包](loaded-package.json)：实际0.24.2、框架embedded路径、failed=false；最终Editor MVID如上，Tests MVID `f8dfad5b-65b4-47e5-86e0-0b31bab1fb40`。
- [模板校验](templates.json)：五个模板实际hash=metadata=封存hash，四个派生模板真实ParentSnapshot匹配base0.10.0，ValidateTemplateGraph为空。platformer-2d仍0.13.0；模板、父快照及0.24.0兼容声明不变，不Save/Bump/部署。
- 本轮8/8和真实两次重载的详细范围与先读结果回执见[上级证据索引](../README.md)。未验收路径见下文，不沿用其他版本的构建结果。

## 候选静态门禁

| 门禁 | 结果 |
| --- | --- |
| `python scripts/check-shared-binaries.py --revision : --checkout` | 4份共享字体的原始Git blob/字节通过，core.autocrlf=false/true的干净检出通过 |
| `python scripts/check-shared-font-config.py --revision :` | 工作树和候选index的Dynamic/Multi Atlas、源字体/GUID、fallback、材质、许可证通过 |
| `pwsh -NoProfile -File scripts/build-ai-skill-bundle.ps1 -Check` | 原版检查通过：11个通用技能、33文件，未重建bundle |
| metadata生成器 | 基线0.24.1→0.24.2；依赖、模板内容和技能sourceCommit沿用原声明，只更新框架身份、覆盖与验证说明 |
| `git diff --cached --check` | 通过 |
| 无关工作区保留 | 1,244个原有无关路径的字节/删除状态保持不变 |

技能门禁最初遇到源码LF与既有bundle CRLF的检出差异。逐文件证明bundle原始Git blob符合manifest指纹，源码与bundle仅换行不同；只在隔离候选按bundle既有字节匹配源码检出。使用独立候选index与仅该进程的core.autocrlf=true刷新状态，前后tree完全相同，再运行原版检查通过。没有更改主工作区配置、技能源、bundle、sourceCommit或门禁脚本，也没有删除现有候选锁来强行继续。

## 发布范围限制

**B02暂不关闭。** Windward原实例、保存重开场景、正常重开Unity、正式生命周期试玩/停止、Windows构建、Player首次加载、实际SpriteAtlas、大图耗时仍未验。Game相机截图为Editor捕获，不能替代正式Play或Player验收。

本次不修改Windward的包、模板或PackageCache，不重铺旧地图；未来消费端接入必须另行授权并使用正式UPM Manager。
