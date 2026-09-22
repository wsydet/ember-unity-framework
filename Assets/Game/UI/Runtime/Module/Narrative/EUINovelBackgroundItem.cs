using Game.Narrative;
using UnityEngine;
namespace Game.UI
{
    public partial class EUINovelBackgroundItem
    {
        #region 外部方法
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
            Picture.color = new Color(1, 1, 1, hiding ? 1 - progress : progress);
            Picture.enabled = true;
        }
        public void SetOpacity(float opacity)
        {
            if (!Picture.sprite) { Clear(); return; }
            Picture.color = new Color(1, 1, 1, Mathf.Clamp01(opacity));
            Picture.enabled = true;
        }
        public void SetSolidColor(Color color)
        {
            Picture.sprite = null; Picture.preserveAspect = false;
            Picture.color = color; Picture.enabled = color.a > 0; Picture.raycastTarget = false;
        }
        public void Clear()
        {
            Picture.enabled = false;
            Picture.color = Color.clear;
            Picture.sprite = null;
        }
        public override void OnClose() { Clear(); base.OnClose(); }
        #endregion
    }
}
