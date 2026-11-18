using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>示例背景切为浅色时使用深色前景；只在模式变化/动态Item加入时扫描。</summary>
    internal sealed class SoftGlassDemoContrast
    {
        private readonly Transform _root;
        private readonly Dictionary<TMP_Text,Color> _original=new();
        private int _mode=-1;
        public SoftGlassDemoContrast(Transform root){_root=root;}
        public void Invalidate(){_mode=-1;}
        public void Refresh()
        {
            int mode=SoftGlassDemoActions.BackgroundMode;if(mode==_mode||!_root)return;_mode=mode;
            foreach(var text in _root.GetComponentsInChildren<TMP_Text>(true))
            {
                if(!_original.TryGetValue(text,out var color)){color=text.color;_original[text]=color;}
                text.color=mode==1?new Color(.035f,.07f,.13f):color;
            }
        }
    }
}
