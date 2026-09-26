using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>
    /// 自定义节点脚本基类 —— 继承它 = 定义一种新的剧情步骤。
    ///
    /// <b>分工（务必遵守）：</b>复杂业务逻辑应放在独立的业务 Module 里；
    /// 本脚本只负责「启动业务 + 接收结果」，不要把玩法逻辑写进脚本。
    /// 需要外部业务时优先继承 <see cref="NovelStepServiceBridgeSO"/>，
    /// 或通过 <see cref="INovelStepService"/> 调用业务模块。
    ///
    /// <b>运行状态：</b>脚本资产是共享的，禁止把本次执行的状态放进脚本字段；
    /// 请使用 <see cref="NovelCustomStepContext.State"/>。
    ///
    /// <b>引用方式：</b>剧情步骤只保存 <see cref="ScriptId"/> 字符串，
    /// 资产实例由 <see cref="NarrativeStorySO"/> 的自定义节点清单登记，
    /// 因此本类不能（也不需要）被塞进 <see cref="NovelCommand"/> 本体。
    /// </summary>
    public abstract class NovelCustomStepSO : EmberBaseSO
    {
        #region 内部参数
        /// <summary>
        /// 稳定 ID：默认取类型全名。存档指纹与步骤引用都以它为准。
        /// 需要重命名类型时显式覆写本属性并固定旧值，否则旧档会被判为不兼容。
        /// </summary>
        public virtual string ScriptId => GetType().FullName;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        /// <summary>步骤下拉与图上显示的名字；被子类覆写时不应返回空。</summary>
        protected string SafeDisplayName => string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>步骤下拉与图上显示的名字。</summary>
        public virtual string DisplayName => SafeDisplayName;

        /// <summary>是否等本节点结束再继续剧情。false = 发射后不管。</summary>
        public virtual bool WaitForCompletion => true;

        /// <summary>
        /// 本脚本的参数是否参与剧情指纹。默认参与：改参数会让旧档判为不兼容，
        /// 与「语义变化拒绝旧档、不静默重置」的既有口径一致。
        /// 关掉意味着作者自行承担「旧档配新参数」的风险。
        /// </summary>
        public virtual bool IncludeInFingerprint => true;

        /// <summary>
        /// 编辑期静态校验。返回 null 表示通过，返回文字则是可定位的校验错误。
        /// 只在剧情会话校验时调用（独立 Start 的旧章节没有清单，不会调用）。
        /// 此时业务 Module 可能尚未激活，所以不要在这里判断「服务是否可用」。
        /// </summary>
        public virtual string Validate(NovelCustomStepValidation validation) => null;

        /// <summary>步骤列表与流程图上的可读摘要。只应读取本脚本自己的参数。</summary>
        public virtual string Summary() => DisplayName;

        /// <summary>
        /// 启动本节点。默认实现立即完成。
        /// <b>必须在最终调用一次 <see cref="NovelCustomStepContext.Complete"/> 或
        /// <see cref="NovelCustomStepContext.Fail"/>：</b>运行器不会自己超时，
        /// 既不完成也不报错会让剧情永久停在等待状态。
        /// </summary>
        public virtual void OnBegin(NovelCustomStepContext context) => context.Complete();

        /// <summary>逐帧推进（可选）。会话被暂停时不会被调用，关键推进请交给业务 Module 的 Update。</summary>
        public virtual void OnTick(NovelCustomStepContext context, float delta) { }

        /// <summary>正常结束后的收尾（Complete / Fail 之后各调用一次）。</summary>
        public virtual void OnEnd(NovelCustomStepContext context) { }

        /// <summary>
        /// 被打断时的收尾：读档、退出 Gameplay、会话故障都会走到这里。
        /// 必须退订事件、释放暂停、通知业务 Module 中止，否则会留下野生的订阅与 UI。
        /// </summary>
        public virtual void OnCancel(NovelCustomStepContext context) { }
        #endregion
    }

    /// <summary>
    /// 校验期上下文：只提供已声明的变量与稳定 ID，不持有会话、页面或资源。
    /// </summary>
    public sealed class NovelCustomStepValidation
    {
        #region 内部参数
        private readonly IReadOnlyDictionary<string, NovelValue> _locals;
        private readonly IReadOnlyDictionary<string, NovelValue> _globals;
        public string ChapterId { get; }
        public string NodeId { get; }
        public string CommandId { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelCustomStepValidation(string chapterId, string nodeId, string commandId,
            IReadOnlyDictionary<string, NovelValue> locals, IReadOnlyDictionary<string, NovelValue> globals)
        {
            ChapterId = chapterId; NodeId = nodeId; CommandId = commandId;
            _locals = locals; _globals = globals;
        }

        /// <summary>查询已声明的变量；未声明或作用域无效应返回 false。</summary>
        [NoGC]
        public bool TryGetVariable(NovelVariableScope scope, string id, out NovelValue value)
        {
            value = default;
            if (!Enum.IsDefined(typeof(NovelVariableScope), scope) || string.IsNullOrWhiteSpace(id)) return false;
            var source = scope == NovelVariableScope.Global ? _globals : _locals;
            return source != null && source.TryGetValue(id, out value);
        }
        #endregion
    }
}
