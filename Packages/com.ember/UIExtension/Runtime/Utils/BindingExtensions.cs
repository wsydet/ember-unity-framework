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
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//    static class BindingExtensions
//    {
//        public static IUIBehaviour CreateUIBehaviour(this GameObject go, GameUILogic logic, string name, GameUIBinding.WidgetTypes type, string extensionName)
//        {
//            IUIBehaviour ctrl = null;
//            var page = logic.Page;
//            if (!go)
//            {
//                return null;
//            }
//            switch (type)
//            {
//                case GameUIBinding.WidgetTypes.Component:
//                    ctrl = new GameUIComponent();
//                    break;
//                case GameUIBinding.WidgetTypes.Image:
//                    ctrl = new GameImage();
//                    break;
//                case GameUIBinding.WidgetTypes.Text:
//                    ctrl = new GameText();
//                    break;
//                case GameUIBinding.WidgetTypes.InputField:
//                    ctrl = new GameInputField();
//                    break;
//                case GameUIBinding.WidgetTypes.Button:
//                    ctrl = new GameButton();
//                    break;
//                case GameUIBinding.WidgetTypes.Toggle:
//                    ctrl = new GameToggle();
//                    break;
//                case GameUIBinding.WidgetTypes.ToggleGroup:
//                    ctrl = new GameToggleGroup();
//                    break;
//                case GameUIBinding.WidgetTypes.ScrollRect:
//                    ctrl = new GameScrollRect();
//                    break;
//                case GameUIBinding.WidgetTypes.ProgressBar:
//                    ctrl = new GameProgressBar();
//                    break;
//                case GameUIBinding.WidgetTypes.UIContainer:
//                    ctrl = new GameUIContainer();
//                    break;
//                case GameUIBinding.WidgetTypes.RawImage:
//                    ctrl = new GameRawImage();
//                    break;
//                case GameUIBinding.WidgetTypes.Canvas:
//                    ctrl = new GameCanvas();
//                    break;
//                case GameUIBinding.WidgetTypes.TabLoader:
//                    ctrl = new GameTabLoader();
//                    break;
//                case GameUIBinding.WidgetTypes.Extension:
//                    ctrl = BurnerUIManager.Instance.CreateBehaviourByComponentName(extensionName);
//                    break;
//                case GameUIBinding.WidgetTypes.UILogic:
//                    var childBinding = go.GetComponent<GameUIBinding>();
//                    if (childBinding != null)
//                    {
//                        if (childBinding.NoCodeGeneration)
//                        {
//                            ctrl = new GameUIComponent();
//                        }
//                        else
//                        {
//                            ctrl = BurnerUIManager.Instance.MakeUILogic(childBinding.ClassName);
//                            if (ctrl == null)
//                                return null;
//                            ((GameUILogic)ctrl).Initialize(childBinding, page);
//                        }
//                    }
//                    else
//                    {
//                        Logger.Error($"{page.PrefabName}:{name}({go}) doesn't have GameUIBinding component");
//                    }
//
//                    break;
//            }
//            if (ctrl is GameUIComponent comp)
//            {
//                comp.UILogic = logic;
//                comp.Initialize(go);
//            }
//
//            return ctrl;
//        }
//    }
//}
