//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;
//
//namespace Burner.UIExtension
//{
//	public class Plaque : Text
//	{
//		public PackedTextureSourceData sourceData;
//
//        private PackedTextureSourceData.Sprite sprite;
//        
//        [SerializeField][HideInInspector]
//        private string spriteName;
//
//		public PackedTextureSourceData.Sprite Sprite
//        {
//            get
//            {
//                if (sprite == null)
//                {
//                    sprite = sourceData?.GetSprite(spriteName);
//                }
//
//                return sprite;
//            }
//        }
//
//        public string SpriteName
//        {
//            get
//            {
//                return spriteName;
//            }
//            set
//            {
//                spriteName = value;
//            }
//        }
//
//        public Vector3 m_SpriteScale = Vector3.one;
//
//        public struct InstanceInfo
//        {
//            public Matrix4x4 matrix;
//            public int flag;
//            public Vector4 positionXY;
//            public float positionZ;
//            public Vector4 topUV;
//            public Vector4 bottomUV;
//            public Color color;
//        }
//
//        public string FontName => font.name;
//
//        private bool m_IsDirty = true;
//
//        private List<InstanceInfo> m_InstanceInfoList = new List<InstanceInfo>();
//
//		protected override void Awake()
//		{
//			GetComponent<CanvasRenderer>().SetAlpha(0);
//		}
//
//        protected override void OnEnable()
//        {
//            PlaqueUpdateRegistry.Register(this);
//        }
//
//        protected override void OnDisable()
//        {
//            PlaqueUpdateRegistry.Unregister(this);
//        }
//
//		protected override void OnPopulateMesh(VertexHelper vh)
//		{
//			vh.Clear();
//            m_IsDirty = true;
//		}
//
//        public float GetPixelsPerUnit()
//        {
//            return 100f / fontSize;
//        }
//
//        public List<InstanceInfo> GetInstanceInfoList()
//        {
//			if (transform.hasChanged)
//            {
//                m_IsDirty = true;
//                transform.hasChanged = false;
//            }
//
//            if (m_IsDirty)
//            {
//                GenInstanceInfo();
//                m_IsDirty = false;
//            }
//
//            return m_InstanceInfoList;
//        }
//
//		private void GenInstanceInfo()
//		{
//            m_InstanceInfoList.Clear();
//
//			if (Sprite != null)
//			{
//                var w = Sprite.width * 1/100f;
//                var h = Sprite.height * 1/100f;
//
//                var info = new InstanceInfo();
//                info.flag = 1; // 1: background
//                info.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.Scale(transform.localScale, m_SpriteScale));
//                info.positionXY = new Vector4(w * (-0.5f), h * (-0.5f), w, h);
//                info.positionZ = transform.position.z;
//                info.topUV = Sprite.ST;
//                info.bottomUV = Vector4.zero;
//                info.color = Color.white;
//
//                m_InstanceInfoList.Add(info);
//			}
//
//            if (string.IsNullOrEmpty(text))
//            {
//                return;
//            }
//
//            Vector2 extents = rectTransform.rect.size;
//
//            var settings = GetGenerationSettings(extents);
//            cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
//
//            // Apply the offset to the vertices
//            IList<UIVertex> verts = cachedTextGenerator.verts;
//            int vertCount = verts.Count;
//
//            if (vertCount <= 0)
//            {
//                return;
//            }
//
//            float unitsPerPixel = 1 / GetPixelsPerUnit();
//
//            Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
//            roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
//			int instanceCount = vertCount / 4;
//
//            for (int i = 0; i < instanceCount; i++)
//            {
//                var info = new InstanceInfo();
//                info.flag = 0; // 0: text
//
//				var pos = new Vector4(verts[i * 4 + 3].position.x, verts[i * 4 + 3].position.y, verts[i * 4 + 1].position.x - verts[i * 4 + 3].position.x, verts[i * 4 + 1].position.y - verts[i * 4 + 3].position.y);
//				pos *= unitsPerPixel;
//				pos.x += roundingOffset.x;
//				pos.y += roundingOffset.y;
//
//				info.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
//                info.positionXY = pos;
//            	
//                info.positionZ = verts[i * 4 + 3].position.z;
//                info.topUV = new Vector4(verts[i * 4].uv0.x, verts[i * 4].uv0.y, verts[i * 4 + 1].uv0.x, verts[i * 4 + 1].uv0.y);
//                info.bottomUV = new Vector4(verts[i * 4 + 3].uv0.x, verts[i * 4 + 3].uv0.y, verts[i * 4 + 2].uv0.x, verts[i * 4 + 2].uv0.y);
//                info.color = new Color(verts[i * 4].color.r / 255f, verts[i * 4].color.g / 255f, verts[i * 4].color.b / 255f, verts[i * 4].color.a / 255f);
//
//                m_InstanceInfoList.Add(info);
//            }
//		}
//	}
//}
