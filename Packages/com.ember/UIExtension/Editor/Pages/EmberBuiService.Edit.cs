// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using J = Ember.Basic.LitJson.JsonData;

namespace Ember.UIExtension.Editor
{
    public static partial class EmberBuiService
    {
        #region 内部方法
        private static void ImportImages(Input input,EmberBuiTargetEdit target)
        {
            foreach(var op in target.nodes.Where(n=>!string.IsNullOrEmpty(n.imagePath)&&n.operation!="preserve"))
            {
                var resource=Get(input.nodes[op.nodeId],"resource");
                if(!File.Exists(op.imagePath))
                { Directory.CreateDirectory(Path.GetDirectoryName(op.imagePath)); File.Copy(ImageSource(input,resource),op.imagePath,false); }
                AssetDatabase.ImportAsset(op.imagePath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(op.imagePath);
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.filterMode=FilterMode.Bilinear;
                importer.wrapMode=TextureWrapMode.Clamp; importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=4096; importer.spriteBorder=Border(resource); importer.SaveAndReimport();
            }
        }
        private static Vector4 Border(J resource)
        {
            var border=Get(resource,"border");
            return border==null?Vector4.zero:new Vector4(F(Get(border,"left")),F(Get(border,"bottom")),F(Get(border,"right")),F(Get(border,"top")));
        }
        private static Sprite SpriteFor(Input input,EmberBuiNodeEdit op,bool preview,List<UnityEngine.Object> temporary)
        {
            if(!preview)return AssetDatabase.LoadAssetAtPath<Sprite>(op.imagePath);
            var resource=Get(input.nodes[op.nodeId],"resource");
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false); temporary.Add(texture);
            Require(texture.LoadImage(File.ReadAllBytes(ImageSource(input,resource))),"PNG_DECODE_FAILED");
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Border(resource));
            temporary.Add(sprite);return sprite;
        }
        private static void Edit(Input input,EmberBuiTargetEdit target,GameObject root,bool preview,List<UnityEngine.Object> temporary)
        {
            if(target.applyReferenceResolution)
            {
                var scaler=root.GetComponent<CanvasScaler>();Require(scaler,"CANVAS_SCALER_MISSING");
                scaler.referenceResolution=V(Get(Get(input.bui,"canvas"),"referenceResolution"));
            }
            // Parent mappings precede children regardless of PSD stack order.
            var operations=target.nodes.Where(n=>n.operation!="preserve"&&n.operation!="reference").OrderBy(n=>(n.path??"").Count(c=>c=='/')).ToArray();
            foreach(var op in operations)
            {
                var node=input.nodes[op.nodeId]; var type=S(node,"type"); var transform=Find(root,op.path);
                if(op.operation=="create" && !transform)
                {
                    var parent=Find(root,op.parentPath); Require(parent,"PARENT_MAPPING_MISSING: "+op.parentPath);
                    GameObject obj;
                    if(type=="PrefabRef" && S(Get(node,"prefabRef"),"kind")=="ExternalPrefab")
                    {
                        var entry=Entry(S(Get(node,"prefabRef"),"assetGuid"));
                        obj=EUILibraryService.Instantiate(EUILibraryService.LoadDefault(),entry.Id,(RectTransform)parent);
                    }
                    else obj=new GameObject(Path.GetFileName(op.path),typeof(RectTransform));
                    obj.name=Path.GetFileName(op.path); obj.layer=parent.gameObject.layer; obj.transform.SetParent(parent,false); transform=obj.transform;
                }
                Require(transform,"NODE_MISSING: "+op.path);
                if(type=="PrefabRef")ApplyReference(input,op,node,transform.gameObject);
                if(op.geometry)Geometry(input,target,op,root,(RectTransform)transform);
                if(!string.IsNullOrEmpty(op.copyLayoutPath))
                {
                    var source=(RectTransform)Find(root,op.copyLayoutPath);Require(source,"LAYOUT_SOURCE_MISSING");
                    var destination=(RectTransform)transform;destination.anchorMin=source.anchorMin;destination.anchorMax=source.anchorMax;
                    destination.pivot=source.pivot;destination.sizeDelta=source.sizeDelta;destination.anchoredPosition=source.anchoredPosition;
                }
                if(op.siblingIndex>=0)transform.SetSiblingIndex(op.siblingIndex);
                if(op.active)transform.gameObject.SetActive((bool)Get(node,"active"));
                if(op.appearance)
                {
                    if(type=="Image"||type=="RawImage")
                    {
                        if(Get(node,"resource")==null)
                        {
                            Require(S(Get(node,"source"),"resourceType")=="DynamicTexture" || S(Get(node,"resourceIntent"),"type")=="DynamicTexture" || node.ToJson().Contains("DynamicTexture"),"IMAGE_RESOURCE_UNRESOLVED");
                            Require(transform.GetComponent<Image>(),"DYNAMIC_IMAGE_MISSING");
                        }
                        else
                        {
                            var sprite=SpriteFor(input,op,preview,temporary);
                            if(type=="RawImage")
                            {
                                var raw=transform.GetComponent<RawImage>()??transform.gameObject.AddComponent<RawImage>();
                                raw.texture=sprite.texture;raw.color=Color.white;raw.raycastTarget=false;
                            }
                            else
                            {
                                var image=transform.GetComponent<Image>()??transform.gameObject.AddComponent<EUIImageEx>();
                                image.sprite=sprite;image.color=Color.white;
                                if(image.type!=Image.Type.Filled)image.type=sprite.border==Vector4.zero?Image.Type.Simple:Image.Type.Sliced;
                                if(op.operation=="create")image.raycastTarget=false;
                            }
                        }
                    }
                    if(type=="Text")
                    {
                        var text=transform.GetComponent<TMP_Text>();
                        if(!text)
                        {
                            text=transform.gameObject.AddComponent<TMPEx>();
                            text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(input.request.newTextFontPath);
                            Require(text.font,"NEW_TEXT_FONT_REQUIRED");
                            ((TMPEx)text).SetFontSkin(-1,0);text.raycastTarget=false;
                        }
                        ApplyText(text,Get(node,"text"),op.textPolicy=="design");
                        if(op.textBoxSize.x>0&&op.textBoxSize.y>0)
                        {
                            var delta=op.textBoxSize-text.rectTransform.sizeDelta;
                            text.rectTransform.sizeDelta=op.textBoxSize;
                            text.rectTransform.anchoredPosition+=new Vector2(delta.x*.5f,-delta.y*.5f);
                        }
                    }
                }
                if(!string.IsNullOrEmpty(op.stateSlot))
                {
                    var toggle=Find(root,op.stateTogglePath).GetComponent<EUIToggleEx>();Require(toggle,"STATE_TOGGLE_MISSING");
                    if(op.stateSlot=="selected")toggle.OnNode=transform.gameObject;
                    else if(op.stateSlot=="disabled")toggle.DisableNode=transform.gameObject;
                    else throw new InvalidDataException("STATE_SLOT_UNSUPPORTED");
                }
                if(!string.IsNullOrEmpty(op.bindingName))
                {
                    var binding=root.GetComponent<EUIBinding>();var snapshot=EUIBindingEditorUtility.GetBindingSnapshot(binding);
                    Require(!snapshot.Entries.Any(e=>e.Name==op.bindingName),"BINDING_ALREADY_EXISTS");
                    Require(!string.IsNullOrEmpty(op.bindingType),"BINDING_TYPE_REQUIRED");
                    snapshot.Entries.Add(new EUIBindingEntrySnapshot { Name=op.bindingName,GameObjectPath=op.path=="."?"":op.path,
                        WidgetType=EUIBinding.WidgetTypes.Extension,ClassName=op.bindingType });
                    EUIBindingEditorUtility.ApplyBindingSnapshot(binding,snapshot);
                }
            }
            foreach(var list in Items(Get(input.map,"lists")))
            {
                var op=target.nodes.SingleOrDefault(n=>n.nodeId==S(list,"nodeId")); if(op==null||op.operation=="preserve")continue;
                var host=Find(root,op.path).GetComponent<EUIDynamicListHost>();Require(host,"LIST_HOST_MISSING");
                var declared=JsonUtility.FromJson<EUIContractList>(Get(list,"contract").ToJson());
                var itemTarget=input.request.targets.Single(t=>t.targetId==S(list,"itemTargetId"));
                if(target.mode!="update" && itemTarget.mode!="update" && !preview)
                {
                    using var serialized=new SerializedObject(host);
                    serialized.FindProperty("_itemTemplate").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(itemTarget.prefabPath);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var itemGuid=target.mode!="update"&&itemTarget.mode!="update"&&!preview?AssetDatabase.AssetPathToGUID(itemTarget.prefabPath):declared.itemGuid;
                Require(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(host.ItemTemplate))==itemGuid
                    && host.CacheCapacity==declared.cacheCapacity&&host.DataBinding==declared.dataBinding,"LIST_WIRING_CHANGED");
                Require(host.ValidateConfiguration().Count==0 && host.Content.childCount==0,"LIST_CONFIGURATION_INVALID");
            }
            foreach(var component in root.GetComponentsInChildren<Component>(true))
                if(component&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        private static EUILibraryEntry Entry(string guid)
        {
            var library=EUILibraryService.LoadDefault();
            return library.Entries.Single(e=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.Prefab))==guid);
        }
        private static void ApplyReference(Input input,EmberBuiNodeEdit op,J node,GameObject instance)
        {
            var reference=Get(node,"prefabRef");Require(S(reference,"kind")=="ExternalPrefab","GENERATED_REFERENCE_REQUIRES_HOST");
            var guid=S(reference,"assetGuid");var entry=Entry(guid);
            var source=PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if(AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance))!=guid)
            {
                Require(!string.IsNullOrEmpty(op.replaceSourceGuid)&&AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))==op.replaceSourceGuid,"NESTED_SOURCE_CHANGED");
                var instanceName=instance.name;
                PrefabUtility.ReplacePrefabAssetOfPrefabInstance(instance,entry.Prefab,InteractionMode.AutomatedAction);
                instance.name=instanceName;
            }
            var mapped=Items(Get(input.map,"publicReferences")).Single(r=>S(r,"nodeId")==op.nodeId);
            foreach(var over in Items(Get(reference,"overrides")))
            {
                var id=S(over,"slot");if(Get(Get(mapped,"slotMap"),id)!=null)id=S(Get(mapped,"slotMap"),id);
                var text=(TMP_Text)EUILibraryService.ResolveInstanceSlot(EUILibraryService.LoadDefault(),entry.Id,instance,id,"m_fontSize");
                EUILibraryService.ResolveInstanceSlot(EUILibraryService.LoadDefault(),entry.Id,instance,id,"m_fontColor");
                ApplyText(text,Get(over,"text"),false);
            }
        }
        private static void ApplyText(TMP_Text text,J payload,bool designValue)
        {
            Require(text&&text.font&&payload!=null,"TEXT_OR_FONT_MISSING");
            var features=Get(Get(payload,"visualStyle"),"features");
            text.fontSize=F(Get(payload,"fontSize"));
            Require(ColorUtility.TryParseHtmlString(S(features,"fillColor"),out var color),"TEXT_COLOR_INVALID");text.color=color;
            // Public slots authorize size/color only. Alignment and font stay with the engineering layout.
            if(designValue)text.text=S(payload,"text");
        }
        private static void Geometry(Input input,EmberBuiTargetEdit target,EmberBuiNodeEdit op,GameObject root,RectTransform rect)
        {
            var source=input.nodes[string.IsNullOrEmpty(op.geometrySourceNodeId)?op.nodeId:op.geometrySourceNodeId];
            var geometry=Get(source,"rect"); var position=V(Get(geometry,"anchoredPosition"));var size=V(Get(geometry,"sizeDelta"));
            var space=(RectTransform)(string.IsNullOrEmpty(target.contentPath)?root.transform:Find(root,target.contentPath));
            Require(space,"COORDINATE_SPACE_MISSING");
            if(rect==space) { rect.sizeDelta=size;return; }
            rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.pivot=Vector2.one*.5f;rect.sizeDelta=size;
            // Canvas prefab roots may serialize zero scale and zero size. World matrices then collapse.
            // Resolve anchor frames in the declared canvas reference space, independently of root scale.
            var resolution=root.GetComponent<CanvasScaler>()?.referenceResolution??V(Get(Get(input.bui,"canvas"),"referenceResolution"));
            Frame(space,(RectTransform)root.transform,resolution,out var spaceOrigin,out var spaceSize);
            Frame((RectTransform)rect.parent,(RectTransform)root.transform,resolution,out var parentOrigin,out var parentSize);
            var targetCenter=spaceOrigin+Vector2.Scale(Vector2.one*.5f-space.pivot,spaceSize);
            var parentCenter=parentOrigin+Vector2.Scale(Vector2.one*.5f-((RectTransform)rect.parent).pivot,parentSize);
            rect.anchoredPosition=targetCenter+position-parentCenter;
            rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
        }
        private static void Frame(RectTransform rect,RectTransform root,Vector2 resolution,out Vector2 origin,out Vector2 size)
        {
            if(rect==root){origin=Vector2.zero;size=root.GetComponent<Canvas>()?resolution:root.sizeDelta;return;}
            Require(rect.localScale==Vector3.one&&Quaternion.Angle(rect.localRotation,Quaternion.identity)<.01f,"TRANSFORM_GEOMETRY_UNSUPPORTED");
            var parent=rect.parent as RectTransform;Require(parent,"RECT_PARENT_REQUIRED");
            Frame(parent,root,resolution,out var parentOrigin,out var parentSize);
            // EUISafeArea drives anchors from Screen.safeArea at runtime. Its saved zero-size rect is not a design frame.
            if(rect.GetComponent<EUISafeArea>())
            { size=parentSize;origin=parentOrigin+Vector2.Scale(rect.pivot-parent.pivot,parentSize);return; }
            size=Vector2.Scale(parentSize,rect.anchorMax-rect.anchorMin)+rect.sizeDelta;
            var anchor=rect.anchorMin+Vector2.Scale(rect.anchorMax-rect.anchorMin,rect.pivot);
            origin=parentOrigin+Vector2.Scale(anchor-parent.pivot,parentSize)+rect.anchoredPosition;
        }
        private static void Verify(Input input,EmberBuiReceipt receipt)
        {
            foreach(var target in input.request.targets)
            {
                var current=EUIContractService.Export(target.prefabPath);
                Require(current.blockers.Count==0,"SAVED_CONTRACT_BLOCKED: "+string.Join(";",current.blockers));
                if(target.mode=="update")
                {
                    var previous=input.contracts[target.prefabGuid];
                    Require(current.prefabGuid==previous.prefabGuid && current.codeMode==previous.codeMode && current.pageType==previous.pageType,"SAVED_IDENTITY_CHANGED");
                    foreach(var binding in previous.bindings)
                        Require(current.bindings.Any(b=>b.name==binding.name&&b.path==binding.path&&b.componentType==binding.componentType&&b.isFramework==binding.isFramework),"SAVED_BINDING_CHANGED: "+binding.name);
                    Require(JsonUtility.ToJson(new ListWrapper { lists=current.lists })==JsonUtility.ToJson(new ListWrapper { lists=previous.lists }),"SAVED_LIST_CONTRACT_CHANGED");
                }
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(target.prefabPath);
                foreach(var op in target.nodes.Where(o=>o.operation!="preserve"&&o.operation!="reference"))
                {
                    var node=input.nodes[op.nodeId];var transform=Find(root,op.path);Require(transform,"SAVED_NODE_MISSING");
                    if(S(node,"type")=="PrefabRef")Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject)==AssetDatabase.GUIDToAssetPath(S(Get(node,"prefabRef"),"assetGuid")),"SAVED_NESTED_SOURCE_CHANGED");
                }
                receipt.verification.Add(target.prefabPath+": saved/reloaded; binding/identity/list/nested source checked");
            }
            foreach(var reference in Items(Get(input.map,"publicReferences")))
            {
                var original=Items(Get(input.map,"inputs")).Single(r=>S(r,"path")==S(reference,"prefabPath"));
                Require(Hash(S(original,"path"))==S(original,"sha256"),"SHARED_SOURCE_MODIFIED");
            }
        }
        [Serializable] private sealed class ListWrapper { public List<EUIContractList> lists; }
        #endregion
    }
}
