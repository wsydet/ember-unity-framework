using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Borodar.RainbowHierarchy
{
    [SuppressMessage("ReSharper", "ConvertIfStatementToNullCoalescingExpression")]
    public static class ColorHelper
    {
        public static readonly Color POPUP_BORDER_CLR_FREE = new Color(0.51f, 0.51f, 0.51f);
        public static readonly Color POPUP_BORDER_CLR_PRO = new Color(0.13f, 0.13f, 0.13f);

        public static readonly Color POPUP_BACKGROUND_CLR_FREE = new Color(0.83f, 0.83f, 0.83f);
        public static readonly Color POPUP_BACKGROUND_CLR_PRO = new Color(0.18f, 0.18f, 0.18f);

        public static readonly Color SEPARATOR_CLR_1_FREE = new Color(0.65f, 0.65f, 0.65f, 1f);
        public static readonly Color SEPARATOR_CLR_2_FREE = new Color(0.9f, 0.9f, 0.9f, 1f);
        public static readonly Color SEPARATOR_CLR_1_PRO = new Color(0.13f, 0.13f, 0.13f, 1f);
        public static readonly Color SEPARATOR_CLR_2_PRO = new Color(0.22f, 0.22f, 0.22f, 1f);
    }
}