using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private sealed class MediaLoop : INovelLoopPlayback
        {
            public float Volume, Elapsed;
            public bool Paused, Disposed;
            public bool MixerRouted { get; set; }
            public void SetVolume(float v) { Assert.IsFalse(Disposed); Volume = v; }
            public void SetPaused(bool p) { Assert.IsFalse(Disposed); Paused = p; }
            public void Tick(float d) { Assert.IsFalse(Disposed); if (!Paused) Elapsed += d; }
            public void Dispose() { Disposed = true; }
        }
        private sealed class MediaAudio : INovelAudio, INovelLoopAudio
        {
            public readonly List<MediaLoop> Loops = new();
            public bool VoicePlaying => false;
            public event Action VoiceCompleted { add { } remove { } }
            public int OneShots;
            public readonly List<bool> LoopKinds = new();
            /// <summary>下一条 CreateLoop 出来的循环音是否算「已接 Mixer 分组」。</summary>
            public bool NextMixerRouted;
            public INovelLoopPlayback CreateLoop(AudioClip clip, bool bgm)
            {
                var loop = new MediaLoop { MixerRouted = NextMixerRouted };
                Loops.Add(loop); LoopKinds.Add(bgm); return loop;
            }
            public void Play(NovelCommandKind kind, AudioClip clip) { OneShots++; }
            public void Tick() { }
            public void StopVoice() { }
            public void SetPaused(bool p) { }
            public void SetVolumes(float b, float s, float v) { }
            public void Dispose() { foreach (var loop in Loops) loop.Dispose(); }
        }
        private sealed class MediaParticle : INovelEffectInstance
        {
            public float Elapsed;
            public bool Disposed;
            public bool IsAlive => Elapsed < 1;
            public void Tick(float delta) { Assert.IsFalse(Disposed); Elapsed += delta; }
            public void Dispose() { Disposed = true; }
        }
        private sealed partial class ActionView : INovelEffectView
        {
            public readonly List<MediaParticle> Particles = new();
            public INovelEffectInstance CreateEffect(GameObject prefab, NovelEffectState state, NovelPortraitSlot slot)
            { var p = new MediaParticle(); Particles.Add(p); return p; }
        }
        private sealed class MediaResources : INovelResources
        {
            internal NarrativeStorySO Story;
            internal AudioClip Clip;
            internal GameObject Prefab;
            internal Sprite Sprite;
            internal bool Ready = true, Missing;
            internal readonly List<Func<bool>> Released = new();
            internal Lease<GameObject> Pending;
            public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object
            {
                UnityEngine.Object asset = typeof(T) == typeof(NarrativeStorySO) ? Story : typeof(T) == typeof(AudioClip) ? Clip : typeof(T) == typeof(Sprite) ? Sprite : Prefab;
                var lease = new Lease<T> { IsDone = typeof(T) != typeof(GameObject) || Ready, Asset = Missing && typeof(T) == typeof(GameObject) ? null : asset as T, Error = "missing " + path };
                if (typeof(T) == typeof(GameObject)) Pending = (Lease<GameObject>)(object)lease;
                Released.Add(() => lease.Disposed); return lease;
            }
        }
        private MediaResources MediaAssets()
        {
            var prefab = new GameObject("media test"); _assets.Add(prefab);
            var ps = prefab.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var texture = new Texture2D(2, 2); _assets.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); _assets.Add(sprite);
            return new MediaResources { Story = _story, Clip = ReadingClip(), Prefab = prefab, Sprite = sprite };
        }
        private NovelCommand Particle(string id, bool persistent = true, bool keep = false, string binding = null) =>
            new(id, NovelCommandKind.EffectPlay, instanceId: "weather", resourceKey: "rain", persistent: persistent,
                keepOnSceneChange: keep, bindingId: binding);
        private NovelCommand Ambient(string id, float volume = .8f, float duration = 2, bool keep = false) =>
            new(id, NovelCommandKind.AmbientPlay, instanceId: "wind", resourceKey: "choice_chime", duration: duration,
                parallel: true, volume: volume, keepOnSceneChange: keep);
        private NovelSession MediaSession(ActionView view, MediaResources resources, MediaAudio audio, params NovelCommand[] commands)
        {
            Commands(commands);
            var session = new NovelSession(new NovelNewGameRequest(), () => _tables, resources, audio);
            session.AttachView(view); session.Tick(0, 0); return session;
        }

        [Test]
        public void E3EnvironmentPauseMultiplierTakeoverAndAutomaticRelease()
        {
            using var view = new ActionView(); var resources = MediaAssets(); var audio = new MediaAudio();
            using var session = MediaSession(view, resources, audio, Particle("first"), Ambient("wind"), SayAction("line"),
                Particle("replace", false), new NovelCommand("stop", NovelCommandKind.AmbientStop, instanceId: "wind"), SayAction("end"));
            int frame = 0; PumpAction(session, "line", ref frame);
            session.ConfigureReading(null, 100, 0, .5f, .25f, 1); session.SetReadingMultiplier(2);
            session.Tick(.5f, ++frame);
            Assert.AreEqual(.5f, view.Particles[0].Elapsed, .001, "Persistent effects use real time");
            Assert.AreEqual(.1f, audio.Loops[0].Volume, .001, "Halfway fade × .8 story × .25 player");
            session.Pause("menu"); session.Tick(50, ++frame);
            Assert.AreEqual(.5f, view.Particles[0].Elapsed, .001); Assert.IsTrue(audio.Loops[0].Paused);
            session.Resume("menu"); session.Tick(.5f, ++frame);
            Assert.AreEqual(.2f, audio.Loops[0].Volume, .001);
            session.Advance(++frame); PumpAction(session, "end", ref frame);
            Assert.IsTrue(view.Particles[0].Disposed); Assert.IsTrue(audio.Loops[0].Disposed);
            session.Tick(1.1f, ++frame); Assert.AreEqual(0, session.ActiveEffectCount);
            session.Dispose(); Assert.IsTrue(resources.Released.All(f => f())); Assert.AreEqual(0, session.OwnedResourceCount);
        }

        [Test]
        public void E3CrossFadeHasTwoTracksTakesOverFromCurrentGainAndReleasesOldLease()
        {
            using var view = new ActionView(); var r = MediaAssets(); var a = new MediaAudio();
            using var s = MediaSession(view, r, a,
                new NovelCommand("music1", NovelCommandKind.BGM, resourceKey: "quiet_afternoon"),
                new NovelCommand("music2", NovelCommandKind.BGM, resourceKey: "quiet_afternoon", duration: 4, parallel: true), SayAction("line"),
                new NovelCommand("music3", NovelCommandKind.BGM, resourceKey: "quiet_afternoon", duration: 2, parallel: true), SayAction("next"));
            int f = 0; PumpAction(s, "line", ref f); s.Tick(1, ++f);
            Assert.AreEqual(2, s.ActiveLoopCount); Assert.AreEqual(.75f, a.Loops[0].Volume, .001); Assert.AreEqual(.25f, a.Loops[1].Volume, .001);
            s.Advance(++f); PumpAction(s, "next", ref f);
            Assert.IsTrue(a.Loops[0].Disposed); Assert.AreEqual(NovelActionStatus.Cancelled, s.Actions.Single(x => x.Id == "music2").Status);
            s.Tick(1, ++f); Assert.AreEqual(.125f, a.Loops[1].Volume, .001); Assert.AreEqual(.5f, a.Loops[2].Volume, .001);
            s.Tick(1, ++f); Assert.IsTrue(a.Loops[1].Disposed); Assert.AreEqual(1, s.ActiveLoopCount);
        }

        [Test]
        public void E3StableRestoreOnlyRecreatesPersistentFinalState()
        {
            using var view = new ActionView(); var r = MediaAssets(); var a = new MediaAudio();
            using var s = MediaSession(view, r, a, Particle("rain"),
                new NovelCommand("spark", NovelCommandKind.EffectPlay, resourceKey: "rain", instanceId: "spark"), Ambient("wind"), SayAction("line"));
            int f = 0; PumpAction(s, "line", ref f); s.Advance(++f);
            Assert.IsTrue(s.TryCapture(out var save, out var error), error);
            Assert.AreEqual(1, save.PersistentEffects.Count); Assert.AreEqual(.8f, save.Loops.Single().Volume);
            Assert.AreEqual(0, a.Loops.Single().Volume, "Capture must not advance live fade");
            s.Dispose(); var nextAudio = new MediaAudio(); var nextResources = MediaAssets();
            using var restored = new NovelSession(save, () => _tables, nextResources, nextAudio);
            for (int i = 0; i < 5 && !restored.RestoreReady; i++) restored.Tick(0, ++f);
            Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString());
            using var restoredView = new ActionView(); restored.CommitRestore(restoredView);
            Assert.AreEqual(1, restoredView.Particles.Count); Assert.AreEqual(.8f, nextAudio.Loops.Single().Volume);
            Assert.AreEqual(0, nextAudio.OneShots); Assert.AreEqual(1, restored.History.Count);
            restored.Dispose(); Assert.IsTrue(nextResources.Released.All(check => check()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void E3BackgroundBoundaryHonorsRetention(bool keep)
        {
            using var view = new ActionView(); var r = MediaAssets(); var a = new MediaAudio();
            using var s = MediaSession(view, r, a, Particle("rain", keep: keep), Ambient("wind", duration: 0, keep: keep),
                new NovelCommand("scene", NovelCommandKind.Background, resourceKey: "campus"), SayAction("line"));
            int f = 0; PumpAction(s, "line", ref f);
            Assert.AreEqual(keep ? 1 : 0, s.ActiveEffectCount); Assert.AreEqual(keep ? 1 : 0, s.ActiveLoopCount);
        }

        [Test]
        public void E3PendingLoadCancellationAndMissingResourceCannotLeakOrCreateLateInstances()
        {
            using var view = new ActionView(); var r = MediaAssets(); r.Ready = false;
            var s = MediaSession(view, r, new MediaAudio(), Particle("rain"), SayAction("line"));
            s.Tick(0, 1); s.Dispose(); Assert.IsTrue(r.Released.All(check => check()));
            r.Pending.IsDone = true; s.Tick(10, 2); Assert.IsEmpty(view.Particles);
            var missing = MediaAssets(); missing.Missing = true;
            using var failed = MediaSession(view, missing, new MediaAudio(), Particle("rain"), SayAction("line"));
            failed.Tick(0, 1); failed.Tick(0, 2);
            Assert.AreEqual(NarrativeState.Faulted, failed.Snapshot.State);
            StringAssert.Contains("rain", failed.Snapshot.Error.ToString()); Assert.IsTrue(missing.Pending.Disposed);
        }

        [Test]
        public void E3SkipSuppressesOneShotConvergesFadeAndKeepsOnePersistentInstance()
        {
            using var view = new ActionView(); var r = MediaAssets(); var a = new MediaAudio();
            using var s = MediaSession(view, r, a, Particle("spark", false), Particle("rain"), Ambient("wind"), SayAction("line"));
            s.ConfigureReading((_, _, _) => true, 100, 1, 1, 1, 1); s.SetReadMode(NarrativeReadMode.Skip);
            int f = 0; PumpAction(s, "line", ref f);
            Assert.AreEqual(1, view.Particles.Count); Assert.AreEqual(1, s.ActiveEffectCount);
            Assert.AreEqual(.8f, a.Loops.Single().Volume); Assert.IsTrue(s.Actions.All(x => x.IsFinished));
        }

        [Test]
        public void E3EditorDrainWaitsForEnvelopeButNotPersistentLifetimes()
        {
            Commands(Particle("rain"), Ambient("wind", duration: .2f));
            using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story);
            var r = MediaAssets(); r.Story = data.Story; var a = new MediaAudio();
            using var view = new ActionView(); using var s = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, r, a);
            s.AttachView(view);
            for (int f = 0; f < 30 && s.Snapshot.State != NarrativeState.Ended; f++) s.Tick(.05f, f);
            Assert.AreEqual(NarrativeState.Ended, s.Snapshot.State, s.Snapshot.Error?.ToString());
            Assert.AreEqual(1, s.ActiveEffectCount); Assert.AreEqual(1, s.ActiveLoopCount);
            Assert.IsTrue(s.Actions.All(x => x.IsFinished));
            s.Dispose(); Assert.IsTrue(view.Particles.Single().Disposed); Assert.IsTrue(a.Loops.Single().Disposed);
        }

        [Test]
        public void E3VolumeStopDelayAndPausedResumePreserveTiming()
        {
            using var view = new ActionView(); var r = MediaAssets(); var a = new MediaAudio();
            using var s = MediaSession(view, r, a, Ambient("start", duration: 0),
                new NovelCommand("gain", NovelCommandKind.AmbientVolume, instanceId: "wind", volume: .2f, duration: 2, parallel: true), SayAction("line"),
                new NovelCommand("stop", NovelCommandKind.AmbientStop, instanceId: "wind", duration: 1, delay: 1, parallel: true), SayAction("next"));
            int f = 0; PumpAction(s, "line", ref f); s.Tick(1, ++f);
            Assert.AreEqual(.5f, a.Loops[0].Volume, .001);
            s.Advance(++f); PumpAction(s, "next", ref f);
            s.Pause("menu"); s.Resume("menu"); s.Tick(.5f, ++f);
            Assert.AreEqual(.5f, a.Loops[0].Volume, .001, "Stop holds gain during delay");
            Assert.IsTrue(s.TryCapture(out var save, out var error), error); Assert.IsEmpty(save.Loops);
            s.Tick(1.5f, ++f); Assert.IsTrue(a.Loops[0].Disposed); Assert.AreEqual(0, s.ActiveLoopCount);
        }

        [TestCase(1920, 1080)]
        [TestCase(1440, 1080)]
        public void E3FormalPreviewParticlesProduceGeometryAndStopReleasesEverything(int width, int height)
        {
            const string path = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
            var hash = AssetDatabase.GetAssetDependencyHash(path);
            Commands(new NovelCommand("motes", NovelCommandKind.EffectPlay, resourceKey: "lastlight_motes", instanceId: "motes",
                persistent: true, position: new Vector2(.5f, .5f)),
                new NovelCommand("wind", NovelCommandKind.AmbientPlay, resourceKey: "choice_chime", instanceId: "wind"), SayAction("line"));
            for (int repeat = 0; repeat < 2; repeat++)
            {
                var preview = new PreviewRenderUtility();
                var host = new GameObject("E3 preview"); preview.AddSingleGO(host);
                try
                {
                    var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), host.transform);
                    using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story);
                    using var view = new NovelPlaybackView(root, preview.camera, new Vector2(width, height));
                    using var audio = new NovelPlaybackAudio(host.transform);
                    using var session = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, data, audio);
                    session.AttachView(view); int f = 0; PumpAction(session, "line", ref f);
                    session.Tick(.8f, ++f); view.Flush();
                    var particles = root.GetComponentInChildren<ParticleSystem>(true);
                    Assert.IsNotNull(particles); Assert.Greater(particles.particleCount, 0);
                    var meshEffect = root.GetComponentsInChildren<BaseMeshEffect>(true).Single(m => m.GetType().Name == "NovelParticleMesh");
                    using var vertices = new VertexHelper(); meshEffect.ModifyMesh(vertices);
                    Assert.Greater(vertices.currentVertCount, 0, "The formal overlay must draw the simulated particles");
                    float time = particles.time;
                    session.Pause("menu"); session.Tick(10, ++f); Assert.AreEqual(time, particles.time);
                    audio.SetMuted(true); session.Resume("menu"); session.Tick(.2f, ++f);
                    Assert.GreaterOrEqual(particles.time, time); Assert.AreEqual(1, audio.ActiveCount);
                    session.Dispose(); Assert.IsNull(root.GetComponentInChildren<ParticleSystem>(true));
                    Assert.IsEmpty(host.GetComponentsInChildren<AudioSource>(true)); Assert.AreEqual(0, audio.ActiveCount);
                }
                finally { preview.Cleanup(); }
            }
            Assert.AreEqual(hash, AssetDatabase.GetAssetDependencyHash(path));
        }

        [Test]
        public void E3BindingRemovalAndUnknownInstanceHaveExplicitBehavior()
        {
            using var view = new ActionView(); var r = MediaAssets(); var a = new MediaAudio();
            using var s = MediaSession(view, r, a, ShowAction("actor", NovelPortraitSlot.Center), Particle("attached", binding: "actor"),
                new NovelCommand("hide", NovelCommandKind.Character, instanceId: "actor", visualAction: NovelVisualAction.Hide), SayAction("line"));
            int f = 0; PumpAction(s, "line", ref f); Assert.AreEqual(0, s.ActiveEffectCount); Assert.IsTrue(view.Particles.Single().Disposed);
            using var missing = MediaSession(view, MediaAssets(), new MediaAudio(), Particle("bad", binding: "missing"));
            missing.Tick(0, 1); missing.Tick(0, 2);
            Assert.AreEqual(NarrativeState.Faulted, missing.Snapshot.State); Assert.AreEqual(0, missing.ActiveEffectCount);
        }

        [TestCase("duplicate")]
        [TestCase("one-shot")]
        [TestCase("volume")]
        [TestCase("binding")]
        public void E3MalformedPersistentStateFailsBeforeAnyPlayback(string change)
        {
            using var view = new ActionView();
            using var s = MediaSession(view, MediaAssets(), new MediaAudio(), Particle("rain"), Ambient("wind", duration: 0), SayAction("line"));
            int f = 0; PumpAction(s, "line", ref f); s.Advance(++f);
            Assert.IsTrue(s.TryCapture(out var save, out var error), error);
            if (change == "duplicate") save.PersistentEffects.Add(save.PersistentEffects[0]);
            if (change == "one-shot") save.PersistentEffects[0].Persistent = false;
            if (change == "volume") save.Loops[0].Volume = 2;
            if (change == "binding") save.PersistentEffects[0].BindingId = "missing";
            var a = new MediaAudio(); var r = MediaAssets();
            using var restored = new NovelSession(save, () => _tables, r, a);
            restored.Tick(0, ++f); restored.Tick(0, ++f);
            Assert.IsFalse(restored.RestoreReady); Assert.AreEqual(NarrativeState.Faulted, restored.Snapshot.State);
            Assert.IsEmpty(a.Loops); Assert.AreEqual(1, s.ActiveEffectCount, "Failed preparation leaves current session untouched");
            restored.Dispose(); Assert.IsTrue(r.Released.All(check => check()));
        }

        [Test]
        public void E6BgmAndAmbientLoopsDeclareTheirMixerRoleToTheAudioAdapter()
        {
            using var view = new ActionView(); var a = new MediaAudio();
            using var s = MediaSession(view, MediaAssets(), a,
                new NovelCommand("bgm", NovelCommandKind.BGM, resourceKey: "quiet_afternoon"), Ambient("wind"), SayAction("line"));
            int f = 0; PumpAction(s, "line", ref f);
            Assert.AreEqual(2, a.LoopKinds.Count);
            Assert.IsTrue(a.LoopKinds[0], "BGM 循环必须按 BGM 分组输出");
            Assert.IsFalse(a.LoopKinds[1], "环境音循环必须按 SFX 分组输出，不能被玩家 BGM 音量带走");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void E6MixerRoutedLoopLeavesPlayerVolumeToTheMixerGroup(bool mixerRouted)
        {
            using var view = new ActionView(); var a = new MediaAudio { NextMixerRouted = mixerRouted };
            using var s = MediaSession(view, MediaAssets(), a, Ambient("wind"), SayAction("line"));
            int f = 0; PumpAction(s, "line", ref f);
            s.ConfigureReading(null, 100, 0, .5f, .25f, 1);
            s.Tick(.5f, ++f);
            Assert.AreEqual(1, a.Loops.Count);
            Assert.AreEqual(mixerRouted ? .2f : .05f, a.Loops[0].Volume, .001f,
                mixerRouted ? "接上 Mixer 分组后音源只写剧情增益，玩家音量交给分组承担"
                    : "未接 Mixer 时仍是原来的音源音量路径：渐变一半 × 剧情 0.8 × 玩家 0.25");
        }

        [Test]
        public void E3SchemaFourMusicMigratesAndCurrentSchemaRoundTripsCameraAndMediaThroughSlotStore()
        {
            using var view = new ActionView();
            using var s = MediaSession(view, MediaAssets(), new MediaAudio(),
                CameraAction("camera"), new NovelCommand("bgm", NovelCommandKind.BGM, resourceKey: "quiet_afternoon"), SayAction("line"));
            int f = 0; PumpAction(s, "line", ref f); s.Advance(++f);
            Assert.IsTrue(s.TryCapture(out var save, out var error), error);
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "novel-e3-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new Game.NovelSave.NovelSaveStore(directory);
                Assert.IsTrue(store.Save(0, save, out error), error);
                Assert.IsTrue(store.Read(0, out var loaded, out error), error);
                Assert.AreEqual(NovelCheckpoint.CurrentSchemaVersion, loaded.SchemaVersion); Assert.AreEqual(1.4f, loaded.CameraZoom); Assert.AreEqual(1, loaded.Loops.Count);
                loaded.SchemaVersion = 4; loaded.Loops.Clear();
                var a = new MediaAudio();
                using var restored = new NovelSession(loaded, () => _tables, MediaAssets(), a);
                for (int i = 0; i < 5 && !restored.RestoreReady; i++) restored.Tick(0, ++f);
                Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString());
                using var restoredView = new ActionView(); restored.CommitRestore(restoredView);
                Assert.AreEqual(1, a.Loops.Count); Assert.AreEqual(1, a.Loops[0].Volume); Assert.AreEqual(0, a.OneShots);
            }
            finally { if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true); }
        }
    }
}
