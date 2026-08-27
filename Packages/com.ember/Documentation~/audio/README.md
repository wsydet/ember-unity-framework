# Audio — 音频管理

## 概述

BGM / SFX 播放与音量控制。通过 Unity AudioMixer 实现音量分组，
每个 SFX 使用独立的临时 AudioSource 支持同时播放多个音效。

## 文件清单

| 角色 | 路径 |
|------|------|
| 主逻辑入口 | `Runtime/EmberAudioManager.cs` |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberSingleton、IEmberManager、EmberEventBus、EmberDebug、GameLauncher |
| `UnityEngine.Audio` | 引擎 | AudioMixer、AudioSource、AudioClip |

## 公开 API

### EmberAudioManager — 音频管理器

继承 EmberSingleton，实现 IEmberManager。[EmberInitOrder(Audio)]。

| 方法 | 说明 |
|------|------|
| `Init(AudioMixer mixer, string bgmParam, string sfxParam)` | 完整初始化（含 Mixer 配置）。已通过 IEmberManager.Init 默认初始化后可再次调用配置 Mixer |
| `PlayBGM(AudioClip clip, bool loop, float fadeDuration)` | 播放 BGM。相同 clip 忽略 |
| `StopBGM()` | 停止 BGM |
| `SetBGMVolume(float volume)` | 设置 BGM 音量（0.0～1.0） |
| `PlaySFX(AudioClip clip, float volumeScale)` | 播放音效。创建临时 AudioSource，播完自动销毁 |
| `SetSFXVolume(float volume)` | 设置 SFX 音量（0.0～1.0） |

## 主流程

**初始化：** `IEmberManager.Init()` → 获取 GameLauncher.Instance.AudioHost → 创建 BGM/SFX AudioSource → 应用音量 → `EmberEventBus.OnNext(AudioReady)`

**PlaySFX：** 在 AudioHost 上 AddComponent<TempAudioSource> → Play → Destroy(this, clip.length + 0.1f)

**销毁：** `IEmberManager.Destroy()` → Dispatch(AudioShutdown) → StopBGM → Destroy AudioSource 组件 → 重置状态

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 依赖 GameLauncher | 需要 GameBoot 下存在 AudioHost 子节点 |
| SFX 多播 | 每个 SFX 创建独立临时 AudioSource，支持同时播放多个音效 |
| BGM 防重 | 相同 AudioClip 再次 PlayBGM 会被忽略 |
