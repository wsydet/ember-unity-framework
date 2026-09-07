// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using Ember.UI;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>根据 Item 角色的 EUIBinding 创建轻量 EUIItem。</summary>
    public static class EUIItemFactory
    {
        /// <summary>
        /// 从实例根节点创建 Item。该方法不会实例化或销毁 GameObject，实例所有权仍属于调用方。
        /// </summary>
        public static bool TryCreate(GameObject instance, out EUIItem item, out string error)
        {
            item = null;
            if (!instance)
            {
                error = "EUIItem instance is missing.";
                return false;
            }

            var binding = instance.GetComponent<EUIBinding>();
            if (!binding)
            {
                error = $"EUIItem '{instance.name}' requires EUIBinding on its root.";
                return false;
            }

            if (binding.Role != EUIBindingRole.Item)
            {
                error = $"EUIBinding on '{instance.name}' is configured as Page, not Item.";
                return false;
            }

            if (!instance.TryGetComponent(out RectTransform _))
            {
                error = $"EUIItem '{instance.name}' requires RectTransform on its root.";
                return false;
            }

            EUILogic logic = null;
            if (!string.IsNullOrWhiteSpace(binding.ClassName))
            {
                Type logicType = EUIBindingBridge.FindLogicType(binding.ClassName);
                if (logicType == null)
                {
                    error = $"EUIItem logic type '{binding.ClassName}' was not found for '{instance.name}'.";
                    return false;
                }

                try
                {
                    logic = (EUILogic)Activator.CreateInstance(logicType);
                }
                catch (Exception ex)
                {
                    error = $"Creating EUIItem logic '{logicType.FullName}' failed: {ex}";
                    return false;
                }
            }

            try
            {
                item = new EUIItem(
                    instance,
                    logic,
                    (map, ownerLogic) =>
                        EUIBindingBridge.PopulateControlMap(binding, map, ownerLogic),
                    binding.PageSettings);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Initializing EUIItem '{instance.name}' failed: {ex}";
                return false;
            }
        }
    }
}
