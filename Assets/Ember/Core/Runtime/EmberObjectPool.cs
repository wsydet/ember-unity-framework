using System;
using System.Collections.Generic;

namespace Ember.Core
{
    /// <summary>
    /// 通用对象池。
    ///
    /// 参考 burner 的 <see cref="BattleCore.ObjectPool"/>，在此基础上扩展：
    /// - 可选最大容量，超出时丢弃
    /// - <see cref="IPoolable"/> 支持 —— 取出/归还时自动调用 Reset
    /// - 统计信息（诊断用）
    /// - <see cref="IDisposable"/> 支持 —— 清理时释放池中对象
    ///
    /// 用法：
    /// <code>
    /// var pool = new EmberObjectPool&lt;MyClass&gt;(maxCapacity: 100);
    /// MyClass obj = pool.Get();
    /// // ... 使用 ...
    /// pool.Return(obj);
    /// </code>
    /// </summary>
    /// <typeparam name="T">池化对象类型，必须有无参构造函数</typeparam>
    public class EmberObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _free;
        private readonly int _maxCapacity;
        private readonly bool _trackStats;

        // ---- 统计 ----
        private int _totalCreated;
        private int _totalReturned;
        private int _totalRetrieved;

        /// <summary>
        /// 池中当前可用的空闲对象数量。
        /// </summary>
        public int FreeCount => _free.Count;

        /// <summary>
        /// 累计创建的对象数量。
        /// </summary>
        public int TotalCreated => _totalCreated;

        /// <summary>
        /// 累计取出的次数。
        /// </summary>
        public int TotalRetrieved => _totalRetrieved;

        /// <summary>
        /// 累计归还的次数。
        /// </summary>
        public int TotalReturned => _totalReturned;

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="initialCapacity">初始预分配数量（对象会提前创建好）</param>
        /// <param name="maxCapacity">最大容量，0 表示无限制</param>
        /// <param name="trackStats">是否跟踪统计信息（轻微性能开销）</param>
        public EmberObjectPool(int initialCapacity = 0, int maxCapacity = 0, bool trackStats = false)
        {
            _maxCapacity = maxCapacity > 0 ? maxCapacity : int.MaxValue;
            _trackStats = trackStats;

            int initCap = Math.Min(initialCapacity, _maxCapacity);
            _free = new Stack<T>(initCap > 0 ? initCap : 16);

            for (int i = 0; i < initCap; i++)
            {
                _free.Push(new T());
                if (trackStats) _totalCreated++;
            }
        }

        /// <summary>
        /// 从池中获取一个对象。若池为空则创建新对象。
        /// </summary>
        public T Get()
        {
            T obj;

            if (_free.Count > 0)
            {
                obj = _free.Pop();
            }
            else
            {
                obj = new T();
                if (_trackStats) _totalCreated++;
            }

            if (_trackStats) _totalRetrieved++;

            if (obj is IPoolable poolable)
            {
                poolable.OnTakeFromPool();
            }

            return obj;
        }

        /// <summary>
        /// 将对象归还池中。若池已满则丢弃该对象。
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            if (_free.Count >= _maxCapacity)
            {
                // 池已满，丢弃
                if (obj is IDisposable disposable)
                    disposable.Dispose();
                return;
            }

            if (obj is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }

            _free.Push(obj);
            if (_trackStats) _totalReturned++;
        }

        /// <summary>
        /// 批量预分配对象到池中。
        /// </summary>
        /// <param name="count">预分配数量</param>
        public void Prewarm(int count)
        {
            int canAdd = _maxCapacity - _free.Count;
            int toAdd = Math.Min(count, canAdd);
            for (int i = 0; i < toAdd; i++)
            {
                _free.Push(new T());
                if (_trackStats) _totalCreated++;
            }
        }

        /// <summary>
        /// 清空池，释放所有空闲对象（若实现了 <see cref="IDisposable"/>）。
        /// </summary>
        public void Clear()
        {
            while (_free.Count > 0)
            {
                T obj = _free.Pop();
                if (obj is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }

    // ============================================================
    // IPoolable — 池化对象接口
    // ============================================================

    /// <summary>
    /// 实现此接口的对象在从对象池取出/归还时会收到回调，
    /// 用于重置对象状态。
    ///
    /// 用法：
    /// <code>
    /// public class Bullet : IPoolable
    /// {
    ///     public Vector3 Position;
    ///
    ///     void IPoolable.OnTakeFromPool() { /* 可选：激活逻辑 */ }
    ///     void IPoolable.OnReturnToPool() { Position = Vector3.zero; /* 重置状态 */ }
    /// }
    /// </code>
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 对象从池中被取出时调用。
        /// </summary>
        void OnTakeFromPool();

        /// <summary>
        /// 对象归还到池中时调用。在此方法中重置对象状态。
        /// </summary>
        void OnReturnToPool();
    }
}
