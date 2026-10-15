# 统一确认服务

Python 3.10+、Pillow；可通过 `load_workspace_dependencies` 找到解释器。服务从同级两项技能加载实际 `Batch.plan`，缺失时停止，不复制维护其规则。只写项目 `.utmp`，不写业务资源。

先按替换技能格式准备 analysis JSON，`items` 必须包含本批每张源图（无候选用空 candidates），顺序由服务按完整源路径规范化。每项可加 `suggestion`：例如 `{"route":"import","kind":"portrait","story":"Chapter1","characterId":"hero","displayName":"主角","variant":"smile"}` 或 `{"route":"replace","candidate":0}`。建议仍须用户核对，警告确认不能预填。无法判断用 `{"route":"skip"}`。目录内全部支持图片必须与 analysis 一一对应，服务拒绝遗漏、重复或范围外来源。

```powershell
& '<python>' -B '<skill>/scripts/confirmation.py' serve --project '<项目>' --source '<指定路径>' --analysis '<analysis.json>' --work '<项目>/.utmp/vn-image-sync/<新批次>' --mcp-verified
```

仅在实时 MCP 三项技能身份、表映射和替换引用/导入设置检查通过后使用 `--mcp-verified`。实际表路径有变时用 `--tables <JSON>`，格式沿用新增技能。服务仅监听回环随机端口。用可返回 session ID 的进程工具保持服务；后台 `Start-Process` 使用 Hidden 并保存日志和 PID。读取 stdout/session.json 的 URL，实际打开并检查标题“视觉小说图片统一导入”、新旧图、字段与按钮，再提供链接。用户提交后返回聊天继续。

```powershell
& '<python>' -B '<skill>/scripts/confirmation.py' verify --work '<本批目录>'
```

只接受退出码 0 且 `status=confirmed`，读取完整 plan。verify 重建两项下层计划并核对源范围、Assets、分析、表定义及依赖脚本指纹。草稿、关闭、取消不授权。服务重启用新批次，不复制旧回执。完成后停止服务，保留备份及逐图执行记录。回执属于本机流程约定，不能防止有本机文件写权限的程序篡改。
