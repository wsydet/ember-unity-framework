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

        private sealed class RenewingProvider : IEUIBackdropProvider
        {
            public readonly System.Collections.Generic.List<FakeLease> Leases = new();
            public IEUIBackdropLease Acquire(in EUIBackdropRequest request)
            { var lease = new FakeLease(); Leases.Add(lease); return lease; }
            public void MarkOrderDirty() { }
        }

        [Test]
        public void MaskRelease_RevokesBorrowBeforeDisposal_AndLocalGlassReacquires_AcrossPoolReuse()
        {
            var old = EUIBackdrop.Provider; var provider = new RenewingProvider(); EUIBackdrop.Provider = provider;
            var root = new GameObject("Modal owner", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(EUIBackdropGroup));
            var child = new GameObject("Glass", typeof(RectTransform), typeof(EUIBackdropGraphic)); child.transform.SetParent(root.transform, false);
            var camera = new GameObject("Lease camera", typeof(Camera));
            var mask = new GameObject("Pooled mask", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var type = typeof(EUIPage).Assembly.GetType("Ember.UI.EUIBackdropMaskSession");
            var session = mask.AddComponent(type);
            var configure = type.GetMethod("Configure"); var release = type.GetMethod("Release");
            var tick = typeof(EUIBackdropGroup).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var group = root.GetComponent<EUIBackdropGroup>();
            try
            {
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera.GetComponent<Camera>();
                for (int i = 0; i < 3; i++)
                {
                    configure.Invoke(session, new object[] { canvas, 1500, EUIMaskEffect.Blur, null, Color.black });
                    var shared = (FakeLease)group.Lease;
                    Assert.That(child.GetComponent<EUIBackdropGraphic>().Lease, Is.SameAs(shared));
                    release.Invoke(session, null); release.Invoke(session, null);
                    Assert.That(group.Lease, Is.Null, "A disposed external reference used to prevent local reacquisition");
                    Assert.That(shared.Disposals, Is.EqualTo(1));
                    tick.Invoke(group, null);
                    Assert.That(group.Lease, Is.Not.Null.And.Not.SameAs(shared));
                    Assert.That(group.Lease.IsDisposed, Is.False);
                    Assert.That(child.GetComponent<EUIBackdropGraphic>().Lease, Is.SameAs(group.Lease));
                }
                group.Release();
                foreach (var lease in provider.Leases) Assert.That(lease.Disposals, Is.EqualTo(1));
            }
            finally
            { Object.DestroyImmediate(mask); Object.DestroyImmediate(root); Object.DestroyImmediate(camera); EUIBackdrop.Provider = old; }
        }

        [Test]
        public void OldSessionRelease_DoesNotDetachANewerExternalLoan()
        {
            var root = new GameObject("Borrower", typeof(RectTransform), typeof(Canvas), typeof(EUIBackdropGroup));
            try
            {
                var group = root.GetComponent<EUIBackdropGroup>(); var old = new FakeLease(); var current = new FakeLease();
                group.SetExternalLease(old); group.SetExternalLease(current);
                typeof(EUIBackdropGroup).GetMethod("RevokeExternalLease", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(group, new object[] { old });
                Assert.That(group.Lease, Is.SameAs(current));
                group.Release(); Assert.That(old.Disposals + current.Disposals, Is.Zero, "Borrowers never dispose external loans");
            }
            finally { Object.DestroyImmediate(root); }
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
