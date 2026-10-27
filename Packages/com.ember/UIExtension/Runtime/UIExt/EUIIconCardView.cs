// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Ember.UIExtension
{
    /// <summary>中性卡片展示；不引用背包、货币或品质配置。</summary>
    public sealed class EUIIconCardView : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title, _quantity;
        [SerializeField] private GameObject _selected, _locked;
        [SerializeField] private Button _button;
        #endregion
        #region 外部方法
        public Button Button => _button;
        public void SetContent(Sprite icon, string title, string quantity, bool selected = false, bool locked = false, bool interactable = true)
        {
            _icon.sprite = icon; _icon.enabled = icon; _title.text = title ?? string.Empty;
            _quantity.text = quantity ?? string.Empty; _quantity.gameObject.SetActive(!string.IsNullOrEmpty(quantity));
            _selected.SetActive(selected); _locked.SetActive(locked); _button.interactable = interactable && !locked;
        }
        #endregion
    }
}
