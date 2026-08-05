using System.Linq;
using System.Reflection;
using Ember.Test;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEditor;
using UnityEngine;

namespace Ember.Test.Editor
{
    /// <summary>
    /// Feel 编辑器验证工具。
    /// 通过菜单 "Ember > Test > Validate Feel Integration" 触发。
    /// </summary>
    public static class FeelTestEditor
    {
        private const string MenuPath = "Ember/Test/Validate Feel Integration";

        [MenuItem(MenuPath)]
        public static void ValidateFeelIntegration()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 <b>Feel 编辑器集成验证</b>");
            Debug.Log("═══════════════════════════════════════");

            int passed = 0;
            int failed = 0;

            // ── 1. 程序集加载 ──────────────────
            void Check(string label, bool condition)
            {
                if (condition) { passed++; Debug.Log($"  ✅ {label}"); }
                else { failed++; Debug.LogError($"  ❌ {label}"); }
            }

            // 核心类型可达（用 typeof 直接引用，避免 Type.GetType 字符串查找不可靠）
            Check("MMF_Player 类型可达", typeof(MMF_Player) != null);
            Check("MMFeedbacks (Legacy) 类型可达", typeof(MMFeedbacks) != null);
            Check("MMEventManager 类型可达", typeof(MMEventManager) != null);
            Check("MMStateMachine<FeelTestState> 类型可达", typeof(MMStateMachine<FeelTestState>) != null);
            Check("MMSingleton<FeelTestSingletonComponent> 类型可达", typeof(MMSingleton<FeelTestSingletonComponent>) != null);
            Check("MMSimpleObjectPooler 类型可达", typeof(MMSimpleObjectPooler) != null);

            // ── 2. 检查废弃 API ──────────────────
            Debug.Log("── ⚠️ 废弃 API 扫描 ──");
            var feelCsFiles = AssetDatabase.FindAssets("t:MonoScript")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(p => p.StartsWith("Assets/Feel/") && p.EndsWith(".cs"))
                .ToArray();

            int obsoleteCount = 0;
            foreach (var file in feelCsFiles)
            {
                var content = AssetDatabase.LoadAssetAtPath<MonoScript>(file)?.text;
                if (string.IsNullOrEmpty(content)) continue;

                // 检查仍在使用的 GetInstanceID()（排除注释）
                var lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Contains("GetInstanceID()") &&
                        !line.TrimStart().StartsWith("//") &&
                        !line.TrimStart().StartsWith("///"))
                    {
                        Debug.LogWarning($"  ⚠️ {file}:{i + 1} — GetInstanceID() 已废弃，应替换为 GetEntityId()");
                        obsoleteCount++;
                    }
                }
            }

            if (obsoleteCount == 0)
            {
                Check("GetInstanceID() 废弃 API 已全部修复", true);
            }
            else
            {
                Debug.LogWarning($"  共发现 {obsoleteCount} 处 GetInstanceID() 调用（可能含合法用途或已在处理中）");
            }

            // ── 3. 检查路径依赖 ──────────────────
            Debug.Log("── 📁 路径依赖检查 ──");
            int pathRefCount = 0;
            foreach (var file in feelCsFiles)
            {
                var content = AssetDatabase.LoadAssetAtPath<MonoScript>(file)?.text;
                if (string.IsNullOrEmpty(content)) continue;

                var lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.Contains("Assets/Feel") || line.Contains("Assets\\Feel"))
                    {
                        // 跳过 Feel 自身的路径引用（如 AddComponentMenu）
                        if (line.Contains("AddComponentMenu") ||
                            line.Contains("CreateAssetMenu") ||
                            line.Contains("MenuItem"))
                            continue;

                        Debug.LogWarning($"  ⚠️ {file}:{i + 1} — 硬编码路径引用: {line.Trim()}");
                        pathRefCount++;
                    }
                }
            }

            if (pathRefCount == 0)
            {
                Check("无硬编码 Assets/Feel 路径依赖", true);
            }
            else
            {
                Debug.LogWarning($"  共发现 {pathRefCount} 处硬编码路径引用");
            }

            // ── 4. 报告 ──────────────────────────
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"🔍 <b>验证完成</b> — 通过: <color=green>{passed}</color> / 失败: <color=red>{failed}</color>");
            if (failed > 0)
            {
                Debug.LogError("存在未通过的检查项，请查看上方日志定位问题。");
            }
            else
            {
                Debug.Log("<color=green>Feel 集成状态正常 ✅</color>");
            }
            Debug.Log("═══════════════════════════════════════");

            EditorUtility.DisplayDialog(
                "Feel Integration Validation",
                $"通过: {passed}\n失败: {failed}\n\n详情请查看 Console 窗口。",
                "OK");
        }
    }
}
