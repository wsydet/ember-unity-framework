using System;
using System.Collections.Generic;

namespace Game.Module
{
    /// <summary>
    /// 红点树节点 —— 对应一个 UI 红点位置，构成树形层级。
    ///
    /// 一个节点可承载多个功能类型的数量，子节点的变化会向上传播，
    /// 父节点的最终数值由「自身 + 所有子节点」聚合得出。
    /// UI 侧通过 <see cref="NumChanged"/> 订阅变化、读 <see cref="Num"/> 决定显示形态。
    /// </summary>
    public class RedDotNode
    {
        #region 内部参数

        /// <summary>显示为普通圆点（非数字）</summary>
        public const int ShowNormal = -1;

        /// <summary>显示为文本标记（如「新」）</summary>
        public const int ShowText = -2;

        /// <summary>显示为特殊标记</summary>
        public const int ShowSpecial = -3;

        /// <summary>父节点（外层红点）。null 表示根。</summary>
        public RedDotNode Parent { get; internal set; }

        /// <summary>子节点列表（内层红点）。未建立子级时为 null。</summary>
        public List<RedDotNode> Children { get; internal set; }

        /// <summary>数值变化回调。UI 订阅它刷新红点显示（多播）。</summary>
        public event Action<RedDotNode> NumChanged;

        /// <summary>该节点承载的各功能类型数量。value 为 0 时移除。</summary>
        private readonly Dictionary<RedDotType, int> _numDic = new();

        /// <summary>该节点的显示配置（itemType + 显示形态）。</summary>
        private readonly RedDotUIItemData _config;

        /// <summary>红点 UI 节点类型。</summary>
        public RedDotUIItemType ItemType => _config.itemType;

        /// <summary>是否显示为普通圆点。</summary>
        public bool ShowAsNormal => _config.showAsNormal;

        /// <summary>是否显示为数字。</summary>
        public bool ShowAsNum => _config.showAsNum;

        /// <summary>是否显示为文本标记。</summary>
        public bool ShowAsText => _config.showAsText;

        /// <summary>是否显示为特殊标记。</summary>
        public bool ShowAsSpec => _config.showAsSpec;

        /// <summary>
        /// 聚合后的红点数值。显示优先级：特殊 → 数字 → 普通 → 文本。
        /// UI 侧按此值 + <see cref="ShowAsNum"/> 等标志决定最终显示形态。
        /// </summary>
        public int Num
        {
            get
            {
                int num = 0;

                // 特殊：自身或任意子节点为特殊即显示特殊
                if (ShowAsSpec)
                {
                    foreach (var item in _numDic)
                        if (item.Value == ShowSpecial) return ShowSpecial;
                    if (Children != null)
                        foreach (var child in Children)
                            if (child.Num == ShowSpecial) return ShowSpecial;
                }

                // 数字：累加自身与所有子节点的正数
                if (ShowAsNum)
                {
                    foreach (var item in _numDic)
                        if (item.Value > 0) num += item.Value;
                    if (Children != null)
                        foreach (var child in Children)
                            num += child.Num;
                    if (num > 0) return Math.Min(num, 99);
                }

                // 普通：自身或任意子节点存在普通/数字/文本即显示普通
                if (ShowAsNormal)
                {
                    foreach (var item in _numDic)
                        if (item.Value == ShowNormal || item.Value > 0) return ShowNormal;
                    if (Children != null)
                        foreach (var child in Children)
                        {
                            int childNum = child.Num;
                            if (childNum == ShowNormal || childNum > 0 || childNum == ShowText)
                                return ShowNormal;
                        }
                }

                // 文本：自身或任意子节点存在文本即显示文本
                if (ShowAsText)
                {
                    foreach (var item in _numDic)
                        if (item.Value == ShowText) return ShowText;
                    if (Children != null)
                        foreach (var child in Children)
                            if (child.Num == ShowText) return ShowText;
                }

                return num;
            }
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>由 <see cref="RedDotModule"/> 在构建树时创建。</summary>
        internal RedDotNode(RedDotUIItemData config)
        {
            _config = config;
        }

        /// <summary>清空该节点所有数量（模块销毁时用）。</summary>
        internal void ClearNums()
        {
            _numDic.Clear();
        }

        /// <summary>子节点变化向上传播：刷新自身数值并继续通知父节点。</summary>
        internal void NotifyFromChild()
        {
            NotifyNumChange();
            Parent?.NotifyFromChild();
        }

        /// <summary>触发 <see cref="NumChanged"/> 事件。</summary>
        private void NotifyNumChange()
        {
            NumChanged?.Invoke(this);
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 设置某功能类型在该节点上的数量，并沿树向上传播刷新。
        /// value 为 0 表示清除该功能在此节点的红点。
        /// </summary>
        public void SetNum(RedDotType type, int value)
        {
            if (value == 0)
            {
                if (_numDic.ContainsKey(type))
                    _numDic.Remove(type);
            }
            else
            {
                _numDic[type] = value;
            }

            NotifyNumChange();
            Parent?.NotifyFromChild();
        }

        #endregion
    }
}
