// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Basic.LitJson;
using Ember.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using J = Ember.Basic.LitJson.JsonData;

namespace Ember.UIExtension.Editor
{
    public static partial class EmberBuiService
    {
        #region 内部方法
        private static Input Read(string requestPath, EmberBuiPlan plan)
        {
            Pin(plan, requestPath);
            var input = new Input { request = JsonUtility.FromJson<EmberBuiRequest>(File.ReadAllText(requestPath)) };
            var request = input.request;
            Require(request.schemaVersion == 1 && request.fontPolicy == "preserve-existing", "REQUEST_VERSION_OR_FONT_POLICY_UNSUPPORTED");
            Pin(plan, request.mapPath);
            input.map = JsonMapper.ToObject(File.ReadAllText(request.mapPath));
            var map = input.map;
            Require(S(map, "schemaVersion") == "1", "MAP_VERSION_UNSUPPORTED");
            Require(Items(Get(map,"requiredCapabilities")).Any(c => c.ToString() == "ember-eui-map-v1"), "MAP_CAPABILITY_MISSING");
            foreach (var cap in Items(Get(map,"requiredCapabilities"))) Require(Capabilities.Contains(cap.ToString()), "CAPABILITY_UNSUPPORTED: " + cap);
            var prior = ReadReceipt(request);
            var after = prior != null && prior.requestDigest == Hash(requestPath) ? prior.after : new List<EmberBuiFileState>();
            void PinnedRecord(J record)
            {
                var path = S(record,"path");
                Pin(plan, path, after.FirstOrDefault(f => f.path == path)?.sha256 ?? S(record,"sha256"));
            }
            PinnedRecord(Get(map,"bui")); PinnedRecord(Get(map,"sourcePsd")); PinnedRecord(Get(map,"workingPsd"));
            foreach (var record in Items(Get(map,"inputs"))) PinnedRecord(record);
            foreach (var record in Items(Get(map,"contracts")))
            {
                PinnedRecord(record);
                var contract = JsonUtility.FromJson<EUIContract>(File.ReadAllText(S(record,"path")));
                Require(contract.schemaVersion == 1 && contract.toolVersion == "1.1", "CONTRACT_VERSION_UNSUPPORTED");
                Require(contract.prefabGuid == S(record,"prefabGuid") && AssetDatabase.AssetPathToGUID(contract.prefabPath) == contract.prefabGuid, "CONTRACT_IDENTITY_CHANGED");
                Pin(plan, contract.prefabPath, after.FirstOrDefault(f=>f.path==contract.prefabPath)?.sha256 ?? contract.assetDigest);
                Pin(plan, contract.prefabPath + ".meta");
                foreach (var cap in contract.requiredCapabilities) Require(Capabilities.Contains(cap), "CONTRACT_CAPABILITY_UNSUPPORTED");
                input.contracts.Add(contract.prefabGuid, contract);
            }
            var buiPath = S(Get(map,"bui"),"path");
            input.bui = JsonMapper.ToObject(File.ReadAllText(buiPath)); var bui = input.bui;
            Require(S(bui,"schemaVersion") == "3", "BUI_VERSION_UNSUPPORTED");
            Require(S(Get(bui,"source"),"psdSha256") == S(Get(map,"workingPsd"),"sha256"), "PSD_IDENTITY_CHANGED");
            var canvas = Get(bui,"canvas");
            Require(S(canvas,"coordinateSystem") == "UnityRectTransform" && S(canvas,"origin") == "Center" && S(canvas,"unit") == "px", "COORDINATES_UNSUPPORTED");
            Require(V(Get(canvas,"referenceResolution")).x > 0 && V(Get(canvas,"referenceResolution")).y > 0, "CANVAS_INVALID");
            foreach(var key in new[]{"retainedTexts","designTexts","resources"}) Require(!Items(Get(bui,key)).Any(), "UNSUPPORTED_SEMANTICS: " + key);
            var reuse = Get(bui,"resourceReuse");
            Require(reuse == null || S(reuse,"state") == "direct-export" || S(reuse,"state") == "none", "HISTORY_REUSE_UNSUPPORTED");
            input.nodes = Unique(Items(Get(bui,"nodes")),"id"); input.targets = Unique(Items(Get(bui,"targets")),"targetId");
            Require(input.targets.Count == request.targets.Count && request.targets.Select(t=>t.targetId).Distinct().Count()==request.targets.Count, "TARGET_COVERAGE");
            var membership = new HashSet<string>();
            foreach (var target in input.targets.Values)
            {
                Require(new[]{"Page","Popup","Component","ListItem"}.Contains(S(target,"targetType")), "TARGET_ROLE_UNSUPPORTED");
                var ids = Items(Get(target,"nodeIds")).Select(x=>x.ToString()).ToArray();
                Require(ids.Contains(S(target,"sourceRootNodeId")), "TARGET_ROOT_MISSING");
                foreach(var id in ids) Require(input.nodes.ContainsKey(id) && membership.Add(id), "DUPLICATE_OR_MISSING_TARGET_NODE");
                foreach(var id in ids) Require(string.IsNullOrEmpty(S(input.nodes[id],"parentId")) || ids.Contains(S(input.nodes[id],"parentId")), "CROSS_TARGET_PARENT");
            }
            Require(membership.Count == input.nodes.Count, "ORPHAN_NODE");
            foreach(var node in input.nodes.Values)
            {
                foreach(var option in Items(Get(Get(Get(node,"source"),"naming"),"options")))
                    Require(new[]{"Vertical","NoBind","DynamicTexture","NineSlice","PrefabAsset","StaticTexture"}.Contains(S(option,"id")),"CRITICAL_OPTION_UNSUPPORTED: "+S(option,"id"));
                Require(new[]{"Group","Bg","Top","Center","Bottom","Image","RawImage","Text","PrefabRef","List"}.Contains(S(node,"type")), "NODE_UNSUPPORTED: " + S(node,"type"));
                Require(Get(node,"active")?.IsBoolean == true, "ACTIVE_INVALID");
                var rect = Get(node,"rect");
                foreach(var key in new[]{"anchorMin","anchorMax","pivot","anchoredPosition","sizeDelta"}) V(Get(rect,key));
                Require(V(Get(rect,"anchorMin")) == Vector2.one*.5f && V(Get(rect,"anchorMax")) == Vector2.one*.5f && V(Get(rect,"pivot")) == Vector2.one*.5f, "NON_CENTER_GEOMETRY_UNSUPPORTED");
                var seen = new HashSet<string>(); var cursor=node;
                while(cursor!=null)
                { Require(seen.Add(S(cursor,"id")), "PARENT_CYCLE"); var parent=S(cursor,"parentId"); Require(parent=="" || input.nodes.ContainsKey(parent),"PARENT_MISSING"); cursor=parent==""?null:input.nodes[parent]; }
                if(Get(node,"resource")!=null) CheckPng(input, Get(node,"resource"), plan);
                CheckText(input, Get(node,"text"), plan);
                if(S(node,"type")=="PrefabRef")
                {
                    var reference=Get(node,"prefabRef");
                    Require(new[]{"ExternalPrefab","GeneratedTarget"}.Contains(S(reference,"kind")), "PREFAB_KIND_UNSUPPORTED");
                    if(S(reference,"kind")=="GeneratedTarget") Require(input.targets.ContainsKey(S(reference,"targetId")), "TARGET_REFERENCE_MISSING");
                    else
                    {
                        var mapped=Items(Get(map,"publicReferences")).SingleOrDefault(r=>S(r,"nodeId")==S(node,"id"));
                        Require(mapped!=null && S(mapped,"guid")==S(reference,"assetGuid"), "PUBLIC_REFERENCE_MISSING");
                        var path=AssetDatabase.GUIDToAssetPath(S(mapped,"guid"));
                        Require(path==S(mapped,"prefabPath"), "PUBLIC_SOURCE_CHANGED"); Pin(plan,path); Pin(plan,path+".meta");
                        var library=EUILibraryService.LoadDefault();
                        var entry=library.Entries.SingleOrDefault(e=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.Prefab))==S(mapped,"guid"));
                        Require(entry!=null,"PUBLIC_ENTRY_MISSING"); EUILibraryService.ResolveExact(library,entry.Id);
                        foreach(var over in Items(Get(reference,"overrides")))
                        {
                            var slotId=S(over,"slot"); var alias=Get(mapped,"slotMap"); if(Get(alias,slotId)!=null)slotId=S(alias,slotId);
                            var slot=entry.Slots.SingleOrDefault(s=>s.Id==slotId);
                            Require(slot!=null && Get(over,"text")!=null && Get(over,"resource")==null, "SLOT_OR_OVERRIDE_UNSUPPORTED");
                            foreach(var property in new[]{"m_fontSize","m_fontColor"})Require(slot.Properties.Contains(property),"SLOT_PROPERTY_FORBIDDEN");
                            CheckText(input,Get(over,"text"),plan);
                        }
                    }
                }
                if(S(node,"type")=="List")
                {
                    Require(S(Get(node,"layout"),"direction")=="Vertical", "LIST_LAYOUT_UNSUPPORTED");
                    var list=Items(Get(map,"lists")).SingleOrDefault(l=>S(l,"nodeId")==S(node,"id"));
                    Require(list!=null && S(list,"key")=="Id" && S(list,"index")=="display-position", "LIST_SEMANTICS_MISSING");
                    var contract=JsonUtility.FromJson<EUIContract>(File.ReadAllText(S(list,"contractPath")));
                    var declared=JsonUtility.FromJson<EUIContractList>(Get(list,"contract").ToJson());
                    Require(JsonUtility.ToJson(contract.lists[int.Parse(S(list,"contractIndex"))])==JsonUtility.ToJson(declared), "LIST_CONTRACT_CHANGED");
                    var item=input.nodes[S(list,"itemNodeId")];
                    Require(S(item,"parentId")==S(node,"id") && S(Get(item,"prefabRef"),"targetId")==S(list,"itemTargetId"), "LIST_REFERENCE_INVALID");
                }
            }
            foreach(var warning in Items(Get(bui,"warnings")))
                Require(new[]{"TEXT_REFERENCE_UNAVAILABLE","BUI_SCHEMA_REFERENCE_ONLY","UXP_FIELD_COVERAGE_FIRST_PASS","PNG_NAME_COLLISION_RENAMED"}.Contains(S(warning,"code")) || S(warning,"severity")=="Info", "EXPORT_DIAGNOSTIC: "+S(warning,"code"));
            Ordered(input).ToArray();
            Pin(plan, EUILibraryService.DefaultPath); Pin(plan,EUILibraryService.CatalogPath);
            if(!string.IsNullOrEmpty(request.newTextFontPath)) { Pin(plan,request.newTextFontPath); Require(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(request.newTextFontPath),"FONT_MISSING"); }
            return input;
        }

        private static Dictionary<string,J> Unique(IEnumerable<J> items,string field)
        {
            var result=new Dictionary<string,J>(StringComparer.Ordinal);
            foreach(var item in items) { var key=S(item,field); Require(key!=""&&!result.ContainsKey(key),"DUPLICATE_ID: "+key); result.Add(key,item); } return result;
        }
        private static string ImageSource(Input input,J resource) => SafePath(Path.GetDirectoryName(S(Get(input.map,"bui"),"path")).Replace('\\','/')+"/"+S(input.bui,"assetsRoot")+"/"+S(resource,"asset"));
        private static void CheckPng(Input input,J resource,EmberBuiPlan plan)
        {
            var path=ImageSource(input,resource); Pin(plan,path,S(resource,"sha256")); var bytes=File.ReadAllBytes(path);
            Require(bytes.Length>=24 && BitConverter.ToString(bytes,0,8)=="89-50-4E-47-0D-0A-1A-0A","PNG_INVALID");
            int Size(int offset)=>(bytes[offset]<<24)|(bytes[offset+1]<<16)|(bytes[offset+2]<<8)|bytes[offset+3];
            Require(Size(16)==int.Parse(S(resource,"width")) && Size(20)==int.Parse(S(resource,"height")),"PNG_SIZE_CHANGED");
            var border=Get(resource,"border");
            if(border!=null) Require(new[]{"left","right","top","bottom"}.All(k=>float.IsFinite(F(Get(border,k)))&&F(Get(border,k))>=0) && F(Get(border,"left"))+F(Get(border,"right"))<Size(16) && F(Get(border,"top"))+F(Get(border,"bottom"))<Size(20),"NINE_SLICE_CENTER_INVALID");
        }
        private static void CheckText(Input input,J text,EmberBuiPlan plan)
        {
            if(text==null)return;
            var style=Get(text,"visualStyle"); var features=Get(style,"features"); var effects=Get(style,"effects");
            Require(S(text,"styleStatus")=="Resolved" && S(style,"status")=="Resolved", "TEXT_STYLE_UNRESOLVED");
            Require(S(features,"strokeEnabled")!="True" && Get(effects,"layerEffects")==null && Get(effects,"strokeStyle")==null,"TEXT_EFFECT_UNSUPPORTED");
            Require(Items(Get(style,"textStyleRanges")).Count()<=1,"MIXED_TEXT_UNSUPPORTED");
            var preview=Get(text,"previewReference");
            if(S(preview,"status")=="captured")
            {
                Require(S(preview,"sourcePsdSha256")==S(Get(input.bui,"source"),"psdSha256"),"TEXT_REFERENCE_SOURCE_CHANGED");
                Pin(plan,Path.GetDirectoryName(S(Get(input.map,"bui"),"path")).Replace('\\','/')+"/"+S(preview,"path"),S(preview,"sha256"));
            }
        }

        private static IEnumerable<EmberBuiTargetEdit> Ordered(Input input)
        {
            var done=new HashSet<string>(); var stack=new HashSet<string>(); var output=new List<EmberBuiTargetEdit>();
            void Visit(string id)
            {
                if(done.Contains(id))return; Require(stack.Add(id),"TARGET_CYCLE");
                foreach(var nodeId in Items(Get(input.targets[id],"nodeIds")))
                { var reference=Get(input.nodes[nodeId.ToString()],"prefabRef"); if(S(reference,"kind")=="GeneratedTarget")Visit(S(reference,"targetId")); }
                stack.Remove(id); done.Add(id); output.Add(input.request.targets.Single(t=>t.targetId==id));
            }
            foreach(var id in input.targets.Keys)Visit(id); return output;
        }
        private static EUICreationRequest Creation(Input input,EmberBuiTargetEdit edit)
        {
            var role=S(input.targets[edit.targetId],"targetType");
            return new EUICreationRequest { PrefabName=edit.className,ClassName=edit.className,PageName=edit.className,
                ClassPath=edit.classPath,Role=role=="Page"||role=="Popup"?EUIBindingRole.Page:EUIBindingRole.Item,
                PageType=role=="Popup"?PageType.Popup:PageType.MainPage,SourceSkeleton=AssetDatabase.LoadAssetAtPath<GameObject>(edit.sourceSkeletonPath),
                ContentSlotPath=edit.contentPath,StructureOnly=edit.structureOnly,InheritBinding=edit.inheritBinding,
                ReferenceResolution=V(Get(Get(input.bui,"canvas"),"referenceResolution")) };
        }
        private static void AddWrite(EmberBuiPlan plan,string path)
        { if(string.IsNullOrEmpty(path))return; SafePath(path); if(!plan.writes.Contains(path))plan.writes.Add(path); if(!plan.writes.Contains(path+".meta"))plan.writes.Add(path+".meta"); }
        private static void AnalyzeTarget(Input input,EmberBuiTargetEdit edit,EmberBuiPlan plan,bool repeat)
        {
            Require(new[]{"update","create","candidate"}.Contains(edit.mode),"MODE_UNSUPPORTED");
            Require(edit.prefabPath.StartsWith("Assets/GameResource/Resources/UI/",StringComparison.Ordinal)&&edit.prefabPath.EndsWith(".prefab",StringComparison.Ordinal),"OUTPUT_PATH_INVALID");
            var mapped=Items(Get(input.map,"targets")).Single(t=>S(t,"targetId")==edit.targetId);
            var old=input.contracts[S(mapped,"prefabGuid")];
            var sourcePath=edit.mode=="update"?edit.prefabPath:edit.sourceSkeletonPath;
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath); Require(root,"SOURCE_MISSING");
            if(edit.mode=="update")
            {
                Require(edit.prefabGuid==old.prefabGuid && edit.prefabPath==old.prefabPath && AssetDatabase.AssetPathToGUID(edit.prefabPath)==edit.prefabGuid,"ASSET_IDENTITY_CHANGED");
                var current=EUIContractService.Export(edit.prefabPath);
                Require(current.blockers.Count==0,"CONTRACT_BLOCKED: "+string.Join(";",current.blockers));
                Require(current.codeMode==old.codeMode && current.role==old.role && current.className==old.className && current.pageType==old.pageType,"BINDING_CONFIG_CHANGED");
                foreach(var binding in old.bindings)
                    Require(current.bindings.Any(b=>(b.ownerId==binding.ownerId || repeat && edit.nodes.Any(n=>!string.IsNullOrEmpty(n.replaceSourceGuid)&&n.path==b.path))&&b.name==binding.name&&b.componentType==binding.componentType&&b.isFramework==binding.isFramework),"BINDING_INTERFACE_CHANGED: "+binding.name);
                AddWrite(plan,current.generatedPath); AddWrite(plan,current.bindingScriptPath); AddWrite(plan,current.pageDefinitionPath);
            }
            else if(!repeat)
            {
                Require(EUICreationService.TryBuildPlan(Creation(input,edit),out var creation,out var result),"CREATION_PREFLIGHT: "+result.Error);
                Require(creation.PrefabPath==edit.prefabPath,"CREATION_OUTPUT_MISMATCH");
                AddWrite(plan,creation.LogicScriptPath); AddWrite(plan,creation.BindingScriptPath); AddWrite(plan,creation.PageDefFile);
                plan.operations.Add(edit.mode+": "+edit.prefabPath+" from "+edit.sourceSkeletonPath+"; old "+old.prefabPath+" retained; external consumers not switched");
            }
            Pin(plan,sourcePath); Pin(plan,sourcePath+".meta"); AddWrite(plan,edit.prefabPath);
            var targetIds=Items(Get(input.targets[edit.targetId],"nodeIds")).Select(x=>x.ToString()).ToArray();
            Require(edit.nodes.Count==targetIds.Length && edit.nodes.Select(n=>n.nodeId).Distinct().Count()==targetIds.Length && edit.nodes.All(n=>targetIds.Contains(n.nodeId)),"NODE_MAPPING_COVERAGE");
            Require(edit.nodes.Where(n=>n.operation!="preserve"&&n.operation!="reference").Select(n=>n.path).Distinct().Count()==edit.nodes.Count(n=>n.operation!="preserve"&&n.operation!="reference"),"DUPLICATE_OUTPUT_NODE");
            foreach(var op in edit.nodes)
            {
                var node=input.nodes[op.nodeId];
                Require(new[]{"update","create","preserve","reference"}.Contains(op.operation),"OPERATION_UNSUPPORTED");
                if(op.operation=="preserve") { Require(!string.IsNullOrEmpty(op.reason),"PRESERVE_REASON_REQUIRED"); plan.operations.Add(op.nodeId+": retained outside scope: "+op.reason); continue; }
                if(op.operation=="reference") { Require(S(Get(node,"prefabRef"),"kind")=="GeneratedTarget","REFERENCE_KIND_INVALID"); continue; }
                var existing=Find(root,op.path);
                Require(op.operation=="create"?(repeat?existing:!existing):existing,"NODE_PRESENCE_CHANGED: "+op.path);
                if(existing && op.operation=="update" && edit.mode=="update" && !repeat)
                { Require(!string.IsNullOrEmpty(op.expectedObjectId)&&Identity(existing.gameObject)==op.expectedObjectId,"OBJECT_IDENTITY_CHANGED: "+op.path); }
                if(existing)Require(!EditorUtility.IsDirty(root),"ASSET_DIRTY");
                if(!string.IsNullOrEmpty(op.imagePath))
                {
                    Require(op.imagePath.StartsWith("Assets/GameResource/Resources/UI/",StringComparison.Ordinal)&&op.imagePath.Contains("/Atlas/")&&op.imagePath.EndsWith(".png",StringComparison.Ordinal),"IMAGE_OUTPUT_PATH_INVALID");
                    var resource=Get(node,"resource"); Require(resource!=null,"IMAGE_RESOURCE_MISSING");
                    if(File.Exists(op.imagePath))Pin(plan,op.imagePath,S(resource,"sha256"));
                    AddWrite(plan,op.imagePath);
                }
                if(S(node,"type")=="Image"||S(node,"type")=="RawImage")Require(Get(node,"resource")==null||!string.IsNullOrEmpty(op.imagePath),"IMAGE_DESTINATION_REQUIRED");
                if(op.textPolicy=="design")Require(op.operation=="create","DESIGN_TEXT_ONLY_FOR_NEW_STATIC_TEXT");
                plan.operations.Add(edit.prefabPath+" :: "+op.nodeId+" -> "+op.path+" ["+op.operation+", geometry="+op.geometry+", appearance="+op.appearance+"]");
                if(!string.IsNullOrEmpty(op.replaceSourceGuid))plan.operations.Add("Explicit nested source migration at "+op.path+": "+op.replaceSourceGuid+" -> "+S(Get(node,"prefabRef"),"assetGuid")+"; object/local IDs may change, root binding name/type and owning page GUID must remain");
            }
            foreach(var path in plan.writes.Where(File.Exists).ToArray())Pin(plan,path);
        }
        private static Transform Find(GameObject root,string path)=>path=="."||path==""?root.transform:root.transform.Find(path);
        private static string Identity(UnityEngine.Object obj)=>AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj,out string guid,out long id)?guid+":"+id:"";
        #endregion
    }
}
