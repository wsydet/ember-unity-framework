using Game.LaneDefense;
using UnityEngine;
namespace Game.UI
{
 public partial class EUILaneCardItem
 {
  #region 内部参数
  private UnitDefinition _definition;
  #endregion
  // --------------------------------------------------------
  #region 外部方法
  public override void OnInit(){base.OnInit();Btn_Select.onClick.AddListener(()=>{if(_definition!=null)LaneDefenseModule.Instance.SelectCard(_definition.Id);});}
  public override void OnOpen(object param){base.OnOpen(param);_definition=(UnitDefinition)param;Txt_Card.text=$"{_definition.Label} / {_definition.Cost}";}
  public void Refresh(BattleSession battle,LaneDefenseModule module)
  {
   float cd=battle.CooldownRemaining(_definition.Id);var reason=battle.CardRejection(_definition.Id);string state=battle.Paused?"PAUSED":reason==PlacementRejection.Cooldown?$"COOLDOWN {cd:0.0}s":reason==PlacementRejection.None?"READY":reason.ToString();
   Txt_State.text=state+(module.SelectedCard==_definition.Id?" [SELECTED]":"");Btn_Select.interactable=module.CanCommand&&!battle.Paused&&!battle.Result.HasValue;
   Btn_Select.image.color=module.SelectedCard==_definition.Id?new Color(.1f,.55f,.5f):new Color(.07f,.23f,.28f);
  }
  public override void OnDispose(){Btn_Select.onClick.RemoveAllListeners();_definition=null;base.OnDispose();}
  #endregion
 }
}
