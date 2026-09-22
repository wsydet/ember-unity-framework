using System;
using System.Collections.Generic;
using System.Linq;
using Ember.UI;
using Game.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public partial class EUINovelReaderPage : INovelEffectView
    {
        #region 内部参数
        private readonly List<ParticleInstance> _particleInstances = new();
        private sealed class ParticleInstance : INovelEffectInstance
        {
            internal EUINovelReaderPage Owner;
            internal GameObject Prefab, Root;
            internal EUIItem Item;
            internal ParticleSystem[] Systems;
            internal NovelParticleMesh Mesh;
            internal int Layer;
            internal bool Persistent;
            internal int ActorIndex = -1;
            private bool _started;
            public bool IsAlive => !_started || Systems != null && Systems.Any(p => p && p.IsAlive(false));
            public void Tick(float delta)
            {
                if (!Root) return;
                Mesh.Alpha = Owner._stageAlpha * (ActorIndex < 0 ? 1 : Owner._visualAlpha[ActorIndex + 1]);
                if (!_started && delta <= 0) { Mesh.Refresh(); return; }
                foreach (var system in Systems)
                {
                    system.Simulate(Mathf.Max(0, delta), false, !_started, false);
                    system.Pause(false); // Neither Unity Update nor Editor Update owns this clock.
                }
                _started = true; Mesh.Refresh();
            }
            public void Dispose()
            {
                Owner?._particleInstances.Remove(this);
                if (Prefab) { Prefab.SetActive(false); Owner.DestroyScreenRoot(Prefab); }
                if (Root) { Root.SetActive(false); Item?.Dispose(); Owner.DestroyScreenRoot(Root); }
                Prefab = Root = null; Systems = null; Owner = null;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public INovelEffectInstance CreateEffect(GameObject prefab, NovelEffectState state, NovelPortraitSlot slot)
        {
            string error = NovelMediaRules.ValidatePrefab(prefab);
            if (error != null) throw new InvalidOperationException(state.Key + "：" + error);
            var parent = string.IsNullOrEmpty(state.BindingId) ? Background.parent : _actorRects[(int)slot];
            var instance = new ParticleInstance { Owner = this, Layer = state.Layer, Persistent = state.Persistent, ActorIndex = string.IsNullOrEmpty(state.BindingId) ? -1 : (int)slot };
            try
            {
                // Reuse the existing EUI background Item and its Binding; no generated UI files are edited.
                instance.Item = CloneScreenItem(_coverItem.RectTransform, parent, "Effect " + state.Id);
                instance.Root = instance.Item.GameObject;
                ((EUINovelBackgroundItem)instance.Item.Logic).SetSolidColor(Color.white);
                var picture = instance.Root.GetComponentInChildren<Image>(true);
                instance.Mesh = picture.gameObject.AddComponent<NovelParticleMesh>();
                instance.Prefab = UnityEngine.Object.Instantiate(prefab, instance.Root.transform);
                instance.Prefab.name = "Particles " + state.Id;
                instance.Systems = instance.Prefab.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var renderer in instance.Prefab.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                foreach (var system in instance.Systems)
                {
                    var main = system.main;
                    if (main.maxParticles > 1024 || main.simulationSpace != ParticleSystemSimulationSpace.Local ||
                        system.subEmitters.enabled || system.trails.enabled || system.collision.enabled || system.trigger.enabled)
                        throw new InvalidOperationException("首批粒子仅支持 Local XY、每系统最多 1024 粒子；不支持子发射器/拖尾/碰撞/触发器：" + state.Key);
                    main.loop = state.Persistent; main.playOnAwake = false; main.stopAction = ParticleSystemStopAction.None;
                    main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                Vector2 position = string.IsNullOrEmpty(state.BindingId) ? state.Position : new Vector2(.5f, .5f) + state.Position;
                instance.Mesh.Configure(instance.Systems, position, state.Scale);
                _particleInstances.Add(instance);
                if (instance.ActorIndex < 0) ApplyCameraBackground();
                // Stage particles stay below the cover and reader controls; layer sorts only peers with the same binding.
                foreach (var item in _particleInstances.Where(p => p.Root && p.Root.transform.parent == parent).OrderBy(p => p.Layer))
                {
                    if (parent == Background.parent) item.Root.transform.SetSiblingIndex(_coverItem.GameObject.transform.GetSiblingIndex());
                    else item.Root.transform.SetAsLastSibling();
                }
                instance.Tick(0); return instance;
            }
            catch { instance.Dispose(); throw; }
        }
        #endregion
    }
}
