// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.UIExtension;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    public sealed class EUIDataBindingEditTests : EUIResourceFixture
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Reopen_RebindsOptionalImageBeforePresentationAndRejectsOldResults(bool hidden)
        {
            var root = new GameObject("Page", typeof(RectTransform));
            Objects.Add(root);
            var optional = new GameObject("Optional", typeof(RectTransform), typeof(Image), typeof(EUIImageLoader));
            optional.transform.SetParent(root.transform, false);
            var decoration = new GameObject("Decoration", typeof(RectTransform), typeof(Image));
            decoration.transform.SetParent(root.transform, false);

            var oldSprite = Sprite();
            var lateSprite = Sprite();
            var optionalImage = optional.GetComponent<Image>();
            var decorationImage = decoration.GetComponent<Image>();
            var loader = optional.GetComponent<EUIImageLoader>();
            optionalImage.sprite = oldSprite;
            decorationImage.sprite = oldSprite;
            loader.LoadingDisplay = EUIImageLoadingDisplay.KeepPrevious;

            var page = new EUIPage(root) { LogicTypeName = typeof(OptionalImageLogic).FullName };
            page.CreateLogic((_, logic) =>
            {
                var optionalLogic = (OptionalImageLogic)logic;
                optionalLogic.Image = optionalImage;
                optionalLogic.Loader = loader;
            });
            var owner = (OptionalImageLogic)page.Logic;
            try
            {
                page.Init("A");
                Assert.That(owner.WasEmptyOnOpen, Is.True);
                page.PlayShow();
                Assert.That(Provider.Finish(0, oldSprite), Is.True);
                Assert.That(optionalImage.sprite, Is.SameAs(oldSprite));
                Assert.That(optionalImage.enabled, Is.True);

                var oldBinding = owner.DataBinding.Capture();
                loader.Load("A-late", _ => owner.OldWriteAccepted = oldBinding.TryApply(() =>
                {
                    optionalImage.sprite = lateSprite;
                    optionalImage.enabled = true;
                }));
                if (hidden)
                {
                    page.HideViewOnly();
                    Assert.That(oldBinding.IsCurrent, Is.True);
                }

                // B has no optional image, so its business binding leaves the registered field alone.
                page.OnReopen(null);
                Assert.That(page.Logic, Is.SameAs(owner));
                Assert.That(page.IsOpened, Is.True);
                Assert.That(owner.WasEmptyOnReopen, Is.True);
                Assert.That(owner.OldWriteAccepted, Is.False);
                Assert.That(Provider.Finish(1, lateSprite), Is.False);
                Assert.That(oldBinding.TryApply(() => optionalImage.enabled = true), Is.False);
                Assert.That(optionalImage.sprite, Is.Null);
                Assert.That(optionalImage.enabled, Is.False);
                Assert.That(decorationImage.sprite, Is.SameAs(oldSprite));
                Assert.That(owner.DataBinding.Capture().IsCurrent, Is.True);
            }
            finally { page.Cleanup(); }
        }

        public sealed class OptionalImageLogic : EUILogic
        {
            public Image Image;
            public EUIImageLoader Loader;
            public bool? OldWriteAccepted;
            public bool WasEmptyOnOpen;
            public bool WasEmptyOnReopen;

            public override void OnInit()
            {
                DataBinding.RegisterReset(() =>
                {
                    Image.enabled = false;
                    Loader.SetSprite(null);
                });
            }

            public override void OnOpen(object param)
            {
                WasEmptyOnOpen = !Image.enabled && !Image.sprite;
                Bind(param);
            }

            private void Bind(object param)
            {
                if (param is not string path) return;
                var binding = DataBinding.Capture();
                Loader.Load(path, result => binding.TryApply(() =>
                {
                    Image.enabled = result.Status == EUIImageLoadStatus.Success;
                }));
            }

            public override void OnReopen(object param)
            {
                WasEmptyOnReopen = !Image.enabled && !Image.sprite;
                Bind(param);
            }
        }
    }
}
