//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using System.Collections.Generic;
//
//namespace Burner.UIExtension
//{
//	public class PlaqueUpdateRegistry
//	{
//		private static readonly int INSTANCING_BATCH_SIZE = 100; // UNITY_INSTANCED_ARRAY_SIZE
//		private static PlaqueUpdateRegistry m_Instance;
//		private HashSet<Plaque> m_PlaqueSet;
//		private Dictionary<string, HashSet<Plaque>> m_PlaqueDict = new Dictionary<string, HashSet<Plaque>>();
//
//		private Mesh m_Mesh = null;
//		private List<Matrix4x4> m_MatrixList = new List<Matrix4x4>();
//
//		private List<Vector4> m_PositionXYList = new List<Vector4>();
//
//		private List<float> m_PositionZList = new List<float>();
//		private List<Vector4> m_BottomUV = new List<Vector4>();
//
//		private List<Vector4> m_TopUV = new List<Vector4>();
//
//		private List<float> m_Flag = new List<float>();
//
//        private List<Vector4> m_ColorList = new List<Vector4>();
//
//		private MaterialPropertyBlock m_PropertyBlock;
//
//		private int id_MainTex, id_Flag, id_PositionXY, id_PositionZ, id_TopUV, id_BottomUV, id_Color;
//
//		public PlaqueUpdateRegistry()
//		{
//			id_MainTex = Shader.PropertyToID("_MainTex");
//			id_Flag = Shader.PropertyToID("_Flag");
//			id_PositionXY = Shader.PropertyToID("_PositionXY");
//			id_PositionZ = Shader.PropertyToID("_PositionZ");
//			id_TopUV = Shader.PropertyToID("_TopUV");
//			id_BottomUV = Shader.PropertyToID("_BottomUV");
//			id_Color = Shader.PropertyToID("_Colors");
//
//			Canvas.willRenderCanvases += PerformUpdate;
//			GenMesh();
//		}
//
//		~PlaqueUpdateRegistry()
//		{
//			Canvas.willRenderCanvases -= PerformUpdate;
//		}
//
//		public static void Register(Plaque plaque)
//		{
//			if (m_Instance == null)
//			{
//				m_Instance = new PlaqueUpdateRegistry();
//			}
//			m_Instance.RegisterPlaque(plaque);
//		}
//
//		public static void Unregister(Plaque plaque)
//			=> m_Instance?.UnregisterPlaque(plaque);
//
//		private void RegisterPlaque(Plaque plaque)
//		{
//			var fontName = plaque.FontName;
//			if (m_PlaqueDict.TryGetValue(fontName, out var plaqueSet))
//			{
//				plaqueSet.Add(plaque);
//			}
//			else
//			{
//				plaqueSet = new HashSet<Plaque>();
//				plaqueSet.Add(plaque);
//				m_PlaqueDict.Add(fontName, plaqueSet);
//			}
//		}
//
//		private void UnregisterPlaque(Plaque plaque)
//		{
//			var fontName = plaque.FontName;
//			if (m_PlaqueDict.TryGetValue(fontName, out var plaqueSet))
//			{
//				plaqueSet.Remove(plaque);
//				if (plaqueSet.Count == 0)
//				{
//					m_PlaqueDict.Remove(fontName);
//				}
//			}
//		}
//
//		private void PerformUpdate()
//		{
//			if (m_PlaqueDict == null || m_PlaqueDict.Count == 0)
//			{
//				return;
//			}
//
//			if (m_Mesh == null)
//			{
//				GenMesh();
//			}
//
//			var instanceCount = 0;
//			m_PropertyBlock ??= new MaterialPropertyBlock();
//			foreach (var plaqueSet in m_PlaqueDict.Values)
//			{
//				if (plaqueSet == null || plaqueSet.Count == 0)
//				{
//					continue;
//				}
//
//				instanceCount = 0;
//				m_PropertyBlock.Clear();
//				Material material = null;
//
//				foreach (var plaque in plaqueSet)
//				{
//					if (plaque == null)
//					{
//						continue;
//					}
//
//					if (material == null)
//					{
//						material = plaque.material;
//						m_PropertyBlock.SetTexture(id_MainTex, plaque.mainTexture);
//					}
//
//					var infoList = plaque.GetInstanceInfoList();
//
//					foreach (var info in infoList)
//					{
//						m_MatrixList.Add(info.matrix);
//						m_PositionXYList.Add(info.positionXY);
//						m_PositionZList.Add(info.positionZ);
//						m_BottomUV.Add(info.bottomUV);
//						m_TopUV.Add(info.topUV);
//						m_Flag.Add(info.flag);
//						m_ColorList.Add(info.color);
//
//						if (++instanceCount >= INSTANCING_BATCH_SIZE)
//						{
//							DoDraw(material, instanceCount);
//							instanceCount = 0;
//						}
//					}
//				}
//
//				if (instanceCount > 0)
//				{
//					DoDraw(material, instanceCount);
//				}
//			}
//		}
//
//		private void GenMesh()
//		{
//			m_Mesh = new Mesh();
//
//			List<Vector3> pos = new List<Vector3>(4);
//			List<Vector2> uvs = new List<Vector2>(4);
//			List<int> triangles = new List<int>(6);
//
//			pos.Add(new Vector3(0, 1, 0));
//			pos.Add(new Vector3(1, 1, 0));
//			pos.Add(new Vector3(1, 0, 0));
//			pos.Add(new Vector3(0, 0, 0));
//
//			uvs.Add(new Vector2(0, 1));
//			uvs.Add(new Vector2(1, 1));
//			uvs.Add(new Vector2(1, 0));
//			uvs.Add(new Vector2(0, 0));
//
//			triangles.AddRange(new int[] { 0, 1, 2, 2, 3, 0 });
//
//			m_Mesh.vertices = pos.ToArray();
//			m_Mesh.uv = uvs.ToArray();
//			m_Mesh.triangles = triangles.ToArray();
//		}
//
//		private void DoDraw(Material material, int count)
//		{
//            m_PropertyBlock.SetFloatArray(id_Flag, m_Flag);
//			m_PropertyBlock.SetVectorArray(id_PositionXY, m_PositionXYList);
//			m_PropertyBlock.SetFloatArray(id_PositionZ, m_PositionZList);
//			m_PropertyBlock.SetVectorArray(id_TopUV, m_TopUV);
//			m_PropertyBlock.SetVectorArray(id_BottomUV, m_BottomUV);
//            m_PropertyBlock.SetVectorArray(id_Color, m_ColorList);
//
//            Graphics.DrawMeshInstanced(m_Mesh, 0, material, m_MatrixList.ToArray(), count, m_PropertyBlock);
//
//			m_MatrixList.Clear();
//			m_Flag.Clear();
//			m_PositionXYList.Clear();
//			m_PositionZList.Clear();
//			m_BottomUV.Clear();
//			m_TopUV.Clear();
//            m_ColorList.Clear();
//		}
//	}
//}
