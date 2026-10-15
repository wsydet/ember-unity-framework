using System;
using Ember.UI;
using Ember.UIExtension;
using Game.Player;
using UnityEngine;

namespace Game.UI
{
    public partial class EUIGamePlayPage
    {
        private EUIItem _healthItem;
        private void RefreshHealthItem(PlayerController player)
        {
            bool visible = player != null && player.Context?.ShowHealth == true;
            if (_healthItem == null)
            {
                if (!ControlMap.TryGetValue("HealthRoot", out var component) || component is not RectTransform root)
                {
                    if (visible) throw new InvalidOperationException("生命HUD已开启，但正式页面缺少HealthRoot绑定。");
                    return;
                }
                if (!visible) { root.gameObject.SetActive(false); return; }
                if (!EUIItemFactory.TryCreate(root.gameObject, out _healthItem, out var error))
                    throw new InvalidOperationException(error);
            }
            ((EUIPlayerHealthItem)_healthItem.Logic).BindHealth(visible ? player.Context.Health : null);
            if (visible) _healthItem.Show(); else _healthItem.Hide();
        }
        private void ReleaseHealthItem(PlayerController player)
        {
            if (_healthItem == null) return;
            ((EUIPlayerHealthItem)_healthItem.Logic).BindHealth(null); _healthItem.Hide();
        }
    }
}
