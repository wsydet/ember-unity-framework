using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>Owns keyboard selection while a clickable modal mask is present; never changes rendering.</summary>
    [DefaultExecutionOrder(-32000)]
    internal sealed class EUIModalInputScope : MonoBehaviour
    {
        #region 内部参数
        private sealed class Scope
        {
            public EUIPage Page;
            public GameObject ReturnSelection;
            public Scope ReturnScope;
        }

        private readonly List<Scope> _scopes = new();
        private readonly List<Selectable> _selectables = new();
        private IReadOnlyList<EUIPage> _pages;
        private IReadOnlyDictionary<EUIPage, GameObject> _masks;
        private Action _synchronizeMasks;
        private Scope _top;
        private GameObject _pendingRestore;
        #endregion

        #region 生命周期
        private void Update() { _synchronizeMasks?.Invoke(); Refresh(); }
        private void OnDestroy()
        {
            if (_pages != null)
                foreach (var page in _pages) page.SetModalInputBlocked(false);
            _scopes.Clear(); _top = null;
        }
        #endregion

        #region 内部方法
        private bool IsModal(EUIPage page)
        {
            return page != null && !page.IsDisposed && page.GameObject && page.GameObject.activeInHierarchy &&
                page.State != PageState.ViewHidden && _masks.TryGetValue(page, out var mask) && mask && mask.activeInHierarchy;
        }

        private bool BelongsTo(GameObject selection, EUIPage page)
        {
            return selection && page != null && page.GameObject && selection.transform.IsChildOf(page.Transform);
        }

        private bool CanSelect(GameObject selection)
        {
            if (!selection || !selection.activeInHierarchy) return false;
            var selectable = selection.GetComponent<Selectable>();
            return !selectable || (selectable.IsActive() && selectable.IsInteractable());
        }

        private bool BelowModal(GameObject selection)
        {
            if (_top == null || !selection) return false;
            foreach (var page in _pages)
                if (page.ModalInputBlocked && BelongsTo(selection, page)) return true;
            return false;
        }

        private GameObject FirstSelection(EUIPage page)
        {
            _selectables.Clear(); page.GameObject.GetComponentsInChildren(false, _selectables);
            foreach (var selectable in _selectables)
                if (CanSelect(selectable.gameObject)) return selectable.gameObject;
            return null;
        }
        #endregion

        #region 外部方法
        internal void Initialize(IReadOnlyList<EUIPage> pages, IReadOnlyDictionary<EUIPage, GameObject> masks, Action synchronizeMasks)
        {
            _pages = pages; _masks = masks; _synchronizeMasks = synchronizeMasks;
        }

        internal void Refresh()
        {
            if (_pages == null || _masks == null) return;
            var es = EventSystem.current;
            var previousSelection = es ? es.currentSelectedGameObject : null;
            bool selectedElsewhere = _top != null && CanSelect(previousSelection) &&
                !BelongsTo(previousSelection, _top.Page) && !BelowModal(previousSelection);
            GameObject restore = null;
            // Repair return chains before removing a lower modal (including out-of-order close).
            for (int i = _scopes.Count - 1; i >= 0; i--)
            {
                var scope = _scopes[i];
                // Hidden/deactivated views keep their opening generation and return chain.
                if (scope.Page != null && !scope.Page.IsDisposed && scope.Page.GameObject &&
                    _masks.ContainsKey(scope.Page)) continue;
                foreach (var other in _scopes)
                    if (other.ReturnScope == scope || BelongsTo(other.ReturnSelection, scope.Page))
                    { other.ReturnSelection = scope.ReturnSelection; other.ReturnScope = scope.ReturnScope; }
                if (scope == _top) restore = scope.ReturnSelection;
                _scopes.RemoveAt(i);
            }
            foreach (var pair in _masks)
            {
                if (!IsModal(pair.Key)) continue;
                bool found = false;
                foreach (var scope in _scopes) if (scope.Page == pair.Key) { found = true; break; }
                if (!found)
                {
                    var selectedBefore = es ? es.currentSelectedGameObject : null;
                    _scopes.Add(new Scope { Page = pair.Key, ReturnSelection = selectedBefore,
                        ReturnScope = _top != null && BelongsTo(selectedBefore, _top.Page) ? _top : null });
                }
            }
            Scope top = null;
            foreach (var scope in _scopes)
                if (IsModal(scope.Page) && (top == null || scope.Page.Canvas.sortingOrder > top.Page.Canvas.sortingOrder)) top = scope;
            bool changed = top != _top;
            if (changed && _top != null && !IsModal(_top.Page) && !restore) restore = _top.ReturnSelection;
            _top = top;
            if (restore) _pendingRestore = restore;
            foreach (var page in _pages)
            {
                var canvas = page.Canvas;
                page.SetModalInputBlocked(top != null && canvas && canvas.sortingOrder < top.Page.Canvas.sortingOrder &&
                    !(page.GameObject && page.Transform.IsChildOf(top.Page.Transform)));
            }
            if (!es) return;
            var selected = es.currentSelectedGameObject;
            // Do not steal focus back from a higher independent UI (for example the GM panel).
            if (changed && selectedElsewhere && !BelowModal(selected)) { _pendingRestore = null; return; }
            // Deselect delivers TMP_InputField.OnDeselect, ending character/IME consumption immediately.
            if (BelowModal(selected) || (selected && (!selected.activeInHierarchy || (top != null && !CanSelect(selected)))) || changed ||
                (CanSelect(_pendingRestore) && !BelowModal(_pendingRestore)) || (!selected && top != null))
            {
                GameObject next = CanSelect(_pendingRestore) && !BelowModal(_pendingRestore) ? _pendingRestore : null;
                if (next) _pendingRestore = null;
                if (!next && top != null)
                    next = CanSelect(selected) && !BelowModal(selected) ? selected : FirstSelection(top.Page);
                es.SetSelectedGameObject(next);
            }
        }
        #endregion
    }
}
