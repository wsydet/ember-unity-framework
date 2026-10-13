using System;
using Ember.Core;
using Game.Player;

namespace Game.UI
{
    public partial class EUIMainPage
    {
        #region 内部参数
        private bool _readingProgress;
        #endregion
        // --------------------------------------------------------
        #region 内部方法

        private async void ContinueProgress()
        {
            if (_readingProgress) return;
            _readingProgress = true;
            Btn_Start.interactable = Btn_Settings.interactable = Btn_Continue.interactable = false;
            Txt_Status.text = "正在读取槽位 0…";
            try
            {
                var result = await GameLauncher.Instance.Fsm.RequestTransitionTo<GameplayState>(PlayerEntryRequest.FromSlot(0)).Completion;
                if (result.Status != EmberTransitionStatus.Completed && Txt_Status != null)
                    Txt_Status.text = "无法继续：" + result.Status + "。请确认槽位 0 已保存且有效。";
            }
            catch (Exception error) { if (Txt_Status != null) Txt_Status.text = "读取失败：" + error.Message; }
            finally
            {
                _readingProgress = false;
                if (Btn_Start != null) Btn_Start.interactable = true;
                if (Btn_Settings != null) Btn_Settings.interactable = true;
                if (Btn_Continue != null) Btn_Continue.interactable = true;
            }
        }
        #endregion
    }
}
