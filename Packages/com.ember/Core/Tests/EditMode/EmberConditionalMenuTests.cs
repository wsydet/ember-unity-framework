using System;
using System.Reflection;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Ember.Core.Fsm.Tests
{
    public sealed class EmberConditionalMenuTests
    {
        private static bool Exists(string path) => (bool)typeof(EditorApplication).Assembly.GetType("UnityEditor.Menu")
            .GetMethod("MenuItemExists", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { path });

        [Test]
        public void VisibilityActuallyAddsAndRemovesMenuAndDisposalRemovesEntry()
        {
            string path = "Tools/Ember/ConditionalMenuTest_" + Guid.NewGuid().ToString("N");
            bool visible = false;
            using (EmberConditionalMenu.Register(path, () => visible, () => { }))
            {
                EmberConditionalMenu.Refresh(); Assert.That(Exists(path), Is.False);
                visible = true; EmberConditionalMenu.Refresh(); Assert.That(Exists(path), Is.True);
                visible = false; EmberConditionalMenu.Refresh(); Assert.That(Exists(path), Is.False);
                visible = true; EmberConditionalMenu.Refresh(); Assert.That(Exists(path), Is.True);
            }
            Assert.That(Exists(path), Is.False);
        }

        [Test]
        public void ExecutionRechecksVisibilityAndModuleConditionBeforeAction()
        {
            string path = "Tools/Ember/ConditionalMenuTest_" + Guid.NewGuid().ToString("N");
            bool visible = true, enabled = true; int calls = 0;
            using (EmberConditionalMenu.Register(path, () => visible, () => calls++, () => enabled))
            {
                EmberConditionalMenu.Refresh(); Assert.That(Exists(path), Is.True);
                EditorApplication.ExecuteMenuItem(path); Assert.That(calls, Is.EqualTo(1));
                enabled = false; EditorApplication.ExecuteMenuItem(path); Assert.That(calls, Is.EqualTo(1));
                Assert.That(Exists(path), Is.True, "模块禁用与模板隐藏不同");
                enabled = true; visible = false;
                EditorApplication.ExecuteMenuItem(path); Assert.That(calls, Is.EqualTo(1), "移除前的陈旧入口也不得执行");
            }
        }
    }
}
