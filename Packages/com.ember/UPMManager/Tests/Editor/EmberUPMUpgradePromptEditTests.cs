// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberUPMUpgradePromptEditTests
    {
        [UnityTest]
        public IEnumerator CompletedPrompt_RepaintsWithoutClearingNewCheckResults()
        {
            var manager = ScriptableObject.CreateInstance<EmberUPMManager>();
            var host = ScriptableObject.CreateInstance<EmberUPMUpgradePromptTestWindow>();
            try
            {
                // 模拟升级完成提示尚未关闭，而下一次检查已找到新版本。
                const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
                var tagsField = typeof(EmberUPMManager).GetField("_newerTags", fields);
                var messageField = typeof(EmberUPMManager).GetField("_checkMessage", fields);
                Assert.IsNotNull(tagsField);
                Assert.IsNotNull(messageField);
                var tags = (List<Version>)tagsField.GetValue(manager);
                var nextVersion = new Version(0, 12, 6);
                tags.Add(nextVersion);
                messageField.SetValue(manager, "检查完成，发现 1 个更新版本。");
                var now = DateTime.UtcNow;
                var completed = new EmberUPMUpgradeSnapshot(EmberUPMUpgradePhase.Succeeded,
                    "0.12.4", "0.12.5", null, now, now, "0.12.5", false, false);

                // 在实际 IMGUI 绘制回调里回归，不改用户的 UpgradeTracker / SessionState。
                host.DrawContent = () => manager.DrawUpgradeOperation(completed);
                host.ShowUtility();
                var deadline = EditorApplication.timeSinceStartup + 5d;
                while (host.RepaintCount < 2 && EditorApplication.timeSinceStartup < deadline)
                {
                    host.Repaint();
                    yield return null;
                }

                Assert.GreaterOrEqual(host.RepaintCount, 2, "需要实际完成两次 IMGUI 重绘。");
                CollectionAssert.AreEqual(new[] { nextVersion }, tags,
                    "旧升级完成提示不能清空新检查得到的升级列表。");
                Assert.AreEqual("检查完成，发现 1 个更新版本。", messageField.GetValue(manager));
            }
            finally
            {
                host.DrawContent = null;
                host.Close();
                UnityEngine.Object.DestroyImmediate(manager);
            }
        }
    }

    internal sealed class EmberUPMUpgradePromptTestWindow : EditorWindow
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
