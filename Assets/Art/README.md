# Art

原始艺术资源，不属于框架代码或业务逻辑。

| 目录 | 内容 |
|------|------|
| `Icons/` | 通用图标（PNG + SVG），编辑器工具和游戏 UI 共用 |

## 图标资源

- **[game-icon-pack v1.4](Icons/game-icon-pack-v1.4/)** — 800+ 圆角风格图标，CC0 许可证。来源：https://github.com/Nieobie/game-icon-pack
  - PNG（带/无间距）+ SVG 双格式
  - 适用于：蓝图节点图标、编辑器工具栏、彩虹文件夹、彩虹视图等

## 入包策略（2026-08-26 定稿）

- **全量图库不进框架包**：23k 文件会让消费端每次 resolve/升级都全量导入（UPM git 包安装卡顿）；且挪进 `Packages/` 后会重新落入 `.gitattributes` 的 `*.png → lfs` 全局规则（本目录靠路径例外豁免 LFS，见根目录 .gitattributes）
- **按需精选子集**：模板/编辑器实际用到的图标，挑少量移入 `Packages/com.ember/SharedAssets/Icons/`（该目录已有 LFS 豁免），模板按 GUID 引用、多模板共享
- 本目录是 dev 专属素材源，供后续挑选；不随 sync-scaffold 进模板
