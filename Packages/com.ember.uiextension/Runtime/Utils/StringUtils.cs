//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.IO;
//using UnityEngine;
//
//namespace Burner.UIExtension.Utils
//{
//
//    /// <summary>
//    /// 比较慢但是省内存的屏蔽字
//    /// </summary>
//    public static class DirtWordServiceCompact
//    {
//        static string[] words;
//        private static String[] replaceKey = {"","*","**","***","****","*****","******","*******","********"
//        ,"*********","**********","***********","************","*************","**************"
//        ,"***************","****************","*****************","******************"};
//        public static void Initialize(string[] words)
//        {
//            /*for (int i = 0; i < words.Length; i++)
//            {
//                words[i] = words[i].ToLower();
//            }*/
//            //DirtWordServiceCompact.words = words;
//            DirtWordServiceCompact.words = new string[0];
//        }
//        public static string ReplaceDirtWord(this string str)
//        {
//            //str = str.ToLower();
//            //for (int i = 0; i < words.Length; i++)
//            //{
//            //    if (words[i] == string.Empty)
//            //        continue;
//            //    if (str.Contains(words[i]))
//            //    {
//            //        str = str.Replace(words[i], replaceKey[words[i].Length]);
//            //    }
//            //}
//            return str;
//        }
//    }
//
//    static class DirtWordService
//    {
//        static DirtWordNode root = new DirtWordNode();
//
//        public static void Initialize(string[] words)
//        {
//            foreach (var i in words)
//            {
//                AddDirtWord(i.Trim('\r'), root);
//            }
//        }
//        public static void AddDirtWord(string word, DirtWordNode root)
//        {
//            DirtWordNode curNode = root;
//            char[] arr = word.ToCharArray();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                char c = arr[i];
//                curNode = curNode.AppendChild(c, i == arr.Length - 1);
//            }
//        }
//
//        public static float DistanceTo(this Vector2 me, Vector2 aim)
//        {
//            Vector2 temp = me - aim;
//            return temp.magnitude;
//        }
//
//        public static bool HasDirtWord(this string str)
//        {
//            List<DirtWordNode> m;
//            ReplaceDirtWord(str, out m);
//            return m.Count > 0;
//        }
//        public static string ReplaceDirtWord(string str)
//        {
//            List<DirtWordNode> m;
//            return ReplaceDirtWord(str, out m);
//        }
//
//        public static string ReplaceDirtWord(this string str, out List<DirtWordNode> match)
//        {
//            return ReplaceDirtWord(str, root, out match);
//        }
//
//        public static string ReplaceDirtWord(this string str, DirtWordNode root, out List<DirtWordNode> match)
//        {
//            match = new List<DirtWordNode>();
//            if (str == null)
//                return null;
//            char[] arr = str.ToCharArray();
//            int curIdx = 0;
//            int wordIdx = 0;
//            StringBuilder sb = new StringBuilder();
//            DirtWordNode curNode = root;
//            DirtWordNode old = curNode;
//            while (curIdx < str.Length)
//            {
//                char c = arr[curIdx + wordIdx];
//                old = curNode;
//                curNode = curNode[c];
//                if (curNode != null)
//                {
//                    wordIdx++;
//                    if (curNode.Terminated)
//                    {
//                        DirtWordNode newNode = new DirtWordNode();
//                        newNode.Word = curNode.Word;
//                        newNode.Index = curIdx;
//                        match.Add(newNode);
//                        for (int i = 0; i < curNode.Word.Length; i++)
//                        {
//                            sb.Append('*');
//                        }
//                        curIdx += wordIdx;
//                        wordIdx = 0;
//                        curNode = root;
//                    }
//                    else if (curIdx + wordIdx >= str.Length)
//                    {
//                        for (int i = 0; i < wordIdx; i++)
//                            sb.Append(arr[curIdx + i]);
//                        curIdx += wordIdx;
//                    }
//                }
//                else
//                {
//                    if (old != null && old != root)
//                    {
//                        if (old.CanTerminate)
//                        {
//                            DirtWordNode newNode = new DirtWordNode();
//                            newNode.Word = old.Word;
//                            newNode.Index = curIdx;
//                            match.Add(newNode);
//                            for (int i = 0; i < old.Word.Length; i++)
//                            {
//                                sb.Append('*');
//                            }
//                        }
//                        else
//                            sb.Append(old.Word);
//                        curNode = root;
//                        curIdx += wordIdx;
//                        wordIdx = 0;
//                        old = null;
//                    }
//                    else
//                    {
//                        sb.Append(c);
//                        curNode = root;
//                        curIdx++;
//                        wordIdx = 0;
//                    }
//                }
//            }
//            if (old != null && old != root)
//            {
//                if (old.CanTerminate)
//                {
//                    DirtWordNode newNode = new DirtWordNode();
//                    newNode.Word = curNode.Word;
//                    newNode.Index = curIdx;
//                    match.Add(newNode);
//                    for (int i = 0; i < old.Word.Length; i++)
//                    {
//                        sb.Append('*');
//                    }
//                }
//                else
//                    sb.Append(old.Word);
//                curNode = root;
//                curIdx += wordIdx;
//                wordIdx = 0;
//                old = null;
//            }
//            return sb.ToString();
//        }
//    }
//
//    class DirtWordNode
//    {
//        Dictionary<char, DirtWordNode> children = new Dictionary<char, DirtWordNode>();
//        string word = "";
//        public char Character { get; set; }
//
//        public bool CanTerminate { get; set; }
//
//        public int Index { get; set; }
//        public string Word { get { return word; } set { word = value; } }
//
//        public bool Terminated { get { return children.Count == 0; } }
//
//        public DirtWordNode this[char c]
//        {
//            get
//            {
//                DirtWordNode res;
//                if (children.TryGetValue(c, out res))
//                    return res;
//                else
//                    return null;
//            }
//        }
//
//        public DirtWordNode AppendChild(char c, bool canTerminate)
//        {
//            if (!children.ContainsKey(c))
//            {
//                DirtWordNode node = new DirtWordNode();
//                node.Character = c;
//                node.Word = word + c;
//                node.CanTerminate = canTerminate;
//                children.Add(c, node);
//                return node;
//            }
//            else
//            {
//                var node = children[c];
//                if (canTerminate && !node.CanTerminate)
//                    node.CanTerminate = canTerminate;
//                return node;
//            }
//        }
//    }
//}
