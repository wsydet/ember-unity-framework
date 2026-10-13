using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>业务编辑器显式提供观察源；不注册/持有/驱动 FSM 本体。</summary>
    public static class EmberFsmObservationSources
    {
        #region 内部参数
        private static readonly Dictionary<string,Func<IEnumerable<EmberFsmObservedInstance>>> Providers=new();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Register(string key,Func<IEnumerable<EmberFsmObservedInstance>> read) => Providers[key]=read;
        public static void Remove(string key) => Providers.Remove(key);
        public static IReadOnlyList<EmberFsmObservedInstance> Read(EmberFsmGraph graph)
        {
            var result=new List<EmberFsmObservedInstance>();
            foreach(var provider in Providers.Values.ToArray())
                foreach(var item in provider())
                    if(item!=null && item.Owner!=null && item.Graph==graph && item.Snapshot!=null)result.Add(item);
            return result.GroupBy(x=>x.Snapshot.InstanceId).Select(x=>x.First()).ToArray();
        }
        #endregion
    }
    public sealed class EmberFsmObservedInstance
    {
        public UnityEngine.Object Owner { get; }
        public EmberFsmGraph Graph { get; }
        public EmberFsmObservation Snapshot { get; }
        public string Metrics { get; }
        public EmberFsmObservedInstance(UnityEngine.Object owner,EmberFsmGraph graph,EmberFsmObservation snapshot,string metrics="")
        { Owner=owner;Graph=graph;Snapshot=snapshot;Metrics=metrics; }
    }
}
