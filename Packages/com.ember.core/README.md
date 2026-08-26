# Ember Core（com.ember.core）

Ember 框架核心：事件总线、服务定位、单例/对象池、GameState 状态机、Update 循环、Manager 自动发现、定时器。

## 依赖

| 包 | 版本/来源 |
|----|----------|
| com.ember.basic | https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.basic#v0.2.0 |
| com.cysharp.unitask | 2.5.10 |
| com.sirenix.odin-inspector | https://github.com/wsydet/ember-thirdparty-upm.git?path=/com.sirenix.odin-inspector#odin-v4.0.2 |

> 第三方依赖说明：com.cysharp.unitask / com.neuecc.unirx 需在项目 manifest.json 配置 OpenUPM scoped registry；com.sirenix.odin-inspector / com.demigiant.dotween 来自私有仓库，需 git 访问凭据（见框架文档 §3.4）。

## 安装

`json
{
  "dependencies": {
    "com.ember.core": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.core#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。