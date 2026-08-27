using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 框架定时器管理器 —— 集中管理延时/周期回调，统一取消。
    ///
    /// 核心设计：
    /// - <b>int-ID 登记表</b>：每个定时器返回唯一 int ID（永不返回 0，0 视为无效），用 Cancel(id) 取消
    /// - <b>时间源来自 EmberTimeManager</b>：逻辑时间（受 TimeScale/Pause 影响）或真实时间（不受影响），由 useLogicTime 参数选择
    /// - <b>由 EmberUpdateManager 自动驱动</b>：无需挂 MonoBehaviour、无需手动 Tick，按 IEmberUpdate 每帧统一驱动
    /// - <b>delta 累加而非 UniTask.Delay</b>：能正确响应 EmberTimeManager 独立于 UnityEngine.Time.timeScale 的
    ///   TimeScale/Pause（UniTask.Delay 只能感知 Unity 的 timeScale，无法感知框架的独立时间）
    ///
    /// 参考 burner 的 <c>TimerManage</c>，把单一 AddTimer 语义化为 Delay / Interval / Schedule 三个入口。
    ///
    /// <b>使用示例：</b>
    /// <code>
    /// int id  = EmberTimerManager.Instance.Delay(() => Respawn(), 3f);        // 3 秒后执行一次
    /// int id2 = EmberTimerManager.Instance.Interval(() => Tick(), 0.5f);      // 每 0.5 秒执行，无限次
    /// int id3 = EmberTimerManager.Instance.Schedule(() => Go(), 1f, 2f, 3);   // 1 秒后开始，每 2 秒一次，共 3 次
    ///
    /// EmberTimerManager.Instance.Cancel(id);                                   // 取消
    /// </code>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.Time)]
    public class EmberTimerManager : EmberSingleton<EmberTimerManager>, IEmberManager, IEmberUpdate
    {
        #region 内部参数

        private const string TAG = LogTags.CoreTimer;

        /// <summary>无效定时器 ID（0）。定时器 ID 从 1 开始，永不返回 0。</summary>
        public const int INVALID_TIMER_ID = 0;

        /// <summary>ID 计数器，自增生成唯一 ID（跳过 0）</summary>
        private int _nextId;

        /// <summary>ID → 定时器映射，O(1) 查找/取消</summary>
        private readonly Dictionary<int, TimerEntry> _map = new();

        /// <summary>活跃定时器列表，用于每帧顺序遍历</summary>
        private readonly List<TimerEntry> _list = new();

        #endregion

        // ============================================================

        #region 生命周期

        void IEmberManager.Init()
        {
            _nextId = 0;
            _map.Clear();
            _list.Clear();
            EmberDebug.LogInit(TAG, "EmberTimerManager initialized.");
        }

        void IEmberManager.Destroy()
        {
            ClearAll();
            EmberDebug.LogCleanup(TAG, "EmberTimerManager destroyed.");
        }

        #endregion

        // ============================================================

        #region 内部方法

        void IEmberUpdate.Update()
        {
            if (_list.Count == 0) return;

            float scaledDelta = EmberTimeManager.Instance.DeltaTime;
            float unscaledDelta = EmberTimeManager.Instance.UnscaledDeltaTime;

            // 快照本轮计数：回调中新增的定时器留到下一帧，避免同帧递归/死循环
            int count = _list.Count;
            for (int i = 0; i < count; i++)
            {
                TimerEntry e = _list[i];
                if (!e.IsValid) continue;

                e.Remaining -= e.UseLogicTime ? scaledDelta : unscaledDelta;
                if (e.Remaining > 0f) continue;

                // 到点：先复位下一段间隔，再触发回调（每帧至多触发一次，对齐 burner）
                e.Remaining = e.Interval;
                InvokeSafe(e);

                if (!e.IsValid) continue;       // 回调中自我取消
                if (e.RepeatCount > 0)
                {
                    e.RepeatCount--;
                    if (e.RepeatCount == 0)
                    {
                        e.IsValid = false;
                        _map.Remove(e.Id);
                    }
                }
            }

            // 反向压缩：清除已失效（被取消 / 已结束）的条目
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                if (!_list[i].IsValid)
                    _list.RemoveAt(i);
            }
        }

        /// <summary>安全触发回调：异常不打断遍历，只记日志</summary>
        private static void InvokeSafe(TimerEntry e)
        {
            try
            {
                e.Callback?.Invoke();
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"Timer callback [{e.Callback?.Method?.Name}] threw:\n{ex}");
            }
        }

        /// <summary>登记新定时器并返回唯一 ID</summary>
        private int Add(TimerEntry entry)
        {
            if (++_nextId == INVALID_TIMER_ID) ++_nextId;   // 跳过 0
            entry.Id = _nextId;
            _map.Add(entry.Id, entry);
            _list.Add(entry);
            return entry.Id;
        }

        /// <summary>定时器条目（内部）：纯数据 + 有效标记，配合延迟清理避免遍历中修改集合</summary>
        private sealed class TimerEntry
        {
            public int Id;
            public bool IsValid = true;
            public bool UseLogicTime;
            public Action Callback;
            public float Interval;      // 周期间隔（0 = 单次）
            public int RepeatCount;     // 剩余执行次数（0 或负数 = 无限）
            public float Remaining;     // 距下次触发的剩余秒数
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>延时执行一次：<paramref name="delaySeconds"/> 秒后触发回调，返回定时器 ID。</summary>
        [HasGC]
        public int Delay(Action callback, float delaySeconds, bool useLogicTime = true)
            => Schedule(callback, delaySeconds, 0f, 1, useLogicTime);

        /// <summary>带 1 个参数的延时执行。</summary>
        [HasGC]
        public int Delay<T>(Action<T> callback, T arg, float delaySeconds, bool useLogicTime = true)
            => Schedule(() => callback(arg), delaySeconds, 0f, 1, useLogicTime);

        /// <summary>
        /// 周期执行：每 <paramref name="intervalSeconds"/> 秒触发一次，共 <paramref name="repeatCount"/> 次。
        /// <paramref name="repeatCount"/> 为 0 或负数表示无限次；<paramref name="intervalSeconds"/> 为 0 表示每帧执行。
        /// </summary>
        [HasGC]
        public int Interval(Action callback, float intervalSeconds, int repeatCount = 0, bool useLogicTime = true)
            => Schedule(callback, intervalSeconds, intervalSeconds, repeatCount, useLogicTime);

        /// <summary>带 1 个参数的周期执行。</summary>
        [HasGC]
        public int Interval<T>(Action<T> callback, T arg, float intervalSeconds, int repeatCount = 0, bool useLogicTime = true)
            => Schedule(() => callback(arg), intervalSeconds, intervalSeconds, repeatCount, useLogicTime);

        /// <summary>
        /// 完整调度：先延迟 <paramref name="delaySeconds"/> 秒，之后每 <paramref name="intervalSeconds"/> 秒
        /// 触发一次，共 <paramref name="repeatCount"/> 次（为 0 或负数表示无限）。
        /// </summary>
        [HasGC]
        public int Schedule(Action callback, float delaySeconds, float intervalSeconds, int repeatCount = 0, bool useLogicTime = true)
        {
            TimerEntry entry = new()
            {
                Callback = callback,
                Interval = Math.Max(0f, intervalSeconds),
                RepeatCount = repeatCount,
                Remaining = Math.Max(0f, delaySeconds),
                UseLogicTime = useLogicTime,
            };
            return Add(entry);
        }

        /// <summary>带 1 个参数的完整调度。</summary>
        [HasGC]
        public int Schedule<T>(Action<T> callback, T arg, float delaySeconds, float intervalSeconds, int repeatCount = 0, bool useLogicTime = true)
            => Schedule(() => callback(arg), delaySeconds, intervalSeconds, repeatCount, useLogicTime);

        /// <summary>取消定时器。已取消的定时器不再触发回调；重复取消安全。</summary>
        [NoGC]
        public void Cancel(int timerId)
        {
            if (timerId == INVALID_TIMER_ID) return;
            if (_map.TryGetValue(timerId, out TimerEntry e))
            {
                e.IsValid = false;
                _map.Remove(timerId);
            }
        }

        /// <summary>取消定时器并把引用归零，防止后续误用悬空 ID。</summary>
        [NoGC]
        public void Cancel(ref int timerId)
        {
            Cancel(timerId);
            timerId = INVALID_TIMER_ID;
        }

        /// <summary>查询定时器是否仍有效。</summary>
        [NoGC]
        public bool HasTimer(int timerId) => timerId != INVALID_TIMER_ID && _map.ContainsKey(timerId);

        /// <summary>查询距下次触发还剩多少秒；不存在或已失效返回 0。</summary>
        [NoGC]
        public float GetRemainingTime(int timerId)
            => _map.TryGetValue(timerId, out TimerEntry e) ? e.Remaining : 0f;

        /// <summary>当前活跃定时器数量（用于调试/泄露排查）。</summary>
        [NoGC]
        public int ActiveCount => _map.Count;

        /// <summary>立即清空所有定时器（不触发任何回调）。</summary>
        [NoGC]
        public void ClearAll()
        {
            _map.Clear();
            _list.Clear();
        }

        #endregion
    }
}
