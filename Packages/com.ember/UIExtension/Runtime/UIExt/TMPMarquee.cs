//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    [RequireComponent(typeof(TMPro.TextMeshProUGUI))]
//    public class TMPMarquee : MonoBehaviour, IMaterialModifier
//    {
//        Material baseMat, originalMat;
//        Material mat;
//        TMPro.TextMeshProUGUI tmp;
//        RectTransform trans;
//
//        float startTime;
//        float speed;
//        int startTimeId, rectangleId, speedId;
//        static Shader marqueeShader;
//
//        Canvas rootCanvas;
//        Vector3[] m_WorldCorners = new Vector3[4];
//        TMP_SubMeshUI[] subMeshes;
//        Vector4 lastRect = Vector4.zero;
//        bool disabled = false;
//        public Canvas RootCanvas
//        {
//            get
//            {
//                if (!rootCanvas)
//                {
//                    rootCanvas = TMP.canvas;
//                    Transform curTrans;
//                    if (rootCanvas)
//                        curTrans = rootCanvas.transform;
//                    else
//                        curTrans = transform;
//
//                    while (curTrans != null)
//                    {
//                        var c = curTrans.GetComponent<Canvas>();
//                        if (c)
//                            rootCanvas = c;
//                        curTrans = curTrans.parent;
//                    }
//                }
//
//                return rootCanvas;
//            }
//        }
//
//        RectTransform RectTransform
//        {
//            get
//            {
//                if (!trans)
//                    trans = transform as RectTransform;
//                return trans;
//            }
//        }
//
//        TMPro.TextMeshProUGUI TMP
//        {
//            get
//            {
//                if (!tmp)
//                {
//                    tmp = GetComponent<TMPro.TextMeshProUGUI>();
//                }
//                return tmp;
//            }
//        }
//
//        TMP_SubMeshUI[] SubmeshesArray
//        {
//            get
//            {
//                if (tmp && subMeshes == null)
//                {
//                    FieldInfo fi = typeof(TextMeshProUGUI).GetField("m_subTextObjects", BindingFlags.Instance | BindingFlags.NonPublic);
//                    subMeshes = fi != null ? fi.GetValue(tmp) as TMP_SubMeshUI[] : null;
//                }
//                return subMeshes;
//            }
//        }
//
//        void Awake()
//        {
//            startTimeId = Shader.PropertyToID("_StartTime");
//            rectangleId = Shader.PropertyToID("_Rectangle");
//            speedId = Shader.PropertyToID("_Speed");
//            tmp = GetComponent<TMPro.TextMeshProUGUI>();
//        }
//
//        void Start()
//        {
//            CreateBaseMaterial();
//        }
//
//        void ResetShaderParameters(Material mat, Vector4 canvasRect, bool resetTime)
//        {
//            if (mat)
//            {
//                mat.SetVector(rectangleId, canvasRect);
//                if (resetTime)
//                    startTime = Time.timeSinceLevelLoad;
//                mat.SetFloat(startTimeId, startTime);
//                mat.SetFloat(speedId, speed);
//            }
//        }
//
//        void OnDestroy()
//        {
//            if (baseMat)
//                Destroy(baseMat);
//            baseMat = null;
//            mat = null;
//            if (originalMat)
//            {
//                TMP.fontSharedMaterial = originalMat;
//                originalMat = null;
//            }
//        }
//
//        private Vector2 WorldToCanvasPos(Canvas canvas, Vector3 world)
//        {
//            Vector2 position = Vector2.zero;
//            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, world, BurnerUIManager.Instance.UICamera, out position);
//            return position;
//        }
//        public Vector4 GetCanvasRect(RectTransform t)
//        {
//            var c = RootCanvas;
//            if (c == null)
//                return new Vector4();
//
//            t.GetWorldCorners(m_WorldCorners);
//            var canvasTransform = c.GetComponent<Transform>();
//            for (int i = 0; i < 4; ++i)
//                m_WorldCorners[i] = canvasTransform.InverseTransformPoint(m_WorldCorners[i]);
//
//            return new Vector4(m_WorldCorners[0].x, m_WorldCorners[0].y, m_WorldCorners[2].x, m_WorldCorners[2].y);
//        }
//
//        void CreateBaseMaterial()
//        {
//            if (TMP && TMP.fontSharedMaterial)
//            {
//                if (TMP.fontSharedMaterial != baseMat)
//                {
//                    if (baseMat)
//                    {
//#if UNITY_EDITOR
//                        DestroyImmediate(baseMat);
//#else
//                        Destroy(baseMat);
//#endif
//                    }
//                    originalMat = baseMat = TMP.fontSharedMaterial;
//                    baseMat = Instantiate(baseMat);
//
//                    if (marqueeShader == null)
//                    {
//                        marqueeShader = Shader.Find("TextMeshPro/Distance Field(Marquee)");
//                    }
//
//                    baseMat.shader = marqueeShader;
//                    var canvasRect = GetCanvasRect(RectTransform);
//                    ResetShaderParameters(baseMat, canvasRect, true);
//                    TMP.fontSharedMaterial = baseMat;
//                }
//            }
//        }
//        private void Update()
//        {
//            if (disabled)
//                return;
//            CreateBaseMaterial();
//            var canvasRect = GetCanvasRect(RectTransform);
//            //优化tmp跑马灯，根据mesh实际宽度来运行
//            if(TMP && TMP.mesh && TMP.mesh.subMeshCount == 1 && TMP.mesh.bounds.size.x != 0)
//            {
//
//                var MinX = TMP.mesh.bounds.min.x;
//                var MaxX = TMP.mesh.bounds.max.x;
//                if (SubmeshesArray != null)
//                {
//                    int cnt = subMeshes.Length;
//                    for (int i = 0; i < cnt; i++)
//                    {
//                        var submesh = subMeshes[i];
//                        if (submesh)
//                        {
//                            MinX = Math.Min(MinX, submesh.mesh.bounds.min.x);
//                            MaxX = Math.Min(MaxX, submesh.mesh.bounds.max.x);
//                        }
//                    }
//                }
//                var halfWidth = (MaxX - MinX)/ 2;
//                var centerX = (canvasRect.x + canvasRect.z) / 2;
//                canvasRect.x = centerX - halfWidth - 10;
//                canvasRect.z = centerX + halfWidth + 10;
//
//            }
//            if (mat)
//            {
//                ResetShaderParameters(mat, canvasRect, false);
//            }
//            if (SubmeshesArray != null)
//            {
//                int cnt = subMeshes.Length;
//                for (int i = 0; i < cnt; i++)
//                {
//                    var submesh = subMeshes[i];
//                    if (submesh)
//                    {
//                        var sMat = submesh.materialForRendering;
//                        if (sMat)
//                        {
//                            ResetShaderParameters(sMat, canvasRect, false);
//                        }
//                    }
//                }
//            }
//        }
//
//        public void SetEnable(bool enable, float speed = 10)
//        {
//            if (enable)
//            {
//                this.speed = speed;
//                if (disabled)
//                {
//                    disabled = false;
//                    CreateBaseMaterial();
//                }
//            }
//            else
//            {
//                if (!disabled)
//                {
//                    disabled = true;
//                    OnDestroy();
//                }
//            }
//        }
//
//        public Material GetModifiedMaterial(Material baseMaterial)
//        {
//            if (disabled)
//                return baseMaterial;
//            mat = baseMaterial;
//            var canvasRect = GetCanvasRect(RectTransform);
//
//            ResetShaderParameters(mat, canvasRect, true);
//            return mat;
//        }
//    }
//}
