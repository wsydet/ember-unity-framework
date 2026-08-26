# Ember Camera（com.ember.camera）

Ember 框架相机管理：Cinemachine 虚拟相机注册/切换与强制霸占堆栈。

## 依赖

**registry 依赖（随本包自动解析，无需手动安装）：**

| 包 | 说明 |
|----|------|
| com.unity.cinemachine 3.1.7 | 自动（OpenUPM/官方源） |

**前置包（UPM 规则：git 来源的包必须由项目 manifest 直接声明，见框架文档 §3.2）：**

| 包 | 说明 |
|----|------|
| com.ember.core | 项目 manifest 直接声明（git URL） |

## 安装

`json
{
  "dependencies": {
    "com.ember.camera": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.camera#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。安装前置包与 OpenUPM 配置详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。