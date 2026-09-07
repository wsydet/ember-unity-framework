using System;

namespace Ember.Input
{
    /// <summary>
    /// 标识 InputActionAsset 中一个可重绑定的 Binding。
    /// 优先使用稳定的 BindingId 定位，BindingIndex 可作为运行期定位提示，
    /// 并用于区分 Composite 的各个 Part。
    /// </summary>
    public readonly struct EmberInputBindingTarget
    {
        public readonly string ActionMapName;
        public readonly string ActionName;
        public readonly Guid BindingId;
        public readonly int BindingIndex;

        public bool IsValid => !string.IsNullOrEmpty(ActionMapName)
            && !string.IsNullOrEmpty(ActionName)
            && (BindingId != Guid.Empty || BindingIndex >= 0);

        public EmberInputBindingTarget(
            string actionMapName,
            string actionName,
            int bindingIndex)
            : this(actionMapName, actionName, Guid.Empty, bindingIndex)
        {
        }

        public EmberInputBindingTarget(
            string actionMapName,
            string actionName,
            Guid bindingId,
            int bindingIndex = -1)
        {
            ActionMapName = actionMapName;
            ActionName = actionName;
            BindingId = bindingId;
            BindingIndex = bindingIndex;
        }
    }

    /// <summary>
    /// 一次交互式重绑定请求。未来实现可以根据控制路径过滤候选输入。
    /// </summary>
    public sealed class EmberInputRebindRequest
    {
        public EmberInputBindingTarget Target { get; }
        public string CancelControlPath { get; set; } = "<Keyboard>/escape";
        public string[] ExcludedControlPaths { get; set; } = Array.Empty<string>();

        public EmberInputRebindRequest(EmberInputBindingTarget target)
        {
            Target = target;
        }
    }

    /// <summary>交互式重绑定的结束状态。</summary>
    public enum EmberInputRebindStatus
    {
        Completed,
        Canceled,
        Failed,
    }

    /// <summary>交互式重绑定完成、取消或失败时返回的数据。</summary>
    public readonly struct EmberInputRebindResult
    {
        public readonly EmberInputBindingTarget Target;
        public readonly EmberInputRebindStatus Status;
        public readonly string EffectiveControlPath;
        public readonly string Error;

        public bool Succeeded => Status == EmberInputRebindStatus.Completed;

        public EmberInputRebindResult(
            EmberInputBindingTarget target,
            EmberInputRebindStatus status,
            string effectiveControlPath = null,
            string error = null)
        {
            Target = target;
            Status = status;
            EffectiveControlPath = effectiveControlPath;
            Error = error;
        }
    }

    /// <summary>
    /// 玩家输入重绑定服务契约。
    /// 当前 Ember 仅预留此契约，不提供默认实现；未来实现可基于
    /// InputActionRebindingExtensions.RebindingOperation 接入。
    /// </summary>
    public interface IEmberInputRebindingService
    {
        /// <summary>当前是否正在等待玩家输入。</summary>
        bool IsRebinding { get; }

        /// <summary>当前正在处理的 Binding；未重绑定时返回 default。</summary>
        EmberInputBindingTarget ActiveTarget { get; }

        /// <summary>一次重绑定流程结束时触发，包括完成、取消和失败。</summary>
        event Action<EmberInputRebindResult> RebindFinished;

        /// <summary>Binding 覆盖发生变化时触发，包括重绑定、恢复和导入。</summary>
        event Action BindingsChanged;

        /// <summary>开始等待玩家输入。无法开始时返回 false 并说明原因。</summary>
        bool TryBeginRebind(EmberInputRebindRequest request, out string error);

        /// <summary>取消当前重绑定。没有活动操作时返回 false。</summary>
        bool TryCancelRebind();

        /// <summary>取得当前 Binding 的用户可读显示文本。</summary>
        bool TryGetBindingDisplayName(
            in EmberInputBindingTarget target,
            out string displayName);

        /// <summary>移除指定 Binding 的覆盖，恢复资源中定义的默认值。</summary>
        bool TryResetBinding(in EmberInputBindingTarget target, out string error);

        /// <summary>移除当前 InputActionAsset 的全部 Binding 覆盖。</summary>
        bool TryResetAllBindings(out string error);

        /// <summary>导出 Binding Overrides，由业务层决定保存位置。</summary>
        bool TryExportBindingOverrides(out string json, out string error);

        /// <summary>导入此前保存的 Binding Overrides。</summary>
        bool TryImportBindingOverrides(string json, out string error);
    }
}
