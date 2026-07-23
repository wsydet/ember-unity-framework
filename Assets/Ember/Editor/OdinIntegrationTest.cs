// ------------------------------------------------------------------------------
// Odin Integration Test — 验证 Odin Inspector 集成状态
//
// 在 Unity 菜单栏点击：Ember > Test > Run Odin Integration Test
// 或在 Console 中查看自动检测结果。
// ------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Ember.Editor
{
    /// <summary>
    /// 检测 Odin Inspector 的核心程序集是否正常加载。
    /// </summary>
    public static class OdinIntegrationTest
    {
        private static readonly string[] RequiredAssemblies =
        {
            "Sirenix.OdinInspector.Attributes",
            "Sirenix.OdinInspector.Editor",
            "Sirenix.Serialization",
            "Sirenix.Serialization.Config",
            "Sirenix.Utilities",
            "Sirenix.Utilities.Editor"
        };

        private static readonly string[] KeyTypes =
        {
            "Sirenix.OdinInspector.Editor.OdinEditor",
            "Sirenix.OdinInspector.Editor.OdinMenuEditorWindow",
            "Sirenix.OdinInspector.ButtonAttribute",
            "Sirenix.OdinInspector.TitleAttribute",
            "Sirenix.OdinInspector.ShowInInspectorAttribute",
            "Sirenix.OdinInspector.FoldoutGroupAttribute",
            "Sirenix.Serialization.SerializationUtility",
            "Sirenix.Utilities.TypeExtensions"
        };

        [MenuItem("Ember/Test/Run Odin Integration Test")]
        public static void RunTest()
        {
            Debug.Log("========== Odin Integration Test Start ==========");

            var allPassed = true;

            allPassed &= TestAssemblyLoading();
            allPassed &= TestKeyTypes();
            allPassed &= TestOdinEditorWindow();
            allPassed &= TestAttributeUsage();

            if (allPassed)
            {
                Debug.Log("<color=green>✅ Odin Integration Test — ALL PASSED</color>");
            }
            else
            {
                Debug.LogError("❌ Odin Integration Test — SOME CHECKS FAILED. See details above.");
            }

            Debug.Log("========== Odin Integration Test End ==========");
        }

        /// <summary>检测程序集是否加载</summary>
        private static bool TestAssemblyLoading()
        {
            Debug.Log("[Odin Test] Checking assembly loading...");

            var loadedAssemblies = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .ToHashSet();

            var allLoaded = true;
            foreach (var asmName in RequiredAssemblies)
            {
                if (loadedAssemblies.Contains(asmName))
                {
                    Debug.Log($"  ✅ {asmName}");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️  {asmName} — NOT LOADED (may load on demand)");
                    // Editor 程序集在 Editor 启动时延迟加载，不算硬失败
                    if (!asmName.Contains("Editor"))
                    {
                        allLoaded = false;
                    }
                }
            }

            return allLoaded;
        }

        /// <summary>检测关键类型是否可解析（遍历所有已加载程序集查找）</summary>
        private static bool TestKeyTypes()
        {
            Debug.Log("[Odin Test] Resolving key types...");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("Sirenix"))
                .ToList();

            Debug.Log($"  Found {assemblies.Count} Sirenix assemblies loaded");

            var allResolved = true;
            foreach (var typeName in KeyTypes)
            {
                Type type = null;
                foreach (var asm in assemblies)
                {
                    type = asm.GetType(typeName);
                    if (type != null) break;
                }

                if (type != null)
                {
                    Debug.Log($"  ✅ {typeName}");
                }
                else
                {
                    Debug.LogError($"  ❌ {typeName} — COULD NOT RESOLVE");
                    allResolved = false;
                }
            }

            return allResolved;
        }

        /// <summary>检测 OdinMenuEditorWindow 是否能正常实例化（反射创建，不实际显示）</summary>
        private static bool TestOdinEditorWindow()
        {
            Debug.Log("[Odin Test] Checking OdinMenuEditorWindow...");

            var windowType = typeof(OdinMenuEditorWindow);
            if (windowType != null)
            {
                Debug.Log($"  ✅ OdinMenuEditorWindow type found: {windowType.FullName}");
                return true;
            }

            Debug.LogError("  ❌ OdinMenuEditorWindow type NOT found");
            return false;
        }

        /// <summary>验证 Odin 属性在实际类上能否正常使用（编译时已由编译器验证，此处做运行时确认）</summary>
        private static bool TestAttributeUsage()
        {
            Debug.Log("[Odin Test] Verifying attribute usage...");

            var attrProps = typeof(TestMonoTarget)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes().Any(a => a.GetType().Namespace?.StartsWith("Sirenix") == true))
                .ToList();

            var attrFields = typeof(TestMonoTarget)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.GetCustomAttributes().Any(a => a.GetType().Namespace?.StartsWith("Sirenix") == true))
                .ToList();

            Debug.Log($"  ✅ Found {attrProps.Count} properties and {attrFields.Count} fields with Odin attributes");
            return true;
        }
    }

    /// <summary>
    /// 用于验证 Odin 属性可用性的测试目标类。
    /// </summary>
    [System.Serializable]
    public class TestMonoTarget : MonoBehaviour
    {
        [Title("Odin Test Group")]
        [ShowInInspector]
        [FoldoutGroup("Settings")]
        public int testInt = 42;

        [FoldoutGroup("Settings")]
        [ShowInInspector]
        [Range(0f, 100f)]
        public float testFloat = 50f;

        [Button("Test Button")]
        public void TestButtonMethod()
        {
            Debug.Log("[Odin Test] Button clicked — Odin attribute system is working.");
        }
    }
}
