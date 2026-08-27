//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Reflection;
//
//namespace Burner.UIExtension
//{
//    public enum GlobalAnimationEvents
//    {
//        Start,
//        Stop,
//        Event
//    }
//
//    public delegate void GlobalAnimationEventDelegate(string pageName, string componentName, GlobalAnimationEvents evt, string arg);
//
//    public delegate void GlobalClickEventDelegate(string pageName, string componentName);
//
//    public delegate void GlobalAnimationEventDelegateEx(GameUIComponent comp, GlobalAnimationEvents evt, string arg);
//
//    public delegate void GlobalClickEventDelegateEx(GameUIComponent comp);
//    public class GlobalEvents
//    {
//        GlobalAnimationEventDelegate animationEvents;
//        GlobalAnimationEventDelegateEx animationEventsEx;
//        GlobalClickEventDelegate clickEvents;
//        GlobalClickEventDelegateEx clickEventsEx;
//
//        public void AddAnimationEventListener(GlobalAnimationEventDelegate evt)
//        {
//            animationEvents += evt;
//        }
//
//        public void AddAnimationEventListenerEx(GlobalAnimationEventDelegateEx evt)
//        {
//            animationEventsEx += evt;
//        }
//
//        public void RemoveAnimationEventListener(GlobalAnimationEventDelegate evt)
//        {
//            animationEvents -= evt;
//        }
//        public void RemoveAnimationEventListenerEx(GlobalAnimationEventDelegateEx evt)
//        {
//            animationEventsEx -= evt;
//        }
//
//        public void AddClickEventListener(GlobalClickEventDelegate evt)
//        {
//            clickEvents += evt;
//        }
//
//        public void AddClickEventListenerEx(GlobalClickEventDelegateEx evt)
//        {
//            clickEventsEx += evt;
//        }
//
//        public void RemoveClickEventListener(GlobalClickEventDelegate evt)
//        {
//            clickEvents -= evt;
//        }
//        public void RemoveClickEventListenerEx(GlobalClickEventDelegateEx evt)
//        {
//            clickEventsEx -= evt;
//        }
//        internal void DispatchAnimationEvent(GameUIComponent comp, GlobalAnimationEvents evt, string arg)
//        {
//            animationEventsEx?.Invoke(comp, evt, arg);
//            animationEvents?.Invoke(comp.UILogic.Page.PrefabName, comp.GameObject.name, evt, arg);
//        }
//
//        internal void DispatchClickEvent(GameUIComponent comp)
//        {
//            clickEventsEx?.Invoke(comp);
//            clickEvents?.Invoke(comp.UILogic.Page.PrefabName, comp.GameObject.name);
//        }
//
//        internal void Clear()
//        {
//            animationEvents = null;
//            animationEventsEx = null;
//            clickEvents = null;
//            clickEventsEx = null;
//        }
//    }
//}
