using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.Guide
{
    /// <summary>
    /// 引导注册表 —— 列出所有引导及其顺序与参数，对应 burner 的 guide.csv。
    ///
    /// 用 ScriptableObject 直引用 <see cref="GuideDefine"/> 替代 CSV + 资源路径字符串，
    /// 避免路径硬编码与异步加载。运行时由 <see cref="GuideModule.Initialize"/> 传入，
    /// 据此构建顺序引导 / 非顺序引导两个 id 列表。
    ///
    /// <b>创建方式：</b>Assets → Create → Ember/Guide/GuideConfig。
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/Guide/GuideConfig", fileName = "GuideConfig")]
    public class GuideConfig : ScriptableObject
    {
        /// <summary>引导条目列表（每个条目 = 一条引导）。</summary>
        public List<GuideEntry> entries = new();
    }

    /// <summary>
    /// 单条引导的注册信息。
    /// </summary>
    [Serializable]
    public class GuideEntry
    {
        /// <summary>引导唯一 id（与 <see cref="GuideProgress"/> 持久化对应）。</summary>
        public int id;

        /// <summary>顺序引导序号（&gt;0 按值升序依次执行；0 = 非顺序引导，条件满足即触发）。</summary>
        public int sequenceOrder;

        /// <summary>引导定义资产（步骤 / 条件 / 事件 / 执行器）。</summary>
        public GuideDefine define;

        /// <summary>字符串参数（供步骤通过 <see cref="GuideGroupBlackboard.GetString"/> 读取）。</summary>
        public string[] stringParams;

        /// <summary>整型参数（供步骤通过 <see cref="GuideGroupBlackboard.GetInt"/> 读取）。</summary>
        public int[] intParams;
    }
}
