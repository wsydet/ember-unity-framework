using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>业务层提供实际配置编辑器；不通过定义工厂创建状态或推测参数。</summary>
    public static class EmberFsmBlueprintParameters
    {
        #region 内部参数
        private static readonly Dictionary<string, Action<EmberFsmBlueprintWindow>> Drawers = new();
        private static readonly Dictionary<string, Action<EmberFsmBlueprintWindow>> Toolbars = new();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Register(string key, Action<EmberFsmBlueprintWindow> draw, Action<EmberFsmBlueprintWindow> toolbar = null)
        { Drawers[key] = draw ?? throw new ArgumentNullException(nameof(draw)); Toolbars[key] = toolbar; }
        public static void Remove(string key) { Drawers.Remove(key); Toolbars.Remove(key); }
        public static void DrawToolbar(EmberFsmBlueprintWindow window)
        { foreach (var draw in Toolbars.Values) draw?.Invoke(window); }
        public static void Draw(EmberFsmBlueprintWindow window)
        {
            foreach (var draw in Drawers.Values) draw(window);
        }

        /// <summary>只编辑调用方开放的真实序列化参数；ApplyModifiedProperties 使用 Unity Undo。</summary>
        public static void DrawProperties(ScriptableObject asset, params string[] fields)
        {
            if (asset == null) { EditorGUILayout.HelpBox("缺少配置资产。", MessageType.Error); return; }
            using var serialized = new SerializedObject(asset);
            serialized.Update();
            foreach (var name in fields)
            {
                var property = serialized.FindProperty(name);
                if (property == null) EditorGUILayout.HelpBox("配置字段已不存在：" + name, MessageType.Error);
                else EditorGUILayout.PropertyField(property, true);
            }
            serialized.ApplyModifiedProperties();
        }
        #endregion
    }

    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 编辑器面板参数
        [SerializeField] private ScriptableObject _parameterPreset;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EmberFsmGraph ParameterGraph => _graph;
        public EmberFsmNodeDescription ParameterNode => _selected;
        public ScriptableObject ParameterPreset { get => _parameterPreset; set => _parameterPreset = value; }
        #endregion
    }
}
