////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections.Generic;
////using com.adjust.sdk;
////using UnityEditor;
////using UnityEngine;
////
////namespace Burner.UIExtension
////{
////    public class PackedTextureEdit
////    {
////		// TODO
////		static string s_texturePrefix = "Assets/Res/PackedTexture/";
////		static string s_sourceDataPath = "Assets/Res/PackedTexture/sourceData.asset";
////		static string s_sourceMeshPath = "Assets/Res/PackedTexture/unit_square_mesh.asset";
////		static string s_sourceTriMeshPath = "Assets/Res/PackedTexture/unit_square_tri_mesh.asset";
////
////		[MenuItem("Burner/Burner UI/Legacy/PackedTexture/生成 SourceData")]
////		public static void GenPackedTextureSourceData()
////		{
////			PackedTextureSourceData sourceData = ScriptableObject.CreateInstance<PackedTextureSourceData>();
////			sourceData.listAtlas = new List<PackedTextureSourceData.Atlas>();
////
////			var guids = AssetDatabase.FindAssets(null, new[] { "Assets/PackedTexture" });
////			foreach (var guid in guids)
////			{
////				string filePath = AssetDatabase.GUIDToAssetPath(guid);
////				if (filePath.EndsWith(".txt"))
////				{
////					var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
////					var json = JSON.Parse(textAsset.text);
////
////					var frameNode = json["frames"];
////					var metaNode = json["meta"];
////					if (frameNode != null && metaNode != null)
////					{
////						PackedTextureSourceData.Atlas atlas = new PackedTextureSourceData.Atlas();
////						atlas.name = s_texturePrefix + metaNode["image"];
////						atlas.height = metaNode["size"]["h"].AsInt;
////						atlas.width = metaNode["size"]["w"].AsInt;
////						atlas.fHeight = atlas.height;
////						atlas.fWidth = atlas.width;
////						atlas.widthRecip = 1.0f / atlas.fWidth;
////						atlas.heightRecip = 1.0f / atlas.fHeight;
////
////						foreach (KeyValuePair<string, JSONNode> c in frameNode.AsObject)
////						{
////							var sizeNode = c.Value["frame"];
////							PackedTextureSourceData.AtlasItem item = new PackedTextureSourceData.AtlasItem();
////							item.x = sizeNode["x"].AsInt;
////							item.y = sizeNode["y"].AsInt;
////							item.width = sizeNode["w"].AsInt;
////							item.height = sizeNode["h"].AsInt;
////
////							item.name = c.Key;
////							atlas.listItems.Add(item);
////						}
////						sourceData.listAtlas.Add(atlas);
////					}
////					else
////					{
////						Debug.LogError($"Build PackedTextureSourceData error: Failed to parse file [{guid}]");
////					}
////				}
////			}
////
////			AssetDatabase.CreateAsset(sourceData, s_sourceDataPath);
////		}
////
////		[MenuItem("Burner/Burner UI/Legacy/PackedTexture/生成 Mesh")]
////		public static void GenPackedTextureMesh()
////		{
////            /*
////			{
////				Mesh mesh = new Mesh();
////
////				List<Vector3> pos = new List<Vector3>(4);
////				List<Vector2> uvs = new List<Vector2>(4);
////				List<int> triangles = new List<int>(6);
////
////				pos.Add(new Vector3(-0.5f, -0.5f, 0));
////				pos.Add(new Vector3(-0.5f, 0.5f, 0));
////				pos.Add(new Vector3(0.5f, -0.5f, 0));
////				pos.Add(new Vector3(0.5f, 0.5f, 0));
////
////				uvs.Add(new Vector2(0, 0));
////				uvs.Add(new Vector2(0, 1));
////				uvs.Add(new Vector2(1, 0));
////				uvs.Add(new Vector2(1, 1));
////
////				triangles.AddRange(new int[] { 0, 1, 2, 2, 1, 3 });
////
////				mesh.vertices = pos.ToArray();
////				mesh.uv = uvs.ToArray();
////				mesh.triangles = triangles.ToArray();
////
////				AssetDatabase.CreateAsset(mesh, s_sourceMeshPath);
////			}
////            */
////
////			{
////				Mesh mesh = new Mesh();
////
////				List<Vector3> pos = new List<Vector3>();
////				List<Vector3> uvs = new List<Vector3>();
////				List<int> triangles = new List<int>();
////
////				pos.Add(new Vector3(-0.5f, -0.5f, 0));
////				pos.Add(new Vector3(-0.5f, 0.5f, 0));
////				pos.Add(new Vector3(0.5f, -0.5f, 0));
////				pos.Add(new Vector3(0.5f, 0.5f, 0));
////				pos.Add(new Vector3(0.5f, -0.5f, 0));
////				pos.Add(new Vector3(0.5f, 0.5f, 0));
////				pos.Add(new Vector3(0.5f, -0.5f, 0));
////				pos.Add(new Vector3(0.5f, 0.5f, 0));
////
////				uvs.Add(new Vector3(0, 0, 0));
////				uvs.Add(new Vector3(0, 1, 0));
////				uvs.Add(new Vector3(1, 0, 1));
////				uvs.Add(new Vector3(1, 1, 1));
////				uvs.Add(new Vector3(1, 0, 2));
////				uvs.Add(new Vector3(1, 1, 2));
////				uvs.Add(new Vector3(1, 0, 3));
////				uvs.Add(new Vector3(1, 1, 3));
////
////				triangles.AddRange(new int[] { 0, 1, 2, 2, 1, 3, 2, 3, 4, 4, 3, 5, 4, 5, 6, 6, 5, 7 });
////
////				mesh.vertices = pos.ToArray();
////				mesh.SetUVs(0, uvs);
////				mesh.triangles = triangles.ToArray();
////
////				AssetDatabase.CreateAsset(mesh, s_sourceTriMeshPath);
////			}
////		}
////	}
////}
