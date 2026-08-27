////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEditor;
////using com.adjust.sdk;
////using System;
////
////namespace Burner.UIExtension
////{
////	[CustomEditor(typeof(ShowTextCreator))]
////	public class ShowTextureCreatorInspector : UnityEditor.Editor
////	{
////		private ShowTextCreator n;
////
////		public override void OnInspectorGUI()
////		{
////			base.OnInspectorGUI();
////
////			n = target as ShowTextCreator;
////			if (GUILayout.Button("生成"))
////			{
////				Create(n);
////			}
////		}
////
////		private string GetFontAndCharName(GameObject[] gos, string spriteName)
////		{
////			foreach (var go in gos)
////			{
////				ShowTextSource sts = go.GetComponent<ShowText>().showTextSource;
////				if (sts == null)
////				{
////					Debug.LogError(go.name + "没有ShowText组件");
////					continue;
////				}
////				string language;
////				var cs = sts.GetCharSourceWithSpriteName(spriteName, out language);
////				if (cs != null)
////				{
////					return sts.fontName + "_" + cs.code + language;
////				}
////			}
////			// Debug.LogError("没有引用的图片：" + spriteName);
////			return spriteName;
////		}
////
////		private static Tuple<int, int> GetCapsuleSize(int i)
////        {
////            var power = Mathf.Log(Mathf.NextPowerOfTwo(i), 2);
////            var widthPower = Mathf.Ceil(power / 2.0f);
////            var heightPower = power - widthPower;
////            return new Tuple<int, int>((int)Mathf.Pow(2, widthPower), (int)Mathf.Pow(2, heightPower));
////        }
////
////		private void Create(ShowTextCreator ns)
////		{
////			var json = JSON.Parse(ns.testAsset.text);
////			var node = json["frames"];
////			var o = node.AsObject;
////
////			float width = ns.texture.width;
////			float height = ns.texture.height;
////			int totalCount = o.Count;
////
////			var (texw, texh) = GetCapsuleSize(totalCount * 2);
////			Texture2D tex = new Texture2D(texw, texh, TextureFormat.RGBAHalf, false, false);
////			tex.filterMode = FilterMode.Point;
////			tex.wrapMode = TextureWrapMode.Clamp;
////			tex.anisoLevel = 0;
////
////			ShowTextInstanceSource sts = ScriptableObject.CreateInstance<ShowTextInstanceSource>();
////			sts.chars = new List<ShowTextInstanceSource.CharElement>();
////
////			int index = 0;
////			foreach (KeyValuePair<string, JSONNode> c in o)
////			{
////				int x = c.Value["frame"]["x"].AsInt;
////				int y = c.Value["frame"]["y"].AsInt;
////				int w = c.Value["frame"]["w"].AsInt;
////				int h = c.Value["frame"]["h"].AsInt;
////				int bx = index % texw;
////				int by = index / texw;
////
////				tex.SetPixel(bx, by, new Color(x / width, y / height, (x + w) / width, y / height));
////				tex.SetPixel(bx + 1, by, new Color((x + w) / width, (y + h) / height, x / width, (y + h) / height));
////
////				ShowTextInstanceSource.CharElement cs = new ShowTextInstanceSource.CharElement();
////				cs.name = GetFontAndCharName(ns.aniObjects, c.Key);
////				cs.index = index / 2;
////				cs.width = w;
////				cs.height = h;
////				sts.chars.Add(cs);
////
////				index += 2;
////			}
////
////			string pathAndName = n.pathAndName.Replace(".", "/");
////			pathAndName = n.pathAndName.Replace("\\", "/");
////
////			AssetDatabase.CreateAsset(tex, "Assets/" + pathAndName + "_texture.asset");
////			AssetDatabase.CreateAsset(sts, "Assets/" + pathAndName + "_textsource.asset");
////			var mesh = CreateMesh(pathAndName);
////
////			GameObject[] gos = new GameObject[ns.aniObjects.Length];
////			for (int i = 0; i < ns.aniObjects.Length; i++)
////			{
////				string name = ns.aniObjects[i].name + "_instance";
////				gos[i] = new GameObject(name);
////
////				var s = gos[i].AddComponent<ShowTextInstance>();
////				var stcom = ns.aniObjects[i].GetComponent<ShowText>();
////				s.text = stcom.text;
////				s.fontName = stcom.showTextSource.fontName;
////				s.source = sts;
////				var mf = gos[i].AddComponent<MeshFilter>();
////				mf.mesh = mesh;
////				var mr = gos[i].AddComponent<MeshRenderer>();
////
////				// gos[i].layer = LayerMask.NameToLayer("Hud");
////				// var sortingGroup = gos[i].AddComponent<SortingGroup>();
////				// sortingGroup.sortingLayerID = SortingLayer.NameToID("HudText");
////			}
////			var mat = BuildAniTex(ns, tex, pathAndName, gos);
////
////			// TODO: Ensure that the folder exists, or else create it
////			for (int i = 0; i < gos.Length; i++)
////			{
////				string name = ns.aniObjects[i].name + "_instance";
////				PrefabUtility.SaveAsPrefabAsset(gos[i], "Assets/" + pathAndName + "/" + name + ".prefab");
////			}
////		}
////
////		private Mesh CreateMesh(string pathAndName)
////		{
////			Mesh mesh = new Mesh();
////
////			List<Vector3> pos = new List<Vector3>();
////			List<Vector2> uvs = new List<Vector2>();
////			List<int> triangles = new List<int>();
////			for (int i = 0; i < 16; i++)
////			{
////				pos.Add(new Vector3(-0.5f, 0.5f, 0));
////				pos.Add(new Vector3(0.5f, 0.5f, 0));
////				pos.Add(new Vector3(0.5f, -0.5f, 0));
////				pos.Add(new Vector3(-0.5f, -0.5f, 0));
////
////				uvs.Add(new Vector2(i, 0));
////				uvs.Add(new Vector2(i, 1));
////				uvs.Add(new Vector2(i, 2));
////				uvs.Add(new Vector2(i, 3));
////
////				triangles.AddRange(new int[] { i * 4, i * 4 + 1, i * 4 + 2 });
////				triangles.AddRange(new int[] { i * 4, i * 4 + 2, i * 4 + 3 });
////			}
////
////			mesh.vertices = pos.ToArray();
////			mesh.uv = uvs.ToArray();
////			mesh.triangles = triangles.ToArray();
////
////			AssetDatabase.CreateAsset(mesh, "Assets/" + pathAndName + "_mesh.asset");
////
////			return mesh;
////		}
////
////		private float GetMaxHight(ShowTextSource source)
////		{
////			float maxHeight = 0;
////			foreach (var c in source.chars)
////			{
////				if (c.sprite != null && c.sprite.rect.height > maxHeight)
////				{
////					maxHeight = c.sprite.rect.height;
////				}
////			}
////			return maxHeight / 100;
////		}
////
////		private Material BuildAniTex(ShowTextCreator ns, Texture2D texTex, string pathAndName, GameObject[] ngos)
////		{
////			int frameRate = 60;
////			AnimationMode.StartAnimationMode();
////
////			int totalCount = 0;
////			for (int i = 0; i < ns.aniObjects.Length; i++)
////			{
////				var com = ns.aniObjects[i].GetComponent<Animation>();
////				totalCount += Mathf.RoundToInt(frameRate * com.clip.length);
////			}
////
////			int width = 1024;
////			int height = Mathf.NextPowerOfTwo((int)totalCount / 16);
////			//1024宽 放16帧动画
////			Texture2D tex = new Texture2D(width, height, TextureFormat.RGBAHalf, false, false);
////			tex.filterMode = FilterMode.Point;
////			tex.wrapMode = TextureWrapMode.Clamp;
////			tex.anisoLevel = 0;
////
////			int curIndex = 0;
////			int curObjIndex = 0;
////			foreach (var aniObject in ns.aniObjects)
////			{
////				float maxHeight = GetMaxHight(aniObject.GetComponent<ShowText>().showTextSource);
////				Vector3 p0 = new Vector3(-0.5f, 0.5f, 0) * maxHeight;
////				Vector3 p1 = new Vector3(0.5f, 0.5f, 0) * maxHeight;
////				Vector3 p2 = new Vector3(0.5f, -0.5f, 0) * maxHeight;
////				Vector3 p3 = new Vector3(-0.5f, -0.5f, 0) * maxHeight;
////
////				var aniCom = aniObject.GetComponent<Animation>();
////				aniObject.transform.position = Vector3.zero;
////				var ani = aniCom.clip;
////				int[] childBeginTime = new int[aniObject.transform.childCount];
////				for (int i = 0; i < childBeginTime.Length; i++)
////				{
////					childBeginTime[i] = -1;
////				}
////				int fCount = Mathf.RoundToInt(frameRate * ani.length);
////
////				var showTexCom = ngos[curObjIndex].GetComponent<ShowTextInstance>();
////				curObjIndex++;
////				showTexCom.aniBeginAndEnd = new Vector2(curIndex, curIndex + fCount - 1);
////
////				for (int i = 0; i < fCount; i++)
////				{
////					AnimationMode.SampleAnimationClip(aniObject, ani, i * 1f / frameRate);
////					for (int j = 0; j < 16; j++)
////					{
////						if (j < aniObject.transform.childCount)
////						{
////							var t1 = aniObject.transform.GetChild(j);
////							if (!t1.gameObject.activeInHierarchy)
////							{
////								continue;
////							}
////							if (childBeginTime[j] == -1)
////							{
////								childBeginTime[j] = i;
////							}
////							Animator animator = t1.GetComponent<Animator>();
////							if (animator != null)
////							{
////								var cc = animator.runtimeAnimatorController.animationClips[0];
////								AnimationMode.SampleAnimationClip(t1.gameObject, cc, (i - childBeginTime[j]) / ani.frameRate);
////							}
////							else
////							{
////								Animation animation = t1.GetComponent<Animation>();
////								if (animation != null)
////								{
////									AnimationMode.SampleAnimationClip(t1.gameObject, animation.clip, (i - childBeginTime[j]) / ani.frameRate);
////								}
////							}
////
////							var t1Sprite = t1.GetChild(0);
////							var oldp = t1Sprite.localPosition;
////							t1Sprite.localPosition = Vector3.zero;
////							var np0 = t1Sprite.TransformPoint(p0);
////							var np1 = t1Sprite.TransformPoint(p1);
////							var np2 = t1Sprite.TransformPoint(p2);
////							var np3 = t1Sprite.TransformPoint(p3);
////							var render = t1Sprite.GetComponent<SpriteRenderer>();
////							t1Sprite.localPosition = oldp;
////							var p = t1Sprite.position;
////
////							int row = (curIndex + i) / 16;
////							int col = (curIndex + i) % 16;
////							tex.SetPixel(col * 64 + j * 4, row, new Color(np0.x, np0.y, np0.z, render.color.a));
////							tex.SetPixel(col * 64 + j * 4 + 1, row, new Color(np1.x, np1.y, np1.z, render.color.a));
////							tex.SetPixel(col * 64 + j * 4 + 2, row, new Color(np2.x, np2.y, np2.z, render.color.a));
////							tex.SetPixel(col * 64 + j * 4 + 3, row, new Color(np3.x, np3.y, np3.z, render.color.a));
////						}
////					}
////				}
////				curIndex += fCount;
////			}
////
////			AnimationMode.StopAnimationMode();
////			AssetDatabase.CreateAsset(tex, "Assets/" + pathAndName + "_texture_ani.asset");
////
////			Material material = new Material(Shader.Find("Burner/UI/ShowTextInstance"));
////			material.SetTexture("_MainTex", ns.texture);
////			material.SetTexture("_TextTex", texTex);
////			material.SetTexture("_TextAniTex", tex);
////			material.SetVector("_textArg", new Vector4(frameRate, 0, 1f / texTex.width, texTex.width / 4f));
////			material.SetVector("_textAniArg", new Vector4(1f / tex.width, 1f / tex.height, 0, 0));
////			material.enableInstancing = true;
////			AssetDatabase.CreateAsset(material, "Assets/" + pathAndName + "_mat.mat");
////
////			foreach (var go in ngos)
////			{
////				var mr = go.GetComponent<MeshRenderer>();
////				mr.sharedMaterial = material;
////			}
////
////			return material;
////		}
////	}
////}
