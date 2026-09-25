using UnityEngine;

namespace Game.UI
{
    /// <summary>Author-defined history column and highlighting settings.</summary>
    public sealed class NovelHistoryAppearance : MonoBehaviour
    {
        #region 编辑器面板参数
        public Color SpeakerColor = new(.6f, .6f, .6f, 1);
        public Color LatestMarkerColor = new(1, .835f, .165f, 1);
        [Min(0)] public float TextIndent = 7;
        [Min(0)] public float MarkerPosition = 6;
        [Min(.1f)] public float SpeakerWidth = 5.5f;
        public bool CenterShortHistory = true;
        #endregion
    }
}
