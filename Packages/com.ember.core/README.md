# Ember Core（com.ember.core）

Ember 框架核心：事件总线、服务定位、单例/对象池、GameState 状态机、Update 循环、Manager 自动发现、定时器。

## 依赖

**registry 依赖（随本包自动解析，无需手动安装）：**

| 包 | 说明 |
|----|------|
| com.cysharp.unitask 2.5.10 | 自动（OpenUPM/官方源） |

**前置包（UPM 规则：git 来源的包必须由项目 manifest 直接声明，见框架文档 §3.2）：**

| 包 | 说明 |
|----|------|
| com.ember.basic | 项目 manifest 直接声明（git URL） |
| com.sirenix.odin-inspector | 项目 manifest 直接声明（git URL） |

## 安装

`json
{
  "dependencies": {
    "com.ember.core": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.core#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。安装前置包与 OpenUPM 配置详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。