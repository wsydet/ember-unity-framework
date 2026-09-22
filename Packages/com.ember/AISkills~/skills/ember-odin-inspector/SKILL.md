---
name: ember-odin-inspector
description: >-
  检查指定脚本或目录的 Odin Inspector 布局、序列化和编辑器依赖边界，并按授权修正。用于 Odin 面板检查、优化面板或 /odin-inspector；仅提到 Odin 不触发全项目扫描。
---

# Ember Odin 面板检查

读取用户指定脚本；未指定目标时先问目标，不自动全项目扫描。通过项目 manifest/lock、实际 package.json 或程序集确认 Odin 与 Unity 版本；缺少 Odin 时报告条件不足，不自动安装。已有明确优化授权则执行已确定范围，只要求检查则给报告。

读取 [检查依据](references/inspector-review.md) 和当前项目已有的面板约定（若有）。可只读参考实际 Ember 包的 EUIBinding，但不存在该文件不阻止分析。项目的历史观察不等于跨版本缺陷。

## 检查与修正

- 收集 Sirenix 特性、字段/属性、分组路径、按钮、Drawer 与可见性条件。对未使用 Odin 的脚本说明不适用。
- 核对序列化与显示职责：public 字段可能是业务 API；ShowInInspector 属性可能是计算值。不得为了面板布局盲目改为 private SerializeField，以免破坏调用方、序列化或运行时行为。
- 检查动态分组名/条件表达式是否有效、按钮分组是否意外合并、绘制过程是否修改集合，以及异步状态变化是否改变同次 Layout/Repaint 控件结构。
- 检查 UnityEditor/源码扫描等编辑器逻辑的条件编译和程序集边界，运行时字段不因面板修改丢失。
- 每个发现附文件位置、可复现现象或源码依据、影响及最小修正。历史可疑组合仅列为待验证，不批量替换。

只改授权范围内的项目自有代码。消费端框架问题记录并回到框架仓库修复，不写 PackageCache。按项目规则验证 Unity 编译和实际面板；纯源码判断不能代表渲染验证。
