using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Entity.Tests
{
    public sealed class EntityEffectContainerTests
    {
        #region 内部参数
        private readonly List<EntityEffectDefinition> _assets=new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Set(object target,string field,object value)
            => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private EntityEffectDefinition Definition(EntityEffectLifetime lifetime=EntityEffectLifetime.Timed,float duration=1)
        {
            var value=ScriptableObject.CreateInstance<EntityEffectDefinition>();_assets.Add(value);
            Set(value,"_id","test.protection");Set(value,"_lifetime",lifetime);Set(value,"_duration",duration);return value;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [TearDown] public void Cleanup()
        {foreach(var value in _assets)UnityEngine.Object.DestroyImmediate(value);_assets.Clear();}
        [Test] public void SameDefinitionAndSourceStillHaveIndependentApplications()
        {
            using var container=new EntityEffectContainer();var definition=Definition();
            var a=container.ApplyIndependent(definition,"same-source");container.Tick(.3f);
            var b=container.ApplyIndependent(definition,"same-source");
            Assert.That(a.ApplicationId,Is.Not.EqualTo(b.ApplicationId));
            container.Tick(.7f);Assert.That(a.IsActive,Is.False);Assert.That(b.IsActive,Is.True);
            a.Dispose();a.Dispose();Assert.That(container.Count,Is.EqualTo(1));
            b.Dispose();Assert.That(container.Count,Is.Zero);
        }
        [Test] public void OwnerAndApplierAreSeparate_OnlyRequestedApplicationIsRemoved()
        {
            using var container=new EntityEffectContainer();var definition=Definition(EntityEffectLifetime.Owned);
            var action=container.ApplyIndependent(definition,"actor");var story=container.ApplyIndependent(definition,"actor");
            action.Dispose();Assert.That(story.IsActive,Is.True);
            container.Tick(100);Assert.That(story.IsActive,Is.True);
            Assert.That(story.EndAction(),Is.True);Assert.That(container.Count,Is.Zero);
        }
        [Test] public void TailStartsAtExplicitActionEnd_RepeatEndDoesNotRestartIt()
        {
            using var container=new EntityEffectContainer();var handle=container.ApplyIndependent(Definition(EntityEffectLifetime.OwnedThenTimed,.3f),"action");
            container.Tick(100);Assert.That(handle.IsActive,Is.True);
            Assert.That(handle.EndAction(),Is.True);container.Tick(.1f);
            Assert.That(handle.EndAction(),Is.False);
            Assert.That(handle.IsActive,Is.True);
            container.Tick(.1f);Assert.That(handle.IsActive,Is.True);
            container.Tick(.1f);Assert.That(handle.IsActive,Is.False);
        }
        [Test] public void TailCanBeExplicitlyRevokedByItsOwnHandle()
        {
            using var container=new EntityEffectContainer();
            var handle=container.ApplyIndependent(Definition(EntityEffectLifetime.OwnedThenTimed),"action");
            handle.EndAction();handle.Dispose();Assert.That(handle.IsActive,Is.False);Assert.That(container.Count,Is.Zero);
        }
        [Test] public void ImmediateRevokeDoesNotInventTailOnInterruption()
        {
            using var container=new EntityEffectContainer();var h=container.ApplyIndependent(Definition(EntityEffectLifetime.OwnedThenTimed),"action");
            h.Dispose();container.Tick(.1f);Assert.That(container.Count,Is.Zero);Assert.That(h.EndAction(),Is.False);
        }
        [Test] public void TimedApplicationIgnoresActionEnd_ButCanBeRevoked()
        {
            using var container=new EntityEffectContainer();var h=container.ApplyIndependent(Definition(),"hit");
            Assert.That(h.EndAction(),Is.False);Assert.That(h.IsActive,Is.True);h.Dispose();Assert.That(h.IsActive,Is.False);
        }
        [Test] public void PauseAndQueriesDoNotAdvanceTimeOrRevision()
        {
            using var container=new EntityEffectContainer();var h=container.ApplyIndependent(Definition(),"source");
            container.Tick(.13f);ulong revision=container.Revision;
            for(int i=0;i<20;i++){container.Tick(0);container.CaptureActive();container.CaptureIndependentTimed();container.TryGet(h.ApplicationId,out _);}
            Assert.That(container.Revision,Is.EqualTo(revision));
            Assert.That(container.CaptureIndependentTimed()[0].RemainingSeconds,Is.EqualTo(.87f).Within(.000001));
        }
        [Test] public void FrozenSnapshotsPreserveIndependentSourcesAndExcludeOwnedOrTail()
        {
            using var container=new EntityEffectContainer();var timed=Definition();
            container.ApplyIndependent(timed,"hit");container.Tick(.2f);container.ApplyIndependent(timed,"story");
            container.ApplyIndependent(Definition(EntityEffectLifetime.Owned),"equipment");
            var tail=container.ApplyIndependent(Definition(EntityEffectLifetime.OwnedThenTimed),"action");tail.EndAction();
            var snapshot=container.CaptureIndependentTimed();var views=container.CaptureActive();
            Assert.That(snapshot.Length,Is.EqualTo(2));Assert.That(views.Length,Is.EqualTo(4));
            container.Tick(.4f);container.Dispose();
            Assert.That(snapshot[0].SourceKey,Is.EqualTo("hit"));Assert.That(snapshot[0].RemainingSeconds,Is.EqualTo(.8f).Within(.000001));
            Assert.That(snapshot[1].RemainingSeconds,Is.EqualTo(1));Assert.That(views.Length,Is.EqualTo(4));
        }
        [Test] public void SharedSoHostsAndOldHandlesCannotAffectEachOther()
        {
            var definition=Definition();using var a=new EntityEffectContainer();using var b=new EntityEffectContainer();
            var old=a.ApplyIndependent(definition,"same");var fresh=b.ApplyIndependent(definition,"same");
            a.Tick(.8f);Assert.That(b.CaptureIndependentTimed()[0].RemainingSeconds,Is.EqualTo(1));
            a.Dispose();old.Dispose();Assert.That(fresh.IsActive,Is.True);Assert.That(old.IsActive,Is.False);
            Assert.Throws<ObjectDisposedException>(()=>a.ApplyIndependent(definition,"same"));
        }
        [Test] public void RuntimeDoesNotFollowSharedConfigMutation()
        {
            using var container=new EntityEffectContainer();var definition=Definition();var h=container.ApplyIndependent(definition,"source");
            Set(definition,"_duration",100f);Set(definition,"_id","changed");Set(definition,"_lifetime",EntityEffectLifetime.Owned);
            Assert.That(container.CaptureActive()[0].EffectId,Is.EqualTo("test.protection"));container.Tick(1);Assert.That(h.IsActive,Is.False);
        }
        [TestCase(EntityEffectLifetime.Timed)]
        [TestCase(EntityEffectLifetime.Owned)]
        [TestCase(EntityEffectLifetime.OwnedThenTimed)]
        public void AllRemovalPathsUpdateOneRevisionAndInvalidateViews(EntityEffectLifetime lifetime)
        {
            using var container=new EntityEffectContainer();var h=container.ApplyIndependent(Definition(lifetime),"source");
            ulong revision=container.Revision;
            Assert.That(container.RemoveAll("test.protection"),Is.EqualTo(1));
            Assert.That(container.Revision,Is.EqualTo(revision+1));h.Dispose();h.EndAction();
            Assert.That(container.Revision,Is.EqualTo(revision+1));Assert.That(h.IsActive,Is.False);
        }
        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void TimeValidationIsAtomic(float seconds)
        {
            using var container=new EntityEffectContainer();container.ApplyIndependent(Definition(),"source");
            if(seconds==0)container.Tick(seconds);else Assert.Throws<ArgumentOutOfRangeException>(()=>container.Tick(seconds));
            Assert.That(container.CaptureIndependentTimed()[0].RemainingSeconds,Is.EqualTo(1));
        }
        [Test] public void InvalidConfigurationAndSourceDoNotPartiallyApply()
        {
            using var container=new EntityEffectContainer();var definition=Definition();
            Assert.Throws<ArgumentException>(()=>container.ApplyIndependent(definition," "));
            Set(definition,"_duration",float.NaN);Assert.Throws<ArgumentOutOfRangeException>(()=>container.ApplyIndependent(definition,"source"));
            Set(definition,"_duration",1f);Set(definition,"_lifetime",(EntityEffectLifetime)99);
            Assert.Throws<ArgumentException>(()=>container.ApplyIndependent(definition,"source"));
            Set(definition,"_lifetime",EntityEffectLifetime.Timed);Set(definition,"_id","");
            Assert.Throws<ArgumentException>(()=>container.ApplyIndependent(definition,"source"));
            Assert.That(container.Count,Is.Zero);Assert.That(container.Revision,Is.Zero);
        }
        [Test] public void ZeroDurationIsImmediateAndDisposeIsIdempotent()
        {
            var container=new EntityEffectContainer();var timed=container.ApplyIndependent(Definition(duration:0),"source");
            Assert.That(timed.IsActive,Is.False);
            var tail=container.ApplyIndependent(Definition(EntityEffectLifetime.OwnedThenTimed,0),"action");
            Assert.That(tail.IsActive,Is.True);tail.EndAction();Assert.That(tail.IsActive,Is.False);
            var owned=container.ApplyIndependent(Definition(EntityEffectLifetime.Owned),"owner");container.Dispose();
            ulong revision=container.Revision;container.Dispose();owned.Dispose();
            Assert.That(container.Count,Is.Zero);Assert.That(container.Revision,Is.EqualTo(revision));
        }
        #endregion
    }
}
