using Ember.Core;
using Ember.UI;
using Game.LaneDefense;
namespace Game.UI
{
 public partial class EUILaneResultPopup
 {
  #region 内部参数
  private string _resultId;
  private bool Current=>LaneDefenseModule.Instance.Current?.Result?.Id==_resultId;
  #endregion
  // --------------------------------------------------------
  #region 外部方法
  public override void OnInit()
  {
   base.OnInit();Btn_Restart.onClick.AddListener(()=>{if(Current&&LaneDefenseModule.Instance.Restart())EUIManager.Instance.ClosePage(Page);});
   Btn_Menu.onClick.AddListener(()=>{if(Current)GameLauncher.Instance.Fsm.TransitionTo<MainState>();});
  }
  public override void OnOpen(object param){base.OnOpen(param);var result=(BattleResult)param;_resultId=result.Id;Txt_Title.text=result.Phase==BattlePhase.Won?"SECTOR SECURED":"BASE LOST";Txt_Detail.text=$"{result.LevelId} | KILLS {result.Kills} | ESCAPES {result.Escapes}";}
  public override void OnDispose(){Btn_Restart.onClick.RemoveAllListeners();Btn_Menu.onClick.RemoveAllListeners();base.OnDispose();}
  #endregion
 }
}