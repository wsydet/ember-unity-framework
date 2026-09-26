using System;
using System.Globalization;
using Ember.Basic;
using Ember.Core;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>
    /// 剧情步骤服务 —— 剧情层与业务模块之间的中性契约，不含任何具体玩法语义。
    /// 业务 Module 实现并注册（<see cref="EmberServiceLocator"/>），
    /// 剧情侧通过 <see cref="NovelStepServiceBridgeSO"/> 或自定义节点调用它。
    ///
    /// <b>实现方必须遵守：</b>
    /// 1. 对同一 <paramref name="executionId"/> 最终恰好调用一次 complete 或 fail；
    /// 2. 结果回调可能发生在剧情会话失效之后，调用前请自行确认时机，不要依赖回调被忽略；
    /// 3. <see cref="Abort"/> 必须立即停止回调并收干净 UI 与订阅。
    /// </summary>
    public interface INovelStepService
    {
        /// <summary>
        /// 发起一次请求。<paramref name="payload"/> 由作者填写，格式由实现方自己解释。
        /// </summary>
        void Invoke(string requestKey, string executionId, string payload, Action<string> complete, Action<string> fail);

        /// <summary>剧情侧取消（读档、退出 Gameplay、故障）。实现方必须停止回调并收尾。</summary>
        void Abort(string executionId);
    }

    /// <summary>
    /// 通用模块桥接节点 —— 多数小游戏与外部系统联动只需要这一个脚本：
    /// 填「请求键 + 结果变量 + 超时」即可，节点侧不需要写任何代码，业务逻辑全部留在业务 Module 里。
    /// 结果写回剧情变量后，继续用既有的选择 / 条件分流节点做后续分支。
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/视觉小说/模块桥接节点", fileName = "StepServiceBridge")]
    public class NovelStepServiceBridgeSO : NovelCustomStepSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _requestKey;
        /// <summary>随请求一起交给业务模块的自由文本；格式由模块自己解释。</summary>
        [SerializeField] private string _payload;
        /// <summary>接收结果的剧情变量 ID；留空表示只看完成 / 失败，不回写变量。</summary>
        [SerializeField] private string _resultVariableId;
        [SerializeField] private NovelVariableScope _resultScope = NovelVariableScope.Global;
        /// <summary>等待上限（秒）。必须大于 0：没有超时的节点会永久阻塞剧情。</summary>
        [SerializeField] private float _timeout = 120;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private sealed class BridgeState
        {
            internal INovelStepService Service;
            internal string ExecutionId;
            internal float Remaining;
            internal bool Settled;
        }
        public string RequestKey => _requestKey;
        public string Payload => _payload;
        public string ResultVariableId => _resultVariableId;
        public NovelVariableScope ResultScope => _resultScope;
        public float Timeout => _timeout;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static BridgeState StateOf(NovelCustomStepContext context) => context?.State as BridgeState;

        private void Settle(NovelCustomStepContext context, BridgeState state, string result, string error)
        {
            if (state == null || state.Settled) return;
            state.Settled = true;
            if (!context.IsAlive) return;
            if (!string.IsNullOrEmpty(error)) { context.Fail(error); return; }
            if (!string.IsNullOrWhiteSpace(_resultVariableId))
            {
                if (!context.TryGetVariable(_resultScope, _resultVariableId, out NovelValue declared))
                { context.Fail("结果变量未声明：" + _resultVariableId); return; }
                if (!TryConvert(declared.Type, result, out NovelValue value))
                { context.Fail("模块返回「" + result + "」无法写入 " + declared.Type + " 变量 " + _resultVariableId); return; }
                if (!context.SetVariable(_resultScope, _resultVariableId, value, out string writeError))
                { context.Fail("结果变量写入失败：" + writeError); return; }
            }
            context.Complete();
        }

        private static bool TryConvert(NovelValueType type, string text, out NovelValue value)
        {
            value = default; text ??= string.Empty;
            switch (type)
            {
                case NovelValueType.String: value = new NovelValue(text); return true;
                case NovelValueType.Int:
                    if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)) return false;
                    value = new NovelValue(number); return true;
                case NovelValueType.Bool:
                    if (bool.TryParse(text.Trim(), out bool flag)) { value = new NovelValue(flag); return true; }
                    if (text.Trim() == "1") { value = new NovelValue(true); return true; }
                    if (text.Trim() == "0") { value = new NovelValue(false); return true; }
                    return false;
                default: return false;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override string DisplayName => "模块桥接节点 · " + (string.IsNullOrWhiteSpace(_requestKey) ? "未填请求键" : _requestKey);

        public override string Summary() => "桥接模块 · " + (string.IsNullOrWhiteSpace(_requestKey) ? "⚠ 未填请求键" : _requestKey)
            + " → " + (string.IsNullOrWhiteSpace(_resultVariableId) ? "不回写变量" : _resultVariableId)
            + " · 超时 " + _timeout.ToString("0.##", CultureInfo.InvariantCulture) + " 秒";

        public override string Validate(NovelCustomStepValidation validation)
        {
            if (string.IsNullOrWhiteSpace(_requestKey)) return "必须填写请求键（业务模块用它区分请求）";
            if (!NovelActionHandle.ValidTime(_timeout) || _timeout <= 0) return "超时必须大于 0；没有超时的节点会让剧情永久停在这里";
            if (string.IsNullOrWhiteSpace(_resultVariableId)) return null;
            if (validation == null) return null;
            // 模块只返回字符串，目标限定为能确定性转换的三种类型（bool / int / string）。
            if (!validation.TryGetVariable(_resultScope, _resultVariableId, out NovelValue declared))
                return "结果变量未声明：" + _resultVariableId + "（作用域 " + (_resultScope == NovelVariableScope.Global ? "全局" : "本章节") + "）";
            if (!Enum.IsDefined(typeof(NovelValueType), declared.Type)) return "结果变量类型无效：" + _resultVariableId;
            return null;
        }

        public override void OnBegin(NovelCustomStepContext context)
        {
            INovelStepService service = EmberServiceLocator.TryResolve<INovelStepService>();
            if (service == null)
            { context.Fail("没有注册 INovelStepService，无法执行桥接节点：" + _requestKey); return; }
            var state = new BridgeState { Service = service, ExecutionId = context.ExecutionId, Remaining = _timeout };
            context.State = state;
            try { service.Invoke(_requestKey, state.ExecutionId, _payload, result => Settle(context, state, result, null), error => Settle(context, state, null, error)); }
            catch (Exception exception)
            { state.Settled = true; context.Fail("桥接服务启动失败：" + exception.Message); }
        }

        public override void OnTick(NovelCustomStepContext context, float delta)
        {
            BridgeState state = StateOf(context);
            if (state == null || state.Settled || _timeout <= 0) return;
            state.Remaining -= delta;
            if (state.Remaining > 0) return;
            state.Settled = true;
            try { state.Service?.Abort(state.ExecutionId); } catch (Exception exception) { EmberDebug.LogWarning("Game.Narrative", "桥接节点中止失败：" + exception.Message); }
            context.Fail("等待模块「" + _requestKey + "」返回超时（" + _timeout.ToString("0.##", CultureInfo.InvariantCulture) + " 秒）");
        }

        public override void OnCancel(NovelCustomStepContext context)
        {
            BridgeState state = StateOf(context);
            state?.Service?.Abort(state.ExecutionId);
        }
        #endregion
    }
}
