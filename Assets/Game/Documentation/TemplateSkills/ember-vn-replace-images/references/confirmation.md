# 确认服务与计划格式

依赖 Python 3.10+、Pillow。Codex 可用 `load_workspace_dependencies` 找到 Python；先检查 Pillow 可导入。使用 UTF-8 JSON，所有路径相对实际项目根目录（外部新图可以绝对路径）。旧图必须在 `Assets/GameResource` 内，所有路径拒绝链接和越界。

代理查看图片、检索引用后，将分析结果保存到 `.utmp/vn-image-replace/analysis.json`，例如：

```json
{
  "items": [{
    "source": "Assets/Incoming/hero_new.png",
    "candidates": [{
      "target": "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/Story/Portraits/hero_smile.png",
      "reason": "角色与服装一致，嘴角与眉眼对应微笑差分；已查看两图",
      "warnings": ["同组还存在 neutral、angry，未纳入替换"],
      "references": ["novel_portraits: hero_smile；Chapter1 节点 12"],
      "blocked": ""
    }]
  }]
}
```

每个新图可列多个候选，理由、警告和引用必须来自实际检查。`blocked` 非空表示不能在本批替换，例如 Sprite Multiple、不支持的导入类型、引用范围尚未核对。没有候选仍应保留该新图，页面显示未找到匹配，不凭空创建旧资源。所有非同内容替换还需确认会影响旧图全部引用。

```powershell
& '<python>' -B '<skill>/scripts/confirmation.py' serve --project '<项目>' --analysis '<分析 JSON>' --work '<项目>/.utmp/vn-image-replace/<新批次>' --mcp-verified
```

只有实时 MCP 项目和技能身份检查成功且引用/导入设置核对完毕才追加 `--mcp-verified`；这是代理前置检查声明，不是自动连接 Unity。未传时仅可保存草稿和预览。服务不会执行资源替换。

用返回会话 ID 保持服务；需要后台进程时 `Start-Process -WindowStyle Hidden` 并记录 PID、重定向日志到批次。读取 stdout 或 `session.json` 的 URL，通过 `open_in_codex` 或浏览器工具实际打开并观察标题“图片替换确认”、缩略图和控件，再提供链接。只监听回环随机端口。真实批次不得由代理提交；自动点击仅限隔离测试图片。

`batch.json` 保存分析、项目 Assets 全文件指纹和外部新图指纹；完整 Assets 指纹用于捕捉 `.meta`、表、Prefab 和代码引用变化，不扫描 Library 或 Packages。`draft.json` 只存草稿。`decision.json` 保存 confirmed/cancelled、完整计划及 reviewHash。指纹检查不是对有本地写权限程序的密码学防护，代理不得编辑回执。

```powershell
& '<python>' -B '<skill>/scripts/confirmation.py' verify --work '<批次>'
```

仅成功且 confirmed 才可读计划执行。首次业务写入前校验全部指纹；本批写入后不再用原基线反复校验，按 SKILL.md 记录输出指纹与恢复信息。服务退出后如需恢复交互，使用新批次重新核对，不能复制旧确认。执行完成后停止本批服务，保留回执和备份。
