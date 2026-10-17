# 三方包发布与干净消费验证

2026-10-17。用户已单独授权三方包发布、依赖接线和干净消费验证；不据此发布整个Ember框架或封存模板。保留前轮公共能力修改。

## 本轮维护源与目标

私有仓库 `C:/Users/wuyu/My/ember-thirdparty-upm`，origin `https://github.com/wsydet/ember-thirdparty-upm.git`。Git认证成功；通过Git凭据调用GitHub只读API确认private=true、default_branch=main。开始时HEAD及远端main均为 `e4b91ccb314d436d2657c0267f499c22e7f05186`。

| 包 | 团队目标版本/tag | 内容 |
|---|---|---|
| Odin | 4.0.3 / odin-v4.0.3 | 供应商4.0.2的PluginImporter路径补丁，原回调保留 |
| Feel | 5.4.1 / feel-v5.4.1 | 供应商5.4的可选PostProcessing默认资产改为Editor资源 |
| UniRx | 7.1.1 / unirx-v7.1.1 | embedded7.1.0的Unity6.5查找与关闭域重载生命周期补丁；新增到三方仓库，保留原GUID和MIT许可 |

以上是团队补丁版本，不是供应商升级。旧标签不覆盖，不向OpenUPM上传。Feel Assets与镜像4902文件SHA256逐一一致。

## 接线及验证进度

框架UPM Manager更新Odin/Feel安装目标，新增UniRx必需依赖显示和三项团队补丁入口；embedded维护副本拒绝直接覆盖。既有可选依赖检测回归9/9通过，job `248e20b4baf14d98bf10fc3d024b0e82`，0失败/跳过。

独立补充依赖声明为 `Packages/com.ember/Dependencies~/thirdparty-2026-10-17.json`，不修改历史release/manifest。该声明仅包含三个目标包，消费项目应保留其他依赖。

干净消费项目：`.utmp/thirdparty-release-20261017/CleanConsumer`，从空Assets、manifest和ProjectVersion开始，没有复制原项目Library、lock、Assets或ProjectSettings。候选阶段三个插件通过本地UPM源接入；Ember采用当前本地源码，MCP暂用已有工具包路径。这不是远端Ember新版本发布验收。

## 已发布结果与用户接手验证

用户已明确取消代理创建新项目验证，随后授权框架仓库与三方仓库都完成修正并发布。新项目创建/消费验证由用户执行；先前隔离工程反复导入未验收，不继续操作。

三方仓库已提交并原子推送main及三个annotated tag：`odin-v4.0.3`、`feel-v5.4.1`、`unirx-v7.1.1`。远端main和三个peeled tag均为 `69866096b109520f9c3ff1bb9c1ce1e901ab19ac`，工作区干净，旧标签不变。

UniRx首次镜像中保留供应商源码和Unity meta的原有尾空格，未为格式检查批量重写第三方文件；本轮发布文档/版本声明格式检查通过。Odin修正DLL的SHA256仍与此前审计结果一致。

MCP重连后完成项目中心封存：platformer-2d 0.5.0；全部模板兼容声明同步至框架0.20.0，父级同步无冲突且不改子模板内容。正式扫描0错误、11条测试状态场景警告、0差异。框架发布资料与完整依赖接线见 [0.20.0发布说明](../release-0.20.0.md)。
