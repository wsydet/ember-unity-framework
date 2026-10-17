# Ember 0.20.0 发布说明

发布版本：Ember 0.20.0 / tag v0.20.0；platformer-2d 0.5.0 已由项目中心正式封存。base 0.8.3、source3d-2p5d 0.5.0、visual-novel 0.20.1 内容版本不变，全部通过正式父级同步声明兼容框架 0.20.0。

## 内容

- platformer-2d：外部运动和局部交互锁、平移支撑、弹射、检查点与世界复位、关卡入口、区域、组合保存、塌陷平台。相机复用现有注册/恢复/卸载能力；T08未触发。
- 框架：修正Editor场景校验误报、发行中的仅Editor字段及Shader警告。
- UPM Manager：Odin4.0.3、Feel5.4.1、UniRx7.1.1安装/团队补丁入口。保护embedded维护副本，不自动覆盖直接导入的Feel。
- 三方私有仓库已发布三个新标签，全部指向69866096b109520f9c3ff1bb9c1ce1e901ab19ac。新清单须明确各包标签，不沿用旧统一标签推断补丁已包含。

## 验证

公共能力完整回归564/564（收尾保护前），收尾专项108/108及29/29；两关正式链路42条断言。最近Windows64 Mono非增量发行构建0错误、0警告、0 Assert，两个独立进程保存16/16、续玩19/19。UniRx关闭域重载的连续三轮Play通过；UPM检测回归9/9通过。详见[实施索引](mountain-rebuild/03-public-capability-progress.md)与[三方发布](mountain-rebuild/11-thirdparty-release.md)。

这些结果不代表最后一轮重新跑过全部564项回归。新项目创建/消费验证由用户自行执行；IL2CPP、其他平台、Windward专属玩法和完整原关卡未验收。

## 升级边界

三方包发布、框架包升级与模板迁移是不同步骤。已有消费项目通过UPM Manager升级；platformer-2d0.4.x→0.5.0跨minor，先备份并保护定制内容，再按项目中心流程迁移。新项目按新发布依赖清单安装，避免同名embedded或Feel Assets与UPM包重复导入。

## 发布前核对

2026-10-17 16:24:13 项目中心：0 错误、11 条测试状态未关联场景警告、0 差异；模板磁盘内容、封存 hash 与父级状态正常。重连后编译状态 scriptCompilationFailed=false。保留 Console 中此前 MCP 断连及临时执行代码诊断，不将其算作项目编译错误。

公共技能 bundle 未修改：32 文件原始 SHA256 全部符合 manifest，记录的源提交文本逐文件一致（仅换行差异）。完整生成器 Check 受工作区模板技能改动及 CRLF 影响未通过，未清理用户内容以强行通过。

候选提交共享二进制完整性检查通过（含 core.autocrlf=false/true 干净检出）；共享字体配置检查通过。字体动态工作区改动未纳入发布。
