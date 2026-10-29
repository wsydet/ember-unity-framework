using System;
using System.Collections.Generic;
using Ember.Resource;
using Ember.UI;
using Ember.UIExtension;
using Game.LaneDefense;
using UnityEngine;
namespace Game.UI
{
 public partial class EUILaneBattleHUD
 {
  #region 内部参数
  private BattleSession _battle;
  private readonly List<EUIItem> _items=new();
  private readonly List<EUILaneCardItem> _cards=new();
  private EmberAssetHandle<GameObject> _cardHandle;
  private GameObject _cardPrefab;
  private bool _opened;
  public override bool NeedUpdate=>true;
  #endregion
  // --------------------------------------------------------
  #region 内部方法
  private void ClearCards(){foreach(var item in _items){var go=item.GameObject;item.Dispose();if(go!=null)UnityEngine.Object.Destroy(go);}_items.Clear();_cards.Clear();}
  private void BindBattle(BattleSession battle)
  {
   if(_battle!=null)_battle.Completed-=OnCompleted;ClearCards();_battle=battle;
   if(battle==null)return; battle.Completed+=OnCompleted;if(_cardPrefab==null)return;
   foreach(var card in battle.Level.Units)
   {
    var go=UnityEngine.Object.Instantiate(_cardPrefab,Root_Cards,false);
    if(!EUIItemFactory.TryCreate(go,out var item,out string error)){UnityEngine.Object.Destroy(go);throw new InvalidOperationException(error);}
    _items.Add(item);_cards.Add((EUILaneCardItem)item.Logic);item.BeginUse(card);item.Show();
   }
  }
  private void CardReady(GameObject prefab){if(!_opened)return;_cardPrefab=prefab;if(prefab==null)throw new InvalidOperationException("Card prefab missing.");BindBattle(LaneDefenseModule.Instance.Current);}
  private void OnCompleted(BattleResult result){if(_opened&&_battle?.Generation==LaneDefenseModule.Instance.Current?.Generation)EUIManager.Instance.ShowPopup(GamePages.EUILaneResultPopup,result);}
  private void Pause(){var m=LaneDefenseModule.Instance;if(_battle==null||_battle.Result.HasValue||!m.CanCommand)return;m.SetUserPaused(true);EUIManager.Instance.ShowPopup(GamePages.EUILanePausePopup,_battle.Generation);}
  #endregion
  // --------------------------------------------------------
  #region 外部方法
  public override void OnInit()
  {
   base.OnInit();Btn_Start.onClick.AddListener(()=>LaneDefenseModule.Instance.StartWave());Btn_Pause.onClick.AddListener(Pause);
   Btn_Cancel.onClick.AddListener(()=>LaneDefenseModule.Instance.CancelSelection());Btn_Demolish.onClick.AddListener(()=>LaneDefenseModule.Instance.SelectDemolish());
  }
  public override void OnOpen(object param)
  {
   base.OnOpen(param);_opened=true;LaneDefenseModule.Instance.SessionChanged+=BindBattle;BindBattle(LaneDefenseModule.Instance.Current);
   _cardHandle=EmberResourceManager.Instance.LoadAssetHandle<GameObject>("UI/Module/LaneDefense/Prefabs/EUILaneCardItem");_cardHandle.Completed+=CardReady;
  }
  public override void OnUpdate()
  {
   base.OnUpdate();if(!_opened||_battle==null)return;var m=LaneDefenseModule.Instance;
   Txt_Stats.text=$"LANE DEFENSE | ENERGY {_battle.Resource} | BASE {_battle.BaseHealth} | WAVE {Math.Max(0,_battle.WaveIndex+1)}/{_battle.Level.Waves.Count}  {_battle.Phase}";
   Txt_Feedback.text=m.Feedback;Btn_Start.interactable=m.CanCommand&&!_battle.Paused&&(_battle.Phase==BattlePhase.Ready||_battle.Phase==BattlePhase.Intermission);
   Btn_Pause.interactable=m.CanCommand&&!_battle.Result.HasValue;foreach(var card in _cards)card.Refresh(_battle,m);
  }
  public override void OnClose()
  {
   _opened=false;LaneDefenseModule.Instance.SessionChanged-=BindBattle;if(_battle!=null)_battle.Completed-=OnCompleted;_battle=null;ClearCards();
   if(_cardHandle!=null){_cardHandle.Completed-=CardReady;_cardHandle.Dispose();_cardHandle=null;}_cardPrefab=null;base.OnClose();
  }
  public override void OnDispose(){Btn_Start.onClick.RemoveAllListeners();Btn_Pause.onClick.RemoveAllListeners();Btn_Cancel.onClick.RemoveAllListeners();Btn_Demolish.onClick.RemoveAllListeners();base.OnDispose();}
  #endregion
 }
}