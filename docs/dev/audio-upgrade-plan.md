# Audio 模块升级方案

> 状态：📋 待实施
> 创建：2026-08-05
> 参考：[burner Audio 模块](../../c:/Users/wuyu/Project/burner/client/game/Assets/Game/GameCore/Runtime/Common/Audio/) + [burner AudioMgr](../../c:/Users/wuyu/Project/burner/client/game/Assets/Game/GameLogic/GameManagers/Audio/AudioMgr.cs)

---

## 一、背景

当前 [EmberAudioManager](../../Assets/Ember/Audio/Runtime/EmberAudioManager.cs) 已具备基本的 BGM/SFX 播放能力，但存在以下局限：

| 问题 | 影响 |
|------|------|
| BGM/SFX 硬编码为两个 AudioSource | 无法扩展第三分类（Voice），无法按游戏需求自定义分组 |
| SFX 每次 `AddComponent<TempAudioSource>()` | 运行时 GC 分配，高频音效场景（战斗、UI）性能差 |
| 无 Agent 池化 | 无法控制并发播放数量上限，无法预分配 |
| 无按 ID 停止 | 无法精准控制某个播放实例（如"停止那个脚步声"） |
| `fadeDuration` 参数存在但未实现 | API 欺骗性——参数接收了但不生效 |
| 无分类静音/音量控制 | 只能控制全局 BGM/SFX 音量，不能按 Sound/Music/Voice 独立开关 |

burner 项目已通过 `AudioType` → `AudioGroupConfig` → `AudioCategory` → `AudioAgent` 四层架构验证了完整的音频播放管道，本方案将其移植到 Ember 框架。

---

## 二、目标架构

```
EmberAudioManager (Singleton, 唯一对外入口)
    │
    ├── AudioCategory[Music]     ← AudioGroupConfig 驱动
    │   ├── AudioAgent (池中空闲)
    │   └── AudioAgent (活跃播放)
    │
    ├── AudioCategory[Sound]
    │   ├── AudioAgent × 3 (活跃)  ← 并发播放多个 SFX
    │   └── AudioAgent × 7 (池中)
    │
    └── AudioCategory[Voice]     ← 可选，默认不创建
        └── AudioAgent × 1
```

### 数据流

```
PlaySFX(clip)
  → AudioCategory[Sound].Play(clip)
    → _agentPool.Pop() 取出空闲 Agent（池空则创建，不超 AgentHelperCount）
    → Agent.Play(clip) → AudioSource.Play()
    → 加入 _activeAgents
    → 返回 Agent 给调用方（可用于后续 Stop(agentId)）

Update() 每帧:
  → 遍历 _activeAgents → Agent.Tick(deltaTime)
    → 检测 fade 状态
    → 检测生命周期（clip.length 到期 / CustomLifeTime 到期）
    → 到期自动 ReturnAgent() → 归还池
```

---

## 三、新增文件

```
Assets/Ember/Audio/Runtime/
├── AudioType.cs              ← 新增
├── AudioGroupConfig.cs       ← 新增
├── AudioAgent.cs             ← 新增
├── AudioCategory.cs          ← 新增
└── EmberAudioManager.cs      ← 重构（保持 API 兼容）
```

### 3.1 `AudioType.cs` — 音频分类枚举

```csharp
namespace Ember.Audio
{
    /// <summary>
    /// 音频分类，可分别关闭/开启对应分类音效。
    /// 命名与 AudioMixer 中分类名保持一致。
    /// </summary>
    public enum AudioType
    {
        /// <summary>音效（UI 点击、战斗打击等短促声音）</summary>
        Sound,

        /// <summary>背景音乐</summary>
        Music,

        /// <summary>语音/对话</summary>
        Voice,

        /// <summary>哨兵值，用于数组分配</summary>
        Max
    }
}
```

**设计要点**：
- `Max` 作为数组长度标记，与 burner 一致
- 与 AudioMixer 的 Group 名称语义对应

### 3.2 `AudioGroupConfig.cs` — 音频组配置

```csharp
namespace Ember.Audio
{
    /// <summary>
    /// 音频轨道组配置。
    /// 当前为纯数据类，后续可改为 ScriptableObject 实现可视化编辑。
    /// </summary>
    public class AudioGroupConfig
    {
        /// <summary>组名，对应 AudioMixer 中的 Group 名</summary>
        public string Name;

        /// <summary>初始静音</summary>
        public bool Mute = false;

        /// <summary>初始音量 (0~1)</summary>
        public float Volume = 1f;

        /// <summary>预分配的 AudioAgent 数量（控制并发上限）</summary>
        public int AgentHelperCount = 1;

        /// <summary>所属分类</summary>
        public AudioType AudioType;
    }
}
```

**默认配置**（初始化时自动生成）：
- Music：`AgentHelperCount = 1`（同一时间只播一首 BGM）
- Sound：`AgentHelperCount = 10`（支持 UI + 战斗等多音效并发）
- Voice：不默认创建，业务层按需添加

**ScriptableObject 演进方向**：字段改为 `[SerializeField] private` + 属性 getter，继承 `EmberBaseSO`，Editor 中可拖拽配置。

### 3.3 `AudioAgent.cs` — 可池化的音频播放代理

**核心设计**：预创建 GameObject + AudioSource 组件，通过 SetActive 控制激活/休眠，零 GC。

```
AudioAgent : MonoBehaviour
├── 内部参数
│   ├── _audioSource       AudioSource 组件引用
│   ├── _instanceId        int 唯一实例 ID（自增）
│   ├── _category          AudioCategory 归属（用于自动归还）
│   ├── _fadeTarget        float fade 目标音量
│   ├── _fadeDuration      float fade 时长
│   ├── _fadeTimer         float fade 计时器
│   ├── _isFading          bool 是否正在 fade
│   ├── _elapsed           float 已播放时间
│   └── _customLifeTime    float 自定义生命周期（-1 = 使用 clip.length）
│
├── 公开属性
│   ├── InstanceId         int 实例 ID
│   ├── Clip               AudioClip 当前播放的 clip
│   ├── IsPlaying          bool 是否正在播放
│   ├── IsLoop             bool 是否循环
│   ├── Volume             float 当前音量
│   └── CustomLifeTime     float 自定义生命周期
│
├── 外部方法
│   ├── Play(clip, loop, volume)     开始播放
│   ├── Stop(fadeOut)                停止（可选渐消）
│   ├── Pause() / Resume()           暂停/恢复
│   ├── SetVolume(volume)            直接设置音量
│   └── FadeTo(target, duration)     渐变到目标音量
│
└── 内部方法
    ├── Update()                     驱动 fade + 生命周期检测
    └── OnDisable()                  自动归还到 Category 池
```

**关键行为**：

| 场景 | 行为 |
|------|------|
| `Play(clip, loop: false)` | 播放，`clip.length` 后自动 Stop 并归还池 |
| `Play(clip, loop: true)` | 循环播放，直到外部调用 `Stop()` |
| `Stop(fadeOut: true)` | 启动 fade out，fade 完成后归还池 |
| `Stop(fadeOut: false)` | 立即停止并归还池 |
| `CustomLifeTime > 0` | 覆盖默认的 `clip.length`，到期自动 Stop |
| GameObject Disable | 自动归还到所属 Category 的池 |

**池化机制**：
- Agent GameObject 预创建在 `AudioHost` 下，初始 `SetActive(false)`
- 取出：`SetActive(true)` + 配置参数 + Play
- 归还：Stop + `SetActive(false)` + 压入 `_agentPool` 栈
- 不 Instantiate / Destroy → 零 GC 分配

### 3.4 `AudioCategory.cs` — 分类管理器

纯 C# 类（非 MonoBehaviour），管理一个 AudioType 对应的 Agent 池。

```
AudioCategory
├── 内部参数
│   ├── _config            AudioGroupConfig 配置
│   ├── _agentPool         Stack<AudioAgent> 空闲 Agent
│   ├── _activeAgents      List<AudioAgent> 活跃 Agent
│   ├── _agentParent       Transform Agent 父节点
│   ├── _enable            bool 分类开关
│   ├── _volume            float 分类音量
│   └── _nextInstanceId    int 实例 ID 生成器
│
├── 外部方法
│   ├── Play(clip, loop, volume) → AudioAgent   取出 Agent 播放
│   ├── Stop(fadeOut)                            停止本类所有 Agent
│   ├── StopAgent(instanceId)                    按 ID 停止
│   ├── ReturnAgent(agent)                       归还 Agent 到池
│   ├── Update(deltaTime)                        驱动所有活跃 Agent
│   └── Cleanup()                                清理所有 Agent
│
└── 公开属性
    ├── Enable             bool 分类开关
    ├── Volume             float 分类音量
    ├── Mute               bool 静音 (= !Enable)
    ├── AudioAgents        IReadOnlyList<AudioAgent> 活跃列表（只读）
    ├── Type               AudioType 分类类型
    └── ActiveCount        int 当前活跃数
```

**Agent 生命周期**：

```
Play(clip)
  │
  ├─ Enable == false? → return null
  ├─ _agentPool.Count > 0? → Pop
  │   └─ else 未超过 AgentHelperCount → 创建新 Agent GameObject
  │       └─ else 池耗尽 → return null（或复用最老的 Agent，待定）
  ├─ Agent.Play(clip, volume)
  ├─ 加入 _activeAgents
  └─ return Agent

Update(deltaTime):
  遍历 _activeAgents：
    ├─ Agent.Tick(deltaTime) → 驱动 fade + 生命周期
    └─ Agent.IsFinished? → ReturnAgent(agent)

ReturnAgent(agent):
  ├─ 从 _activeAgents 移除
  ├─ Agent.Stop() → SetActive(false)
  └─ 压入 _agentPool
```

### 3.5 `EmberAudioManager.cs` — 重构

**对外 API 兼容性保证**：现有 6 个公开方法签名**不变**，内部实现切换到 Category 驱动。

**变更清单**：

| 现有 API | 变更 |
|----------|------|
| `Init(mixer, bgmParam, sfxParam)` | 不变。内部改为创建 Category 数组 + 默认 Music/Sound 配置 |
| `PlayBGM(clip, loop, fadeDuration)` | 不变。内部委托 `_categories[Music].Play()` |
| `StopBGM()` | 不变。内部委托 `_categories[Music].Stop()` |
| `SetBGMVolume(volume)` | 不变。内部更新 `_categories[Music].Volume` |
| `PlaySFX(clip, volumeScale)` | 返回类型 `void` → `AudioAgent`（兼容：调用方不接收返回值即可） |
| `SetSFXVolume(volume)` | 不变 |

**新增 API**：

| 方法 | 说明 |
|------|------|
| `Play(AudioType type, AudioClip clip, ...)` | 按分类播放 |
| `PlayVoice(AudioClip clip, ...)` | 便捷方法：播放语音 |
| `Stop(AudioType type, bool fadeOut)` | 停止某分类全部音频 |
| `StopAgent(int agentId)` | 按实例 ID 停止 |
| `StopAll(bool fadeOut)` | 停止所有分类 |
| `SetVolume(AudioType type, float volume)` | 设置某分类音量 |
| `SetMute(AudioType type, bool mute)` | 某分类静音/取消 |
| `Enable { get; set; }` | 总开关 |
| `SetMasterVolume(float volume)` | 总音量 |

**默认初始化**（无 `_customGroups` 时）：

```csharp
_categories[(int)AudioType.Music] = new AudioCategory(new AudioGroupConfig
{
    Name = "Music", AudioType = AudioType.Music, AgentHelperCount = 1
}, audioHostTransform);

_categories[(int)AudioType.Sound] = new AudioCategory(new AudioGroupConfig
{
    Name = "Sound", AudioType = AudioType.Sound, AgentHelperCount = 10
}, audioHostTransform);

// Voice 默认不创建，业务层通过 _customGroups 或 Init 扩展参数添加
```

**自定义分组**（Inspector 中拖入 ScriptableObject 数组）：

```csharp
[SerializeField] private AudioGroupConfig[] _customGroups;
// 如果 _customGroups 非空，用其替代默认 Music/Sound 配置
```

**Update 驱动**：`EmberAudioManager` 自身不实现 `IEmberUpdate`（避免额外的 Update 遍历），改为在 `PlaySFX` 被调用时惰性注册到 `EmberUpdateManager`，`_activeAgents` 全部归池后自动注销。

> **备选方案**：如果惰性注册/注销的复杂度太高，改为 `IEmberUpdate`，在 Update 中遍历 `_categories`，无活跃 Agent 时几乎零开销。

---

## 四、实施步骤

| 步骤 | 文件 | 内容 | 预估 |
|------|------|------|------|
| S1 | `AudioType.cs` | 新建音频分类枚举 | 5 min |
| S2 | `AudioGroupConfig.cs` | 新建音频组配置数据类 | 5 min |
| S3 | `AudioAgent.cs` | 新建可池化播放代理（含 fade、生命周期、池归还逻辑） | 1.5 h |
| S4 | `AudioCategory.cs` | 新建分类管理器（Agent 池、并发控制、Update 驱动） | 1 h |
| S5 | `EmberAudioManager.cs` | 重构：内部切换到 Category 驱动，保持 API 兼容 | 1 h |
| S6 | `README.md` + 日志标签 + 事件常量 | 更新文档和基础设施 | 15 min |
| S7 | 自测 | 验证 BGM/SFX 播放、音量、Mute、Agent 池复用 | 30 min |

**总计：约 4.5 小时**

---

## 五、风险与缓解

| # | 风险 | 缓解 |
|---|------|------|
| 1 | `PlaySFX` 返回类型从 `void` → `AudioAgent`，现有调用方编译失败 | 如果调用方不接收返回值，完全兼容。如果 `var x = PlaySFX(...)` 后续使用 `x`，类型变为 `AudioAgent`，需要检查。框架内部无此用法，业务层搜索确认 |
| 2 | Agent 池耗尽（高频 SFX 超过 `AgentHelperCount`） | 默认 Sound=10 足够；提供 `_customGroups` 覆盖；池耗尽时 Return null（调用方判空） |
| 3 | AudioMixer Group 路由 | Agent 的 AudioSource 需设置 `outputAudioMixerGroup`，当前全部路由到 Master。后续可通过 `AudioGroupConfig.Name` 匹配 Mixer 中的 Group |
| 4 | Update 遍历开销 | 只遍历活跃 Agent，空闲 Agent 不参与。`AudioCategory.Update()` 在 `_activeAgents.Count == 0` 时立即返回 |
| 5 | 与 GameLauncher.AudioHost 耦合 | 不变，Agent GameObject 仍创建在 AudioHost 下 |

---

## 六、明确不做

- ❌ **WWISE / FMOD 集成层** — 框架不绑定特定音频中间件，burner 的 `#if !USE_WWISE` 模式是业务层的事
- ❌ **资源异步加载** — 音频 Clip 加载走 `EmberResourceManager`，AudioManager 只负责"拿到 Clip 后播放"
- ❌ **3D 空间音频** — 属于业务层需求，框架提供 `AudioAgent` 引用，业务层自行设置 `spatialBlend`
- ❌ **AudioClip 资源池（PreloadAudioInPool）** — burner 的 `AudioClipPool` 本质是资源缓存，应在 Resource 层统一做
- ❌ **BattleSound / RTPC** — burner 中特定于 SLG 战斗的音效管理逻辑
- ❌ **ScriptableObject 化 AudioGroupConfig** — 本方案保留为纯 C# 类，SO 化在后续迭代中做

---

## 七、与现有基础设施的对齐

| 框架设施 | 对齐方式 |
|----------|---------|
| `EmberSingleton<T>` | 不新增 Singleton，`EmberAudioManager` 仍是唯一入口 |
| `IEmberManager` | Init 中初始化 Category 数组，Destroy 中逆序 Cleanup |
| `EmberInitOrder` | 不变，`Audio = 300` |
| `EmberEventBus` | 不变，`AudioReady (5001)` / `AudioShutdown (5002)` 广播 |
| `LogTags.AudioManager` | `AudioCategory` / `AudioAgent` 复用同一 TAG，或新增子标签 `EmberAudio.Category` / `EmberAudio.Agent` |
| `EmberObjectPool<T>` | 不直接使用。Agent 是 MonoBehaviour，走 GameObject 池而非 C# 对象池 |
| `GameLauncher.AudioHost` | 不变，Agent GameObject 挂在此节点下 |
| `EmberUpdateManager` | `EmberAudioManager` 注册 `IEmberUpdate`，每帧驱动各 Category |

---

## 八、变更日志

| 日期 | 变更 |
|------|------|
| 2026-08-05 | 创建文档，基于 burner Audio 模块分析制定四层架构方案 |
