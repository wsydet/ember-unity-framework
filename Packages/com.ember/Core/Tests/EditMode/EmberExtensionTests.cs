using System;
using System.Linq;
using Ember.Core.Editor;
using NUnit.Framework;

namespace Ember.Core.Fsm.Tests
{
    public sealed class EmberExtensionTests
    {
        private static EmberExtensionDefinition D(string id, params string[] dependencies) => new(id, id, EmberExtensionKind.EditorExtension, dependencies);
        [Test] public void CompositionResolvesTransitiveDependenciesAndSharedBranches()
        {
            var result = EmberExtensionCatalog.Resolve(new[] { D("a", "b", "c"), D("b", "d"), D("c", "d"), D("d") }, new[] { "a" });
            CollectionAssert.AreEquivalent(new[] { "a", "b", "c", "d" }, result.Enabled); Assert.IsEmpty(result.Problems);
        }
        [Test] public void MissingAndCyclicBranchesFailClosedWithoutBlockingUnrelatedExtension()
        {
            var result = EmberExtensionCatalog.Resolve(new[] { D("a", "b"), D("b", "a"), D("c", "missing"), D("good") }, new[] { "a", "c", "good", "unknown" });
            Assert.IsFalse(result.IsEnabled("a")); Assert.IsFalse(result.IsEnabled("b")); Assert.IsFalse(result.IsEnabled("c"));
            Assert.IsTrue(result.IsEnabled("good")); Assert.IsTrue(result.Problems.ContainsKey("unknown"));
        }
        [Test] public void CompatibilityAndExceptionsBlockDependents()
        {
            foreach (Func<string> check in new Func<string>[] { () => "不兼容", () => throw new InvalidOperationException("模块不可用") })
            {
                var result = EmberExtensionCatalog.Resolve(new[] { D("a", "b"), new EmberExtensionDefinition("b", "b", EmberExtensionKind.EditorExtension, checkCompatibility: check) }, new[] { "a" });
                Assert.IsFalse(result.IsEnabled("a")); Assert.IsFalse(result.IsEnabled("b")); Assert.IsNotEmpty(result.Problems);
            }
        }
        [Test] public void DisablePreviewIncludesTransitiveRootsAndDoesNotMutateRequested()
        {
            var requested = new[] { "a", "b", "other" };
            var definitions = new[] { D("a", "b"), D("b", "c"), D("c"), D("other") };
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, EmberExtensionCatalog.PreviewDisable(definitions, requested, "c"));
            Assert.AreEqual(3, requested.Length);
            CollectionAssert.AreEquivalent(new[] { "other" }, EmberExtensionCatalog.Resolve(definitions, requested.Except(new[] { "a", "b" })).Enabled);
        }
        [Test] public void StableIdsAndDuplicateDefinitionsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => D("Bad ID"));
            Assert.Throws<ArgumentException>(() => EmberExtensionCatalog.Resolve(new[] { D("a"), D("a") }, new[] { "a" }));
        }
        [Test] public void InfrastructureCannotBeRemovedFromComposition()
        {
            var result = EmberExtensionCatalog.Resolve(new[] { new EmberExtensionDefinition("core", "core", EmberExtensionKind.Infrastructure) }, Array.Empty<string>());
            Assert.IsTrue(result.IsEnabled("core"));
        }
    }
}
