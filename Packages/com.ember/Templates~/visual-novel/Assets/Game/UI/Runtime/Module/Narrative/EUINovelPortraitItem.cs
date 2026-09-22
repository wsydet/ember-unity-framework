using Game.Narrative;
using UnityEngine;
namespace Game.UI
{
    public partial class EUINovelPortraitItem
    {
        #region 内部参数
        private float _brightness = 1;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public float Brightness => _brightness;
        public Sprite CurrentSprite => Picture.sprite;
        public void Apply(NovelCommand command, Sprite sprite, float progress)
        {
            bool hiding = command.VisualAction == NovelVisualAction.Hide;
            progress = Mathf.Clamp01(progress);
            // An empty uGUI Image draws a white quad. Never fade an empty slot back in.
            if (hiding ? !Picture.sprite || progress >= 1 : !sprite)
            {
                Clear();
                return;
            }
            if (!hiding) Picture.sprite = sprite;
            Picture.color = new Color(_brightness, _brightness, _brightness, hiding ? 1 - progress : progress);
            Picture.enabled = true;
        }
        public void SetOpacity(float opacity)
        {
            if (!Picture.sprite) { Clear(); return; }
            Picture.color = new Color(_brightness, _brightness, _brightness, Mathf.Clamp01(opacity));
            Picture.enabled = true;
        }
        public void SetBrightness(float brightness)
        {
            _brightness = Mathf.Clamp01(brightness);
            var color = Picture.color; color.r = color.g = color.b = _brightness; Picture.color = color;
        }
        public void Clear()
        {
            _brightness = 1;
            Picture.enabled = false;
            Picture.color = Color.clear;
            Picture.sprite = null;
        }
        public override void OnClose() { Clear(); base.OnClose(); }
        #endregion
    }
}
