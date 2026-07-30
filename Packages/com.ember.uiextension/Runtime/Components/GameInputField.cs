//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using System.Globalization;
//using UnityEngine;
//
//using UnityEngine.UI;
//using TMPro;
//
//namespace Burner.UIExtension
//{
//    public class GameInputField : GameUIComponent
//    {
//        public UnityEngine.UI.InputField input;
//        public TMPro.TMP_InputField tmpInput;
//        bool onEndEditAdded;
//        System.Action<string> onEndEditDele, onValueChangeDele;
//        public int MaxCharNum
//        {
//            get; set;
//        }
//
//        public override void OnInit()
//        {
//            input = GetComponent<UnityEngine.UI.InputField>();
//            if (input != null)
//            {
//                input.onValueChanged.AddListener(onValueChange);
//            }
//            else
//            {
//                tmpInput = GetComponent<TMPro.TMP_InputField>();
//                tmpInput.onValueChanged.AddListener(onValueChange);
//            }
//
//        }
//
//        public System.Action<string> OnEndEdit
//        {
//            get => onEndEditDele;
//            set
//            {
//                if(!onEndEditAdded)
//                {
//                    onEndEditAdded = true;
//                    if (input)
//                    {
//                        input.onEndEdit.AddListener(onEndEdit);
//                    }
//                    else
//                    {
//                        tmpInput.onEndEdit.AddListener(onEndEdit);
//                    }
//                }
//                onEndEditDele = value;
//            }
//        }
//
//        public System.Action<string> OnValueChange
//        {
//            get => onValueChangeDele;
//            set
//            {
//                onValueChangeDele = value;
//            }
//        }
//
//        private void onEndEdit(string newVal)
//        {
//            onEndEditDele?.Invoke(newVal);
//        }
//        private void onValueChange(string newVal)
//        {
//            onValueChangeDele?.Invoke(newVal);
//            if (MaxCharNum <= 0)
//                return;
//            if (input != null)
//            {
//                string v = input.text;
//                if (v.Length > MaxCharNum)
//                {
//                    input.text = v.Substring(0, MaxCharNum);
//                }
//            }
//            else
//            {
//                string v = tmpInput.text;
//                if (v.Length > MaxCharNum)
//                {
//                    tmpInput.text = v.Substring(0, MaxCharNum);
//                }
//            }
//        }
//
//        /// <summary>
//        /// 设置是否可编辑
//        /// </summary>
//        public bool Editable
//        {
//            set
//            {
//                if (input != null)
//                    input.interactable = value;
//                if (tmpInput != null)
//                    tmpInput.interactable = value;
//            }
//            get
//            {
//                if (input != null)
//                    return input.interactable;
//                if (tmpInput != null)
//                    return tmpInput.interactable;
//
//                return false;
//            }
//        }
//
//        public void SetTextWithoutNotify(string text)
//        {
//            if (tmpInput != null)
//                tmpInput.SetTextWithoutNotify(text);
//        }
//
//        /// <summary>
//        /// 设置或获取当前文本框内文本
//        /// </summary>
//        public string Text
//        {
//            set
//            {
//                if (input != null)
//                    input.text = value;
//                if (tmpInput != null)
//                    tmpInput.text = value;
//            }
//            get
//            {
//                if (input != null)
//                    return input.text;
//                if (tmpInput != null)
//                    return tmpInput.text;
//                return "";
//            }
//        }
//
//        public void ActivateInputField()
//        {
//            if (tmpInput != null)
//            {
//                tmpInput.ActivateInputField();
//            }
//        }
//
//        /// <summary>
//        /// 设置是否支持换行
//        /// </summary>
//        public bool MultipleLine
//        {
//            set
//            {
//                if (input != null)
//                    input.lineType = value ? UnityEngine.UI.InputField.LineType.MultiLineNewline : UnityEngine.UI.InputField.LineType.SingleLine;
//            }
//            get
//            {
//                if (input != null)
//                    return input.lineType == UnityEngine.UI.InputField.LineType.MultiLineNewline;
//
//                return false;
//            }
//        }
//
//        protected override void ClearEventCallbacks()
//        {
//            base.ClearEventCallbacks();
//            onEndEditDele = null;
//            onValueChangeDele = null;
//        }
//
//        /// <summary>
//        /// 光标处插入文本
//        /// </summary>
//        public void InsertTextAtCursor(string str)
//        {
//            if(tmpInput != null)
//            {
//                tmpInput.InsertInput(str);
//            }
//        }
//        
//        /// <summary>
//        /// 删除光标左侧字符
//        /// </summary>
//        public void DeleteTextAtCursor()
//        {
//            if (tmpInput == null || string.IsNullOrEmpty(tmpInput.text))
//                return;
//            tmpInput.DeleteInput();
//        }
//    }
//}
