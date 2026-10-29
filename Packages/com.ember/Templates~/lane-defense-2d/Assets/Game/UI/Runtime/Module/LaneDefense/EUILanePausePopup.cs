using Ember.Core;
using Ember.UI;
using Game.LaneDefense;
namespace Game.UI
{
 public partial class EUILanePausePopup
 {
  #region 内部参数
  private long _generation;
  private bool Current=>LaneDefenseModule.Instance.Current?.Generation==_generation;
  #endregion
  // --------------------------------------------------------
  #region 外部方法
  public override void OnInit()
  {
   base.OnInit();Btn_Resume.onClick.AddListener(()=>{if(Current)LaneDefenseModule.Instance.SetUserPaused(false);EUIManager.Instance.ClosePage(Page);});
   Btn_Restart.onClick.AddListener(()=>{if(Current&&LaneDefenseModule.Instance.Restart())EUIManager.Instance.ClosePage(Page);});
   Btn_Menu.onClick.AddListener(()=>{if(Current)GameLauncher.Instance.Fsm.TransitionTo<MainState>();});
  }
  public override void OnOpen(object param){base.OnOpen(param);_generation=param is long value?value:0;Txt_Title.text="BATTLE PAUSED";Txt_Detail.text="Combat time is stopped. UI remains available.";}
  public override void OnClose(){if(Current)LaneDefenseModule.Instance.SetUserPaused(false);base.OnClose();}
  public override void OnDispose(){Btn_Resume.onClick.RemoveAllListeners();Btn_Restart.onClick.RemoveAllListeners();Btn_Menu.onClick.RemoveAllListeners();base.OnDispose();}
  #endregion
 }
}