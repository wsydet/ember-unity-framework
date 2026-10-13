using Game.Entity;

namespace Game.Player
{
    /// <summary>将宿主开放解释为玩家解锁；独立于 Context/FSM 寿命，不是存档格式。</summary>
    public sealed class PlayerPowerProgress
    {
        #region 内部参数
        private readonly EntityPowerAvailability _availability;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerPowerProgress(EntityPowerAvailability availability) => _availability = availability;
        public bool IsAbilityUnlocked(string id) => _availability.IsAbilityOpen(id);
        public bool IsSkillUnlocked(string id) => _availability.IsSkillOpen(id);
        #endregion
    }
}
