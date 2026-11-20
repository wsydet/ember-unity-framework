# Ember 0.27.0 发布说明

2026-11-20。本版功能、模板封存与验证记录。发布身份以origin/main与annotated v0.27.0的peeled commit核对为准，本页文件本身不构成远端发布证明。

## 本版行为

GM控制中心采用紧凑固定区与独立操作滚动区，消费项目通过Provider注册稳定操作；最多五项收藏使用同一执行/危险确认入口及轻弧快捷HUD。入口保留吸边和位置偏好，短按、长按、拖动互斥；HUD完整命中圆避让、分档缩小与连续稳定后恢复。

设置与GM共享三模式桌面帧率策略，自动目标按实际刷新率加档位余量，自定义正整数精确生效；模式、类型和值经现有偏好模块持久化。关闭全部控件后继续检查，新运行含禁域重载Play重新加载应用，外部主动接管受保护。FPS启动窗口使用真实采样数。

玻璃安装独立于GM，Loading、设置与公共弹窗有对应接线。两项维护修复为GMCenter四个region中文标题，以及Apply/Inspect统一输出中性目录的独立回执。回执目录`.utmp/ember-ui/gm-control-center/`，UTC时间+GUID+操作名，API和制作窗口均可查最近路径，不覆盖任务归档。

## 正式封存模板

| 模板 | 内容版本 | 兼容框架 | 父base版本 | 封存hash |
| --- | --- | --- | --- | --- |
| base | 0.12.0 | 0.27.0 | 无 | `cad30f499213dc7c946e32ae45dfffc7` |
| lane-defense-2d | 0.1.6 | 0.27.0 | 0.12.0 | `1d718d86736e081a29fc142ab62f194c` |
| platformer-2d | 0.13.7 | 0.27.0 | 0.12.0 | `361466b7ac703e8f392a7f3d7ebc81eb` |
| source3d-2p5d | 0.6.6 | 0.27.0 | 0.12.0 | `e35e08a49f46c5e376b5f921c6ea2886` |
| topdown-2d | 0.1.7 | 0.27.0 | 0.12.0 | `7c33795be43bed897056e1daaa07a3d4` |
| ui-game | 0.2.6 | 0.27.0 | 0.12.0 | `5030a03ea18cd59fced92d37da85d5c8` |
| visual-novel | 0.22.7 | 0.27.0 | 0.12.0 | `69bb5bc67310964b04b996fb950f3103` |

base0.12.0与lane-defense-2d0.1.6沿用R1封存，没有重复Bump。R2用fresh计划完成其它五派生父同步。platformer/topdown的文件冲突先KeepChild保留自有启动，正式同步改变内容时按API要求封存patch；随后正式Load到Assets各补一行InitializeUI，经编译及已加载IL确认后Save/Bump第二patch，故最终为0.13.7/0.1.7。没有手写Templates~/ParentSnapshot~/metadata。子设置页、小说皮肤/字体GUID和独有业务保持覆盖，不统一替换为base的新设置布局。

## 验证范围与限制

原GM功能由[原任务独立验收](../tasks/20261119-gm-control-center/acceptance.md)承接，本轮未重跑原24AC/123项或全量测试。本轮维护编译/Inspect，正式Save/Bump、七模板实际资产/meta/GUID/hash/父快照/指针校验，及候选共享二进制/字体检查分别留证。platformer/topdown均只补一行启动调用，编译后的实际IL包含InitializeUI，保留玩家白盒/配置、试玩存储及过场处理。

R1目录Move阻塞经用户重启解除，新Unity进程原目录句柄为空，R2正式同步成功。R2域重载MCP中断按30秒有界重连，用户协助恢复，未重复结果不明的编译。平台切换期间一次旧时间戳导入错误与既有Consumer测试夹具UAC1001警告已保留；精确源脚本重导入及后续编译/IL确认分开记录，不宣称全程零日志。返回base后的最终编译与域重载已确认，Console为空；七模板逐项实际内容、封存与父快照/当前父指针校验均通过，候选共享二进制与字体检查通过。具体证据和中间日志见本轮实施回交。

尚未运行六模板Play、Player性能/VSync、移动/XR、物理触摸、多指、真实硬件切刷新率及消费端升级。不得把编译/静态封存通过描述为这些平台已验收。

## 依赖与技能

第三方依赖沿用0.26.0（此前沿用0.25.5），完整manifest除com.ember tag外不变。公共技能源与bundle未改，sourceCommit保持`fcc06dcb65404036eb2c6908fcd68379f3306239`。直接bundle -Check的换行限制保留：34个bundle原字节匹配manifest，源提交、HEAD、工作区与bundle仅文本换行等价；不伪造sourceCommit或重建无变更bundle。详见任务R1/R2原始检查证据，不能写成直接命令无条件通过。

## 升级与发布

已有消费项目先通过Ember/UPM Manager升级框架，再按项目中心正式模板升级/三方预览保护定制；包升级不会自动部署业务Assets。base0.11→0.12属于minor结构增量，按完整保护和模板迁移契约处理，派生patch也不授权覆盖自定义。消费升级未在本任务执行。

发布声明由release_metadata.py从真实封存内容预览后生成；本地候选已通过[RV1独立验收](../tasks/20261120-gm-release/acceptance-rv1.md)，CHECK-N01直接bundle检查限制保留。交付采用选择性commit、不可变annotated v0.27.0，仅将HEAD到origin/main和该tag作atomic非force推送并核对；实际远端结果按[发布核对契约](../tasks/20261120-gm-release/handoff.md)记录，本页不提前声明网络成功。旧0.26发布记录与第三方资源保持。
