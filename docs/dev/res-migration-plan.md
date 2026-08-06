# 资源模块增强方案：从 burner 提取 Handle + File 模式

> 来源：burner `Assets/Game/GameCore/Runtime/Common/Res/` 评估（2026-08-06）
> 评估结论：整目录不建议迁移（耦合度 > 70%），仅提取 3 个核心模式自行实现

---

## 总览

```
burner Res 目录（24 个文件）
    │
    ├── 🔴 不迁移（14 个）── 深度绑定 YooAsset / burner auth / CDN / 加密
    │
    ├── 🟢 步骤 1（当前）── 提取 AssetHandleSlot + ResFileHandle 设计模式
    │       目标：为 ember 增加可追踪的异步加载句柄 + Raw File 加载能力
    │
    ├── 🟡 步骤 2 ── UniEvent 对比评估（详见 §二）
    │
    └── 🔵 步骤 3 ── YooAssetProvider 写入未来待办（详见 §三）
```

---

## 一、步骤 1：EmberAssetHandle + EmberFileHandle

### 1.1 要迁移的两个文件是什么？

#### AssetHandleSlot\<T\>（147 行，`Handles/AssetHandleSlot.cs`）

这是一个**线程安全的异步资源加载槽**，解决了一个很实际的业务痛点：

**场景**：一个 UI Image 组件，用户快速切换头像——
```
LoadAsync("avatar_01") → 还没加载完 → LoadAsync("avatar_02")
```

普通的 callback 做法会导致：
1. avatar_01 加载完成后设置到 Image 上（但用户已经切走了）
2. avatar_02 加载完成后又设置一次
3. 如果没有去重，两个请求同时进行浪费带宽

`AssetHandleSlot` 用一个槽（Slot）来解决：
- 槽里同时只有 **一个"当前已加载"** + **一个"正在加载中"** 的状态
- 请求同一个资源 → 复用回调，不发起新请求
- 请求不同资源 → 取消旧请求，只等新请求
- 回调里再次调用 LoadAsync → 安全（重入保护）

核心机制：

```
┌─────────────────────────────────────┐
│        AssetHandleSlot<T>           │
│                                     │
│  _currentAssetName: "avatar_01"     │  ← 当前持有的资源名
│  _currentHandle: AssetHandle        │  ← 当前资源的 YooAsset Handle
│                                     │
│  _loadingAssetName: "avatar_02"     │  ← 正在加载的资源名（null = 没有在加载）
│  _loadingHandle: AssetHandle        │  ← 正在加载的 Handle
│  _loadingApplyCallback: Action<T>   │  ← 加载完成后的回调
│                                     │
│  LoadAsync("avatar_03") →           │
│    1. 如果 == _currentAssetName → 立即回调（已加载）│
│    2. 如果 == _loadingAssetName → 替换回调（正在加载中）│
│    3. 否则 → CancelLoading + 发起新请求   │
└─────────────────────────────────────┘
```

**关键设计细节**（代码中注释非常清晰）：
- 空字符串资源名不参与去重 —— 视为非法输入，每次都交给底层报错
- 回调完成后释放 oldHandle 之前检查 `oldHandle != _currentHandle` —— 防止回调里重入了同一个资源
- 资源名规范化：`ToLower()`，统一大小写

#### ResFileHandle（158 行，`Handles/ResFileHandle.cs`）

这是一个**原始文件/字节流加载的抽象 Handle**，提供统一的完成状态和错误信息。

它解决的问题：YooAsset 加载 RawFile 时有多种路径（编辑器直接读文件、内置 Bundle、下载的 Bundle），
每种路径的 API 不同（`AssetHandle` vs `EnsureBundleFileOperation`），但调用方不想关心这些。

```
ResFileHandle
├── 构造来源（三选一）：
│   ├── AssetHandle（异步加载的 Bundle 中的文件）
│   ├── EnsureBundleFileOperation（只确保文件在磁盘上）
│   └── byte[]（编辑器模式直接读文件，或 LoadBytesSync 结果）
│
├── 统一查询：
│   ├── IsDone / Succeeded / Error
│   └── GetAssetInfo()
│
├── 统一读取：
│   ├── GetBytes()   → byte[]（防御性拷贝）
│   ├── GetText()    → string（懒解析 + 缓存）
│   └── GetFilePath() → string（仅 EnsureBundleFile 模式）
│
└── 生命周期：
    ├── Release() → 释放底层 Handle
    ├── Dispose() → Release()
    └── Failed()  → 静态工厂，生成已失败的 Handle
```

---

### 1.2 迁移到 ember 后能实现什么？

#### 当前 ember 资源加载的能力

```csharp
// 当前：纯 callback，无法取消、无法去重、无法追踪状态
EmberResourceManager.Instance.LoadAssetAsync<Sprite>("ui/icon", sprite => {
    if (sprite != null) image.sprite = sprite;
});
// 问题：
// 1. 如果这个 Image 组件快速切换了 5 次资源，5 个回调都会触发
// 2. 无法知道"当前正在加载哪个资源"
// 3. 无法取消正在进行的加载
// 4. 组件销毁时，回调仍然会触发（可能的 NRE）
```

#### 迁移后

```csharp
// 组件中声明一个槽
private readonly EmberAssetHandleSlot<Sprite> _iconSlot = new();

// 切换资源：自动取消旧请求、去重、重入安全
public void SetIcon(string iconPath)
{
    _iconSlot.LoadAsync(iconPath, sprite => {
        if (sprite != null) iconImage.sprite = sprite;
    });
}

// 查询当前状态
string currentIcon = _iconSlot.CurrentAssetPath;   // 当前持有的资源
string loadingIcon = _iconSlot.LoadingAssetPath;   // 正在加载的资源（null = 无）

// 组件销毁时
void OnDestroy()
{
    _iconSlot.Dispose();  // 取消加载中请求 + 释放当前资源
}
```

#### 具体增益

| 能力 | 当前 ember | 迁移后 |
|------|-----------|--------|
| **请求去重** | ❌ 每次 `LoadAssetAsync` 都发起新请求 | ✅ 同一个资源只加载一次 |
| **取消加载** | ❌ 无法取消 | ✅ `CancelLoading()` 或再次 `LoadAsync` |
| **状态追踪** | ❌ 无法知道"IsDone" | ✅ `CurrentAssetPath` / `LoadingAssetPath` / `IsLoading` |
| **重入安全** | ❌ 回调中再次 Load 可能出问题 | ✅ 回调中任意操作都安全 |
| **组件销毁安全** | ❌ 回调触发时组件可能已销毁 | ✅ `Dispose()` 后回调不再触发 |
| **Raw File 加载** | ❌ 不支持 | ✅ `LoadFileAsync` → `EmberFileHandle` |
| **File 进度/错误** | ❌ 无法获取 | ✅ `IsDone` / `Succeeded` / `Error` |

---

### 1.3 完整设计方案

#### 新增类型总览

```
Assets/Ember/Resource/Runtime/
├── IResourceProvider.cs          ← 扩展：增加 LoadAssetAsyncHandle / LoadFileAsync
├── EmberResourceManager.cs       ← 扩展：暴露 Handle 版本的加载方法
├── EmberAssetHandle.cs           ← 新增：资源加载句柄
├── EmberAssetHandleSlot.cs       ← 新增：异步加载槽（核心迁移）
├── EmberFileHandle.cs            ← 新增：文件加载句柄
└── ResourcesProvider.cs          ← 扩展：实现新增接口方法
```

#### 1.3.1 EmberAssetHandle\<T\>

```csharp
namespace Ember.Resource
{
    /// <summary>
    /// 异步资源加载句柄 —— 封装一次资源加载请求的完整生命周期。
    ///
    /// 对应 burner 的 GameResourceHandle，但去掉了对 YooAsset HandleBase 的依赖，
    /// 改为后端无关的纯回调模式。
    ///
    /// 用法：
    /// <code>
    /// var handle = EmberResourceManager.Instance.LoadAssetHandle&lt;Sprite&gt;("ui/icon");
    /// handle.Completed += (sprite) => { image.sprite = sprite; };
    /// // ...
    /// handle.Cancel();  // 取消加载（已完成的忽略）
    /// handle.Dispose(); // 释放资源引用
    /// </code>
    /// </summary>
    public sealed class EmberAssetHandle<T> : IDisposable where T : UnityEngine.Object
    {
        #region 内部参数

        private readonly string _assetPath;
        private T _asset;
        private bool _isDone;
        private bool _isCancelled;
        private bool _succeeded;
        private string _error;
        private Action _cancelAction; // 由 Provider 注入，用于取消底层操作

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>资源路径。</summary>
        public string AssetPath => _assetPath;

        /// <summary>加载是否已完成（成功、失败或取消）。</summary>
        public bool IsDone => _isDone;

        /// <summary>加载是否成功。</summary>
        public bool Succeeded => _succeeded;

        /// <summary>已加载的资源（仅 IsDone && Succeeded 时有值）。</summary>
        public T Asset => _asset;

        /// <summary>错误信息（仅失败时有值）。</summary>
        public string Error => _error;

        /// <summary>加载完成事件。</summary>
        public event Action<T> Completed;

        /// <summary>
        /// 取消加载请求。已完成的请求忽略。
        /// 取消后会触发 Completed(null)。
        /// </summary>
        public void Cancel()
        {
            if (_isDone || _isCancelled) return;
            _isCancelled = true;
            _cancelAction?.Invoke();
            Complete(null, false, "Cancelled");
        }

        /// <summary>
        /// 释放资源引用。如果加载中则先取消。
        /// </summary>
        public void Dispose()
        {
            Cancel();
            _asset = null;
            Completed = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法（由 Provider 调用）

        /// <summary>Provider 注入取消委托。</summary>
        internal void SetCancellation(Action cancel)
        {
            _cancelAction = cancel;
        }

        /// <summary>Provider 完成通知。</summary>
        internal void Complete(T asset, bool succeeded, string error)
        {
            if (_isDone || _isCancelled) return;
            _isDone = true;
            _succeeded = succeeded;
            _asset = asset;
            _error = error;
            Completed?.Invoke(succeeded ? asset : null);
        }

        #endregion
    }
}
```

#### 1.3.2 EmberAssetHandleSlot\<T\>

这是从 burner `AssetHandleSlot` 提取的核心模式，去掉 YooAsset 依赖，改为依赖 `EmberResourceManager` 的 Handle API。

```csharp
namespace Ember.Resource
{
    /// <summary>
    /// 异步资源加载槽 —— 持有"当前已加载"和"正在加载中"两个状态，
    /// 自动处理去重、取消和重入。
    ///
    /// 这是框架中最常用的资源加载模式，适用于 UI 头像、背景图、模型等需要
    /// 动态切换资源的场景。
    ///
    /// 用法：
    /// <code>
    /// private readonly EmberAssetHandleSlot&lt;Sprite&gt; _iconSlot = new();
    ///
    /// public void SetIcon(string path)
    /// {
    ///     _iconSlot.LoadAsync(path, sprite => {
    ///         if (sprite != null) iconImage.sprite = sprite;
    ///     });
    /// }
    ///
    /// void OnDestroy() { _iconSlot.Dispose(); }
    /// </code>
    /// </summary>
    public sealed class EmberAssetHandleSlot<T> : IDisposable where T : UnityEngine.Object
    {
        #region 内部参数

        private string _currentPath;
        private EmberAssetHandle<T> _currentHandle;
        private string _loadingPath;
        private EmberAssetHandle<T> _loadingHandle;
        private Action<T> _loadingCallback;
        private bool _disposed;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>当前已加载的资源名（null 表示尚未加载任何资源）。</summary>
        public string CurrentAssetPath => _currentPath;

        /// <summary>正在加载的资源名（null 表示无加载中请求）。</summary>
        public string LoadingAssetPath => _loadingPath;

        /// <summary>槽的目标资源名：优先返回正在加载的，其次已加载的。</summary>
        public string TargetAssetPath => _loadingPath ?? _currentPath;

        /// <summary>是否正在加载。</summary>
        public bool IsLoading => _loadingHandle != null;

        /// <summary>
        /// 异步加载资源。
        ///
        /// - 如果 assetPath 与已加载的相同 → 立即用当前资源回调（reapplyIfCurrent=true 时）
        /// - 如果 assetPath 与正在加载的相同 → 替换回调（合并请求）
        /// - 如果是新的路径 → 取消旧加载，发起新加载
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="onLoaded">加载完成回调，失败时为 null</param>
        /// <param name="reapplyIfCurrent">路径与当前相同时是否重新回调</param>
        public void LoadAsync(string assetPath, Action<T> onLoaded, bool reapplyIfCurrent = true)
        {
            if (_disposed)
            {
                EmberDebug.LogWarning(LogTags.ResourceManager,
                    "[EmberAssetHandleSlot] 已释放的资源槽不应再次发起加载。");
                return;
            }

            var normalized = NormalizePath(assetPath);

            // 1. 与当前已加载的相同 → 立即回调
            if (IsSamePath(_currentPath, normalized))
            {
                CancelLoading();
                if (reapplyIfCurrent)
                    ApplyCurrent(onLoaded);
                return;
            }

            // 2. 与正在加载的相同 → 替换回调
            if (IsSamePath(_loadingPath, normalized))
            {
                _loadingCallback = onLoaded;
                return;
            }

            // 3. 新资源 → 取消旧加载，发起新加载
            CancelLoading();

            var handle = EmberResourceManager.Instance.LoadAssetHandle<T>(assetPath);
            _loadingPath = normalized;
            _loadingCallback = onLoaded;
            _loadingHandle = handle;
            handle.Completed += OnHandleCompleted;
        }

        /// <summary>取消正在进行的加载（不影响已加载的资源）。</summary>
        public void CancelLoading()
        {
            _loadingPath = null;
            _loadingCallback = null;
            if (_loadingHandle != null)
            {
                _loadingHandle.Completed -= OnHandleCompleted;
                _loadingHandle.Cancel();
                _loadingHandle = null;
            }
        }

        /// <summary>释放槽：取消加载 + 释放当前资源。释放后不能再使用。</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelLoading();
            ClearCurrent();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnHandleCompleted(T asset)
        {
            if (_disposed) return;

            // 防御：回调可能晚于取消
            if (_loadingHandle == null) return;

            var path = _loadingPath;
            var callback = _loadingCallback;
            var handle = _loadingHandle;

            _loadingHandle = null;
            _loadingPath = null;
            _loadingCallback = null;
            handle.Completed -= OnHandleCompleted;

            // 先持有新资源，再回调（回调里可能立即 LoadAsync 或 Dispose）
            var oldHandle = _currentHandle;
            _currentHandle = handle;
            _currentPath = path;

            callback?.Invoke(asset);

            // 防止误释放在回调里重新启用的 handle
            if (oldHandle != null && oldHandle != _currentHandle)
            {
                oldHandle.Dispose();
            }
        }

        private void ApplyCurrent(Action<T> callback)
        {
            if (_currentHandle != null && _currentHandle.Succeeded)
                callback?.Invoke(_currentHandle.Asset);
            else
                callback?.Invoke(null);
        }

        private void ClearCurrent()
        {
            _currentPath = null;
            if (_currentHandle != null)
            {
                _currentHandle.Dispose();
                _currentHandle = null;
            }
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.ToLower();
        }

        private static bool IsSamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        #endregion
    }
}
```

#### 1.3.3 EmberFileHandle

```csharp
namespace Ember.Resource
{
    /// <summary>
    /// 文件加载句柄 —— 统一管理 Raw File / Bytes / Text 的异步加载。
    ///
    /// 对应 burner 的 ResFileHandle，去掉 YooAsset 依赖，改为后端无关的设计。
    ///
    /// 用法：
    /// <code>
    /// var handle = EmberResourceManager.Instance.LoadFileAsync("config/game_data.json");
    /// handle.Completed += (h) => {
    ///     if (h.Succeeded) {
    ///         string json = h.GetText();
    ///         byte[] bytes = h.GetBytes();
    ///     }
    /// };
    /// </code>
    /// </summary>
    public sealed class EmberFileHandle : IDisposable
    {
        #region 内部参数

        private readonly string _assetPath;
        private byte[] _bytes;
        private string _text; // 懒解析
        private string _filePath;
        private bool _isDone;
        private bool _succeeded;
        private string _error;
        private Action _cancelAction;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>文件路径。</summary>
        public string AssetPath => _assetPath;

        /// <summary>加载是否已完成。</summary>
        public bool IsDone => _isDone;

        /// <summary>加载是否成功。</summary>
        public bool Succeeded => _succeeded;

        /// <summary>错误信息。</summary>
        public string Error => _error;

        /// <summary>加载完成事件。</summary>
        public event Action<EmberFileHandle> Completed;

        /// <summary>取消加载。</summary>
        public void Cancel()
        {
            if (_isDone) return;
            _cancelAction?.Invoke();
            Complete(null, null, false, "Cancelled");
        }

        /// <summary>获取文件原始字节（防御性拷贝）。</summary>
        public byte[] GetBytes()
        {
            if (_bytes == null || _bytes.Length == 0) return Array.Empty<byte>();
            var copy = new byte[_bytes.Length];
            Buffer.BlockCopy(_bytes, 0, copy, 0, _bytes.Length);
            return copy;
        }

        /// <summary>获取文件文本内容（UTF-8，懒解析）。</summary>
        public string GetText()
        {
            if (_bytes == null || _bytes.Length == 0) return string.Empty;
            return _text ??= Encoding.UTF8.GetString(_bytes);
        }

        /// <summary>
        /// 获取文件在磁盘上的路径。仅在 Provider 支持文件路径时有效，
        /// 否则返回 string.Empty。
        /// </summary>
        public string GetFilePath() => _filePath ?? string.Empty;

        /// <summary>释放。</summary>
        public void Dispose()
        {
            Cancel();
            _bytes = null;
            _text = null;
            Completed = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法（由 Provider 调用）

        internal void SetCancellation(Action cancel)
        {
            _cancelAction = cancel;
        }

        internal void Complete(byte[] bytes, string filePath, bool succeeded, string error)
        {
            if (_isDone) return;
            _isDone = true;
            _succeeded = succeeded;
            _bytes = bytes;
            _filePath = filePath;
            _error = error;
            Completed?.Invoke(this);
        }

        /// <summary>创建已失败的句柄。</summary>
        internal static EmberFileHandle Failed(string path, string error)
        {
            var handle = new EmberFileHandle(path);
            handle.Complete(null, null, false, error);
            return handle;
        }

        #endregion /内部方法

        private EmberFileHandle(string path)
        {
            _assetPath = path;
        }
    }
}
```

#### 1.3.4 IResourceProvider 扩展

在现有接口上增加 3 个方法：

```csharp
// === 新增 ===

/// <summary>
/// 异步加载资源并返回可追踪的句柄（支持取消和状态查询）。
/// </summary>
EmberAssetHandle<T> LoadAssetHandle<T>(string path) where T : UnityEngine.Object;

/// <summary>
/// 异步加载原始文件（bytes/text）。返回 EmberFileHandle 可追踪进度和取消。
/// </summary>
EmberFileHandle LoadFileAsync(string path);

/// <summary>
/// 同步加载原始文件。Editor 下直接读文件，运行时从 Bundle 读取。
/// </summary>
byte[] LoadFileSync(string path);
```

#### 1.3.5 EmberResourceManager 扩展

```csharp
// === 新增方法 ===

/// <summary>异步加载资源并返回句柄。</summary>
public EmberAssetHandle<T> LoadAssetHandle<T>(string path) where T : Object
{
    if (!IsProviderReady()) return null;
    return _provider.LoadAssetHandle<T>(path);
}

/// <summary>异步加载文件。</summary>
public EmberFileHandle LoadFileAsync(string path)
{
    if (!IsProviderReady()) return null;
    return _provider.LoadFileAsync(path);
}

/// <summary>同步加载文件。</summary>
public byte[] LoadFileSync(string path)
{
    if (_provider == null) return null;
    return _provider.LoadFileSync(path);
}
```

#### 1.3.6 ResourcesProvider 适配

```csharp
// ResourcesProvider 实现新增接口方法

public EmberAssetHandle<T> LoadAssetHandle<T>(string path) where T : Object
{
    var handle = new EmberAssetHandle<T>(path);
    // Resources 同步加载，直接完成
    T asset = Resources.Load<T>(path);
    if (asset != null)
        handle.Complete(asset, true, null);
    else
        handle.Complete(null, false, $"Resource not found: {path}");
    return handle;
}

public EmberFileHandle LoadFileAsync(string path)
{
    var handle = new EmberFileHandle(path);
    var asset = Resources.Load<TextAsset>(path);
    if (asset != null)
        handle.Complete(asset.bytes, null, true, null);
    else
        handle.Complete(null, null, false, $"File not found: {path}");
    return handle;
}

public byte[] LoadFileSync(string path)
{
    var asset = Resources.Load<TextAsset>(path);
    return asset?.bytes;
}
```

---

### 1.4 实施步骤

| 步骤 | 内容 | 预计工作量 |
|------|------|-----------|
| 1.1 | 新建 `EmberAssetHandle.cs` — 句柄基类 | ~80 行 |
| 1.2 | 新建 `EmberAssetHandleSlot.cs` — 异步加载槽 | ~140 行 |
| 1.3 | 新建 `EmberFileHandle.cs` — 文件加载句柄 | ~100 行 |
| 1.4 | 扩展 `IResourceProvider` 接口 — 增加 3 个方法 | ~15 行 |
| 1.5 | 扩展 `EmberResourceManager` — 暴露 Handle API | ~40 行 |
| 1.6 | 适配 `ResourcesProvider` — 实现新接口 | ~60 行 |
| 1.7 | 更新 `ember-api-reference.md` — 文档 | — |
| **合计** | | **~435 行新代码** |

---

## 二、步骤 2：UniEvent vs EmberEventBus 对比

### 2.1 功能对比矩阵

| 维度 | burner UniEvent | ember EmberEventBus | 差距 |
|------|----------------|---------------------|------|
| **Key 类型** | `typeof(T).GetHashCode()`（type-keyed） | `int` 常量（int-keyed） | 各有优势 |
| **消息载体** | `IEventMessage` 接口 —— 每个事件定义一个 class | 泛型参数（0~4 个）—— 直接传值 | ember 更简洁，无装箱 |
| **立即派发** | `SendMessage(message)` | `OnNext(key, args)` | 同等 |
| **延迟派发** | `PostMessage(message)` —— 下一帧执行 | ❌ 无 | **UniEvent 独有** |
| **批量管理** | `EventGroup` —— AddListener 多个，一次 RemoveAll | `IDisposable` —— 每个订阅独立管理 | 不同模式 |
| **生命周期** | 显式 `Initalize()` / `Destroy()` + Driver GameObject | 静态类，无显式生命周期 | ember 更轻量 |
| **遍历安全** | 倒序遍历 LinkedList（简单，但无法完全防止嵌套派发问题） | `_dispatchDepth` + `_pendingOps` 延迟队列 | ember 更完善 |
| **Listener 存储** | `Dictionary<int, LinkedList<Action<IEventMessage>>>` | `Dictionary<int, Action/Delegate>` | ember 用 Delegate.Combine，内存更紧凑 |
| **订阅粒度** | 按 eventId 整体管理 | 每个 `Subscribe` 返回独立 `IDisposable` | ember 更细粒度 |
| **HasSubscribers** | ❌ 无 | ✅ `HasSubscribers(key)` | ember 独有 |
| **ClearSubscribers** | ❌ 无（只有 ClearAll） | ✅ 按 Key 清理 | ember 独有 |
| **日志** | `UniLogger`（Conditional("DEBUG")） | `EmberDebug.LogEvent`（紫色+标签过滤） | ember 更完善 |

### 2.2 UniEvent 有什么是 EmberEventBus 没有的？

#### 差距 1：PostMessage（延迟派发）⭐ 主要差距

```csharp
// UniEvent: 延迟到下一帧触发
UniEvent.PostMessage(message);  // 不会立即执行，等待下一帧 Update

// EmberEventBus: 只能立即触发
EmberEventBus.OnNext(key);      // 立即同步执行所有订阅者
```

**实际使用场景**：
- 资源更新流程中的步骤通知（`LaunchStepNotify`）：确保当前帧所有初始化代码跑完后再通知下一步
- 避免在初始化过程中触发尚未就绪的监听者

**ember 已有替代方案吗？**
- ember 架构文档已将"需要操作符/延迟"的事件分配给 **UniRx**
- `Observable.NextFrame()` 可以实现等效效果
- 但 UniRx 是外部依赖，EmberEventBus 在框架 Core 中零依赖

#### 差距 2：Type-keyed 事件

```csharp
// UniEvent: 按类型区分事件
UniEvent.AddListener<PatchEventDefine.FoundUpdateFiles>(handler);
UniEvent.SendMessage(new FoundUpdateFiles { TotalCount = 10, ... });

// EmberEventBus: 按 int 常量区分
EmberEventBus.Subscribe(EmberBroadcastEvent.ResourceReady, handler);
EmberEventBus.OnNext(EmberBroadcastEvent.ResourceReady);
```

**评估**：两种方式各有优势。
- Type-keyed：不需要维护常量表，自动通过类型区分，适合"有很多小事件"的场景
- Int-keyed：编译期易追踪（IDE Find References），区间分配防冲突，适合"框架级广播"

ember 的 int-keyed 方案对框架场景是更好的选择（常量表清晰可见、IDE 可跳转、区间隔离模块）。

#### 差距 3：EventGroup（批量管理）

```csharp
var group = new EventGroup();
group.AddListener<EventA>(handlerA);
group.AddListener<EventB>(handlerB);
group.AddListener<EventC>(handlerC);
group.RemoveAllListener(); // 一键清理所有
```

**ember 等效方案**：`List<IDisposable>` 模式。

```csharp
var disposables = new List<IDisposable>();
disposables.Add(EmberEventBus.Subscribe(keyA, handlerA));
disposables.Add(EmberEventBus.Subscribe(keyB, handlerB));
// 清理
foreach (var d in disposables) d.Dispose();
disposables.Clear();
```

功能等效，只是写法稍长。ember 也可以加一个简单的 `EmberEventGroup` 包装类。

### 2.3 推荐方案

#### 结论：**不迁移 UniEvent，给 EmberEventBus 加 PostNext**

理由：
1. ember 已有完善的 `EmberEventBus`，UniEvent 的功能重叠度 > 80%
2. Type-keyed vs int-keyed 各有适用场景，ember 选择 int-keyed 是正确的
3. 唯一真正的差距是 `PostMessage`（延迟派发）

#### 推荐改动：EmberEventBus 增加 PostNext

```csharp
// === 新增到 EmberEventBus ===

private static readonly List<PostEntry> _postQueue = new();

private struct PostEntry
{
    public int EventKey;
    public object Arg1, Arg2, Arg3, Arg4;
    public int ArgCount;
}

/// <summary>
/// 将事件延迟到下一帧播报。适用于初始化阶段：确保所有模块注册完监听后再广播。
/// </summary>
public static void PostNext(int eventKey)
{
    _postQueue.Add(new PostEntry { EventKey = eventKey, ArgCount = 0 });
}

public static void PostNext<T>(int eventKey, T arg)
{
    _postQueue.Add(new PostEntry { EventKey = eventKey, Arg1 = arg, ArgCount = 1 });
}

// 在 EmberUpdateManager 的新增 PostDispatch 中每帧执行：
internal static void FlushPostQueue()
{
    var entries = new List<PostEntry>(_postQueue);
    _postQueue.Clear();
    foreach (var entry in entries)
    {
        switch (entry.ArgCount)
        {
            case 0: OnNext(entry.EventKey); break;
            case 1: OnNext(entry.EventKey, entry.Arg1); break;
            // ... 更多参数
        }
    }
}
```

以及可选的 `EmberEventGroup` 辅助类：

```csharp
/// <summary>
/// 事件组 —— 批量管理事件订阅，一键清理。
/// </summary>
public sealed class EmberEventGroup : IDisposable
{
    private readonly List<IDisposable> _subs = new();

    public void Add(int key, Action handler)
        => _subs.Add(EmberEventBus.Subscribe(key, handler));

    public void Add<T>(int key, Action<T> handler)
        => _subs.Add(EmberEventBus.Subscribe(key, handler));

    public void Clear()
    {
        foreach (var sub in _subs) sub.Dispose();
        _subs.Clear();
    }

    public void Dispose() => Clear();
}
```

| 改动 | 工作量 |
|------|--------|
| `PostNext` 0~4 参数版本 | ~50 行 |
| `_postQueue` + `FlushPostQueue` | ~40 行 |
| 在 `EmberUpdateManager` 中调用 `FlushPostQueue()` | ~3 行 |
| `EmberEventGroup` 辅助类 | ~30 行 |
| **合计** | **~123 行** |

---

## 三、步骤 3：YooAssetProvider（未来待办）

> 已写入 [framework-progress.md §技术债务 & 待扩展](#)

当 ember 进入 Phase 2（网络适配 / 热更新需求）时，从头编写一个干净的 `YooAssetProvider : IResourceProvider`：

**参考来源**：
- burner `GameResourceProxy` — Provider 实现模式
- burner `YooAssetInitializer` — 初始化流程（仅流程，不搬运 auth/加密）
- burner `RemoteServices` — CDN 主备切换思路

**不搬运的部分**：
- `YooAssetUpdater` — auth 服务器协议私有
- `YooAssetMixedBundleTypeAdapter` — YooAsset 内部反射 hack，版本升级即崩
- `StartupResourceLoadStat` — 性能剖析，锦上添花
- `PatchEventDefine` — burner 特定的 UI 事件
- `ResManager` 整体 — ember 已有 EmberResourceManager 门面

**核心流程**（约 200 行）：
```
Initialize()
  → YooAssets.Initialize()
  → CreatePackage()
  → InitializePackageAsync() ← 根据 PlayMode 选择参数
  → onComplete

LoadAssetHandle<T>(path)
  → package.LoadAssetAsync<T>(path)
  → 包装为 EmberAssetHandle<T>（注入 Completed + Cancel 委托）

LoadFileAsync(path)
  → package.LoadAssetAsync(path) 或 EnsureBundleFileAsync()
  → 包装为 EmberFileHandle
```
