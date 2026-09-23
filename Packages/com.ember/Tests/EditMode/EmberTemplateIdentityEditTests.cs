// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System.Collections.Generic;
using Ember.Core.Editor;
using NUnit.Framework;

namespace Ember.UI.Tests
{
    public sealed class EmberTemplateIdentityEditTests
    {
        #region 外部方法

        [TestCase(true, "visual-novel", null, true)]
        [TestCase(false, null, "visual-novel", true)]
        [TestCase(true, "novel-child", "base", true)]
        [TestCase(false, "base", "novel-child", true)]
        [TestCase(true, "novel-grandchild", null, true)]
        [TestCase(false, null, "novel-grandchild", true)]
        [TestCase(true, "base", "visual-novel", false)]
        [TestCase(false, "visual-novel", "base", false)]
        [TestCase(true, null, "visual-novel", false)]
        [TestCase(false, "visual-novel", null, false)]
        [TestCase(false, null, "missing", false)]
        [TestCase(false, null, "broken", false)]
        [TestCase(false, null, "cycle-a", false)]
        [TestCase(false, null, "Visual-Novel", false)]
        public void FormalIdentityAndAncestryDetermineAvailability(bool embedded, string editing, string deployed, bool expected)
        {
            var templates = new List<TemplateInfo>
            {
                new() { id = "base" },
                new() { id = "visual-novel", parentId = "base" },
                new() { id = "novel-child", parentId = "visual-novel" },
                new() { id = "novel-grandchild", parentId = "novel-child" },
                new() { id = "broken", parentId = "missing" },
                new() { id = "cycle-a", parentId = "cycle-b" },
                new() { id = "cycle-b", parentId = "cycle-a" }
            };
            Assert.AreEqual(expected, EmberProjectSetup.IsTemplateActiveForIdentity(
                "visual-novel", embedded, editing, deployed, templates));
        }

        [Test]
        public void UnknownTargetAndDuplicateIdentityAreRejected()
        {
            var templates = new List<TemplateInfo> { new() { id = "visual-novel" } };
            Assert.IsFalse(EmberProjectSetup.IsTemplateActiveForIdentity(null, false, null, "visual-novel", templates));
            Assert.IsFalse(EmberProjectSetup.IsTemplateActiveForIdentity("visual-novel", false, null, "visual-novel", new List<TemplateInfo>()));
            templates.Add(new TemplateInfo { id = "visual-novel", parentId = "base" });
            Assert.IsFalse(EmberProjectSetup.IsTemplateActiveForIdentity("visual-novel", false, null, "visual-novel", templates));
        }

        #endregion
    }
}
