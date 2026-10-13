using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Game.Entity
{
    /// <summary>宿主显式持有的可选效果寿命容器；不自行驱动，不定义具体效果重复施加策略。</summary>
    public sealed class EntityEffectContainer : IDisposable
    {
        #region 内部参数
        private readonly List<Entry> _entries = new();
        private bool _disposed;
        private bool _notifying;
        private Action _changed;
        private Delegate[] _handlers = Array.Empty<Delegate>();
        /// <summary>一次修改完成后通知；订阅者只读，不能在通知中再次修改本容器。</summary>
        public event Action Changed
        {
            add
            {
                if (_disposed) throw new ObjectDisposedException(nameof(EntityEffectContainer));
                _changed += value; _handlers = _changed?.GetInvocationList() ?? Array.Empty<Delegate>();
            }
            remove { _changed -= value; _handlers = _changed?.GetInvocationList() ?? Array.Empty<Delegate>(); }
        }
        public int Count => _entries.Count;
        public ulong Revision { get; private set; }
        public bool IsDisposed => _disposed;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private sealed class Entry
        {
            internal Guid ApplicationId;
            internal string EffectId, SourceKey;
            internal EntityEffectKind Kind;
            internal EntityEffectLifetime Lifetime;
            internal float Duration, Remaining;
            internal bool IsTiming;
            internal double Elapsed;
            internal EntityEffectRules Rules;
            internal EntityEffectAppearance Appearance;
            internal EntityEffectView View => new(ApplicationId,EffectId,SourceKey,Kind,Lifetime,IsTiming,Remaining,Elapsed,Rules,Appearance);
        }
        private int IndexOf(Guid id)
        { for(int i=0;i<_entries.Count;i++) if(_entries[i].ApplicationId==id) return i; return -1; }
        private void RemoveAt(int index)
        { _entries.RemoveAt(index); Revision++; }
        private void CheckMutation()
        { if (_notifying) throw new InvalidOperationException("效果通知中只能读取；后续修改应由宿主在通知结束后发起。"); }
        private void Publish()
        {
            var handlers = _handlers;
            if (handlers.Length == 0) return;
            _notifying = true;
            try
            {
                foreach (Action handler in handlers)
                    try { handler(); }
                    catch (Exception error) { EmberDebug.LogError("Game.Entity", "效果订阅失败：" + error); }
            }
            finally { _notifying = false; }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>明确的独立施加原语；刷新/合并策略由具体效果适配选择，普通退出只释放句柄。</summary>
        public EntityEffectHandle ApplyIndependent(EntityEffectDefinition definition, string sourceKey)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return ApplyCaptured(definition.Capture(), sourceKey);
        }
        /// <summary>从已验证的冻结配置创建新的独立施加，身份与计时不共享。</summary>
        public EntityEffectHandle ApplyCaptured(EntityEffectSpec definition, string sourceKey)
        {
            CheckMutation();
            if(_disposed)throw new ObjectDisposedException(nameof(EntityEffectContainer));
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            if(string.IsNullOrWhiteSpace(sourceKey)||sourceKey!=sourceKey.Trim())throw new ArgumentException("需要明确的效果来源标识。",nameof(sourceKey));
            var entry=new Entry {ApplicationId=Guid.NewGuid(),EffectId=definition.Id,SourceKey=sourceKey,
                Kind=definition.Kind,Lifetime=definition.Lifetime,Duration=definition.Duration,
                Remaining=definition.Duration,IsTiming=definition.Lifetime==EntityEffectLifetime.Timed,
                Rules=definition.Rules,Appearance=definition.Appearance};
            var handle=new EntityEffectHandle(this,entry.ApplicationId);
            if(!entry.IsTiming || entry.Remaining>0) { _entries.Add(entry); Revision++; Publish(); }
            return handle;
        }
        public bool TryGet(Guid id, out EntityEffectView view)
        {
            int index=IndexOf(id);view=index<0?default:_entries[index].View;return index>=0;
        }
        public EntityEffectView GetAt(int index) => _entries[index].View;
        public bool ProtectsDamage(string category)
        {
            foreach (var entry in _entries) if (entry.Rules.ProtectsDamage(category)) return true;
            return false;
        }
        public bool PreventsStagger
        {
            get { foreach (var entry in _entries) if (entry.Rules.PreventsStagger) return true; return false; }
        }
        public int ExcludedLayers
        {
            get { int mask = 0; foreach (var entry in _entries) mask |= entry.Rules.ExcludedLayers; return mask; }
        }
        public EntityEffectView[] CaptureActive()
        {
            var result=new EntityEffectView[_entries.Count];
            for(int i=0;i<result.Length;i++)result[i]=_entries[i].View;
            return result;
        }
        /// <summary>只采集独立计时来源。动作/尾随/其他持有来源须按来源恢复契约处理，不生成无主永久效果。</summary>
        public EntityTimedEffectSnapshot[] CaptureIndependentTimed()
        {
            var result=new List<EntityTimedEffectSnapshot>();
            foreach(var entry in _entries)
                if(entry.Lifetime==EntityEffectLifetime.Timed)
                    result.Add(new EntityTimedEffectSnapshot(entry.EffectId,entry.SourceKey,entry.Remaining,entry.Rules,entry.Appearance,entry.Kind));
            return result.ToArray();
        }
        internal bool EndAction(Guid id)
        {
            CheckMutation();
            int index=IndexOf(id);if(index<0)return false;
            var entry=_entries[index];
            if(entry.Lifetime==EntityEffectLifetime.Timed||entry.IsTiming)return false;
            if(entry.Lifetime==EntityEffectLifetime.Owned || entry.Duration==0) RemoveAt(index);
            else { entry.IsTiming=true;entry.Remaining=entry.Duration;Revision++; }
            Publish();
            return true;
        }
        /// <summary>拒绝遗漏来源状态；完整采集调用此入口，不能只筛选 Timed 冒充完整快照。</summary>
        public EntityTimedEffectSnapshot[] CaptureRestorableTimed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EntityEffectContainer));
            foreach (var entry in _entries)
                if (entry.Lifetime != EntityEffectLifetime.Timed)
                    throw new NotSupportedException("效果 " + entry.EffectId + "（来源 " + entry.SourceKey + "，" + entry.Lifetime + "）没有来源恢复适配，不能保存完整快照。");
            return CaptureIndependentTimed();
        }
        [HasGC]
        public static EntityEffectContainer RestoreTimed(EntityTimedEffectSnapshot[] snapshots)
        {
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            foreach (var record in snapshots) record.Validate();
            var result = new EntityEffectContainer();
            foreach (var record in snapshots)
                result._entries.Add(new Entry { ApplicationId = Guid.NewGuid(), EffectId = record.EffectId,
                    SourceKey = record.SourceKey, Kind = record.Kind, Lifetime = EntityEffectLifetime.Timed,
                    Duration = record.RemainingSeconds, Remaining = record.RemainingSeconds, IsTiming = true,
                    Rules = record.Rules, Appearance = record.Appearance });
            return result;
        }
        internal void Remove(Guid id)
        { CheckMutation();int index=IndexOf(id);if(index>=0){RemoveAt(index);Publish();} }
        public void Tick(float gameSeconds)
        {
            CheckMutation();
            if(_disposed)return;
            if(!float.IsFinite(gameSeconds)||gameSeconds<0)throw new ArgumentOutOfRangeException(nameof(gameSeconds));
            if(gameSeconds==0)return;
            ulong revision = Revision;
            for(int i=_entries.Count-1;i>=0;i--)
            {
                var entry=_entries[i];entry.Elapsed+=gameSeconds;
                if(!entry.IsTiming){Revision++;continue;}
                entry.Remaining=Math.Max(0,entry.Remaining-gameSeconds);
                if(entry.Remaining<=.000001f)RemoveAt(i);
                else Revision++;
            }
            if (Revision != revision) Publish();
        }
        /// <summary>显式业务驱散；普通状态退出不调用此接口。</summary>
        public int RemoveAll(string effectId)
        {
            CheckMutation();
            int count=0;
            for(int i=_entries.Count-1;i>=0;i--)
                if(string.Equals(_entries[i].EffectId,effectId,StringComparison.Ordinal)) { RemoveAt(i);count++; }
            if(count>0)Publish();
            return count;
        }
        public void Dispose()
        {
            CheckMutation();
            if(_disposed)return;
            _disposed=true;
            for(int i=_entries.Count-1;i>=0;i--)RemoveAt(i);
            Publish(); _changed=null; _handlers=Array.Empty<Delegate>();
        }
        #endregion
    }
}
