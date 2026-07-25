using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    [InitializeOnLoad]
    public static class HierarchyWindowAdapter
    {
        private const string EDITOR_WINDOW_TYPE = "UnityEditor.SceneHierarchyWindow";
        private const double EDITOR_WINDOWS_CACHE_TTL = 2;

        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags INSTANCE_PUBLIC = BindingFlags.Instance | BindingFlags.Public;

        private static readonly FieldInfo SCENE_HIERARCHY_FIELD;
        private static readonly FieldInfo TREE_VIEW_FIELD;
        private static readonly PropertyInfo TREE_VIEW_DATA_PROPERTY;
        private static readonly MethodInfo TREE_VIEW_ITEMS_METHOD;
        private static readonly PropertyInfo TREE_VIEW_OBJECT_PROPERTY;

        // Windows cache
        private static double _nextWindowsUpdate;
        private static EditorWindow[] _windowsCache;

        //---------------------------------------------------------------------
        // Ctor
        //---------------------------------------------------------------------

        static HierarchyWindowAdapter()
        {
            // Reflection
            var assembly = Assembly.GetAssembly(typeof(EditorWindow));

            var hierarchyWindowType = assembly.GetType("UnityEditor.SceneHierarchyWindow");
            SCENE_HIERARCHY_FIELD = hierarchyWindowType.GetField("m_SceneHierarchy", INSTANCE_PRIVATE);

            var sceneHierarchyType = assembly.GetType("UnityEditor.SceneHierarchy");
            TREE_VIEW_FIELD = sceneHierarchyType.GetField("m_TreeView", INSTANCE_PRIVATE);

            var treeViewControllerTypeGeneric = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController`1");
            var treeViewControllerType = treeViewControllerTypeGeneric.MakeGenericType(typeof(EntityId));
            TREE_VIEW_DATA_PROPERTY = treeViewControllerType .GetProperty("data", INSTANCE_PUBLIC);

            var treeViewDataType = assembly.GetType("UnityEditor.GameObjectTreeViewDataSource");
            TREE_VIEW_ITEMS_METHOD = treeViewDataType.GetMethod("GetRows", INSTANCE_PUBLIC);

            var treeViewItem = assembly.GetType("UnityEditor.GameObjectTreeViewItem");
            TREE_VIEW_OBJECT_PROPERTY = treeViewItem.GetProperty("objectPPTR", INSTANCE_PUBLIC);

            // Callbacks
            HierarchyRulesetV2.OnRulesetChangeCallback += ApplyDefaultIconsToAll;
        }

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static EditorWindow GetFirstHierarchyWindow()
        {
            return GetAllHierarchyWindows().FirstOrDefault();
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "InvertIf")]
        public static IEnumerable<EditorWindow> GetAllHierarchyWindows(bool forceUpdate = false)
        {
            if (forceUpdate || _nextWindowsUpdate < EditorApplication.timeSinceStartup)
            {
                _nextWindowsUpdate = EditorApplication.timeSinceStartup + EDITOR_WINDOWS_CACHE_TTL;
                _windowsCache = HierarchyEditorUtility.GetAllWindowsByType(EDITOR_WINDOW_TYPE).ToArray();
            }

            return _windowsCache;
        }

        public static void ApplyIconByInstanceId(EntityId entityId, Texture2D icon)
        {
            var hierarchyWindows = GetAllHierarchyWindows();

            foreach (var window in hierarchyWindows)
            {
                var treeViewItems = GetTreeViewItems(window);
                var treeViewItem = treeViewItems.FirstOrDefault(item => item.id == entityId);
                if (treeViewItem != null) treeViewItem.icon = icon;
            }
        }

        //---------------------------------------------------------------------
        // Helpers
        //---------------------------------------------------------------------

        private static IEnumerable<TreeViewItem<EntityId>> GetTreeViewItems(EditorWindow window)
        {
            var sceneHierarchy = SCENE_HIERARCHY_FIELD.GetValue(window);
            var treeView = TREE_VIEW_FIELD.GetValue(sceneHierarchy);
            var treeViewData = TREE_VIEW_DATA_PROPERTY.GetValue(treeView, null);
            var treeViewItems = (IEnumerable<TreeViewItem<EntityId>>) TREE_VIEW_ITEMS_METHOD.Invoke(treeViewData, null);

            return treeViewItems;
        }

        private static void ApplyDefaultIconsToAll()
        {
            var hierarchyWindows = GetAllHierarchyWindows(true);
            foreach (var window in hierarchyWindows)
            {
                var treeViewItems = GetTreeViewItems(window);
                foreach (var item in treeViewItems)
                {
                    var gameObject = TREE_VIEW_OBJECT_PROPERTY.GetValue(item, null);
                    if (gameObject == null) continue;
                    item.icon = PrefabUtility.GetIconForGameObject((GameObject) gameObject);
                }
            }
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}