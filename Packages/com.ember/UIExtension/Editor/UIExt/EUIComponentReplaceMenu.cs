// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// 原生 UI 组件 → 增强组件的替换工具。
    /// 通过 <see cref="EditorUtility.CopySerialized"/> 保留同名序列化字段
    /// （targetGraphic / colors / transition / sprite / group 等），支持 Undo 撤销。
    /// </summary>
    public static class EUIComponentReplaceUtility
    {
        #region 外部方法

        /// <summary>
        /// 判断能否把 context 组件替换为目标增强组件：
        /// 属于原生类型，且尚未是目标类型（避免重复替换 / 已是增强组件时菜单变灰）。
        /// </summary>
        public static bool CanReplace<TOriginal, TReplacement>(UnityEngine.Object context)
            where TOriginal : Component
            where TReplacement : Component
        {
            return context is TOriginal && !(context is TReplacement);
        }

        /// <summary>
        /// 将 original 替换为增强组件，复制同名序列化字段，返回新组件。
        /// </summary>
        public static TReplacement Replace<TOriginal, TReplacement>(TOriginal original)
            where TOriginal : Component
            where TReplacement : Component
        {
            if (original == null) return null;
            if (original is TReplacement replacement) return replacement;

            var go = original.gameObject;

            // Selectable 等 [DisallowMultipleComponent] 组件与增强组件互斥
            // （一个 GameObject 只能有一个 Selectable），不能「先加后删」。
            // 因此先克隆备份序列化数据 → 删除原组件 → 添加增强组件 → 从备份恢复同名字段。
            var backup = UnityEngine.Object.Instantiate(original);
            if (backup != null)
            {
                backup.gameObject.SetActive(false);
                backup.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            Undo.RegisterCompleteObjectUndo(go, $"替换 {typeof(TOriginal).Name} → {typeof(TReplacement).Name}");
            Undo.DestroyObjectImmediate(original);

            var added = go.AddComponent<TReplacement>();

            if (backup != null)
            {
                CopySerializedFields(backup, added);
                UnityEngine.Object.DestroyImmediate(backup.gameObject);
            }

            EditorUtility.SetDirty(go);
            return added;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>复制时需要跳过的 MonoBehaviour 元字段（脚本引用 / 隐藏标志 / 编辑器类标识）。</summary>
        private static readonly string[] SkipSerializedProperties =
            { "m_Script", "m_ObjectHideFlags", "m_EditorClassIdentifier" };

        /// <summary>
        /// 跨类型复制同名序列化字段（按字段名匹配）。
        /// <see cref="EditorUtility.CopySerialized"/> 要求源与目标类型完全相同，
        /// 无法用于 Button → EUIButtonEx 这类继承替换，故手动遍历复制。
        /// </summary>
        private static void CopySerializedFields(Component source, Component dest)
        {
            var srcSo = new SerializedObject(source);
            var dstSo = new SerializedObject(dest);

            var it = srcSo.GetIterator();
            bool enterChildren = true;
            while (it.Next(enterChildren))
            {
                enterChildren = true;
                if (Array.IndexOf(SkipSerializedProperties, it.propertyPath) >= 0)
                    continue;

                if (dstSo.FindProperty(it.propertyPath) != null)
                    dstSo.CopyFromSerializedProperty(it);
            }

            dstSo.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }

    /// <summary>
    /// 原生组件 Inspector 右上角三点菜单的「替换为增强组件」入口。
    /// <c>[MenuItem("CONTEXT/原生类型/...")]</c> 会注册到该组件 Inspector 的
    /// context menu（即三点菜单 + 右键菜单）。
    ///
    /// <para><b>重要：</b>菜单点击发生在 Inspector 的 GUI 事件处理期间，
    /// 此时直接 DestroyImmediate + AddComponent 修改组件列表，会导致 Unity 的
    /// PropertyHandler 缓存失效检查（TestInvalidateCache，重绘 RectTransform 时触发）
    /// 访问失效的组件缓存而崩溃。因此所有替换操作统一延迟到 GUI 事件结束后执行。</para>
    /// </summary>
    public static class EUIComponentReplaceMenu
    {
        /// <summary>延迟到当前 GUI 事件结束后执行替换，避免 Inspector 绘制期间修改组件列表导致崩溃。</summary>
        private static void ReplaceDeferred<TOriginal, TReplacement>(MenuCommand command)
            where TOriginal : Component
            where TReplacement : Component
        {
            var original = (TOriginal)command.context;
            EditorApplication.delayCall += () =>
            {
                if (original == null) return; // 延迟期间组件已被删除
                var go = original.gameObject;
                if (go == null) return;
                var added = EUIComponentReplaceUtility.Replace<TOriginal, TReplacement>(original);
                if (added != null && Selection.activeObject == go)
                    EditorUtility.SetDirty(go);
            };
        }

        #region Button

        [MenuItem("CONTEXT/Button/替换为 EUIButtonEx", true)]
        private static bool ReplaceButtonValidate(MenuCommand command)
            => EUIComponentReplaceUtility.CanReplace<Button, EUIButtonEx>(command.context);

        [MenuItem("CONTEXT/Button/替换为 EUIButtonEx")]
        private static void ReplaceButton(MenuCommand command)
            => ReplaceDeferred<Button, EUIButtonEx>(command);

        #endregion

        // --------------------------------------------------------

        #region Toggle

        [MenuItem("CONTEXT/Toggle/替换为 EUIToggleEx", true)]
        private static bool ReplaceToggleValidate(MenuCommand command)
            => EUIComponentReplaceUtility.CanReplace<Toggle, EUIToggleEx>(command.context);

        [MenuItem("CONTEXT/Toggle/替换为 EUIToggleEx")]
        private static void ReplaceToggle(MenuCommand command)
            => ReplaceDeferred<Toggle, EUIToggleEx>(command);

        #endregion

        // --------------------------------------------------------

        #region Image

        [MenuItem("CONTEXT/Image/替换为 EUIImageEx", true)]
        private static bool ReplaceImageExValidate(MenuCommand command)
            => EUIComponentReplaceUtility.CanReplace<Image, EUIImageEx>(command.context);

        [MenuItem("CONTEXT/Image/替换为 EUIImageEx")]
        private static void ReplaceImageEx(MenuCommand command)
            => ReplaceDeferred<Image, EUIImageEx>(command);

        [MenuItem("CONTEXT/Image/替换为 EUICircleImage", true)]
        private static bool ReplaceCircleImageValidate(MenuCommand command)
            => EUIComponentReplaceUtility.CanReplace<Image, EUICircleImage>(command.context);

        [MenuItem("CONTEXT/Image/替换为 EUICircleImage")]
        private static void ReplaceCircleImage(MenuCommand command)
            => ReplaceDeferred<Image, EUICircleImage>(command);

        #endregion
    }
}
