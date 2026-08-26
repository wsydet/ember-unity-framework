# Ember Editor（com.ember.editor）

Ember 框架编辑器工具：状态↔场景映射、快速场景打开、Toolbar 按钮等框架级工具。

## 依赖

| 包 | 版本/来源 |
|----|----------|
| com.ember.basic | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.basic#v0.2.0 |
| com.ember.core | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.core#v0.2.0 |

> 第三方依赖说明：com.cysharp.unitask / com.neuecc.unirx 需在项目 manifest.json 配置 OpenUPM scoped registry；com.sirenix.odin-inspector / com.demigiant.dotween 来自私有仓库，需 git 访问凭据（见框架文档 §3.4）。

## 安装

`json
{
  "dependencies": {
    "com.ember.editor": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.editor#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。