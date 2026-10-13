using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.Core.Editor
{
    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 编辑器面板参数
        [SerializeField] private string _observedId="";
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private PopupField<string> _instancePicker;
        private Label _runtimeStatus, _runtimeMetrics;
        private ScrollView _runtimeHistory;
        private readonly List<EmberFsmObservedInstance> _instances=new();
        public EmberFsmObservation ObservedSnapshot { get; private set; }
        private string _historyStamp;
        public string RecentRuntimeDeclarationKey { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void CreateRuntimeView()
        {
            var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;
            row.style.flexShrink=0;
            _instancePicker=new PopupField<string>("观察实例",new List<string>{"静态查看"},0);
            _instancePicker.style.minWidth=360;
            _instancePicker.RegisterValueChangedCallback(e=>
            {
                int index=_instancePicker.choices.IndexOf(e.newValue)-1;
                _observedId=index>=0 && index<_instances.Count?_instances[index].Snapshot.InstanceId.ToString():"";
                _historyStamp=null;RefreshRuntimeObservation();
            });
            row.Add(_instancePicker);
            row.Add(new Button(()=>{var item=_instances.FirstOrDefault(x=>x.Snapshot.InstanceId.ToString()==_observedId);if(item!=null)Selection.activeObject=item.Owner;}){text="定位宿主"});
            _runtimeStatus=new Label("静态查看 · 请选择具体运行实例");_runtimeStatus.style.whiteSpace=WhiteSpace.Normal;
            rootVisualElement.Add(row);rootVisualElement.Add(_runtimeStatus);
            _runtimeStatus.style.flexShrink=0;
            var metrics=new Foldout {text="宿主详细指标（速度 / 输入 / 能力 / 效果）",value=false};
            var metricsScroll=new ScrollView();metricsScroll.style.maxHeight=120;
            _runtimeMetrics=new Label();_runtimeMetrics.style.whiteSpace=WhiteSpace.Normal;
            metricsScroll.Add(_runtimeMetrics);metrics.Add(metricsScroll);rootVisualElement.Add(metrics);
            var history=new Foldout {text="运行历史 / 拒绝 / 故障（已采集事实）",value=false};
            _runtimeHistory=new ScrollView();_runtimeHistory.style.maxHeight=145;history.Add(_runtimeHistory);
            rootVisualElement.Add(history);
            rootVisualElement.schedule.Execute(RefreshRuntimeObservation).Every(200);
        }
        private void ApplyRuntimeColors()
        {
            var recent=ObservedSnapshot?.History.Where(x=>x.Result==EmberFsmResult.Changed && !string.IsNullOrEmpty(x.TransitionId)).LastOrDefault();
            var last=ObservedSnapshot?.LastOperation;
            if(last!=null && last.Result==EmberFsmResult.Changed && !string.IsNullOrEmpty(last.TransitionId)
                && (recent==null || last.Sequence>recent.Sequence))recent=last;
            RecentRuntimeDeclarationKey=recent==null?null:RelationKey(recent.SourceNodeId,false,recent.TransitionId);
            foreach(var path in _peripherals.OfType<EmberFsmBlueprintPath>())path.SetRuntimeRecent((string)path.userData==RecentRuntimeDeclarationKey);
            if(recent!=null)
            {
                var declared=_relations.FirstOrDefault(x=>x.Key==RecentRuntimeDeclarationKey);
                _runtimeStatus.text+=$"\n最近已记录转换 #{recent.Sequence}：{Display(recent.SourceNodeId)} / {recent.TransitionId} · 声明目标 {Display(declared?.Target)} · 实际解析目标 {Display(recent.TargetId)} · 最终叶 {Display(recent.LeafId)}（绿线；黄线仅为静态选择）";
            }
            foreach(var pair in _nodes)
            {
                bool active=ObservedSnapshot?.ActivePath.Contains(pair.Key)==true;
                bool leaf=active && ObservedSnapshot.ActivePath.LastOrDefault()==pair.Key;
                var body=pair.Value.mainContainer;
                var color=active?(leaf?new Color(.35f,1,.65f):new Color(.2f,.65f,.45f)):new Color(.3f,.36f,.43f);
                body.style.borderLeftColor=body.style.borderRightColor=body.style.borderTopColor=body.style.borderBottomColor=color;
                body.style.borderLeftWidth=body.style.borderRightWidth=body.style.borderTopWidth=body.style.borderBottomWidth=active?3:1;
                var label=pair.Value.titleButtonContainer.Q<Label>("runtime-time");
                if(label==null){label=new Label {name="runtime-time"};label.style.color=new Color(.45f,1,.7f);pair.Value.titleButtonContainer.Add(label);}
                label.text=active?$"{(leaf?"●":"路径")} {ObservedSnapshot.Elapsed[pair.Key]:F2}s":"";
            }
        }
        private void RenderRuntimeHistory(EmberFsmObservation snapshot)
        {
            string stamp=snapshot==null?"":snapshot.InstanceId+":"+snapshot.LastOperation?.Sequence+":"+snapshot.LastFault?.Sequence+":"+snapshot.History.Count;
            if(stamp==_historyStamp)return;_historyStamp=stamp;_runtimeHistory.Clear();
            if(snapshot==null)return;
            _runtimeHistory.Add(new Label("未记录不等于条件不满足；未进入的其他段/子节点没有被查询。"));
            if(!snapshot.DiagnosticsEnabled)_runtimeHistory.Add(new Label("诊断采集已关闭；历史不回填，未记录不等于未发生。"));
            var items=snapshot.History.ToList();
            if(snapshot.LastOperation!=null&&!items.Any(x=>x.Sequence==snapshot.LastOperation.Sequence))items.Add(snapshot.LastOperation);
            if(snapshot.LastFault!=null&&!items.Any(x=>x.Sequence==snapshot.LastFault.Sequence))items.Add(snapshot.LastFault);
            foreach(var item in items.OrderByDescending(x=>x.Sequence))
            {
                var text=$"#{item.Sequence} {item.Origin} · {item.Result} · {item.SourceNodeId}/{item.TransitionId} → {item.TargetId} · 叶 {item.LeafId} · {item.Phase}";
                text += "\n" + EmberFsmBlueprintRules.Describe(item.Attempt);
                var button=new Button(()=>SelectDeclaration(item.SourceNodeId??"",false,item.TransitionId)) {text=text,tooltip=item.Exception?.ToString()??string.Join(" → ",item.ActivePath)};
                button.style.whiteSpace=WhiteSpace.Normal;button.style.flexShrink=0;
                button.style.unityTextAlign=TextAnchor.MiddleLeft;_runtimeHistory.Add(button);
                if(item.Exception!=null){var fault=new Label(item.Exception.Message);fault.style.color=new Color(1,.5f,.4f);_runtimeHistory.Add(fault);}
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void ObserveInstance(Guid id){_observedId=id.ToString();_historyStamp=null;RefreshRuntimeObservation();}
        public void RefreshRuntimeObservation()
        {
            if(_instancePicker==null || _canvas==null)return;
            _instances.Clear();ObservedSnapshot=null;_runtimeMetrics.text="";
            try{_instances.AddRange(EmberFsmObservationSources.Read(_graph));}
            catch(Exception ex){_runtimeStatus.text="观察源不可用："+ex.Message;ApplyRuntimeColors();RenderRuntimeHistory(null);return;}
            var choices=new List<string>{"静态查看"};
            choices.AddRange(_instances.Select(x=>x.Owner.name+" · "+x.Snapshot.InstanceId));
            _instancePicker.choices=choices;
            int index=_instances.FindIndex(x=>x.Snapshot.InstanceId.ToString()==_observedId);
            _instancePicker.SetValueWithoutNotify(index<0?choices[0]:choices[index+1]);
            if(index<0)_runtimeStatus.text=string.IsNullOrEmpty(_observedId)?"静态查看 · 请选择具体运行实例":"所选实例已结束或不可用；不会自动改选其他宿主。";
            else
            {
                var item=_instances[index];
                if(!item.Snapshot.MatchesSource(_graph,_description))_runtimeStatus.text="运行结构或来源身份与当前图不一致；请重新试跑，已停止映射高亮。";
                else
                {
                    ObservedSnapshot=item.Snapshot;
                    _runtimeStatus.text=$"运行观察 · {ObservedSnapshot.Status} · {string.Join(" → ",ObservedSnapshot.ActivePath.Select(Display))}\n计时：宿主逻辑 Update，暂停不累计；绿色为活动路径，● 为叶。";
                    _runtimeMetrics.text=item.Metrics;
                }
            }
            ApplyRuntimeColors();RenderRuntimeHistory(ObservedSnapshot);
        }
        #endregion
    }
}
