using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module
{
    /// <summary>
    /// 单个红点 UI 节点的显示配置 —— 该节点以哪种形态显示（圆点 / 数字 / 文本 / 特殊）。
    /// </summary>
    [Serializable]
    public class RedDotUIItemData
    {
        [Tooltip("红点 UI 节点类型")]
        public RedDotUIItemType itemType;

        [Tooltip("显示为普通圆点（默认）")]
        public bool showAsNormal = true;

        [Tooltip("显示为数字（如 3、99）")]
        public bool showAsNum;

        [Tooltip("显示为文本标记（如「新」）")]
        public bool showAsText;

        [Tooltip("显示为特殊标记（优先级最高）")]
        public bool showAsSpec;
    }

    /// <summary>
    /// 一个红点功能的配置 —— 功能类型 + 它要传播到的 UI 节点链（外 → 内）。
    /// </summary>
    [Serializable]
    public class RedDotData
    {
        [Tooltip("红点功能类型")]
        public RedDotType id;

        [Tooltip("红点 UI 节点层级（外 → 内），如 [主界面邮件, 邮件页签]")]
        public List<RedDotUIItemType> uis = new();
    }

    /// <summary>
    /// 红点配置资产 —— 在编辑器中描述所有红点节点与功能的传播关系。
    ///
    /// 通过 Create Asset Menu 创建（Game / Red Dot Config），
    /// 传入 <see cref="RedDotModule.Initialize"/> 使用。
    /// </summary>
    [CreateAssetMenu(fileName = "RedDotConfig", menuName = "Game/Red Dot Config")]
    public class RedDotConfig : ScriptableObject
    {
        [Header("红点 UI 节点列表")]
        [Tooltip("每个 UI 节点类型的显示形态配置")]
        public List<RedDotUIItemData> items = new();

        [Header("红点功能列表")]
        [Tooltip("每个功能类型对应的 UI 节点传播链（外 → 内）")]
        public List<RedDotData> datas = new();
    }
}
