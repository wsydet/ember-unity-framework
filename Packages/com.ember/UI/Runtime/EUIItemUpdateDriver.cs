// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// EUIItem 的运行时 Update 桥。由 EUIItem 自动附加，业务预制体无需手动配置。
    /// </summary>
    [AddComponentMenu("")]
    public sealed class EUIItemUpdateDriver : MonoBehaviour
    {
        private EUIItem _item;

        internal void Bind(EUIItem item)
        {
            _item = item;
        }

        private void Update()
        {
            _item?.Tick();
        }
    }
}
