using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>角色与目标碰撞体的临时豁免。宿主负责释放；不修改层或 Effector 掩码，不自行驱动物理。</summary>
    public sealed class EntityCollisionIgnoreLease2D : IDisposable
    {
        #region 内部参数
        private static readonly Dictionary<(Collider2D, Collider2D), Entry> Entries = new();
        private Entry _entry;
        public bool IsDisposed => _entry == null;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private sealed class Entry
        {
            internal Collider2D A, B;
            internal bool Previous, Active = true;
            internal int Owners;
        }
        private EntityCollisionIgnoreLease2D(Entry entry) { _entry = entry; entry.Owners++; }
        private static bool Find(Collider2D a, Collider2D b, out Entry entry)
            => Entries.TryGetValue((a, b), out entry) || Entries.TryGetValue((b, a), out entry);
        private static bool Available(Collider2D collider)
            => collider != null && collider.enabled && collider.gameObject.activeInHierarchy;
        private static void Restore(Entry entry)
        {
            entry.Active = false;
            if (Available(entry.A) && Available(entry.B))
                Physics2D.IgnoreCollision(entry.A, entry.B, entry.Previous);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach (var entry in Entries.Values) Restore(entry);
            Entries.Clear();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static EntityCollisionIgnoreLease2D Acquire(Collider2D a, Collider2D b)
        {
            if (!Available(a) || !Available(b) || a == b)
                throw new ArgumentException("碰撞豁免需要两个不同且已启用的碰撞体。");
            if (!Find(a, b, out var entry))
            {
                entry = new Entry { A = a, B = b, Previous = Physics2D.GetIgnoreCollision(a, b) };
                Entries.Add((a, b), entry);
            }
            Physics2D.IgnoreCollision(a, b, true);
            return new EntityCollisionIgnoreLease2D(entry);
        }
        /// <summary>供检测过滤使用。包含本服务仍持有的豁免与既有的 Unity 碰撞对豁免。</summary>
        [NoGC]
        public static bool IsIgnored(Collider2D a, Collider2D b)
            => a != null && b != null && (Find(a, b, out var entry) && entry.Active || Physics2D.GetIgnoreCollision(a, b));

        /// <summary>宿主恢复物理模拟或重新启用碰撞体后调用，不创建第二份所有权。</summary>
        public void Reapply()
        {
            if (_entry != null && _entry.Active && Available(_entry.A) && Available(_entry.B))
                Physics2D.IgnoreCollision(_entry.A, _entry.B, true);
        }
        public void Dispose()
        {
            var entry = _entry; _entry = null;
            if (entry == null || !entry.Active || --entry.Owners != 0) return;
            Entries.Remove((entry.A, entry.B)); Restore(entry);
        }
        #endregion
    }
}
