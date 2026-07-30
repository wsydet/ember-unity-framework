////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEditor;
////using System.IO;
////using System.Security.Policy;
////using System;
////using TMPro;
////
////namespace Burner.UIExtension
////{
////    public class UIBakeUtils
////    {
////        static bool IsMultipleSprites(string path)
////        {
////            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
////            if (ti)
////            {
////                return ti.spriteImportMode == SpriteImportMode.Multiple;
////            }
////            else
////                return false;
////        }
////        public static bool GenerateStripedPrefab(string path, string outputPath, Func<string, bool> isValidAssetPath, Action<GameObject> preprocessor = null, Action<GameObject, List<PagePreloader.PreloadInfo>> additionalProcessor = null)
////        {
////            string hash, md5File;
////            if (!ShouldGenerateAsset(path, outputPath, out hash, out md5File))
////                return true;
////
////            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
////            if (!asset)
////                Burner.Logger.Error("Cannot load asset:" + path);
////
////            UnityEngine.GameObject obj = UnityEngine.Object.Instantiate(asset) as GameObject;
////            if (!obj)
////                Burner.Logger.Error("Cannot load asset:" + path);
////            obj.SetActive(false);
////            //PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
////
////            preprocessor?.Invoke(obj);
////            List<PagePreloader.PreloadInfo> loadInfo = new List<PagePreloader.PreloadInfo>();
////            UnityEngine.UI.Image[] textures = obj.GetComponentsInChildren<UnityEngine.UI.Image>(true);
////            List<string> assetsToLoad = new List<string>();
////            foreach (var i in textures)
////            {
////                if ((i.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
////                    continue;
////                PagePreloader.PreloadInfo info = new PagePreloader.PreloadInfo();
////                assetsToLoad.Clear();
////                if(i is ImageEx ex)
////                {
////                    {
////                        if (GetAssetPath(i.sprite, isValidAssetPath, out var p, out var fullP))
////                        {
////                            if (IsMultipleSprites(fullP))
////                            {
////                                assetsToLoad.Add($"{p}/{i.sprite.name}");
////                                info.HasMultipleSprites = true;
////                                i.sprite = null;
////                            }
////                            else
////                            {
////                                i.sprite = null;
////                                assetsToLoad.Add(p);
////                            }
////                        }
////                        else
////                            assetsToLoad.Add(null);
////                    }
////                    var arr = ex.GetSpriteArray();
////                    if (arr != null && arr.Length > 0)
////                    {
////                        for (int j = 0; j < arr.Length; j++)
////                        {
////                            var s = arr[j];
////                            if (!GetAssetPath(s, isValidAssetPath, out var p, out var fullP))
////                                assetsToLoad.Add(null);
////                            else
////                            {
////                                arr[j] = null;
////                                if (IsMultipleSprites(fullP))
////                                {
////                                    info.HasMultipleSprites = true;
////                                    assetsToLoad.Add($"{p}/{s.name}");
////                                }
////                                else
////                                    assetsToLoad.Add(p);
////                            }
////                        }
////                    }
////                    Array.Clear(arr, 0, arr.Length);
////                    i.sprite = null;
////                }
////                else
////                {
////                    if (GetAssetPath(i.sprite, isValidAssetPath, out var p, out var fullP))
////                    {
////                        if (IsMultipleSprites(fullP))
////                        {
////                            assetsToLoad.Add($"{p}/{i.sprite.name}");
////                            info.HasMultipleSprites = true;
////                            i.sprite = null;
////                        }
////                        else
////                        {
////                            i.sprite = null;
////                            assetsToLoad.Add(p);
////                        }
////                    }
////                }
////
////                if (assetsToLoad.Count > 0)
////                {
////                    info.Target = i;
////                    info.Type = PagePreloader.ComponentTypes.Image;
////                    info.AssetsToLoad = assetsToLoad.ToArray();
////
////                    loadInfo.Add(info);
////                }
////            }
////
////            var rawImgs = obj.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);
////            foreach (var i in rawImgs)
////            {
////                if ((i.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
////                    continue;
////
////                PagePreloader.PreloadInfo info = new PagePreloader.PreloadInfo();
////                if (GetAssetPath(i.texture, isValidAssetPath, out var p, out var fullP))
////                {
////                    i.texture = null;
////                    assetsToLoad.Add(p);
////                }
////                if (assetsToLoad.Count > 0)
////                {
////                    info.Target = i;
////                    info.Type = PagePreloader.ComponentTypes.RawImage;
////                    info.AssetsToLoad = assetsToLoad.ToArray();
////
////                    loadInfo.Add(info);
////                }
////            }
////
////#if LOVE_ENGINE_MULTILANGUAGE
////            var tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
////            foreach (var i in tmps)
////            {
////                if ((i.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
////                    continue;
////                var mtmp = i.GetComponent<MultiLanguageTMP>();
////                if (mtmp && !string.IsNullOrEmpty(mtmp.LID))
////                    i.text = null;
////            }
////#endif
////            additionalProcessor?.Invoke(obj, loadInfo);
////
////            var preloader = obj.AddComponent<PagePreloader>();
////            preloader.PreloadInfos = loadInfo.ToArray();
////
////            bool isSuccess = false;
////            GameObject objPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj, outputPath, out isSuccess);
////
////            
////            UnityEngine.Object.DestroyImmediate(obj);
////            if (isSuccess == false)
////            {
////                Burner.Logger.Error("生成Prefab 出错 " + outputPath);
////                return false;
////            }
////
////            using (System.IO.StreamWriter sw = new StreamWriter(md5File, false, System.Text.Encoding.ASCII))
////            {
////                sw.WriteLine(hash);
////                sw.Flush();
////            }
////            return true;
////        }
////
////        static bool GetAssetPath(UnityEngine.Object obj, Func<string, bool> isValidAssetPath, out string path, out string fullPath)
////        {
////            if (!obj)
////            {
////                path = null;
////                fullPath = null;
////                return false;
////            }
////            fullPath = AssetDatabase.GetAssetPath(obj);
////            if (!isValidAssetPath(fullPath))
////            {
////                path = null;
////                return false;
////            }
////            string p = System.IO.Path.GetFileName(fullPath);
////            if (p == "unity_builtin_extra")
////            {
////                path = null;
////                return false;
////            }
////            else
////            {
////                path = p.ToLower();
////                return true;
////            }
////        }
////
////        static bool ShouldGenerateAsset(string file, string genPath, out string hash, out string md5File)
////        {
////            string genFolder = System.IO.Path.GetDirectoryName(genPath);
////            string genFileName = System.IO.Path.GetFileNameWithoutExtension(genPath);
////            md5File = genFolder + "/" + genFileName + ".md5";
////            hash = AssetDatabase.GetAssetDependencyHash(file).ToString();
////
////            if (System.IO.File.Exists(md5File))
////            {
////                if (System.IO.File.Exists(genPath))
////                {
////                    using (System.IO.StreamReader sr = new StreamReader(md5File, System.Text.Encoding.ASCII))
////                    {
////                        if (hash != sr.ReadLine())
////                            return true;
////                        else
////                        {
////                            return false;
////                        }
////                    }
////                }
////                else
////                    return true;
////            }
////            else
////                return true;
////        }
////    }
////}
