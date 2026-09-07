// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Ember.Basic;
using Ember.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 桥接 EUIBinding 到 EUIPage 的 Logic 层。
    /// 在 EUIManager.OnPageCreated 钩子中自动配置 Logic。
    ///
    /// <para>通过 <c>[RuntimeInitializeOnLoadMethod]</c> 自动注册，
    /// 无需手动调用。引入 com.ember 包即自动生效。</para>
    /// </summary>
    public static class EUIBindingBridge
    {
        private const string TAG = LogTags.EmberUI;

        private static bool _registered;
        private static readonly Dictionary<string, Type> LogicTypeCache =
            new Dictionary<string, Type>(StringComparer.Ordinal);

        /// <summary>
        /// 自动注册到 EUIManager.OnPageCreated 钩子。
        /// 由 <c>[RuntimeInitializeOnLoadMethod]</c> 驱动，应用启动时自动调用。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoRegister()
        {
            Register();
        }

        /// <summary>
        /// 注册到 EUIManager.OnPageCreated 钩子。
        /// 幂等操作，多次调用安全。
        /// </summary>
        public static void Register()
        {
            if (_registered) return;
            _registered = true;
            EUIManager.OnPageCreated += OnPageCreated;
        }

        /// <summary>
        /// 从 EUIBinding 读取配置并初始化 EUIPage 的 Logic 层。
        /// </summary>
        public static void Attach(EUIPage page, EUIBinding binding)
        {
            if (page == null || binding == null) return;
            if (binding.Role != EUIBindingRole.Page)
            {
                EmberDebug.LogError(
                    TAG,
                    $"[EUI] Item '{binding.name}' cannot be attached to EUIPage or opened through EUIManager.");
                return;
            }

            // 遮罩配置注入（与 Logic/ClassName 无关，无 Logic 的页面同样生效）
            page.UseMask = binding.UseMask;
            page.MaskColorOverride = binding.MaskColor;
            page.ClickMaskToClose = binding.ClickMaskToClose;

            if (string.IsNullOrEmpty(binding.ClassName)) return;

            Type logicType = FindLogicType(binding.ClassName);

            if (logicType != null)
            {
                page.LogicTypeName = logicType.FullName;
                // 传入逻辑实例引用，供子 UIBinding 注册
                page.CreateLogic((map, logic) => {
                    logic.CustomSettings = binding.PageSettings;
                    PopulateControlMap(binding, map, logic);
                });
            }
        }

        /// <summary>
        /// 从 EUIBinding 的 BindingEntry 列表填充 ControlMap。
        /// 遇到 UILogic 类型时自动创建子 Logic 实例并注册到父 Logic。
        /// </summary>
        public static void PopulateControlMap(EUIBinding binding, Dictionary<string, Component> map, EUILogic parentLogic = null)
        {
            if (binding == null || map == null) return;
            if (binding.Bindings == null) return;

            foreach (var entry in binding.Bindings)
            {
                if (string.IsNullOrEmpty(entry.Name) || entry.GameObject == null)
                    continue;

                if (entry.Type == EUIBinding.WidgetTypes.UILogic)
                {
                    // 嵌套 UIBinding：创建独立的子 Logic（对标 Burner CreateNewLogicFromBinding）
                    var childBinding = entry.GameObject.GetComponent<EUIBinding>();
                    if (childBinding != null && parentLogic != null && !string.IsNullOrEmpty(childBinding.ClassName))
                    {
                        Type childLogicType = FindLogicType(childBinding.ClassName);

                        if (childLogicType != null)
                        {
                            var childLogic = (EUILogic)Activator.CreateInstance(childLogicType);
                            childLogic.ControlMap = new Dictionary<string, Component>();
                            parentLogic.RegisterChildLogic(childLogic);
                            // 递归填充子 UIBinding 的控件（可能还有更深层嵌套）
                            PopulateControlMap(childBinding, childLogic.ControlMap, childLogic);
                            childLogic.OnBeginLoad();
                            childLogic.OnBind();
                        }
                    }
                    continue;
                }

                var comp = GetComponentForType(entry.GameObject, entry.Type, entry.ClassName);
                if (comp != null)
                    map[entry.Name] = comp;
            }
        }

        private static void OnPageCreated(EUIPage page)
        {
            if (page == null) return;
            var binding = page.GameObject.GetComponent<EUIBinding>();
            if (binding != null)
            {
                // 注入过渡动画配置（独立于 Logic 绑定，即使没有 Logic 类也生效）
                page.SetTransition(binding.UsePresetFade, binding.UseTransitionBlock, binding.UseAnimator, binding.UseCustomTransition, binding.FadeInTime, binding.FadeOutTime);

                Attach(page, binding);
            }
        }

        private static Component GetComponentForType(GameObject go, EUIBinding.WidgetTypes type, string className)
        {
            switch (type)
            {
                case EUIBinding.WidgetTypes.Text:
                    return go.GetComponent<TMP_Text>() ?? (Component)go.GetComponent<Text>();
                case EUIBinding.WidgetTypes.Image:
                    return go.GetComponent<Image>();
                case EUIBinding.WidgetTypes.RawImage:
                    return go.GetComponent<RawImage>();
                case EUIBinding.WidgetTypes.Button:
                    return go.GetComponent<Button>();
                case EUIBinding.WidgetTypes.Toggle:
                    return go.GetComponent<Toggle>();
                case EUIBinding.WidgetTypes.ToggleGroup:
                    return go.GetComponent<ToggleGroup>();
                case EUIBinding.WidgetTypes.InputField:
                    return go.GetComponent<TMP_InputField>() ?? (Component)go.GetComponent<InputField>();
                case EUIBinding.WidgetTypes.ScrollRect:
                    return go.GetComponent<ScrollRect>();
                case EUIBinding.WidgetTypes.ProgressBar:
                    return go.GetComponent<Slider>();
                case EUIBinding.WidgetTypes.Canvas:
                    return go.GetComponent<Canvas>();
                case EUIBinding.WidgetTypes.CanvasGroup:
                    return go.GetComponent<CanvasGroup>();
                case EUIBinding.WidgetTypes.Component:
                    return GetCustomComponent(go);
                case EUIBinding.WidgetTypes.UILogic:
                case EUIBinding.WidgetTypes.Extension:
                    if (!string.IsNullOrEmpty(className))
                    {
                        Type componentType = FindType(className, typeof(Component));
                        if (componentType != null)
                            return go.GetComponent(componentType) as Component;
                    }
                    return null;
                default:
                    return go.GetComponent<Transform>();
            }
        }

        /// <summary>
        /// 「Component」绑定类型解析：返回节点上非 UI 基础设施组件（Transform/RectTransform/Canvas/CanvasGroup/CanvasRenderer）的自定义组件。
        /// 自动收集会把无法归类为内置 UI 类型的自定义组件标记为 Component，运行时需还原出该组件（如 EUITransitionBlock），
        /// 否则退回 Transform 会让业务层的接口强转（as IEUITransitionEffect）恒为空。
        /// </summary>
        private static Component GetCustomComponent(GameObject go)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                // Transform 已覆盖 RectTransform（RectTransform 继承自 Transform）；跳过基础设施，返回真正的自定义组件
                if (comp is Transform || comp is Canvas || comp is CanvasGroup || comp is CanvasRenderer)
                    continue;
                return comp;
            }
            return go.GetComponent<Transform>();
        }

        internal static Type FindLogicType(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return null;

            if (LogicTypeCache.TryGetValue(className, out Type cachedType))
                return cachedType;

            Type logicType = FindType(className, typeof(EUILogic));
            if (logicType != null)
                LogicTypeCache[className] = logicType;
            return logicType;
        }

        private static Type FindType(string className, Type requiredBaseType)
        {
            if (string.IsNullOrWhiteSpace(className) || requiredBaseType == null)
                return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type exactType = assembly.GetType(className, false);
                if (IsUsableType(exactType, requiredBaseType))
                    return exactType;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                    continue;

                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (type != null
                        && string.Equals(type.Name, className, StringComparison.Ordinal)
                        && IsUsableType(type, requiredBaseType))
                        return type;
                }
            }

            return null;
        }

        private static bool IsUsableType(Type type, Type requiredBaseType)
        {
            return type != null
                && requiredBaseType.IsAssignableFrom(type)
                && !type.IsAbstract;
        }
    }
}
