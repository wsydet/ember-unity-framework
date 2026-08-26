# Ember UI（com.ember.ui）

Ember 框架 UI 管理：四层 Canvas 界面栈、EUIViewEngine 视图引擎、页面生命周期与过渡管道。

## 依赖

| 包 | 版本/来源 |
|----|----------|
| com.ember.basic | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.basic#v0.2.0 |
| com.ember.core | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.core#v0.2.0 |
| com.ember.resource | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.resource#v0.2.0 |
| com.ember.scene | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.scene#v0.2.0 |
| com.neuecc.unirx | 7.1.0 |
| com.cysharp.unitask | 2.5.10 |
| com.unity.ugui | 2.5.0 |
| com.demigiant.dotween | https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.demigiant.dotween#dotween-v1.2.815 |

> 第三方依赖说明：com.cysharp.unitask / com.neuecc.unirx 需在项目 manifest.json 配置 OpenUPM scoped registry；com.sirenix.odin-inspector / com.demigiant.dotween 来自私有仓库，需 git 访问凭据（见框架文档 §3.4）。

## 安装

`json
{
  "dependencies": {
    "com.ember.ui": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.ui#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。