using System;
using System.Collections.Generic;
using System.IO;
using Ember.Core.Editor;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public static class PlayerSchemeWorkflow
    {
        #region 内部方法
        private static Weapons.WeaponDefinition CopyWeapon(Weapons.WeaponDefinition source,string folder,List<string> created)
        {
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();var copy=UnityEngine.Object.Instantiate(source);
            var graph=ScriptableObject.CreateInstance<EmberFsmGraph>();var nodes=new List<EmberFsmNode>();var index=0;
            foreach(var node in source.Graph.Nodes)
            {
                var definition=UnityEngine.Object.Instantiate(node.Definition);var path=folder+"/State"+(index++)+".asset";AssetDatabase.CreateAsset(definition,path);created.Add(path);
                var bindings=new List<EmberFsmTargetBinding>();foreach(var binding in node.Bindings)bindings.Add(new EmberFsmTargetBinding(binding.Slot,binding.TargetId));
                nodes.Add(new EmberFsmNode(node.Id,node.DisplayName,definition,node.ParentId,node.DefaultChildId,bindings.ToArray()));
            }
            graph.Configure(source.Graph.EntryId,nodes.ToArray());var graphPath=folder+"/Graph.asset";AssetDatabase.CreateAsset(graph,graphPath);created.Add(graphPath);
            using(var data=new SerializedObject(copy))
            {
                var powerField=data.FindProperty("_powers");var powers=UnityEngine.Object.Instantiate(powerField.objectReferenceValue);
                using(var powerData=new SerializedObject(powers))
                {
                    index=0;foreach(var collection in new[]{"_abilities","_skills"})
                    {
                        var slots=powerData.FindProperty(collection);for(var slot=0;slot<slots.arraySize;slot++)
                        {
                            var field=slots.GetArrayElementAtIndex(slot).FindPropertyRelative("_definition");var definition=UnityEngine.Object.Instantiate(field.objectReferenceValue);
                            var path=folder+"/Power"+(index++)+".asset";AssetDatabase.CreateAsset(definition,path);created.Add(path);field.objectReferenceValue=definition;
                        }
                    }
                    powerData.ApplyModifiedPropertiesWithoutUndo();
                }
                var powersPath=folder+"/Powers.asset";AssetDatabase.CreateAsset(powers,powersPath);created.Add(powersPath);powerField.objectReferenceValue=powers;
                data.FindProperty("_graph").objectReferenceValue=graph;data.FindProperty("_id").stringValue="weapon."+Guid.NewGuid().ToString("N");data.ApplyModifiedPropertiesWithoutUndo();
            }
            var weaponPath=folder+"/Weapon.asset";AssetDatabase.CreateAsset(copy,weaponPath);created.Add(weaponPath);return copy;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>独立复制可变配置与图声明；Sprite 等美术仍共享引用。目标文件夹必须不存在。</summary>
        public static PlayerScheme Copy(PlayerScheme source,string destinationFolder)
        {
            PlayerEditorAccess.RequireWrite();
            if(!EmberProjectSetup.IsTemplateActive("topdown-2d")||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("当前身份或编辑状态不能复制俯视方案。");
            var full=Path.GetFullPath(destinationFolder);
            var assets=Path.GetFullPath("Assets")+Path.DirectorySeparatorChar;
            if(!full.StartsWith(assets,StringComparison.OrdinalIgnoreCase)||Directory.Exists(full)||File.Exists(full)||source==null)
                throw new InvalidOperationException("方案副本必须创建在 Assets 内全新目录。");
            using(var check=source.Capture()){}
            var created=new List<string>();
            try
            {
                Directory.CreateDirectory(full);AssetDatabase.Refresh();
                var copy=UnityEngine.Object.Instantiate(source);
                using(var data=new SerializedObject(copy))
                {
                    var field=data.FindProperty("_powers");var sourcePowers=field.objectReferenceValue;
                    if(sourcePowers!=null)
                    {
                        var powers=UnityEngine.Object.Instantiate(sourcePowers);
                        using(var powerData=new SerializedObject(powers))
                        {
                            var index=0;
                            foreach(var collection in new[]{"_abilities","_skills"})
                            {
                                var slots=powerData.FindProperty(collection);
                                for(var slot=0;slot<slots.arraySize;slot++)
                                {
                                    var definitionField=slots.GetArrayElementAtIndex(slot).FindPropertyRelative("_definition");
                                    var definition=UnityEngine.Object.Instantiate(definitionField.objectReferenceValue);
                                    var definitionPath=destinationFolder+"/Power"+(index++)+".asset";
                                    AssetDatabase.CreateAsset(definition,definitionPath);created.Add(definitionPath);definitionField.objectReferenceValue=definition;
                                }
                            }
                            powerData.ApplyModifiedPropertiesWithoutUndo();
                        }
                        var powersPath=destinationFolder+"/PlayerPowers.asset";AssetDatabase.CreateAsset(powers,powersPath);created.Add(powersPath);
                        field.objectReferenceValue=powers;data.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                var graph=ScriptableObject.CreateInstance<EmberFsmGraph>();var nodes=new List<EmberFsmNode>();
                var i=0;
                foreach(var node in source.Graph.Nodes)
                {
                    var definition=UnityEngine.Object.Instantiate(node.Definition);
                    var definitionPath=destinationFolder+"/State"+(i++)+".asset";
                    AssetDatabase.CreateAsset(definition,definitionPath);created.Add(definitionPath);
                    var bindings=new List<EmberFsmTargetBinding>();
                    foreach(var binding in node.Bindings)bindings.Add(new EmberFsmTargetBinding(binding.Slot,binding.TargetId));
                    nodes.Add(new EmberFsmNode(node.Id,node.DisplayName,definition,node.ParentId,node.DefaultChildId,bindings.ToArray()));
                }
                graph.Configure(source.Graph.EntryId,nodes.ToArray());
                var graphPath=destinationFolder+"/PlayerGraph.asset";AssetDatabase.CreateAsset(graph,graphPath);created.Add(graphPath);
                using(var data=new SerializedObject(copy))
                {data.FindProperty("_graph").objectReferenceValue=graph;data.FindProperty("_id").stringValue="topdown."+Guid.NewGuid().ToString("N");data.ApplyModifiedPropertiesWithoutUndo();}
                var weapons=new List<Weapons.WeaponDefinition>();
                for(var weapon=0;weapon<source.Weapons.Count;weapon++)weapons.Add(CopyWeapon(source.Weapons[weapon],destinationFolder+"/Weapon"+weapon,created));
                copy.ConfigureWeapons(weapons.ToArray());
                var path=destinationFolder+"/PlayerScheme.asset";AssetDatabase.CreateAsset(copy,path);created.Add(path);
                AssetDatabase.SaveAssets();return copy;
            }
            catch
            {
                for(var i=created.Count-1;i>=0;i--)AssetDatabase.DeleteAsset(created[i]);
                throw;
            }
        }
        #endregion
    }
}
