# Ember API 速查手册

> **写代码前先查这里，避免重复造轮子。**
> 最后更新：2026-08-06 | 覆盖 78 个文件、~115 个公开类型、590+ 个公开成员

---

## 目录

- [集合池](#集合池)
- [基础数据结构](#基础数据结构)
- [扩展方法](#扩展方法)
- [异步 STTask](#异步-sttask)
- [JSON](#json)
- [Unsafe / 原生内存](#unsafe--原生内存)
- [加密与哈希](#加密与哈希)
- [性能分级](#性能分级)
- [标记 Attribute](#标记-attribute)
- [事件系统](#事件系统)
- [服务定位 & 单例](#服务定位--单例)
- [日志](#日志)
- [状态机](#状态机)
- [Update 循环](#update-循环)
- [Manager 自动发现](#manager-自动发现)
- [启动器](#启动器)
- [资源管理](#资源管理)
- [UI 管理](#ui-管理)
- [场景管理](#场景管理)
- [音频管理](#音频管理)
- [输入管理](#输入管理)
- [相机管理](#相机管理)
- [Editor 工具（Editor-only）](#editor-工具editor-only)
- [其他接口](#其他接口)

---

## 集合池

> 使用规则：**Get/Return 必须成对出现**，Return 后不要再持有引用。

### ListPool\<T\>

| | |
|---|---|
| **位置** | `com.ember.basic/Base/ListPool.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | List 对象池。从池中取出的 List 保证容量 >= 指定值。归还时自动 Clear。 |

```csharp
var list = ListPool<int>.Get(16);   // 取
ListPool<int>.Return(list);         // 还

int cached = ListPool<int>.CachedCount;  // 当前缓存数量
ListPool<int>.Clear();                   // 清空所有缓存
```

| 成员 | 签名 |
|------|------|
| Get | `static List<T> Get(int capacity = 16)` |
| Return | `static void Return(List<T> list)` |
| Clear | `static void Clear()` |
| CachedCount | `static int CachedCount` |

### DictionaryPool\<K, V\>

| | |
|---|---|
| **位置** | `com.ember.basic/Base/DictionaryPool.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | Dictionary 对象池。归还时自动 Clear。 |

```csharp
var dict = DictionaryPool<string, object>.Get(16);
DictionaryPool<string, object>.Return(dict);
```

| 成员 | 签名 |
|------|------|
| Get | `static Dictionary<K, V> Get(int capacity = 16)` |
| Return | `static void Return(Dictionary<K, V> dict)` |
| Clear | `static void Clear()` |
| CachedCount | `static int CachedCount` |

### HashSetPool\<V\>

| | |
|---|---|
| **位置** | `com.ember.basic/Base/HashSetPool.cs` |
| **命名空间** | `Ember.Basic` |

```csharp
var set = HashSetPool<int>.Get();
HashSetPool<int>.Return(set);
```

| 成员 | 签名 |
|------|------|
| Get | `static HashSet<V> Get()` |
| Return | `static void Return(HashSet<V> set)` |
| Clear | `static void Clear()` |
| CachedCount | `static int CachedCount` |

### MemoryPool\<T\>

| | |
|---|---|
| **位置** | `com.ember.basic/Base/MemoryPool.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 按实例管理的泛型对象池，每个池有独立的最大容量。池空返回 null，池满丢弃。适用纯 C# class（POCO、StringBuilder 等）。 |

```csharp
var pool = new MemoryPool<MyClass>(maxCapacity: 10);

var obj = pool.Get();          // 池空返回 null
bool ok = pool.Return(obj);    // 池满返回 false
bool can = pool.CanReturn;     // 是否还能归还
pool.Clear();
```

| 成员 | 签名 |
|------|------|
| ctor | `MemoryPool(int maxCapacity = 10)` |
| Get | `T Get()` |
| Return | `bool Return(T obj)` |
| Contains | `bool Contains(T obj)` |
| Clear | `void Clear()` |
| Count | `int Count` |
| CanReturn | `bool CanReturn` |

### EmberObjectPool\<T\>

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/Service/EmberObjectPool.cs` |
| **命名空间** | `Ember.Core` |
| **说明** | 带 IPoolable 回调的对象池（`T : class, new()`）。支持预填充、容量限制、统计。适用需要 OnTake/OnReturn 回调的对象。 |

```csharp
var pool = new EmberObjectPool<MyPoolable>(initial: 4, max: 100, trackStats: true);
pool.Prewarm(4);
var obj = pool.Get();
pool.Return(obj);

// 统计
int free = pool.FreeCount;
int created = pool.TotalCreated;
```

| 成员 | 签名 |
|------|------|
| ctor | `EmberObjectPool(int initialCapacity=0, int maxCapacity=0, bool trackStats=false)` |
| Get | `T Get()` |
| Return | `void Return(T obj)` |
| Prewarm | `void Prewarm(int count)` |
| Clear | `void Clear()` |
| FreeCount | `int FreeCount` |
| TotalCreated | `int TotalCreated` |
| TotalRetrieved | `int TotalRetrieved` |
| TotalReturned | `int TotalReturned` |

### IPoolable

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/Service/EmberObjectPool.cs` |
| **说明** | 被 `EmberObjectPool` 管理的对象实现此接口以接收回调。 |

```csharp
void OnTakeFromPool();   // 从池中取出时
void OnReturnToPool();   // 归还到池中时
```

### IPool

| | |
|---|---|
| **位置** | `com.ember.basic/Base/IPool.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 简单的可池化对象接口：`Dispose()` + `Revive()`。 |

### PoolRefCount（Editor 调试）

| | |
|---|---|
| **位置** | `com.ember.basic/Base/PoolRefCount.cs` |
| **说明** | Editor 下追踪对象池泄漏。`EnableCheck = true` 后记录每次 Get/Return 的堆栈。 |

```csharp
PoolRefCount.EnableCheck = true;              // 开启追踪
// ... 运行疑似泄漏的逻辑 ...
Debug.Log(poolRefCount.AllLeakedObjStacks()); // 查看泄漏报告
PoolRefCount.EnableCheck = false;
poolRefCount.ClearAllStacks();
```

---

## 基础数据结构

### FloatCurve2D

| | |
|---|---|
| **位置** | `com.ember.basic/Base/FloatCurve2D.cs` |
| **说明** | 二维 AnimationCurve 组合 `{ AnimationCurve x, y; }`。Evaluate(t) 一次采样两条曲线返回 Vector2。 |

```csharp
var path = new FloatCurve2D { x = curveX, y = curveY };
Vector2 pos = path.Evaluate(0.5f);
```

### NaturalStringComparer

| | |
|---|---|
| **位置** | `com.ember.basic/Base/NaturalStringComparer.cs` |
| **说明** | 自然排序：把数字当数值比而不是字符比。"Frame_2" 排在 "Frame_10" 前面。单例 `Instance`。 |

```csharp
Array.Sort(files, NaturalStringComparer.Instance);
// "Frame_2", "Frame_10", "Frame_100" ← 自然序
// "Frame_10", "Frame_100", "Frame_2" ← 字典序（默认）
```

### QuickQueue\<T\>

| | |
|---|---|
| **位置** | `com.ember.basic/Base/QuickQueue.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | Dictionary + LinkedList 实现的快速双端队列。头尾 Push/Pop O(1)，任意位置 Remove O(1)，通过内部节点池实现 Push/Pop 零 GC。支持排序模式。 |

```csharp
var q = new QuickQueue<string>();
q.PushLast("a");          // 尾部插入
q.PushFirst("b");         // 头部插入
var first = q.PopFirst(); // 头部弹出
q.Remove("a");            // 任意位置删除 O(1)
bool has = q.Contains("x");

// 排序模式
var sorted = new QuickQueue<int>((a, b) => a.CompareTo(b));
sorted.Push(3); sorted.Push(1); sorted.Push(2);
// 内部自动排序: 1, 2, 3
```

| 关键成员 | 签名 |
|----------|------|
| PushFirst / PushLast | `void PushFirst(T key)` / `void PushLast(T key)` |
| PopFirst / PopLast | `T PopFirst()` / `T PopLast()` |
| Remove / TryPop | `bool Remove(T key)` / `bool TryPop(T key)` |
| RemoveAll | `void RemoveAll(Predicate<T>)` — 条件批量删除 |
| Peek | `T Peek(bool first=false)` — 查看但不移除 |
| CopyTo | `void CopyTo(List<T> list)` |
| Clear / FreeCache | `void Clear()` / `void FreeCache()` |

### CacheSortedList\<K, V\>

| | |
|---|---|
| **位置** | `com.ember.basic/Base/CacheSortedList.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 红黑树有序列表，优于 `SortedList<K,V>`：节点缓存无 GC、同 Key 可多值、O(1) ContainsKey、lower_bound / upper_bound。<br>⚠️ **Key 必须能比大小**——推荐 int 或 enum。Key 之间只有 Equals 关系的用 Dictionary。 |

```csharp
// int Key —— 技能等级表
var skills = new CacheSortedList<int, string>();
skills.Add(1, "火球");
skills.Add(5, "冰箭");
skills.Add(10, "雷击");
skills.TryGetGreaterOrEqual(7, out var kv);  // (10, "雷击")

// enum Key —— 品质倍率
var config = new CacheSortedList<Quality, float>();
config.Add(Quality.Common, 1f);
config.Add(Quality.Rare, 1.5f);

// 同 Key 多值
var list = new CacheSortedList<int, string>();
list.Add(3, "a");
list.Add(3, "b");   // Key=3 下存了 "a" "b" 两个值

// lower_bound / upper_bound
list.TryGetGreaterOrEqual(2, out var kv); // 查找 >= 2 的最小 key
list.TryPopGreater(1, out kv);           // 查找 > 1 的最小 key 并移除

list.GetKeys(myList);     // 按 Key 排序输出
list.FreeCache();         // 释放节点缓存
```

| 关键成员 | 签名 |
|----------|------|
| Add | `void Add(TKey key, TValue value)` |
| ContainsKey | `bool ContainsKey(TKey key)` — O(1) |
| Remove / RemoveKey | `bool Remove(TKey key, TValue value)` / `bool RemoveKey(TKey key, bool lastOrAll=true)` |
| TryGetGreaterOrEqual | `bool TryGetGreaterOrEqual(TKey key, out KeyValuePair<TKey,TValue> kv, bool remove=false)` |
| TryGetGreater | `bool TryGetGreater(TKey key, out KeyValuePair<TKey,TValue> kv, bool remove=false)` |
| TryPopGreaterOrEqual / TryPopGreater | 同上但 remove=true |
| Min / Max | `KeyValuePair<TKey, TValue> Min` / `Max` |
| FreeCache | `void FreeCache()` |

### ValueTypeList\<T\>

| | |
|---|---|
| **位置** | `com.ember.basic/Base/ValueTypeList.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 值类型专用 List（`T : struct`）。与 `List<T>` 类似但提供 `GetRef(index)` 返回 ref 引用，支持零拷贝读写。 |

```csharp
var list = new ValueTypeList<Matrix4x4>(32);
list.Add(ref matrix);
ref var m = ref list.GetRef(0);  // 零拷贝引用
list.Sort((a, b) => ...);
```

| 关键成员 | 签名 |
|----------|------|
| Add | `void Add(ref T item)` |
| GetRef | `ref T GetRef(int index)` — 零拷贝 |
| Sort | `void Sort()` / `Sort(IComparer<T>)` / `Sort(Comparison<T>)` |
| BinarySearch | `int BinarySearch(T item)` |
| Remove / RemoveAt / RemoveRange | 标准 List 风格 |
| Reverse | `void Reverse()` / `Reverse(int index, int count)` |

### StringView

| | |
|---|---|
| **位置** | `com.ember.basic/Base/StringView.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 零分配子串视图。不复制字符，只记录起始位置和长度。支持 == 比较 string 和 StringView、忽略大小写比较、Substring 链式截取、零分配 Split。 |

```csharp
var view = new StringView("hello/world/file.txt");

// 零分配 Split
var parts = "a.b.c".SplitToStringViews('.');
// parts: [StringView("a"), StringView("b"), StringView("c")]

// 比较
if (view == "hello") { ... }
if (view.Equals("hello", ignoreCase: true)) { ... }

// 链式子串
var sub = view.Substring(6, 5);  // "world"
```

| 成员 | 签名 |
|------|------|
| ctor | `StringView(string str, int start, int length, bool ignoreCase=false, bool calcHash=false)` |
| Substring | `StringView Substring(int start, int length, bool calcHash=false)` — 零分配 |
| Equals | `bool Equals(string other)` / `bool Equals(StringView other)` |
| == / != | 支持 `StringView==StringView`, `StringView==string`, `string==StringView` |
| ToString | `override string ToString()` — **有 GC 分配** |
| indexer | `char this[int index]` |

### NativeDataView / NativeUDTView

| | |
|---|---|
| **位置** | `com.ember.basic/Unsafe/NativeDataTypes.cs` |
| **命名空间** | `Ember.Basic` |

`IntPtr` 是一个整数大小的指针，指着 C# 托管堆之外的某块原生内存。GC 完全不知道这块内存的存在，
必须自己管理生命周期。这两个结构体就是给 `IntPtr` 套一层有语义的壳：

- `NativeDataView` = IntPtr + Length + Managed（是否自己管释放）
- `NativeUDTView` = 纯 IntPtr（指向某个 C++ 对象的指针）

放在 `Unsafe/` 目录是因为 IntPtr 天然不安全——用错了就是硬崩溃，跟用没用 `unsafe` 关键字无关。

### SharedConst

| | |
|---|---|
| **位置** | `com.ember.basic/Utils/Const.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 程序里有些值被反复 new 几万次但永远不变。与其每次都 new，不如 new 一次全局共用。 |

```csharp
// ❌ 每次都 new
string[] result = new string[0];
string[] lines = text.Split(new[] { "\r\n", "\n" }, ...);

// ✅ 用预分配好的
string[] result = SharedConst.EmptyStringArray;
string[] lines = text.Split(SharedConst.LineSeparators, ...);
```

| 字段 | 场景 |
|------|------|
| `EmptyStringArray` | 方法返回空结果，替代 `new string[0]` |
| `EmptyUintArray` | uint 版本 |
| `ZeroUintArray` | 需要 `new uint[] { 0 }` 时 |
| `LineSeparators` | `string.Split` 按换行分割 |
| `SharedStringBuilder` | 临时拼字符串，**用前先 Clear** |
| `SharedStringBuilder2` | 备用，同上 |

StringBuilder 是共享可变对象——只在确定不会被并发访问的单线程场景下用。

### PerformanceLevel

| | |
|---|---|
| **位置** | `com.ember.basic/Base/PerformanceLevel.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 框架统一的设备性能五档分级枚举。画质分级、LOD 策略、特效密度、帧率目标等都基于此枚举做判断。 |

```csharp
var level = GraphicLevelUtils.GetCurrentLevel();
if (level >= PerformanceLevel.High) { EnableHighQualityEffects(); }
```

| 枚举值 | 说明 |
|--------|------|
| `VeryHigh` | 旗舰设备（如 iPhone 15 Pro、Adreno 750 + 12GB） |
| `High` | 高端设备（如 iPhone 13、Adreno 660 + 8GB） |
| `Mid` | 中端设备（如 iPhone 10、Adreno 620 + 6GB） |
| `Low` | 低端设备（如 iPhone 8、Adreno 512 + 4GB） |
| `VeryLow` | 入门设备（如 Mali-T、3GB 以下） |

---

## 存档

### DataSaver

| | |
|---|---|
| **位置** | `com.ember.basic/Runtime/DataSaver.cs` |
| **说明** | 基于 JsonUtility 的 JSON 存档工具，读写 `Application.persistentDataPath`。 |

```csharp
DataSaver.Save("settings.json", mySettings);
if (DataSaver.TryLoad《MySettings》("settings.json", out var data))
    ApplySettings(data);
DataSaver.Delete("settings.json");
bool exists = DataSaver.Exists("settings.json");
```

> 异步版本（UniTask）待迁移到 com.ember.extensions。

---

## 扩展方法

### 集合扩展 (`CollectionExtension`)

> 位置: `com.ember.basic/Extension/CollectionExtension.cs`, 命名空间 `Ember.Basic`

| 方法 | 说明 |
|------|------|
| `dict.ForEach((k,v) => ...)` | 零 GC 遍历 Dictionary |
| `e.ForEach(x => ...)` | 零 GC 遍历 IEnumerable |
| `e.ForEach((x,i) => ...)` | 带索引遍历 |
| `list.ParallelForEach(x => ...)` | 并行遍历，异常收集后统一抛出 |
| `e.ToHashSet()` | IEnumerable → HashSet |
| `e.JoinToString(",")` | 分隔符连接为字符串 |
| `obj.ConvertTo<T>()` | 类型转换（⚠️ 有装箱） |
| `dict.Add(key, value)` | `Dictionary<K,List<V>>` 专用：Key 不存在自动创建 List |
| `collection.IsNullOrEmpty()` | 集合空判 |
| `hashSet.AddRange(items)` | HashSet 批量添加 |
| `hashSet.RemoveAll(pred)` | HashSet 条件批量删除 |
| `dict.RemoveAll(keyPred)` | Dictionary 按 Key 条件批量删除 |
| `linkedList.RemoveAll(pred)` | LinkedList 条件批量删除 |

### 数学扩展 (`MathExtension`)

> 位置: `com.ember.basic/Extension/MathExtension.cs`, 命名空间 `Ember.Basic`

补上 Unity 自带 Mathf/AnimationCurve 没给的工具。

| 方法 | 说明 |
|------|------|
| `x.IsBetween(min, max)` | x 是否在区间内（不含边界），`[NoGC]` |
| `x.IsBetweenInclusive(min, max)` | 含边界版本，`[NoGC]` |
| `PointOnCircle(center, radius, angle)` | 圆上坐标，`[NoGC]` |
| `TrySolveQuadratic(a,b,c, out x1, out x2)` | 解一元二次方程。true=实数解，false=复数解，`[NoGC]` |
| `seconds.ToTimeString()` | 秒数 → "HH:MM:SS"，`[HasGC]` |
| `curve.EvaluateDerivative(t)` | 数值微分求导数，`[NoGC]` |
| `curve.CreateDerivativeCurve()` | 创建导数曲线，`[HasGC]` |

> `WrapAngle360` 已移除 → 用 `Mathf.Repeat(angle, 360f)`。
> `GetComponentsInChildrenDeep` 已移除 → 用 `GetComponentsInChildren<T>(true)`。

### 字符串扩展 (`StringExtension`)

> 位置: `com.ember.basic/Extension/StringExtension.cs`, 命名空间 `Ember.Basic`

**全部走 ASCII-only 路径，跳过 Unicode 全表映射。**

C# 自带的 `string.ToLower()` 会查 10 万+ 字符的 Unicode 大小写表，还需要考虑 CultureInfo、
Turkish I 问题等，代价很大。游戏中 99% 的字符串场景（日志标签、配置键、资源路径、事件名）
都是纯 ASCII，用这套方法比自带方法快一个数量级，且大部分标注了 `[NoGC]` 零分配。

核心方法 `ToAlphaLower(char)` 的实现就是一行位运算：`c >= 'A' && c <= 'Z' ? c + 32 : c`。
没有字典查表、没有 CultureInfo、没有分配。

| 方法 | 说明 |
|------|------|
| `str.IsNullOrEmpty()` | 同 `string.IsNullOrEmpty` |
| `str.IsEmpty()` | 是否为空白字符串 |
| `str.HasNonASCII()` | 是否包含非 ASCII 字符（c >= 255） |
| `sb.HasNonASCII()` | StringBuilder 版 |
| `str.ToAlphaLower()` | ASCII-only 小写，先检查是否有大写，无则直接返回原串避免分配 |
| `str.HasUpperChar(str)` | 是否包含大写 ASCII 字符 |
| `str.ContainsIgnoreCase(cmp)` | ASCII-only 忽略大小写 Contains，`[HasGC]`（内部分配） |
| `a.EqualsIgnoreCase(b)` | 忽略大小写相等判断 |
| `str.ToInt()` | 安全 int 解析，失败 = 0 |
| `path.ParseLowerCaseFilename(ref buf)` | 提取文件名转小写，复用 char 缓冲区，`[HasGC]` |
| `str.StartsWithIdx(cmp, startIdx, ignoreCase)` | 从指定位置比较前缀，**`[NoGC]`** |
| `str.EndsWithIdx(cmp, endIdx, ignoreCase)` | 到指定位置比较后缀，**`[NoGC]`** |
| `str.EndsWith(StringView)` | 与 StringView 比较后缀 |
| `str.SplitToStringViews('│')` | 零分配分割为 StringView 数组 |
| `str.SplitToStringViews(char[])` | 多字符分割 |

### GameObject / Component 扩展 (`GameObjectComponentExtensions`)

> 位置: `com.ember.extensions/Extension/GameObjectComponentExtensions.cs`, 命名空间 `Ember.Extensions`

| 方法 | 说明 |
|------|------|
| `obj.GetOrAddComponent<T>()` | 获取组件，不存在则自动添加。`[NoGC]` |
| `component.GetOrAddComponent<T>()` | 在同一 GameObject 上获取或添加组件。`[NoGC]` |

```csharp
// GameObject 版本
var rigidbody = obj.GetOrAddComponent<Rigidbody>();

// Component 版本（在已有组件所在 GameObject 上操作）
var collider = transform.GetOrAddComponent<BoxCollider>();
```

---

## 异步 STTask

> 位置: `com.ember.basic/Async/`, 命名空间 `Ember.Basic.Tasks`

**核心类型**：值类型 Task，零 GC 的 async/await 原语。

<h3>与 UniTask 的关系</h3>

STTask 和 UniTask 功能重叠但定位不同，跟事件系统（EmberEventBus vs UniRx）的分层策略一样：

| | STTask | UniTask |
|---|---|---|
| 谁用 | 框架内部 | 业务代码 |
| 规模 | 7 个文件，核心 ~200 行 | 完整库，几百个文件 |
| 特色能力 | await / FromResult / FromCanceled / CompletionSource | WhenAll / WhenAny / Delay / Yield / 协程桥接 |
| 分配 | 值类型 struct，已完成状态零分配 | struct 实现，操作符丰富 |
| 依赖 | 零（Unity 引擎除外），Compatible ember basic | 独立的 UniTask.dll |

<b>框架不依赖 UniTask</b>——保持 Core 零外部依赖的铁律。STTask 就是框架级的异步信号：
"这件事做完了通知我"。业务层用 UniTask 获取丰富的操作符（Delay/WhenAll/Yield 等）。

```csharp
// 框架内部用 STTask —— Manager 异步初始化
class EmberResourceManager {
    public STTask Initialize() {
        var tcs = new STTaskCompletionSource();
        _provider.Initialize(success => tcs.TrySetResult());
        return tcs.Task;
    }
}

// 业务层用 UniTask
async UniTaskVoid OnBattleStart() {
    await UniTask.Delay(1000);           // STTask 没有 Delay
    await UniTask.WhenAll(t1, t2, t3);  // STTask 没有 WhenAll
}
```

```csharp
// 创建已完成 Task
STTask<int> t1 = STTask.FromResult(42);
STTask t2 = STTask.CompletedTask;

// 手动控制完成
var tcs = new STTaskCompletionSource<int>();
StartAsyncWork(result => tcs.TrySetResult(result));
int val = await tcs.Task;

// 异常 / 取消
tcs.TrySetException(new Exception("fail"));
tcs.TrySetCanceled();
```

| 类型 | 说明 |
|------|------|
| `STTask` | 值类型 partial struct，无返回值 |
| `STTask<T>` | 值类型 struct，有返回值。支持 `implicit operator STTask` 转换 |
| `STTaskCompletionSource` | class，手动控制 STTask 完成 |
| `STTaskCompletionSource<T>` | class，手动控制 STTask\<T\> 完成 |
| `IAwaiter` | 接口，无返回值 |
| `IAwaiter<T>` | 接口，有返回值 |
| `AwaiterStatus` | 枚举：`Pending / Succeeded / Faulted / Canceled` |
| `STAsyncTaskMethodBuilder` | struct，编译器构建器（无返回值） |
| `STAsyncTaskMethodBuilder<T>` | struct，编译器构建器（有返回值） |

**工厂方法** (`STTask` 静态方法)：
- `STTask.FromResult(T value)` / `STTask.CompletedTask`
- `STTask.FromException(Exception)` / `STTask.FromException<T>(Exception)`
- `STTask.FromCanceled()` / `STTask.FromCanceled(CancellationToken)`
- `STTask.FromCanceled<T>()` / `STTask.FromCanceled<T>(CancellationToken)`

---

## JSON

> 位置: `com.ember.basic/LitJson/`, 命名空间 `Ember.Basic.LitJson`
> LitJSON 库 (public domain)，完整 JSON 库，8 个文件。

<h3>与 Unity JsonUtility 的区别</h3>

Unity 内置的 `JsonUtility` 只能处理"提前定义好 class 结构"的对象，不支持 Dictionary、
不支持动态字段。LitJson 补上这些缺口：

| 场景 | 用什么 |
|------|--------|
| 简单对象序列化（已知结构） | `JsonUtility` |
| JSON 结构不固定、需要动态访问 | `JsonData` |
| 大文件、逐 Token 读取 | `JsonReader` / `JsonWriter` |

<h3>常用 API</h3>

```csharp
// 对象 ↔ JSON
string json = JsonMapper.ToJson(myObj);
MyType obj = JsonMapper.ToObject《MyType》(jsonString);

// 动态解析 —— 不提前定义类，像字典一样访问
JsonData data = JsonMapper.ToObject(jsonString);
string name = (string)data["name"];
int age = (int)data["age"];
foreach (JsonData item in data["items"]) { ... }

// 流式读取 —— 大文件逐 Token 处理
var reader = new JsonReader(jsonString);
while (reader.Read()) {
    if (reader.Token == JsonToken.PropertyName) { ... }
}
```

| 类型 | 说明 |
|------|------|
| `JsonMapper` | JSON ↔ 对象互转（最常用，静态方法） |
| `JsonData` | 动态 JSON 数据，索引访问，隐式类型转换 |
| `JsonReader` | 流式读取 JSON |
| `JsonWriter` | 流式写入 JSON（支持 PrettyPrint 格式化） |
| `IJsonWrapper` | JSON 数据统一接口 |
| `JsonType` | enum: `None/Object/Array/String/Int/Long/Double/Boolean` |
| `JsonException` | JSON 解析异常 |

内部实现文件（不需要直接使用）：`Lexer`、`ParserToken`、`JsonMockWrapper`、`Netstandard15Polyfill`

---

## Unsafe / 原生内存

> 这些工具绕过 C# 内存安全，直接操作指针或原生内存。**日常写游戏逻辑用不到**，底层网络/资源/序列化才用。

| 类型 | 位置 | 说明 |
|------|------|------|
| `UnsafeStringExtensions` | `com.ember.basic/Unsafe/UnsafeString.cs` | UTF-8 字节流直接写入 string 内部缓冲区，绕过 `Encoding.UTF8.GetString` 的中间分配 |
| `NativeDataView` | `com.ember.basic/Unsafe/NativeDataTypes.cs` | IntPtr + Length + Managed，给原生内存指针套一层语义壳 |
| `NativeUDTView` | `com.ember.basic/Unsafe/NativeDataTypes.cs` | 纯 IntPtr 视图，指向某个 C++ 对象 |

UnsafeStringExtensions 最危险的一行：`*((int*)dest - 1) = destIdx` —— 直接覆盖了 .NET 运行时 string 对象的内部长度字段。
正常 C# 里 string 是不可变的，永远不能改。搞错了会把运行时搞崩。

```csharp
// 零分配 UTF-8 解码 —— 仅底层网络/资源层用
unsafe {
    byte* utf8Bytes = ...;
    int charCount = UnsafeStringExtensions.Utf8Length(utf8Bytes);
    var str = new string('\0', charCount);                     // 先 new 空壳
    str.CopyFromUTF8ByteBuffer(utf8Bytes, sizeInBytes);       // 指针灌数据
}
```

> 正常业务代码用 `Encoding.UTF8.GetString()` 就行，不要碰这些。

---

## 加密与哈希

> 位置: `com.ember.basic/Utils/CryptographyUtils.cs`, 命名空间 `Ember.Basic`

### CryptographyUtils

CRC32C、MD5、Base64、XOR 混淆等常用算法的静态工具类。所有方法标注了 `[NoGC]` / `[HasGC]`。

```csharp
// CRC32C
int crc = CryptographyUtils.ComputeCrc32("hello");
int crc2 = CryptographyUtils.ComputeCrc32(bytes);

// MD5
string md5 = CryptographyUtils.GetMD5("hello");
string md5File = CryptographyUtils.GetMD5File("path/to/file.bundle");

// Base64
string b64 = CryptographyUtils.EncodeBase64("hello");
string decoded = CryptographyUtils.DecodeBase64(b64);

// XOR 混淆（原地修改，非加密）
int reserve = CryptographyUtils.Obfuscate(ref seed, data, offset: 0);

// 字节数组 → hex 字符串
string hex = CryptographyUtils.ArrayToHexString(bytes);
```

| 方法 | 说明 | GC |
|------|------|-----|
| `ComputeCrc32(string)` | 字符串 CRC32C（UTF-8 编码） | `[HasGC]` |
| `ComputeCrc32(byte[])` | 字节数组 CRC32C | `[NoGC]` |
| `ComputeCrc32(byte[], int offset, int length)` | 指定范围 CRC32C | `[NoGC]` |
| `GetMD5(string)` | 字符串 MD5，返回小写 hex | `[HasGC]` |
| `GetMD5(byte[])` | 字节数组 MD5，返回小写 hex | `[HasGC]` |
| `GetMD5File(string)` | 文件 MD5，用 FileStream 读取 | `[HasGC]` |
| `EncodeBase64(string)` / `EncodeBase64(byte[])` | Base64 编码 | `[HasGC]` |
| `DecodeBase64(string)` | Base64 解码为 UTF-8 字符串 | `[HasGC]` |
| `ArrayToHexString(byte[])` | 字节数组 → 小写 hex 字符串 | `[HasGC]` |
| `Obfuscate(ref int seed, byte[] data, int offset=0)` | XOR 混淆（原地修改），返回 reserve | `[NoGC]` |

> CRC32C 使用 Castagnoli 多项式（0x82F63B78）查表法，与 Java CRC32C 行为一致。
> MD5 已从 `MD5CryptoServiceProvider` 迁移为 `MD5.Create()`（兼容 .NET 5+）。
> **Obfuscate 不是加密算法**，不可用于安全敏感场景。

---

## 性能分级

> 位置: `com.ember.basic/Utils/GraphicLevelUtils.cs` + `com.ember.basic/Base/PerformanceLevel.cs`, 命名空间 `Ember.Basic`

### GraphicLevelUtils

自动检测手机 GPU / CPU / RAM，映射到 `PerformanceLevel` 五档（VeryHigh → VeryLow）。
检测结果缓存在 PlayerPrefs 中，后续启动直接读取。

```csharp
// 确保已初始化（首次调用时自动检测并缓存）
GraphicLevelUtils.EnsurePhoneLevelInitialized();

// 查询
var level = GraphicLevelUtils.GetCurrentLevel();
bool isHigh = GraphicLevelUtils.IsHighOrHighestPhone();
bool isFlagship = GraphicLevelUtils.IsHighestPhone();
bool isEntry = GraphicLevelUtils.IsLowestPhone();

// 帧率
GraphicLevelUtils.SetFrameRatePrefs(60);
int fps = GraphicLevelUtils.GetFrameRatePrefs(60);
```

检测策略：

| 平台 | 方法 | 依据 |
|------|------|------|
| iOS | iPhone/iPad 代数 | iPhone 15+ → VeryHigh, iPhone 10-12 → Mid |
| Android | GPU 型号数据库 | Adreno 740 / Mali-G715 / Maleoon 910 等 30+ 款 |
| Fallback | RAM + CPU | 12GB+2.5GHz → VeryHigh, 3GB以下 → VeryLow |

| 方法 | 说明 |
|------|------|
| `EnsurePhoneLevelInitialized()` | 检测并缓存手机档位（已缓存则跳过） |
| `GetCurrentLevel()` | 返回 `PerformanceLevel` 枚举值 |
| `IsHighOrHighestPhone()` | High 或 VeryHigh |
| `IsHighestPhone()` | VeryHigh |
| `IsLowestPhone()` | VeryLow |
| `SetFrameRatePrefs(int)` / `GetFrameRatePrefs(int)` | 帧率设置 |
| `GetGraphicLevel(Action<int,int>)` | 协程：综合硬件档位+画质设置，回调返回有效档位和帧率 |

> TODO: GPU 型号阈值（Adreno / Mali / Maleoon / PowerVR 等 30+ 款）计划提取为 `EmberPerformanceConfigSO`
> ScriptableObject，使规则可在 Inspector 编辑。

### PerformanceLevel

五档枚举：`VeryHigh`(0) → `High`(1) → `Mid`(2) → `Low`(3) → `VeryLow`(4)。
详见 [基础数据结构](#基础数据结构)。

---

## 标记 Attribute

| Attribute | 命名空间 | 说明 |
|-----------|---------|------|
| `[HasGC]` | `Ember.Basic` | 标记会产生 GC 分配 |
| `[NoGC]` | `Ember.Basic` | 标记零 GC 分配 |
| `[ForTest]` | `Ember.Basic` | 仅供测试 |
| `[ForDebug]` | `Ember.Basic` | 仅供调试 |
| `[Legacy]` | `Ember.Basic` | 遗留代码，计划移除 |
| `[EmberInitOrder(order)]` | `Ember.Core` | 指定 Manager 初始化顺序 |
| `[Il2CppEagerStaticClassConstruction]` | `Unity.IL2CPP.CompilerServices` | IL2CPP polyfill |
| `[Il2CppSetOption(option, value)]` | `Unity.IL2CPP.CompilerServices` | IL2CPP polyfill |
| `[AsyncMethodBuilder(typeof(...))]` | `System.Runtime.CompilerServices` | async/await polyfill |
| `[DisplayFirstElementInHeader]` | `Ember.Basic` | Inspector 中数组 foldout 用第一个子元素的值当标题 |

**EmberInitOrder 预设值**：`Core=100, Resource=200, Audio=300, Input=400, UI=500, Scene=600, Game=700, Default=1000`

---

## 事件系统

> 位置: `Ember/Core/Runtime/Event/`, 命名空间 `Ember.Core`

### EmberEventBus

全局事件总线。int-key + 0~4 个泛型参数。Subscribe 返回 `IDisposable`，Unsubscribe 或 Dispose 均可取消。延迟操作队列保证遍历安全。

```csharp
// 订阅 — 返回 IDisposable
IDisposable sub = EmberEventBus.Subscribe(EmberBroadcastEvent.SceneLoaded, OnSceneLoaded);
EmberEventBus.Subscribe<int>(MyEvents.PlayerDied, score => { ... });

// 播报
EmberEventBus.OnNext(EmberBroadcastEvent.ResourceReady);
EmberEventBus.OnNext(MyEvents.PlayerDied, 100);

// 取消
sub.Dispose();  // 或 EmberEventBus.Unsubscribe(key, handler);

// 查询
bool has = EmberEventBus.HasSubscribers(key);
```

| 方法 | 说明 |
|------|------|
| `Subscribe(key, Action)` | 无参订阅，返回 IDisposable |
| `Subscribe<T>(key, Action<T>)` | 1 参数 |
| `Subscribe<T1,T2>(key, Action<T1,T2>)` | 2 参数 |
| `Subscribe<T1,T2,T3>(key, Action<T1,T2,T3>)` | 3 参数 |
| `Subscribe<T1,T2,T3,T4>(key, Action<T1,T2,T3,T4>)` | 4 参数 |
| `OnNext(key)` / `OnNext<T>(key, arg)` | 同步播报 |
| `Unsubscribe(key, handler)` | 取消订阅 |
| `HasSubscribers(key)` | 是否有订阅者 |
| `ClearSubscribers(key)` / `ClearAllSubscribers()` | 清空 |

### EmberBroadcastEvent

框架广播事件 Key 常量表。按模块分配区间，间隔 1000：

| 常量 | 值 | 含义 |
|------|----|------|
| `CoreReady` | 1001 | Core 初始化完成 |
| `CoreShutdown` | 1002 | Core 退出 |
| `GameStateChanged` | 1003 | 游戏状态切换 |
| `InitSceneReady` | 1004 | Init 场景加载完毕 |
| `InitAnimationDone` | 1005 | 启动动画播放完毕 |
| `ResourceReady` | 2001 | Resource 初始化完成 |
| `ResourceShutdown` | 2002 | Resource 退出 |
| `UIReady` | 3001 | UI 初始化完成 |
| `UIShutdown` | 3002 | UI 退出 |
| `SceneLoaded` | 4001 | 场景加载完毕 |
| `SceneLoadStart` | 4003 | 场景开始加载 |
| `SceneLoadDone` | 4004 | 场景加载完成 |
| `SceneUnloading` | 4002 | 场景即将卸载 |
| `AudioReady` | 5001 | Audio 初始化完成 |
| `AudioShutdown` | 5002 | Audio 退出 |
| `InputReady` | 6001 | Input 初始化完成 |
| `InputShutdown` | 6002 | Input 退出 |

---

## 服务定位 & 单例

### EmberSingleton\<T\> / EmberMonoSingleton\<T\>

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/Service/EmberSingleton.cs` |
| **命名空间** | `Ember.Core` |

| 类型 | 说明 |
|------|------|
| `EmberSingleton<T>` | 纯 C# 单例基类（`T : class, new()`）。线程安全，懒初始化。`Instance` / `IsValid` / `Destroy()` |
| `EmberMonoSingleton<T>` | MonoBehaviour 单例。**无** DontDestroyOnLoad |
| `EmberMonoSingletonDontDestroy<T>` | MonoBehaviour 单例。**有** DontDestroyOnLoad |

```csharp
var mgr = EmberSingleton<MyManager>.Instance;
bool ok = EmberSingleton<MyManager>.IsValid;
EmberSingleton<MyManager>.Destroy();

// MonoBehaviour 版本
var mgr = EmberMonoSingleton<MyMono>.Instance;
```

### EmberServiceLocator

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/Service/EmberServiceLocator.cs` |
| **说明** | 轻量 DI：接口→实现映射。支持即时注册和延迟工厂。 |

```csharp
// 注册
EmberServiceLocator.Register<IMyService>(new MyService());
EmberServiceLocator.RegisterLazy<IMyService>(() => new MyService());

// 解析
var svc = EmberServiceLocator.Resolve<IMyService>();
var svc = EmberServiceLocator.TryResolve<IMyService>();  // null if not registered

// 查询 & 移除
bool ok = EmberServiceLocator.IsRegistered<IMyService>();
EmberServiceLocator.Unregister<IMyService>();
EmberServiceLocator.ClearAll();
```

### EmberBaseSO

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/Service/EmberBaseSO.cs` |
| **说明** | 带继承溯源面板的 ScriptableObject 基类。创建 SO 时继承此类。 |

---

## 日志

> 位置: `Ember/Core/Runtime/Debug/`, 命名空间 `Ember.Basic`
> **规则**: 禁止直接用 `Debug.Log`，全部走 `EmberDebug`。

### EmberDebug

```csharp
private const string TAG = LogTags.CoreEventBus;

EmberDebug.Log(TAG, "常规消息");          // 白色
EmberDebug.LogInit(TAG, "初始化完成");     // 绿色
EmberDebug.LogEvent(TAG, "事件播报");      // 紫色
EmberDebug.LogCleanup(TAG, "清理资源");    // 灰色
EmberDebug.LogShutdown(TAG, "框架退出");   // 淡紫色
EmberDebug.LogWarning(TAG, "警告");        // 白+黄底
EmberDebug.LogError(TAG, "错误");          // 白+红底（不受开关控制）

// 过滤
EmberDebug.Disable(LogTags.Audio);          // 父标签关闭 → 所有子标签静默
EmberDebug.Enable(LogTags.CoreEventBus);    // 只开子标签
EmberDebug.GlobalOpen = false;              // 全关（Error 除外）
```

### LogTags

两级标签体系（`Parent.Child`），按模块+组件组织：

| 父标签 | 子标签 |
|--------|--------|
| `EmberBasic` | `BasicCrypto`, `BasicPerformance`, `BasicAppQuit`（Editor 工具用动态标签 `EmberBasic.ToolName`，由 autoCollect 自动收集） |
| `EmberCore` | `CoreEventBus`, `CoreServiceLocator`, `CoreSingleton`, `CoreObjectPool`, `CoreManagerCollector`, `CoreUpdateManager`, `CoreStateMachine`, `CoreGameLauncher`, `CoreCameraManager`, `CoreEditor` |
| `EmberResource` | `ResourceManager`, `ResourceProvider` |
| `EmberUI` | `UIManager` |
| `EmberScene` | `SceneManager` |
| `EmberAudio` | `AudioManager` |
| `EmberInput` | `InputManager` |
| `Game` | — |

```csharp
string parent = LogTags.GetParent(LogTags.CoreEventBus); // "EmberCore"
var allTags = LogTags.All;  // 所有标签的 HashSet
```

### EmberFileLog

文件日志持久化。后台线程异步将 EmberDebug 日志写入 `.log` 文件。

```csharp
// 生命周期（在 GameLauncher 中调用）
EmberFileLog.Start();    // 启动文件日志（Awake）
EmberFileLog.Stop();     // 停止并刷写（OnDestroy）
bool running = EmberFileLog.IsRunning;

// 上传（事件 / 接口二选一）
EmberFileLog.OnLogFileReady += path => { /* 上传到服务端 */ };

public class MyLogUploader : IEmberLogUploader
{
    public void Upload(string filePath) { /* 实现上传逻辑 */ }
}
```

日志文件输出纯文本格式：`HH:mm:ss.fff [LEVEL] [TAG] message (at path:line)`。
LEVEL 缩写：`I`/`N`/`V`/`C`/`S`/`W`/`E`（对应 Info/Init/Event/Cleanup/Shutdown/Warning/Error）。

### IEmberLogUploader

```csharp
public interface IEmberLogUploader
{
    void Upload(string filePath);
}
```

框架不内置上传实现。业务层实现此接口，配合 `EmberFileLog.OnLogFileReady` 事件使用。

### EmberDebugConfigSO（文件日志字段）

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `enableFileLog` | `bool` | `true` | 启用文件日志 |
| `logDirectory` | `string` | `""` | 空则自动：Editor=`{项目}/Logs/ember/`，Build=`persistentDataPath/logs/` |
| `maxFileSizeMB` | `int` | `10` | 单文件最大 MB |
| `maxFileCount` | `int` | `5` | 最多保留文件数 |
| `retentionDays` | `int` | `30` | 保留天数 |

---

## 状态机

> 位置: `Ember/Core/Runtime/State/`, 命名空间 `Ember.Core`

### EmberStateMachine

```csharp
var fsm = new EmberStateMachine();

// 注册状态
fsm.Register(new InitState());
fsm.Register(new MainState());
fsm.Register(new GameplayState());

// 场景加载委托
fsm.LoadSceneAsync = (sceneName, onComplete) => {
    EmberSceneManager.Instance.LoadSceneAsync(sceneName, onComplete);
};

// 场景切换钩子
fsm.OnSceneTransition = ctx => {
    if (ctx.FromScene != ctx.ToScene)
        EmberSceneManager.Instance.TransitionTo(ctx.ToScene, ctx.FromScene, ctx.Proceed);
    else ctx.Proceed();
};

// 启动
fsm.Start<InitState>();            // 自动: Init → Main
fsm.TransitionTo<GameplayState>();  // 替换式: Main → Gameplay
fsm.Push<SettingsState>();          // 覆盖式: 暂停当前，打开设置
fsm.Pop();                          // 关闭设置，恢复

// 查询
fsm.Is<GameplayState>();            // 当前是否 Gameplay
var state = fsm.GetState<MainState>();
```

| 方法 | 说明 |
|------|------|
| `Register(state)` | 注册状态 |
| `Unregister<T>()` | 注销（不能删 IsRequired 的状态） |
| `Start<T>(args)` | 启动状态机 |
| `TransitionTo<T>(args, skipSceneLoad)` | 替换式切换 |
| `Push<T>(args)` | 覆盖式（暂停当前） |
| `Pop()` | 恢复上一个 |
| `Current` / `Previous` | 当前/上一个状态 |
| `OnStateChanged` | event `Action<EmberGameState, EmberGameState>` |

### EmberGameState（抽象基类）

| 虚成员 | 说明 |
|--------|------|
| `Name` | 状态名称 |
| `Description` | 状态描述（编辑器展示用） |
| `IsRequired` | 是否必需状态（不可注销） |
| `AllowReEnter` | 是否允许重入 |
| `ScenePath` | 关联的场景路径 |
| `OnEnter(args)` / `OnExit()` | 进入/退出 |
| `OnUpdate()` | 每帧 |
| `OnPause()` / `OnResume()` | 暂停/恢复 |
| `GetTransitions()` | 返回 TransitionDescriptor 数组（编辑器用） |
| `GetPushTargets()` | 返回 Push 目标（编辑器用） |

### 内置状态

| 状态 | IsRequired | 说明 |
|------|------------|------|
| `InitState` | ✅ | 初始化所有 Manager → 广播 CoreReady → 自动 TransitionTo MainState |
| `MainState` | ✅ | 大厅/主界面。子类 override `OnMainEnter` / `OnMainExit` |
| `GameplayState` | ✅ | 核心玩法。子类 override `OnGameplayEnter/Exit/Update/Pause/Resume` |
| `SettingsState` | ❌ | Push 模式设置界面。`SettingsContext` 枚举区分上下文 |

### TransitionDescriptor

声明状态流转目标（编辑器+运行时校验用）：
```csharp
new TransitionDescriptor(typeof(MainState), "返回大厅", "player clicks back", () => true)
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `TargetState` | `Type` | 目标状态类型 |
| `Label` | `string` | UI 标签 |
| `Condition` | `string` | 条件描述（编辑器展示） |
| `Guard` | `Func<bool>` | 运行时守卫 |

---

## Update 循环

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/Update/`, 命名空间 `Ember.Core` |

### 接口

```csharp
public interface IEmberUpdate      { void Update(); }
public interface IEmberLateUpdate  { void LateUpdate(); }
public interface IEmberFixedUpdate { void FixedUpdate(); }
```

实现任一接口 + `[EmberInitOrder]` → 自动被 `EmberUpdateManager` 扫描并每帧驱动。

### EmberUpdateManager

纯 C# 类（无 MonoBehaviour），反射扫描所有实现者，统一驱动。

```csharp
EmberUpdateManager.Instance.DoUpdate();       // 由 GameLauncher Update 调用
EmberUpdateManager.Instance.DoLateUpdate();   // 由 GameLauncher LateUpdate 调用
EmberUpdateManager.Instance.DoFixedUpdate();  // 由 GameLauncher FixedUpdate 调用
```

---

## Manager 自动发现

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/Manager/`, 命名空间 `Ember.Core` |

### IEmberManager（框架管道）

```csharp
public interface IEmberManager {
    void Init();     // 启动时由 EmberManagerCollector 调用
    void Destroy();  // 退出时逆序调用
}
```

实现此接口 + `[EmberInitOrder]` → 自动扫描并初始化。

### IEmberModule（业务模块）

```csharp
public interface IEmberModule {
    int Phase { get; }           // 所属阶段（Login=1, Gameplay=2, ...）
    void OnInit();               // 状态机驱动的初始化
    void OnDestroy();            // 状态机驱动的销毁
    void ResetModuleData();      // 热重启复用
}
```

两者**平行不继承**——Collector 只扫 IEmberManager，ModuleCollector（待实现）只扫 IEmberModule。

### EmberManagerCollector

```csharp
EmberManagerCollector.Instance.InitializeAll();  // 反射扫描 → 排序 → 依次 Init
EmberManagerCollector.Instance.DestroyAll();     // 逆序 Destroy
int count = EmberManagerCollector.Instance.ManagerCount;
```

---

## 启动器

### GameLauncher

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/GameLauncher.cs` |
| **基类** | `EmberMonoSingleton<GameLauncher>`（无 DontDestroyOnLoad，由 FrameworkScene 保活） |
| **说明** | 框架集中入口：驱动 Manager 初始化 → 状态机 → Update 循环 |

```csharp
GameLauncher.Instance.Fsm             // EmberStateMachine
GameLauncher.Instance.IsInitialized   // 是否初始化完毕
GameLauncher.Instance.UIRoot          // UI 根节点
GameLauncher.Instance.UICamera        // UI 相机
GameLauncher.Instance.MainCamera      // 主相机

// 子类重写以定制状态机
protected virtual void ConfigureStateMachine(EmberStateMachine fsm) { ... }
```

---

## 资源管理

> 位置: `Ember/Resource/Runtime/`, 命名空间 `Ember.Resource`

### EmberResourceManager

```csharp
// 初始化
EmberResourceManager.Instance.Initialize(new ResourcesProvider(), success => { ... });

// 加载
EmberResourceManager.Instance.LoadAssetAsync<Sprite>("ui/icon", sprite => { ... });
EmberResourceManager.Instance.LoadSceneAsync("Battle");

// 卸载
EmberResourceManager.Instance.UnloadAsset("ui/icon");
EmberResourceManager.Instance.UnloadUnusedAssets();

bool ready = EmberResourceManager.Instance.IsInitialized;
float prog = EmberResourceManager.Instance.Progress;
```

### IResourceProvider（接口）

```csharp
public interface IResourceProvider {
    float Progress { get; }
    void Initialize(Action<bool> onComplete);
    void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : Object;
    void LoadSceneAsync(string sceneName, Action onComplete);
    void UnloadAsset(string path);
    void UnloadUnusedAssets();
}
```

### ResourcesProvider（默认实现）

Unity Resources API 实现。开发/小项目用，正式项目替换为 AddressablesProvider 或 YooAssetProvider。

---

## UI 管理

> 位置: `Ember/UI/Runtime/`, 命名空间 `Ember.UI`
> ⚠️ **此模块处于结构性重写中（2026-08-04 启动），API 即将变化。**

### EmberUIManager（当前版本）

```csharp
// 打开页面
EmberUIManager.Instance.Push(GamePages.Settings, args: null);

// 关闭
EmberUIManager.Instance.Pop(UILayer.Popup);
EmberUIManager.Instance.CloseAll();

// 查询
var view = EmberUIManager.Instance.GetTopView((int)UILayer.Normal);
bool has = EmberUIManager.Instance.HasView((int)UILayer.Popup);
```

### IUIView

```csharp
public interface IUIView {
    void OnOpen(object args);   // 首次展示
    void OnClose();             // 被关闭
    void OnPause();             // 被覆盖
    void OnResume();            // 恢复可见
}
```

### PageDef / UILayer

```csharp
public class PageDef {
    public string PrefabPath { get; }
    public int Layer { get; }
    public PageDef(string prefabPath, int layer);
}

public enum UILayer { Background=0, Normal=100, Popup=200, TopMost=300 }
```

---

## 场景管理

> 位置: `Ember/Scene/Runtime/`, 命名空间 `Ember.Scene`

### EmberSceneManager

```csharp
var mgr = EmberSceneManager.Instance;

// 叠加加载
mgr.LoadSceneAsync("Battle", () => Debug.Log("done"));

// 切换
mgr.TransitionTo("BattleScene", "MainMenu");

// 激活前回调
mgr.OnBeforeActivate += (scene, activate) => { ...; activate(); };

// 查询
bool loading = mgr.IsLoading;
float prog = mgr.Progress;
string current = mgr.CurrentScene;
```

---

## 音频管理

> 位置: `Ember/Audio/Runtime/`, 命名空间 `Ember.Audio`

### EmberAudioManager

```csharp
var audio = EmberAudioManager.Instance;
audio.Init(mixer);                    // 传 AudioMixer
audio.PlayBGM(bgmClip, loop: true);
audio.StopBGM();
audio.PlaySFX(sfxClip);
audio.SetBGMVolume(0.8f);
audio.SetSFXVolume(1.0f);
```

---

## 输入管理

> 位置: `Ember/Input/Runtime/`, 命名空间 `Ember.Input`

### EmberInputManager

```csharp
var input = EmberInputManager.Instance;
input.Init(inputActionAsset, defaultMap: "Gameplay");
input.SwitchMap("UI");                  // 切换到 UI 模式

var move = input.GetAxis("Move");
bool jump = input.IsPressed("Jump");
var action = input.GetAction("Attack");
```

---

## 相机管理

> 位置: `Ember/Camera/Runtime/`, 命名空间 `Ember.Camera`

### EmberCameraManager

```csharp
var cam = EmberCameraManager.Instance;

// 注册虚拟相机
cam.Register("follow", followVcam);
cam.Switch("follow");                         // 切换
cam.Switch("overview", force: true);          // 强制切换

// 覆盖模式（对话、特写等临时切换）
cam.PushOverride("closeup", localCamera);     // 压入覆盖
cam.PopOverride("follow");                    // 弹出恢复

// 锁定（防止其他系统切换相机）
cam.Lock();
cam.Unlock();

// 查询
bool locked = cam.IsLocked;
int stack = cam.OverrideStackCount;
var active = cam.ActiveCamera;
```

---

## Editor 工具（Editor-only）

> 这些只在 `#if UNITY_EDITOR` 下编译，运行时不可用。

### FileEncodingUtility

| | |
|---|---|
| **位置** | `com.ember.basic/Editor/FileEncodingUtility.cs` |
| **说明** | UTF-8 BOM 检测和转换。外部导入的脚本可能是 ANSI/GBK 编码导致中文乱码，用这个工具检测和批量转换。 |

```csharp
bool hasBom = FileEncodingUtility.HasBOM("path/to/script.cs");
FileEncodingUtility.ConvertToUTF8BOM("path/to/script.cs");
```

### DisplayFirstElementInHeaderDrawer

| | |
|---|---|
| **位置** | `com.ember.basic/Editor/DisplayFirstElementInHeaderDrawer.cs` |
| **说明** | `[DisplayFirstElementInHeader]` 的 PropertyDrawer。 |

### ProjectLocalPrefs

| | |
|---|---|
| **位置** | `com.ember.basic/Editor/Utils/ProjectLocalPrefs.cs` |
| **命名空间** | `Ember.Basic.Editor` |
| **说明** | Editor-only 的 JSON 文件持久化 key-value 存储。数据保存在 `{ProjectRoot}/Library/EmberLocalPrefs/prefs.json`，不污染 Assets 目录。支持数据迁移回调。 |

```csharp
// 读写
ProjectLocalPrefs.SetString("LastExportPath", "Assets/Game/");
var path = ProjectLocalPrefs.GetString("LastExportPath", "Assets/");

// 从旧 key 迁移数据
var val = ProjectLocalPrefs.GetString("NewKey", "", () => EditorPrefs.GetString("OldKey"));

// 其他类型
ProjectLocalPrefs.SetInt("TabIndex", 2);
int idx = ProjectLocalPrefs.GetInt("TabIndex", 0);

ProjectLocalPrefs.SetFloat("Scale", 1.5f);
ProjectLocalPrefs.SetBool("ShowAdvanced", true);

// 删除
ProjectLocalPrefs.DeleteKey("TabIndex");
ProjectLocalPrefs.DeleteAll();
```

| 方法 | 说明 |
|------|------|
| `GetString(key, default, migrateProvider?)` | 读取字符串，支持迁移回调 |
| `SetString(key, value)` | 写入字符串 |
| `GetInt(key, default, migrateProvider?)` / `SetInt(key, value)` | 读写整数 |
| `GetFloat(key, default, migrateProvider?)` / `SetFloat(key, value)` | 读写浮点数（Round-trip 格式） |
| `GetBool(key, default, migrateProvider?)` / `SetBool(key, value)` | 读写布尔值 |
| `DeleteKey(key)` | 删除指定 key |
| `DeleteAll()` | 清空所有存储 |

> 与 `EditorPrefs` 的区别：EditorPrefs 存 Windows 注册表（换机器丢失），ProjectLocalPrefs 存项目 `Library/` 下（可 Git 管理、手动编辑 JSON）。

---

## 其他接口

### IUpdater / IDelayDisposable

| | |
|---|---|
| **位置** | `com.ember.basic/Resource/IUpdater.cs` |
| **命名空间** | `Ember.Basic` |

```csharp
public interface IUpdater {
    bool Update();             // 每帧调用，返回 true 表示已失效
    bool PreOrPostAsyncList;   // 在异步加载列表之前还是之后
    int Priority;              // 优先级（越小越高）
}

public interface IDelayDisposable : IDisposable {
    bool NotYet();  // true = 还不能释放，等
}
```

### ApplicationQuitUtil

| | |
|---|---|
| **位置** | `com.ember.basic/Utils/ApplicationQuitUtil.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | 应用退出工具。Android 上先通过 `android.os.Process.killProcess` 杀进程，失败时回退到 `Application.Quit()`。 |

```csharp
ApplicationQuitUtil.Quit(); // 替代 Application.Quit()
```

### UrlUtils

| | |
|---|---|
| **位置** | `com.ember.basic/Utils/UrlUtils.cs` |
| **命名空间** | `Ember.Basic` |
| **说明** | URL 编解码、路径提取、URL 拼接工具。编码基于 `Uri.EscapeDataString`，符合 RFC 3986。 |

```csharp
var encoded = UrlUtils.UrlEncode("hello world");       // "hello%20world"
var decoded = UrlUtils.UrlDecode("hello%20world");     // "hello world"
var name    = UrlUtils.GetFileName("path/to/file.png"); // "file"
var url     = UrlUtils.CombineUrl("http://host", "api"); // "http://host/api"
url         = UrlUtils.EnsureTrailingSlash("http://h");  // "http://h/"
url         = UrlUtils.AppendRandomVersion("http://h/api"); // "http://h/api?v=0.314159"
var rel     = UrlUtils.GetRelativePath("C:/a/b/c.txt", "C:/a/"); // "b/c.txt"
```

| 方法 | 说明 | GC |
|------|------|-----|
| `UrlEncode(string)` / `UrlDecode(string)` | percent-encoding / decoding | `[HasGC]` |
| `GetFileName(string url)` | 从 URL 提取文件名（不含扩展名） | `[HasGC]` |
| `GetRelativePath(string file, string folder)` | 计算相对路径 | `[HasGC]` |
| `EnsureTrailingSlash(string)` | 确保以 "/" 结尾 | `[HasGC]` |
| `CombineUrl(string base, string relative)` | 安全拼接两个 URL 片段 | `[HasGC]` |
| `AppendRandomVersion(string)` | 追加随机参数破坏缓存 | `[HasGC]` |

### EmberSceneField

| | |
|---|---|
| **位置** | `Ember/Core/Runtime/EmberSceneField.cs` |
| **说明** | 可拖拽的场景引用。在 Inspector 中拖 .unity 文件替代手写字符串。隐式转换 string。 |

```csharp
[SerializeField] private EmberSceneField _battleScene;
// Inspector 中拖拽场景文件
string path = _battleScene;  // 隐式转换
```
