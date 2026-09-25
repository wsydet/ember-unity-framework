using System;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public interface INovelEffectInstance : IDisposable
    {
        bool IsAlive { get; }
        void Tick(float delta);
    }
    public interface INovelEffectView
    {
        INovelEffectInstance CreateEffect(GameObject prefab, NovelEffectState state, NovelPortraitSlot slot);
    }
    public interface INovelLoopPlayback : IDisposable
    {
        void SetVolume(float volume);
        void SetPaused(bool paused);
        void Tick(float delta);
    }
    public interface INovelLoopAudio
    {
        INovelLoopPlayback CreateLoop(AudioClip clip);
    }
    [Serializable]
    public sealed class NovelEffectState
    {
        public string Id, Key, BindingId;
        public bool Persistent, KeepOnSceneChange;
        public Vector2 Position, Scale = Vector2.one;
        public int Layer;
        internal NovelEffectState Copy() => (NovelEffectState)MemberwiseClone();
    }
    [Serializable]
    public sealed class NovelLoopState
    {
        public string Id, Key;
        public bool Bgm, KeepOnSceneChange;
        public float Volume = 1;
        internal NovelLoopState Copy() => (NovelLoopState)MemberwiseClone();
    }
    public static class NovelMediaRules
    {
        #region 外部方法
        [NoGC] public static bool IsAudio(NovelCommandKind k) => k == NovelCommandKind.BGM ||
            k >= NovelCommandKind.BGMStop && k <= NovelCommandKind.AmbientVolume;
        [NoGC] public static bool IsMedia(NovelCommandKind k) => IsAudio(k) || k == NovelCommandKind.EffectPlay || k == NovelCommandKind.EffectStop;
        [NoGC] public static bool NeedsResource(NovelCommandKind k) => k == NovelCommandKind.BGM || k == NovelCommandKind.AmbientPlay || k == NovelCommandKind.EffectPlay;
        // 句柄解析只有一处真源；保留原签名与既有调用点，不在这里复制回退规则。
        [NoGC] public static string ActionId(NovelCommand c) => NovelActionHandle.ResolveId(c);
        [HasGC] public static string EffectPath(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            foreach (char ch in key) if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')) return null;
            return "Effects/Narrative/" + key;
        }
        [HasGC] public static string Validate(NovelCommand c)
        {
            if (!IsMedia(c.Kind)) return null;
            if (!NovelActionHandle.ValidTime(c.Duration) || !NovelActionHandle.ValidTime(c.Delay) ||
                !NovelActionHandle.ValidTime(c.Volume) || c.Volume > 1 || !Enum.IsDefined(typeof(NovelEase), c.Ease)) return "E3 音量须为 0–1，时间须有限非负，缓动须有效";
            if (c.Kind != NovelCommandKind.BGM && c.Kind != NovelCommandKind.BGMStop && string.IsNullOrWhiteSpace(c.InstanceId)) return "E3 需要独立实例 ID";
            if (c.Kind == NovelCommandKind.EffectPlay &&
                (EffectPath(c.ResourceKey) == null || !NovelActorRules.Coordinates(c.Position) || !NovelActorRules.Scaling(c.Scale) || c.Layer < -100 || c.Layer > 100))
                return "效果需要有效资源键、归一化位置、0.05–5 缩放和 -100–100 层级";
            if ((c.Kind == NovelCommandKind.EffectPlay || c.Kind == NovelCommandKind.EffectStop) && (c.Duration != 0 || c.Delay != 0))
                return "效果创建/停止是即时操作；一次性粒子自然结束回收，持续效果不参与等待";
            return null;
        }
        [HasGC] public static string ValidatePrefab(GameObject prefab)
        {
            if (!prefab) return "效果 Prefab 为空";
            var systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            if (systems.Length != 1 || systems[0].gameObject != prefab)
                return "首批效果需要根节点上的单个 ParticleSystem";
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 || prefab.GetComponentsInChildren<AudioSource>(true).Length != 0)
                return "效果 Prefab 不支持附带脚本或音源；声音使用剧情音频指令";
            var p = systems[0]; var main = p.main;
            if (main.maxParticles > 1024 || main.simulationSpace != ParticleSystemSimulationSpace.Local ||
                p.subEmitters.enabled || p.trails.enabled || p.collision.enabled || p.trigger.enabled ||
                !NovelActionHandle.ValidTime(main.duration) || main.duration <= 0 || main.duration > 60 ||
                !NovelActionHandle.ValidTime(main.startLifetime.constantMax) || main.startLifetime.constantMax > 60 ||
                !NovelActionHandle.ValidTime(main.startLifetime.constantMin) ||
                !NovelActionHandle.ValidTime(main.startDelay.constantMax) || main.startDelay.constantMax > 60 ||
                main.startDelay.mode != ParticleSystemCurveMode.Constant && main.startDelay.mode != ParticleSystemCurveMode.TwoConstants ||
                main.startLifetime.mode != ParticleSystemCurveMode.Constant && main.startLifetime.mode != ParticleSystemCurveMode.TwoConstants)
                return "粒子须为 Local XY、最多 1024 个、周期/寿命不超过 60 秒（寿命常量或双常量）；不支持子发射器/拖尾/碰撞/触发器";
            return null;
        }
        #endregion
    }
}
