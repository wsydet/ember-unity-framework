// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections;
using System.Reflection;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ember.UI.Tests
{
    public class EUICatalogLayoutEditTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void ProjectChange_PreservesSnapshotUntilNextLayout()
        {
            var manager = ScriptableObject.CreateInstance<EUIPrefabManagerWindow>();
            try
            {
                var snapshot = new EUIPrefabCatalogSnapshot();
                var catalog = typeof(EUIPrefabManagerWindow).GetField("_catalog", PRIVATE_INSTANCE);
                catalog.SetValue(manager, snapshot);
                Invoke(manager, "OnProjectChange");
                Assert.AreSame(snapshot, catalog.GetValue(manager),
                    "资源保存通知不能清空当前 Layout / Repaint 共用的目录快照。");
                Assert.AreEqual(true, typeof(EUIPrefabManagerWindow)
                    .GetField("_catalogRefreshPending", PRIVATE_INSTANCE).GetValue(manager));
            }
            finally { UnityEngine.Object.DestroyImmediate(manager); }
        }

        [UnityTest]
        public IEnumerator ProjectSaveBeforeLayout_OverviewRepaintsWithMatchingControls()
        {
            var manager = ScriptableObject.CreateInstance<EUIPrefabManagerWindow>();
            var host = ScriptableObject.CreateInstance<EUICatalogLayoutTestWindow>();
            try
            {
                var tab = typeof(EUIPrefabManagerWindow).GetField("_tab", PRIVATE_INSTANCE);
                tab.SetValue(manager, Enum.ToObject(tab.FieldType, 1)); // UI 总览
                var catalog = typeof(EUIPrefabManagerWindow).GetField("_catalog", PRIVATE_INSTANCE);
                var invalidated = false;
                host.DrawContent = () =>
                {
                    if (Event.current.type == EventType.Layout && !invalidated)
                    {
                        invalidated = true;
                        Invoke(manager, "OnProjectChange");
                    }
                    Invoke(manager, "OnGUI");
                };
                host.position = new Rect(50, 50, 1000, 650);
                host.ShowUtility();
                var deadline = EditorApplication.timeSinceStartup + 5;
                while (host.RepaintCount < 2 && EditorApplication.timeSinceStartup < deadline)
                {
                    host.Repaint();
                    yield return null;
                }
                Assert.IsTrue(invalidated);
                Assert.GreaterOrEqual(host.RepaintCount, 2, "资源变更后需要实际完成两次重绘。");
                Assert.IsNotNull(catalog.GetValue(manager), "目录应在工具栏绘制前完成刷新。");
                Assert.IsTrue(((EUIPrefabCatalogSnapshot)catalog.GetValue(manager)).IsConfigured,
                    "本回归需要有效 UI 目录，覆盖统计标签的出现条件。");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                host.DrawContent = null;
                host.Close();
                UnityEngine.Object.DestroyImmediate(manager);
            }
        }

        private static void Invoke(EUIPrefabManagerWindow manager, string method)
            => typeof(EUIPrefabManagerWindow).GetMethod(method, PRIVATE_INSTANCE).Invoke(manager, null);
    }

    internal sealed class EUICatalogLayoutTestWindow : EditorWindow
    {
        internal Action DrawContent;
        internal int RepaintCount;

        private void OnGUI()
        {
            DrawContent?.Invoke();
            if (Event.current.type == EventType.Repaint) RepaintCount++;
        }
    }
}
