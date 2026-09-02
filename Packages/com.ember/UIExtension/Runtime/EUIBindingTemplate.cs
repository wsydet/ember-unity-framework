// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 绑定配置模板 ScriptableObject。
    /// 保存一份 EUIBinding 的完整快照，用于 Inspector 中的复制/粘贴/加载/保存。
    /// </summary>
    public class EUIBindingTemplate : ScriptableObject
    {
        #region 嵌套类型

        /// <summary>
        /// 模板中的绑定条目（使用路径而非直接引用）。
        /// </summary>
        [Serializable]
        public struct BindingEntry
        {
            public string Name;
            public string GameObjectPath;
            public EUIBinding.WidgetTypes Type;
            public string ClassName;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        [SerializeField] private string pageName;
        [SerializeField] private string classPath;
        [SerializeField] private string className;
        [SerializeField] private bool isPage;
        [SerializeField] private bool useUIUpdate;
        [SerializeField] private bool generateAutoCreateClickableMaskOverride;
        [SerializeField] private bool generateOnClickMaskOverride;
        [SerializeField] private EUIBinding.WidgetTypes selfWidgetType;
        [SerializeField] private string selfWidgetClassName;
        [SerializeField] private bool noCodeGen;
        [SerializeField] private BindingEntry[] bindings;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public BindingEntry[] Bindings => bindings;
        public EUIBinding.WidgetTypes SelfWidgetType => selfWidgetType;
        public string SelfWidgetClassName => selfWidgetClassName;
        public bool IsPage => isPage;
        public bool UseUIUpdate => useUIUpdate;
        public bool GenerateAutoCreateClickableMaskOverride => generateAutoCreateClickableMaskOverride;
        public bool GenerateOnClickMaskOverride => generateOnClickMaskOverride;
        public string PageName => pageName;
        public string ClassName => className;
        public string ClassPath => classPath;
        public bool NoCodeGeneration => noCodeGen;

        /// <summary>从 EUIBinding 组件复制所有配置</summary>
        public void CopyFromUIBinding(EUIBinding binding)
        {
            noCodeGen = binding.NoCodeGeneration;
            pageName = binding.PageName;
            isPage = binding.IsPage;
            useUIUpdate = binding.UseUIUpdate;
            generateAutoCreateClickableMaskOverride = binding.GenerateAutoCreateClickableMaskOverride;
            generateOnClickMaskOverride = binding.GenerateOnClickMaskOverride;
            classPath = binding.ClassPath;
            className = binding.ClassName;
            selfWidgetType = binding.SelfWidgetType;
            selfWidgetClassName = binding.SelfWidgetClassName;

            bindings = new BindingEntry[binding.Bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
            {
                var bd = binding.Bindings[i];
                bindings[i] = BindingEntryToTemplate(bd, binding.gameObject);
            }
        }

        /// <summary>将 BindingEntry 转为模板条目</summary>
        public static BindingEntry BindingEntryToTemplate(EUIBinding.BindingEntry bd, GameObject baseObj)
        {
            BindingEntry entry = new BindingEntry();
            entry.ClassName = bd.ClassName;
            entry.Name = bd.Name;
            entry.Type = bd.Type;
            entry.GameObjectPath = GetPathForObject(bd.GameObject, baseObj);
            return entry;
        }

        /// <summary>获取 GameObject 相对于 baseObj 的路径</summary>
        internal static string GetPathForObject(GameObject target, GameObject relativeTo)
        {
            if (!target) return null;
            Transform endT = relativeTo.transform;
            Transform cur = target.transform;
            string res = null;

            while (cur && cur != endT)
            {
                if (string.IsNullOrEmpty(res))
                    res = cur.name;
                else
                    res = cur.name + "/" + res;
                cur = cur.parent;
            }
            return res;
        }

        #endregion
    }
}
