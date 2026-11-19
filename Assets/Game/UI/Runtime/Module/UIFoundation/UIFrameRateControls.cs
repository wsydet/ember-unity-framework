using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>编辑器制作的固定控件；运行时只接线与呈现同一策略。</summary>
    public sealed class UIFrameRateControls : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Button _economy, _balanced, _responsive;
        [SerializeField] private Toggle _automatic;
        [SerializeField] private TMP_InputField _custom;
        [SerializeField] private TMP_Text _summary, _error;
        [SerializeField] private bool _darkSurface = true;
        #endregion
        #region 内部参数
        private float _nextRefresh;
        #endregion
        #region 生命周期
        private void OnEnable()
        {
            _economy.onClick.AddListener(Economy); _balanced.onClick.AddListener(Balanced); _responsive.onClick.AddListener(Responsive);
            if (_automatic) _automatic.onValueChanged.AddListener(Automatic);
            if (_custom) _custom.onEndEdit.AddListener(Custom);
            UIFrameRatePolicy.Changed += Refresh; Refresh();
        }
        private void OnDisable()
        {
            _economy.onClick.RemoveListener(Economy); _balanced.onClick.RemoveListener(Balanced); _responsive.onClick.RemoveListener(Responsive);
            if (_automatic) _automatic.onValueChanged.RemoveListener(Automatic);
            if (_custom) _custom.onEndEdit.RemoveListener(Custom);
            UIFrameRatePolicy.Changed -= Refresh;
        }
        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .5f; Refresh();
        }
        #endregion
        #region 内部方法
        private void Economy() => UIFrameRatePolicy.SetMode(UIFrameRateMode.Economy);
        private void Balanced() => UIFrameRatePolicy.SetMode(UIFrameRateMode.Balanced);
        private void Responsive() => UIFrameRatePolicy.SetMode(UIFrameRateMode.Responsive);
        private void Automatic(bool value) => UIFrameRatePolicy.SetAutomatic(value);
        private void Custom(string text)
        {
            if (!UIFrameRatePolicy.SetCustom(text)) { if (_error) _error.text = "请输入正整数"; }
            else if (_error) _error.text = "";
        }
        private void Refresh()
        {
            bool desktop = UIFrameRatePolicy.IsDesktop, responsive = UIFrameRatePolicy.Mode == UIFrameRateMode.Responsive;
            _economy.interactable = _balanced.interactable = _responsive.interactable = desktop;
            var buttons = new[] { _economy, _balanced, _responsive };
            for (int i = 0; i < buttons.Length; i++)
            {
                bool selected=(int)UIFrameRatePolicy.Mode==i;
                if (buttons[i].targetGraphic) buttons[i].targetGraphic.color = selected ? new Color(.88f, .96f, 1f, .92f) : _darkSurface?new Color(.025f,.07f,.11f,.82f):new Color(.35f, .55f, .85f, .18f);
                var label=buttons[i].GetComponentInChildren<TMP_Text>();if(label)label.color=selected||!_darkSurface?new Color(.035f,.1f,.18f):Color.white;
            }
            if (_summary) _summary.text = UIFrameRatePolicy.DescribeActual();
            if (_automatic) { _automatic.interactable = desktop && responsive; _automatic.SetIsOnWithoutNotify(UIFrameRatePolicy.Automatic); }
            if (_custom)
            {
                _custom.interactable = desktop && responsive && !UIFrameRatePolicy.Automatic;
                if (!_custom.isFocused) _custom.SetTextWithoutNotify(UIFrameRatePolicy.CustomTarget.ToString());
            }
        }
        #endregion
    }
}
