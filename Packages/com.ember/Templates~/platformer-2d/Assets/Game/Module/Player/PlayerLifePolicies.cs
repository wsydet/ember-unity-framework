using UnityEngine;

namespace Game.Player
{
    /// <summary>显式装配在玩家实体上的自定义死亡处理；由模块在FSM调用返回后通知一次。</summary>
    public interface IPlayerDeathHandler
    {
        string HandlerId { get; }
        void OnDeath(PlayerModule module, PlayerController player);
    }
    /// <summary>只解析候选位置，不移动当前玩家或重置世界；项目可在自己的流程实现世界恢复。</summary>
    public interface IPlayerRecoveryPositionProvider
    {
        string ProviderId { get; }
        Vector2 ResolvePosition(PlatformerSnapshot checkpoint);
    }
}
