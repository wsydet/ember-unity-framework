using System.Collections.Generic;
using Ember.UI;
using Ember.UIExtension;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class GMHudController : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private EUIFloatingEntry _entry;
        [SerializeField] private RectTransform _container;
        [SerializeField] private Button _outside;
        [SerializeField] private TMP_Text _empty;
        [SerializeField] private GameObject _actionPrefab;
        #endregion
        #region 内部参数
        private readonly List<EUIItem> _items = new();
        private int _registry, _favorites, _tier, _pendingTier = -1;
        private float _pendingAt, _nextRefresh;
        private bool _open;
        private Rect _lastSafe;
        private Vector2 _lastEntry;
        private int _avoidHash;
        public bool IsOpen => _open;
        public int SizeTier => _tier;
        #endregion
        #region 生命周期
        private void OnEnable()
        {
            _entry.LongPressEnabled = true; _entry.LongPressed += Toggle; _entry.DragStarted += Close;
            _entry.DockConstraint = ConstrainDock; _outside.onClick.AddListener(Close); GMFavorites.Changed += FavoritesChanged;
            Close();
        }
        private void OnDisable()
        {
            _entry.LongPressed -= Toggle; _entry.DragStarted -= Close; _entry.LongPressEnabled = false; _entry.DockConstraint = null;
            _outside.onClick.RemoveListener(Close); GMFavorites.Changed -= FavoritesChanged; Close();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) Close(); }
        private void LateUpdate()
        {
            if (!_open) return;
            if (GMActionRegistry.ConfirmationActive) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .08f;
            if (_registry != GMActionRegistry.Version || _favorites != GMFavorites.Version) Build();
            foreach (var item in _items) ((EUISoftGlassGMActionItem)item.Logic).Refresh();
            Position(false);
        }
        #endregion
        #region 内部方法
        private void FavoritesChanged() { if (_open) Build(); }
        private void Clear()
        {
            foreach (var item in _items) { var go = item.GameObject; item.Dispose(); if (go) Destroy(go); }
            _items.Clear();
        }
        private void Build()
        {
            Clear();
            foreach (string key in GMFavorites.Snapshot())
            {
                if (!GMActionRegistry.TryFind(key, out _)) continue;
                var go = Instantiate(_actionPrefab, _container, false);
                if (!EUIItemFactory.TryCreate(go, out var item, out _)) { Destroy(go); continue; }
                ((EUISoftGlassGMActionItem)item.Logic).BindAction(key); item.Show(); _items.Add(item);
            }
            _registry = GMActionRegistry.Version; _favorites = GMFavorites.Version;
            _empty.transform.parent.gameObject.SetActive(_items.Count == 0); _empty.text = "长按或右键项目操作来收藏";
            _pendingTier = -1; Position(true); GetComponent<EUIBackdropGroup>()?.RefreshSurfaces();
        }
        private Rect SafeBounds()
        {
            var parent = (RectTransform)_container.parent; var canvas = _entry.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.min, canvas.worldCamera, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Screen.safeArea.max, canvas.worldCamera, out var max);
            return Rect.MinMaxRect(min.x + 12, min.y + 12, max.x - 12, max.y - 12);
        }
        private Vector2 ConstrainDock(Vector2 position)
        {
            var parent = (RectTransform)_container.parent; var avoid = GMHudLayout.GetAvoidRects(parent); float r = ((RectTransform)_entry.transform).rect.width * .5f;
            float best = position.y, distance = float.PositiveInfinity;
            var safe = SafeBounds();
            var candidates = new List<float> { position.y, safe.yMin + r, safe.yMax - r };
            foreach (var rect in avoid) { candidates.Add(rect.yMin - r - 1); candidates.Add(rect.yMax + r + 1); }
            foreach (float y in candidates)
            {
                if (y - r < safe.yMin || y + r > safe.yMax) continue;
                bool clear = true;
                foreach (var rect in avoid) if (position.x + r > rect.xMin && position.x - r < rect.xMax && y + r > rect.yMin && y - r < rect.yMax) { clear = false; break; }
                if (clear && Mathf.Abs(y - position.y) < distance) { best = y; distance = Mathf.Abs(y - position.y); }
            }
            return new Vector2(position.x, best);
        }
        private void Position(bool force)
        {
            if (!_open) return;
            var parent = (RectTransform)_container.parent; var entryRect = (RectTransform)_entry.transform;
            Vector2 entry = parent.InverseTransformPoint(entryRect.position); float size = entryRect.rect.width;
            Rect safe = SafeBounds(); var avoid = GMHudLayout.GetAvoidRects(parent); int hash = 17;
            foreach (var rect in avoid) hash = hash * 31 + rect.GetHashCode();
            bool geometryChanged = safe != _lastSafe || entry != _lastEntry || hash != _avoidHash;
            if (!force && !geometryChanged && _pendingTier < 0) return;
            // 180ms 必须是同一可用几何的连续稳定时间，避让重新出现后不能沿用旧计时。
            if (geometryChanged) _pendingTier = -1;
            _lastSafe = safe; _lastEntry = entry; _avoidHash = hash;
            if (_items.Count == 0)
            {
                var r = (RectTransform)_empty.transform.parent;
                foreach(float yOffset in new[]{0f,80f,-80f,160f,-160f})
                {
                    float x=Mathf.Clamp(entry.x+(entry.x>safe.center.x?-190:190),safe.xMin+130,safe.xMax-130);
                    float y=Mathf.Clamp(entry.y+yOffset,safe.yMin+30,safe.yMax-30);var card=new Rect(x-130,y-30,260,60);
                    bool clear=card.xMin>=safe.xMin&&card.xMax<=safe.xMax&&card.yMin>=safe.yMin&&card.yMax<=safe.yMax;
                    foreach(var block in avoid)if(card.Overlaps(block)){clear=false;break;}
                    var entryBounds=new Rect(entry.x-size*.5f,entry.y-size*.5f,size,size);if(card.Overlaps(entryBounds))clear=false;
                    if(clear){r.anchoredPosition=new Vector2(x,y);return;}
                }
                Close();EUICommonDialogs.ShowAlert(new EUIDialogRequest{Title="收藏快捷操作",Message="长按或右键主面板中的项目操作来收藏。"});return;
            }
            for (int tier = 0; tier < 4; tier++)
            {
                if (!GMHudLayout.TryFit(_items.Count, entry, size, safe, avoid, tier, 0, out var points, out float visible, out float hit)) continue;
                if (!force && tier < _tier)
                {
                    if (!GMHudLayout.TryFit(_items.Count, entry, size, safe, avoid, tier, 4, out _, out _, out _)) { _pendingTier = -1; tier = _tier - 1; continue; }
                    if (_pendingTier != tier) { _pendingTier = tier; _pendingAt = Time.unscaledTime; }
                    if (Time.unscaledTime - _pendingAt < .18f) { tier = _tier - 1; continue; }
                }
                if (tier != _tier || _pendingTier < 0) _pendingTier = -1;
                _tier = tier;
                for (int i = 0; i < points.Length; i++)
                {
                    var rect = (RectTransform)_items[i].GameObject.transform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = points[i];
                    ((EUISoftGlassGMActionItem)_items[i].Logic).SetShortcut(visible, hit);
                }
                return;
            }
            Close(); EUICommonDialogs.ShowAlert(new EUIDialogRequest { Title = "快捷操作空间不足", Message = "请短按 GM 入口，在主面板操作。收藏已保留。" });
        }
        #endregion
        #region 外部方法
        public void Toggle()
        {
            if (_open) { Close(); return; }
            _open = true; _container.gameObject.SetActive(true); _outside.gameObject.SetActive(true); Build();
        }
        public void Close()
        {
            _open = false; _pendingTier = -1; Clear();
            _container.gameObject.SetActive(false); _outside.gameObject.SetActive(false);
            GetComponent<EUIBackdropGroup>()?.RefreshSurfaces();
        }
        #endregion
    }
}
