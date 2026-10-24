using System;
using Ember.Core;
using Game.Player;

namespace Game.UI
{
    public partial class EUIGamePlayPage
    {
        #region 内部参数
        private bool _playerOperation;
        private PlayerModule _observedLife;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void BindLifeNotice()
        {
            UnbindLifeNotice();
            if (!EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var player)) return;
            _observedLife = player; player.DeathAwaitingConfirmation += OnPlayerDeath;
            player.SessionCommitted += RefreshHealthItem;
            player.SessionReleased += ReleaseHealthItem;
            RefreshHealthItem(player.Controller);
            if (player.Controller != null && player.Controller.Context?.Health?.Current <= 0) OnPlayerDeath(player.Controller);
        }
        private void UnbindLifeNotice()
        {
            if (_observedLife != null)
            {
                _observedLife.DeathAwaitingConfirmation -= OnPlayerDeath;
                _observedLife.SessionCommitted -= RefreshHealthItem;
                _observedLife.SessionReleased -= ReleaseHealthItem;
            }
            _healthItem?.Dispose(); _healthItem = null; _observedLife = null;
        }
        private void OnPlayerDeath(PlayerController player)
        { if (!_playerOperation) ReportPlayer("角色已死亡。点击「恢复」确认回到当前存档点；世界保持当前状态。"); }
        private void OpenPlayerSettings()
        {
            var flow = GameLauncher.Instance.Fsm;
            if (!_playerOperation && !flow.IsTransitioning && flow.Is<GameplayState>())
                flow.TransitionTo<SettingsState>(SettingsContext.Gameplay);
        }


        private void SetPlayerBusy(bool busy)
        {
            _playerOperation = busy;
            if (Btn_Save != null) Btn_Save.interactable = !busy;
            if (Btn_SaveExit != null) Btn_SaveExit.interactable = !busy;
            if (Btn_Respawn != null) Btn_Respawn.interactable = !busy;
            if (Btn_Back != null) Btn_Back.interactable = !busy;
            if (Btn_Settings != null) Btn_Settings.interactable = !busy;
        }

        private void ReportPlayer(string message)
        { if (Txt_Status != null) Txt_Status.text = message; }

        private async void SaveProgress()
        {
            if (_playerOperation || !EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var player)) return;
            SetPlayerBusy(true);
            try
            {
                ReportPlayer("正在保存槽位 0…");
                // 普通保存不暂停世界；模块在接受时冻结本次快照。
                var result = await player.Save(0).Completion;
                ReportPlayer(result.Succeeded ? "槽位 0 保存成功" : "保存失败：" + result.Status);
            }
            catch (Exception error) { ReportPlayer("保存失败：" + error.Message); }
            finally { SetPlayerBusy(false); }
        }

        private void SaveAndExit() => Exit(0);

        private async void Exit(int? slot)
        {
            if (_playerOperation || !EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var player)) return;
            SetPlayerBusy(true);
            try
            {
                ReportPlayer(slot.HasValue ? "正在保存槽位 0 并返回…" : "正在等待已接受的写入…");
                var result = await player.ExitToMenuAsync(GameLauncher.Instance.Fsm, slot);
                if (result.Status != PlayerExitStatus.Completed)
                    ReportPlayer("返回未完成：" + result.Status + (result.Save == null ? "" : " / " + result.Save.Status));
            }
            catch (Exception error) { ReportPlayer("返回失败：" + error.Message); }
            finally { SetPlayerBusy(false); }
        }

        private async void Respawn()
        {
            if (_playerOperation || !EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var player)) return;
            SetPlayerBusy(true);
            try
            {
                var operation = player.Recover(GameLauncher.Instance.Fsm);
                var result = await operation.WhenInteractive;
                ReportPlayer(result.Status == EmberTransitionStatus.Completed
                    ? "已恢复本会话的存档点或初始状态" : "恢复未完成：" + result.Status + " / " + result.Error);
            }
            catch (Exception error) { ReportPlayer("重生未完成：" + error.Message); }
            finally { SetPlayerBusy(false); }
        }
        #endregion
    }
}
