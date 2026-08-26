# Ember Resource（com.ember.resource）

Ember 框架资源管理：IResourceProvider 接口 + EmberResourceManager 门面，可挂 Resources/Addressables/YooAsset 后端。

## 依赖

| 包 | 版本/来源 |
|----|----------|
| com.ember.core | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.core#v0.2.0 |

> 第三方依赖说明：com.cysharp.unitask / com.neuecc.unirx 需在项目 manifest.json 配置 OpenUPM scoped registry；com.sirenix.odin-inspector / com.demigiant.dotween 来自私有仓库，需 git 访问凭据（见框架文档 §3.4）。

## 安装

`json
{
  "dependencies": {
    "com.ember.resource": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.resource#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。