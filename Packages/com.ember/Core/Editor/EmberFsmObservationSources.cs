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
        private static readonly Dictionary<string,Func<bool>> Availability=new();
        public static bool HasAvailableProvider => Availability.Values.Any(available=>available());
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Register(string key,Func<IEnumerable<EmberFsmObservedInstance>> read, string ownerTemplateId=null, Func<bool> enabled=null)
        {
            var owner=ownerTemplateId??EmberFsmBlueprintAccess.CurrentTemplateId;
            Providers[key]=read??throw new ArgumentNullException(nameof(read));
            Availability[key]=()=>EmberFsmBlueprintAccess.Allows(owner,enabled);
        }
        public static void Remove(string key) { Providers.Remove(key); Availability.Remove(key); }
        public static IReadOnlyList<EmberFsmObservedInstance> Read(EmberFsmGraph graph)
        {
            var result=new List<EmberFsmObservedInstance>();
            foreach(var provider in Providers.ToArray())
                if(Availability[provider.Key]()) foreach(var item in provider.Value())
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
