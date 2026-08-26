# Ember Audio（com.ember.audio）

Ember 框架音频管理：BGM/SFX 分离播放与 Mixer 音量控制。

## 依赖

**registry 依赖（随本包自动解析，无需手动安装）：**

| 包 | 说明 |
|----|------|
| （无） | — |

**前置包（UPM 规则：git 来源的包必须由项目 manifest 直接声明，见框架文档 §3.2）：**

| 包 | 说明 |
|----|------|
| com.ember.core | 项目 manifest 直接声明（git URL） |

## 安装

`json
{
  "dependencies": {
    "com.ember.audio": "https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember.audio#v0.2.0"
  }
}
`

本包与 Ember 框架其他包 lockstep 统一版本。安装前置包与 OpenUPM 配置详见 [docs/dev/upm-migration-plan.md](../../docs/dev/upm-migration-plan.md)。