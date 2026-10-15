using Game.Entity;
using UnityEngine;

namespace Game.UI
{
    public partial class EUIPlayerHealthItem
    {
        private EntityHealth _health;
        public void BindHealth(EntityHealth health)
        {
            if (_health != null) _health.Changed -= OnHealthChanged;
            _health = health;
            if (_health != null) _health.Changed += OnHealthChanged;
            RefreshHealth();
        }
        private void OnHealthChanged(float previous, float current) => RefreshHealth();
        private void RefreshHealth()
        {
            float ratio = _health == null ? 0 : Mathf.Clamp01(_health.Current / _health.Maximum);
            var anchors = HealthFill.anchorMax; anchors.x = ratio; HealthFill.anchorMax = anchors;
            HealthValue.text = _health == null ? "" : $"{_health.Current:0.#} / {_health.Maximum:0.#}";
        }
    }
}
