//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//    [ExecuteAlways]
//    [RequireComponent(typeof(MeshRenderer))]
//    public class PackedTextureScript : MonoBehaviour
//    {
//        private static readonly int unitLength = 128;
//        private static readonly float unitRecip = 1.0f / unitLength;
//
//        private static readonly Vector4 trisectionDefault = new Vector4(0, 1, 1, 1);
//        private static int kSt = Shader.PropertyToID("_uv_st");
//        private static int kTrisectionOffset = Shader.PropertyToID("_trisection_offset");
//        private static int kTrisectionUV = Shader.PropertyToID("_trisection_uv");
//
//        private static int kTrisectionEnabled = Shader.PropertyToID("_trisection_enabled");
//
//        public PackedTextureSourceData sourceData;
//
//        private PackedTextureSourceData.Sprite sprite;
//        [SerializeField][HideInInspector]
//        private string spriteName;
//
//        [SerializeField][HideInInspector]
//        private Vector2 size;
//
//        [SerializeField][HideInInspector]
//        private Vector2 anchor = new Vector2(0.5f, 0.5f);
//
//        [SerializeField][HideInInspector][Range(0,1.0f)]
//        private float fillAmount = 1.0f;
//
//        [SerializeField][HideInInspector]
//        private bool fillVertical = false;
//
//        [SerializeField] [HideInInspector] private bool trisectionEnabled = false;
//
//        [SerializeField] [HideInInspector][Range(0,1.0f)]
//        private Vector2 trisectionValue = Vector2.one;
//
//        public bool Dirty { get; set; } = false;
//
//        public PackedTextureSourceData.Sprite Sprite
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
//        public string SpriteName
//        {
//            get
//            {
//                return spriteName;
//            }
//            set
//            {
//                spriteName = value;
//                Refresh(true);
//            }
//        }
//
//        public Vector2 Size
//        {
//            get
//            {
//                return size;
//            }
//            set
//            {
//                if (value != size)
//                {
//                    size = value;
//                    RefreshTransform();
//                }
//            }
//        }
//
//        public Vector2 Anchor
//        {
//            get
//            {
//                return anchor;
//            }
//            set
//            {
//                if (value != anchor)
//                {
//                    anchor = value;
//                    RefreshTransform();
//                }
//            }
//        }
//
//        public float FillAmount
//        {
//            get
//            {
//                return fillAmount;
//            }
//            set
//            {
//                if (float.IsNaN(value))
//                {
//                    value = 0;
//                }
//                value = Mathf.Clamp(value, 0, 1);
//                if (value != fillAmount)
//                {
//                    fillAmount = value;
//                    RefreshRenderer();
//                    RefreshTransform();
//                }
//            }
//        }
//
//        public bool FillVertical
//        {
//            get { return fillVertical; }
//            set
//            {
//                if (value != fillVertical)
//                {
//                    fillVertical = value;
//                    RefreshRenderer();
//                    RefreshTransform();
//                }
//            }
//        }
//
//        public bool TrisectionEnabled
//        {
//            get
//            {
//                return trisectionEnabled;
//            }
//            set
//            {
//                if (value != trisectionEnabled)
//                {
//                    trisectionEnabled = value;
//                    RefreshRenderer();
//                }
//            }
//        }
//
//        public Vector2 TrisectionValue
//        {
//            get
//            {
//                return trisectionValue;
//            }
//            set
//            {
//                value.x = Mathf.Clamp(value.x, 0, 1);
//                value.y = Mathf.Clamp(value.y, value.x, 1);
//                if (value != trisectionValue)
//                {
//                    trisectionValue = value;
//                    if (trisectionEnabled)
//                    {
//                        RefreshRenderer();
//                    }
//                }
//            }
//        }
//
//        private MaterialPropertyBlock block;
//
//        private void Start()
//        {
//            Refresh(true);
//        }
//
//        private void Refresh(bool force = false)
//        {
//            if (force)
//            {
//                sprite = null;
//            }
//            var sp = Sprite;
//            if (sp != null)
//            {
//                size = new Vector2(sp.width, sp.height);
//                RefreshRenderer();
//                RefreshTransform();
//            }
//        }
//
//        public void EditorRefresh()
//        {
//            Refresh();
//        }
//
//        public void RefreshRenderer()
//        {
//            PackedTextureSourceData.Sprite sp = Sprite;
//            if (sp != null)
//            {
//                Renderer renderer = GetComponent<MeshRenderer>();
//                if (block == null)
//                {
//                    block = new MaterialPropertyBlock();
//                }
//
//                var st = sp.ST;
//                renderer.GetPropertyBlock(block);
//                block.SetFloat(kTrisectionEnabled, trisectionEnabled ? 1f : 0f);
//                if (trisectionEnabled && fillAmount > 0 )
//                {
//                    float oneMinusFill = 1 - fillAmount;
//                    float trisectionMiddle = trisectionValue.y - trisectionValue.x;
//                    float trisectionLeft = trisectionValue.x;
//                    float trisectionRight = 1 - trisectionValue.y;
//                    if (trisectionMiddle < oneMinusFill)
//                    {
//                        float overflow = (oneMinusFill - trisectionMiddle) / (trisectionLeft + trisectionRight);
//                        trisectionLeft -= overflow * trisectionLeft;
//                        trisectionRight -= overflow * trisectionRight;
//                        trisectionMiddle = 1 - trisectionLeft - trisectionRight;
//                    }
//                    Vector4 trisectVertexOffset = new Vector4(0, Mathf.Clamp(trisectionLeft / fillAmount, 0, 1), Mathf.Clamp(1 - trisectionRight/ fillAmount, 0, 1),  1);
//                    Vector4 trisectUV = new Vector4(0, trisectionLeft, 1 - trisectionRight, 1);
//
//                    block.SetVector(kTrisectionOffset, trisectVertexOffset);
//                    block.SetVector(kTrisectionUV, trisectUV);
//                }
//                else
//                {
//                    if (fillVertical)
//                    {
//                        float mul = st.y * fillAmount;
//                        float add = st.w + st.y - st.y * fillAmount;
//                        st.y = mul;
//                        st.w = add;
//                    }
//                    else
//                    {
//                        st.x *= fillAmount;
//                    }
//                }
//                block.SetVector(kSt, st);
//                renderer.SetPropertyBlock(block);
//                Dirty = true;
//            }
//        }
//
//        private void RefreshTransform()
//        {
//            float sizeX = size.x * unitRecip;
//            float sizeY = size.y * unitRecip;
//            float fillAmountX, fillAmountY;
//            if (fillVertical)
//            {
//                fillAmountX = 1;
//                fillAmountY = fillAmount;
//            }
//            else
//            {
//                fillAmountX = fillAmount;
//                fillAmountY = 1;
//            }
//            transform.localPosition = new Vector3(-(anchor.x - 0.5f) * sizeX * fillAmountX, -(anchor.y - 0.5f) * sizeY * fillAmountY, transform.localPosition.z);
//            transform.localScale = new Vector3(sizeX * fillAmountX, sizeY * fillAmountY, 1.0f);
//            Dirty = true;
//        }
//    }
//}
