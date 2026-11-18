using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Ember.UI;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    public sealed class RuntimeExtensionPageProbe:EUILogic
    {
        public bool Invoked;
        public override void OnBind()
        {
            var control=ControlMap["Control"];
            if(control is EUIImageLoader image)image.Load(null);
            if(control is EUIRawImageLoader raw)raw.Load(null);
            if(control is EUIScrollController scroll)scroll.Stop();
            if(control is TMPMarquee marquee)marquee.SetMarqueeEnabled(false);
            Invoked=true;
        }
    }
    public sealed class EUIRuntimeExtensionsBindingEditTests
    {
        [TestCase(typeof(EUIScrollController),EUIBinding.WidgetTypes.ScrollRect)]
        [TestCase(typeof(TMPMarquee),EUIBinding.WidgetTypes.Text)]
        [TestCase(typeof(EUIImageLoader),EUIBinding.WidgetTypes.Image)]
        [TestCase(typeof(EUIRawImageLoader),EUIBinding.WidgetTypes.RawImage)]
        public void CompanionIsDiscoverableAndExact_WhileAutomaticTypeStaysNative(Type companion,EUIBinding.WidgetTypes oldType)
        {
            var root=new GameObject("BindingFixture",typeof(RectTransform),typeof(EUIBinding));
            var node=new GameObject("m_Control",typeof(RectTransform));node.transform.SetParent(root.transform,false);
            var component=node.AddComponent(companion);
            string folder="Assets/__EmberBindingFixture_"+Guid.NewGuid().ToString("N");
            EUIBindingCodeGenUtility.TryRegenerateCode(null,out _);
            var previousRoot=EUIBinding.OnGetCodeRootPath;
            try
            {
                Assert.Contains(companion.Name,EUIBindingEditorUtility.GetComponentTypeNames());
                Assert.IsTrue(EUIBindingEditorUtility.GetAvailableComponentTypeOptions(node,-1,null).Any(x=>x.Name==companion.Name));
                Assert.AreEqual(oldType,EUIBindingEditorUtility.DetectWidgetType(node).Type);
                var binding=root.GetComponent<EUIBinding>();
                EUIBindingEditorUtility.SetBindings(binding,new[]{new EUIBindingEntrySnapshot{Name="Control",GameObjectPath="m_Control",WidgetType=EUIBinding.WidgetTypes.Extension,ClassName=companion.Name}});
                var map=new Dictionary<string,Component>();EUIBindingBridge.PopulateControlMap(binding,map);Assert.AreSame(component,map["Control"]);
                var implementation=CSharpLogicImplementationData.FindDefault();Assert.IsNotNull(implementation);
                AssetDatabase.CreateFolder("Assets",Path.GetFileName(folder));
                EUIBindingEditorUtility.SetPageInfo(binding,"","Module/RuntimeExtensions", "RuntimeExtensionFixture",false,PageType.MainPage);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/RuntimeExtensionFixture.prefab");
                EUIBinding.OnGetCodeRootPath=_=>folder+"/Code";
                Assert.IsTrue(EUIBindingCodeGenUtility.TryRegenerateCode(prefab.GetComponent<EUIBinding>(),out var error),error);
                var generated=File.ReadAllText(folder+"/Code/Module/RuntimeExtensions/RuntimeExtensionFixture.Binding.cs");StringAssert.Contains(companion.FullName,generated);StringAssert.Contains("Control",generated);
                var page=new EUIPage(root){LogicTypeName=typeof(RuntimeExtensionPageProbe).FullName};
                page.CreateLogic((map,logic)=>EUIBindingBridge.PopulateControlMap(binding,map,logic));Assert.IsTrue(((RuntimeExtensionPageProbe)page.Logic).Invoked);
                if(component is EUIImageLoader image)image.Clear();if(component is EUIRawImageLoader raw)raw.Clear();if(component is EUIScrollController scroll)scroll.Stop();if(component is TMPMarquee marquee)marquee.SetMarqueeEnabled(false);
            }
            finally{EUIBinding.OnGetCodeRootPath=previousRoot;AssetDatabase.DeleteAsset(folder);UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void ExistingImageExRemainsAnAutomaticExtension()
        {
            var go=new GameObject("ImageEx",typeof(RectTransform),typeof(EUIImageEx),typeof(EUIImageLoader));
            try{var type=EUIBindingEditorUtility.DetectWidgetType(go);Assert.AreEqual(EUIBinding.WidgetTypes.Extension,type.Type);Assert.AreEqual(nameof(EUIImageEx),type.ClassName);}
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
