// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System.Reflection;
using Ember.UIExtension;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Ember.UI.Tests
{
    public class EUICommonControlsEditTests
    {
        private GameObject _root;
        [SetUp] public void Setup() { _root=new GameObject("Fixture",typeof(RectTransform)); _root.SetActive(false); }
        [TearDown] public void Cleanup() { if(_root.activeSelf) Disable(); Object.DestroyImmediate(_root); }
        private void Lifecycle(string name) { foreach(var c in _root.GetComponents<MonoBehaviour>()) c.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null); }
        private void Enable() { _root.SetActive(true); Lifecycle("OnEnable"); }
        private void Disable() { Lifecycle("OnDisable"); _root.SetActive(false); }
        private T Child<T>(string name) where T:Component
        { var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(_root.transform,false);return g.AddComponent<T>(); }
        private void Set(Object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        [Test] public void NumericRangeChange_IsSilent_AndClampsWithoutRecursiveNotification()
        {
            var slider=Child<Slider>("Slider");var number=_root.AddComponent<EUINumericControl>();Set(number,"_slider",slider);
            var count=0;number.Changed.AddListener(_=>count++);Enable();
            number.Configure(100,200,5,127);Assert.AreEqual(125,number.Value);Assert.AreEqual(125,slider.value);Assert.AreEqual(0,count);
            number.SetValue(999);Assert.AreEqual(200,number.Value);Assert.AreEqual(1,count);
            number.SetValue(999);Assert.AreEqual(1,count);
            Disable();Enable();slider.value=145;Assert.AreEqual(145,number.Value);Assert.AreEqual(2,count);
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>number.SetValue(float.NaN));
        }
        [Test] public void ReadOnlyInput_RejectsClear_AndReenableDoesNotDuplicateEvents()
        {
            var input=Child<TMP_InputField>("Input");var clear=Child<Button>("Clear");var actions=_root.AddComponent<EUIInputActions>();
            Set(actions,"_input",input);Set(actions,"_clear",clear);Enable();input.text="keep";
            actions.SetReadOnly(true);clear.onClick.Invoke();Assert.AreEqual("keep",input.text);Assert.IsFalse(clear.interactable);
            actions.SetReadOnly(false);Disable();Enable();var count=0;input.onValueChanged.AddListener(_=>count++);
            clear.onClick.Invoke();Assert.AreEqual("",input.text);Assert.AreEqual(1,count);
        }
        [Test] public void BadgeModes_AreExclusive_AndOverflowIsCapped()
        {
            var dot=Child<Image>("Dot").gameObject;var tag=Child<Image>("Tag").gameObject;var label=Child<TextMeshProUGUI>("Label");var badge=_root.AddComponent<EUIBadgeView>();
            Set(badge,"_dot",dot);Set(badge,"_tag",tag);Set(badge,"_label",label);
            badge.ShowDot();Assert.IsTrue(dot.activeSelf);Assert.IsFalse(tag.activeSelf);
            badge.ShowCount(150);Assert.IsFalse(dot.activeSelf);Assert.IsTrue(tag.activeSelf);Assert.AreEqual("99+",label.text);
            badge.ShowCount(0);Assert.IsFalse(dot.activeSelf);Assert.IsFalse(tag.activeSelf);
        }
        [Test] public void StaticChoices_RejectDisabled_AndNotifyOnlyForChanges()
        {
            var menu=_root.AddComponent<EUIStaticChoiceMenu>();var trigger=Child<Button>("Trigger");var blocker=Child<Button>("Blocker");
            var panel=Child<Image>("Panel").rectTransform;var caption=Child<TextMeshProUGUI>("Caption");
            var options=new[]{Child<Button>("A"),Child<Button>("B")};var labels=new TMP_Text[]{Child<TextMeshProUGUI>("LA"),Child<TextMeshProUGUI>("LB")};
            Set(menu,"_trigger",trigger);Set(menu,"_blocker",blocker);Set(menu,"_panel",panel);Set(menu,"_caption",caption);Set(menu,"_options",options);Set(menu,"_labels",labels);
            var count=0;menu.Changed.AddListener(_=>count++);Enable();
            menu.SetOption(0,"Enabled");menu.SetOption(1,"Disabled",false);menu.Select(1);Assert.AreEqual(-1,menu.SelectedIndex);
            menu.Select(0);menu.Select(0);Assert.AreEqual(1,count);Assert.AreEqual("Enabled",caption.text);
            Disable();Enable();menu.SetOption(1,"Now enabled");options[1].onClick.Invoke();Assert.AreEqual(2,count);
        }
    }
}

