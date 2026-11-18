using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EUIBackdropLeaseEditTests
    {
        private sealed class FakeLease : IEUIBackdropLease
        {
            public Texture Texture => Texture2D.whiteTexture;
            public bool IsReady => true;
            public bool IsDisposed { get; private set; }
            public int Generation => 1;
            public int TextureVersion => 1;
            public string Diagnostic => "Test lease";
            public int Disposals;
            public void Invalidate() { }
            public void Freeze() { }
            public void Dispose() { if (IsDisposed) return; IsDisposed = true; Disposals++; }
        }
        private sealed class FakeProvider : IEUIBackdropProvider
        {
            public readonly FakeLease Lease = new();
            public int Acquisitions;
            public IEUIBackdropLease Acquire(in EUIBackdropRequest request) { Acquisitions++; return Lease; }
            public void MarkOrderDirty() { }
        }

        [Test]
        public void Group_OneLeaseForManySurfaces_DisableReleasesOnceAndClearsBorrowers()
        {
            var old = EUIBackdrop.Provider; var provider = new FakeProvider(); EUIBackdrop.Provider = provider;
            var root = new GameObject("Lease owner", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(EUIBackdropGroup));
            var child = new GameObject("Borrower", typeof(RectTransform), typeof(EUIBackdropGraphic)); child.transform.SetParent(root.transform, false);
            var cameraRoot = new GameObject("Lease fixture camera", typeof(Camera));
            try
            {
                root.GetComponent<Canvas>().worldCamera = cameraRoot.GetComponent<Camera>();
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                var group = root.GetComponent<EUIBackdropGroup>();
                typeof(EUIBackdropGroup).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(group, null);
                typeof(EUIBackdropGroup).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(group, null);
                Assert.That(provider.Acquisitions, Is.EqualTo(1));
                Assert.That(child.GetComponent<EUIBackdropGraphic>().Lease, Is.SameAs(provider.Lease));
                root.GetComponent<CanvasGroup>().alpha=0;
                typeof(EUIBackdropGroup).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(group, null);
                group.Release(); group.Release();
                Assert.That(provider.Lease.Disposals, Is.EqualTo(1));
                Assert.That(child.GetComponent<EUIBackdropGraphic>().Lease, Is.Null);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(cameraRoot); EUIBackdrop.Provider = old; }
        }
    }
}
