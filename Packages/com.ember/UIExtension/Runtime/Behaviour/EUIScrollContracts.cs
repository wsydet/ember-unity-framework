using UnityEngine;

namespace Ember.UIExtension
{
    public enum EUIScrollAlignment { Begin, Center, End }
    public enum EUIScrollAxis { Horizontal, Vertical }
    public enum EUIScrollStatus { Completed, Stopped, Replaced, UserInterrupted, Disabled, TargetLost, Invalid }

    public readonly struct EUIScrollResult
    {
        public long OperationId { get; }
        public RectTransform Target { get; }
        public Vector2 NormalizedPosition { get; }
        public EUIScrollStatus Status { get; }
        public EUIScrollResult(long operationId, RectTransform target, Vector2 position, EUIScrollStatus status)
        { OperationId=operationId; Target=target; NormalizedPosition=position; Status=status; }
    }
}
