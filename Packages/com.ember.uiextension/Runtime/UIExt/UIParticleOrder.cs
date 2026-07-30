//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Extensions;
//
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.UI;
//#if UNITY_EDITOR
//#if UNITY_2022_1_OR_NEWER
//using UnityEditor.SceneManagement;
//#else
//using UnityEditor.Experimental.SceneManagement;
//#endif
//#endif
//
//namespace Burner.UIExtension
//{
//    [ExecuteAlways]
//    public class UIParticleOrder : MonoBehaviour, ICanvasSortingOrderHandler, IMaskable
//    {
//        public class ParticleSystemInfo
//        {
//            public ParticleSystem ps = null;
//            public Renderer renderer = null;
//            public Material originalMaterial = null;
//            public Material stencilMaterial = null;
//            public bool isOriginalShareMaterial;
//        }
//
//        public
//        int orderOffset = 1;
//
//        [SerializeField]
//        private bool maskable = false;
//
//        [SerializeField]
//        private bool important = false;
//        
//        bool dirty = false;
//
//        ParticleSystemInfo[] m_PSInfoList;
//        private int baseSortingOrder = 0;
//        private int curSortingOrder = 0;
//
//        //全局隐藏所有粒子的显示
//        private static bool m_DisalbeAllParticles = false;
//        public static bool DisableAllParticles
//        {
//            get { return m_DisalbeAllParticles;}
//            set
//            {
//                if (m_DisalbeAllParticles != value)
//                {
//                    m_DisalbeAllParticles = value;
//                    var allParticleSystemRenderers = FindObjectsOfType<ParticleSystemRenderer>();
//                    foreach (var renderer in allParticleSystemRenderers)
//                    {
//                        var order = renderer.gameObject.GetComponent<UIParticleOrder>();
//                        if (order != null && !order.important)
//                        {
//                            renderer.enabled = value;
//                        }
//                    }
//                }
//            } 
//        }
//        public void Refresh()
//        {
//            DoInit();  
//            DoProcess();
//        }
//
//        void DoInit()
//        {
//            DisposePSInfoList();
//            ParticleSystem[] particle_list = gameObject.GetComponentsInChildren<ParticleSystem>(true) as ParticleSystem[];
//            m_PSInfoList = new ParticleSystemInfo[particle_list.Length];
//            for (int i = 0; i < particle_list.Length; ++i)
//            {
//                var pi = new ParticleSystemInfo();
//                pi.ps = particle_list[i];
//                pi.renderer = pi.ps.GetComponent<ParticleSystemRenderer>();
//                
//                // pi.renderer.material will return a Instantiated one which should be Destroy at OnDestroy
//                pi.isOriginalShareMaterial = !Application.isPlaying;
//                
//#if UNITY_EDITOR
//                if(!pi.isOriginalShareMaterial)
//                {
//                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
//                    if(prefabStage != null)
//                    {
//                        // follow code will throw exception: Requesting 'prefabContentsRoot' from Awake and OnEnable is not supported
//                        //pi.isOriginalShareMaterial = prefabStage.IsPartOfPrefabContents(gameObject);
//                        
//                        // if in Prefab Edit Mode, just use share material directly, to avoid material lost when Application.isPlaying
//                        // it's going to set back when OnDisable is called
//                        pi.isOriginalShareMaterial = true;
//                    }    
//                }
//#endif 
//                
//                pi.originalMaterial = pi.isOriginalShareMaterial ? pi.renderer.sharedMaterial : pi.renderer.material;
//                
//                m_PSInfoList[i] = pi;
//            }
//        }
//
//        void DoProcess()
//        {
//            if (m_PSInfoList != null)
//            {
//                foreach (var pi in m_PSInfoList)
//                {
//                    if (maskable)
//                    {
//                        ModifyMaterialForStencil(pi);
//                    }
//                    else
//                    {
//                        pi.renderer.material = pi.originalMaterial;
//                    }
//                }
//
//                UpdateSortingOrder();
//            }
//        }
//
//        void Awake()
//        {
//            dirty = true;
//            DoInit();
//        }
//        void OnEnable()
//        {
//            dirty = true;
//            if(DisableAllParticles && !important) GetComponent<ParticleSystemRenderer>().enabled = false;
//        }
//
//        void OnDisable()
//        {
//            if (gameObject == null)
//                return;
//
//            dirty = true;
//            if (m_PSInfoList != null)
//            {
//                for (int i = 0; i < m_PSInfoList.Length; ++i)
//                {
//                    var pi = m_PSInfoList[i];
//
//                    StencilMaterial.Remove(pi.stencilMaterial);
//                    pi.renderer.material = pi.originalMaterial;
//                }
//            }
//        }
//
//        private void OnDestroy() => DisposePSInfoList();
//
//        private void DisposePSInfoList()
//        {
//            if (!m_PSInfoList.IsNullOrEmpty())
//            {
//                for (int i = 0; i < m_PSInfoList.Length; ++i)
//                {
//                    var pi = m_PSInfoList[i];
//                    if(!pi.isOriginalShareMaterial && pi.originalMaterial.IsNotNull())
//                    {
//                        Destroy(pi.originalMaterial);
//                    }
//                    pi.originalMaterial = null;
//                }
//                
//                m_PSInfoList = null;
//            }
//        }
//
//        void Update()
//        {
//            if (dirty)
//            {
//                DoProcess();
//
//                dirty = false;
//            }
//
//            if (curSortingOrder != baseSortingOrder + orderOffset)
//            {
//                UpdateSortingOrderImpl();
//            }
//        }
//
//        public void UpdateSortingOrder()
//        {
//            if (m_PSInfoList == null)
//                return;
//
//            var parentCanvas = transform.parent.gameObject.GetComponentInParent<Canvas>(true);
//            if (parentCanvas == null)
//                return;
//            
//            baseSortingOrder = parentCanvas.sortingOrder;
//            UpdateSortingOrderImpl();
//        }
//
//        private void UpdateSortingOrderImpl()
//        {
//            curSortingOrder = baseSortingOrder + orderOffset;
//
//            foreach (var pi in m_PSInfoList)
//            {
//                pi.ps.GetComponent<ParticleSystemRenderer>().sortingOrder = curSortingOrder;
//            }
//        }
//
//        void ModifyMaterialForStencil(ParticleSystemInfo info)
//        {
//            if (info.ps == null || info.ps.emission.enabled == false)
//                return;
//
//            var toUse = info.renderer.sharedMaterial;
//
//            Transform root_canvas = null;
//            if (!Mask3D.IsGlobeMasking(transform))
//            {
//                root_canvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
//            }
//
//            int stencil_value = MaskUtilities.GetStencilDepth(transform, root_canvas);
//
//            if (stencil_value > 0)
//            {
//                var mask_mat = StencilMaterial.Add(info.originalMaterial, (1 << stencil_value) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << stencil_value) - 1, 0);
//                if (mask_mat != info.stencilMaterial)
//                {
//                    StencilMaterial.Remove(info.stencilMaterial);
//                    info.stencilMaterial = mask_mat;
//                    toUse = info.stencilMaterial;
//                }
//            }
//
//            info.renderer.material = toUse;
//        }
//
//        public void RecalculateMasking()
//        {
//            dirty = true;
//        }
//
//    }
//}
