using System;
using Ember.Basic;
using UnityEngine;

namespace Ember.UI
{
    public enum EUIMaskEffect { Inherit = 0, Solid = 1, Blur = 2 }
    public enum EUIBackdropUpdateMode { Live, Throttled, Snapshot, Off }

    public readonly struct EUIBackdropRequest
    {
        public readonly Canvas Foreground;
        public readonly int SortingOrder;
        public readonly EUIBackdropSettings Settings;
        public readonly int Generation;
        public bool FollowsForegroundOrder { get; }
        public int CurrentSortingOrder => FollowsForegroundOrder && Foreground ? Foreground.sortingOrder : SortingOrder;

        public EUIBackdropRequest(Canvas foreground, int sortingOrder, EUIBackdropSettings settings, int generation)
        {
            Foreground = foreground;
            SortingOrder = sortingOrder;
            Settings = settings;
            Generation = generation;
            FollowsForegroundOrder = false;
        }

        internal EUIBackdropRequest(Canvas foreground, EUIBackdropSettings settings, int generation)
            : this(foreground, foreground.sortingOrder, settings, generation)
        {
            FollowsForegroundOrder = true;
        }
    }

    public interface IEUIBackdropLease : IDisposable
    {
        Texture Texture { get; }
        bool IsReady { get; }
        bool IsDisposed { get; }
        int Generation { get; }
        int TextureVersion { get; }
        string Diagnostic { get; }
        void Invalidate();
        void Freeze();
    }

    public interface IEUIBackdropProvider
    {
        IEUIBackdropLease Acquire(in EUIBackdropRequest request);
        void MarkOrderDirty();
    }

    /// <summary>管线无关的接线点；后端由项目显式装配和释放。</summary>
    public static class EUIBackdrop
    {
        #region 内部参数
        public static IEUIBackdropProvider Provider { get; set; }
        public static EUIBackdropSettings DefaultSettings { get; set; }
        public static event Action<Camera, Camera, Transform> InstallationRequested;
        #endregion

        #region 外部方法
        [NoGC]
        public static EUIMaskEffect ResolveEffect(EUIMaskEffect page, EUIMaskEffect global)
        {
            if (page == EUIMaskEffect.Solid || page == EUIMaskEffect.Blur) return page;
            return global == EUIMaskEffect.Solid ? EUIMaskEffect.Solid : EUIMaskEffect.Blur;
        }

        [NoGC]
        public static void MarkOrderDirty() => Provider?.MarkOrderDirty();

        [HasGC]
        public static void RequestInstallation(Camera baseCamera, Camera uiCamera, Transform uiRoot)
            => InstallationRequested?.Invoke(baseCamera, uiCamera, uiRoot);
        #endregion
    }
}
