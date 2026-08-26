# Ember UI（com.ember.ui）

Ember 框架 UI 管理：四层 Canvas 界面栈、EUIViewEngine 视图引擎、页面生命周期与过渡管道。

## 依赖

**registry 依赖（随本包自动解析，无需手动安装）：**

| 包 | 说明 |
|----|------|
| com.neuecc.unirx 7.1.0 | 自动（OpenUPM/官方源） |
| com.cysharp.unitask 2.5.10 | 自动（OpenUPM/官方源） |
| com.unity.ugui 2.5.0 | 自动（OpenUPM/官方源） |

**前置包（UPM 规则：git 来源的包必须由项目 manifest 直接声明，见框架文档 §3.2）：**

| 包 | 说明 |
|----|------|
| com.ember.basic | 项目 manifest 直接声明（git URL） |
| com.ember.core | 项目 manifest 直接声明（git URL） |
| com.ember.resource | 项目 manifest 直接声明（git URL） |
| com.ember.scene | 项目 manifest 直接声明（git URL） |
| com.demigiant.dotween | 项目 manifest 直接声明（git URL） |

## 安装

`json
{
  "dependencies": {
    "com.ember.ui": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.ui#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。安装前置包与 OpenUPM 配置详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。