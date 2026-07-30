//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System;
//
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//using UnityEngine.LowLevel;
//using UnityEngine.PlayerLoop;
//using UnityEngine.Profiling;
//
//using Burner.Basic;
//
//namespace Burner.UIExtension
//{
//    public struct UIContainerData
//    {
//        public int TemplateIndex;
//        public object Data;
//        public Vector2 Position;
//    }
//
//    [DisallowMultipleComponent]
//    public class UIContainer : UIBehaviour
//    {
//        [Serializable]
//        struct ChildInfo
//        {
//            public string Name;
//            public Vector2 Position;
//            public Vector2 Size;
//        }
//
//        struct ChildEntry
//        {
//            public int TemplateIndex;
//            public int ObjectIndex;
//            public Vector2 Size;
//            public int SizeDiffCounter;
//            public Vector2 Position;
//            public object Data;
//        }
//        internal struct Child
//        {
//            public int TemplateIndex;
//            public GameObject GameObject;
//        }
//        enum LayoutTypes
//        {
//            Horizontal,
//            Vertical,
//            Grid
//        }
//        [SerializeField]
//        private GameObject templateNode;
//        [SerializeField]
//        private GameUIBinding.WidgetTypes templateType;
//        [SerializeField]
//        private bool multiTemplateEnabled;
//        [SerializeField]
//        private GameObject[] templateNodes;
//        [SerializeField]
//        private string templateClassName;
//        [SerializeField]
//        private bool isPredefined;
//        [SerializeField]
//        private bool disableParentChange;
//        [SerializeField]
//        private GameObject[] predefinedChildren;
//        [SerializeField]
//        private ChildInfo[] predefinedChildrenInfo;
//        [SerializeField]
//        private bool isPredefinedRecycle;
//        [SerializeField]
//        private bool disableLayoutGroup;
//        [SerializeField]
//        private LayoutGroup[] affectingLayoutGroups;
//
//        [SerializeField] 
//        private bool ignoreRecycleCheck = false;
//
//        int firstSibling;
//        bool recycleModeEnabled;
//        bool templateSizeGot;
//        bool positionDirty = false;
//        bool recycleDirty = false;
//        bool noCheckSize = false;
//        List<int>[] streamingOut;
//        Queue<int> streamingIn;
//        Vector2[] templateSize;
//        RectOffset layoutPadding;
//        Vector2 layoutSpacing;
//        int visibleElementCount;
//        int visibleElementColumnCount;
//        ScrollRect parentScrollRect;
//        RectTransform scrollRectViewport;
//        LayoutTypes layoutType;
//        bool sharedPool = false;
//        bool needInvokePoolEvent = false;
//        int currentInstantiationPerFrame;
//        Vector2 templatePivot;
//        Vector2 childVisibleRectShiftBuffSize;
//        Dictionary<string, int> nameIndexMapping;
//        static HashSet<LayoutGroup> updatedLayoutGroup;
//        LayoutElement containerLayoutElement;
//        bool needCalcLayout = false;
//#if DEBUG
//        CustomSampler instiantiateSampler;
//#endif
//        public ScrollRect ScrollRect => parentScrollRect;
//        public bool SupressRecyleModeCheck { get; set; }
//        public bool DiscardRecycleModeCheck { get; set; } = false;
//
//        public static int RecycleModeCheckThreshold { get; set; } = 20;
//        public GameObject TemplateNode
//        {
//            get => templateNode;
//            set
//            {
//                templateNode = value;
//            }
//        }
//
//
//        public GameObject[] TemplateNodes
//        {
//            get => templateNodes;
//            set
//            {
//                templateNodes = value;
//            }
//        }
//
//        public bool MultiTemplateEnabled
//        {
//            get => multiTemplateEnabled;
//            set
//            {
//                multiTemplateEnabled = value;
//            }
//        }
//        public static int InstantiationPerFrame { get; set; } = 2;
//
//        public int CurrentInstantiationPerFrame
//        {
//            get => currentInstantiationPerFrame;
//            set
//            {
//                currentInstantiationPerFrame = Math.Max(value, 1);
//            }
//        }
//        public GameUIBinding.WidgetTypes TemplateType => templateType;
//
//        public string TemplateClassName => templateClassName;
//        /// <summary>
//        /// 当创建子节点时调用，第二个参数为true时为真实创建，false为从对象池返回对象
//        /// </summary>
//        public Action<GameObject, bool> OnCreateChild { get; set; }
//
//        /// <summary>
//        /// 当共享缓存池时，入池出池回调
//        /// </summary>
//        public Action<GameObject, bool> OnPoolSharedChild { get; set; }
//
//        /// <summary>
//        /// 当销毁子节点时调用。第二个参数为true时为真实销毁，false为回对象池
//        /// </summary>
//        public Action<GameObject, bool> OnDisposeChild { get; set; }
//
//        public Action<GameObject, int, object> OnRefreshChild { get; set; }
//        
//        public Action<GameObject> OnShowChild { get; set; }
//        
//        public Action<GameObject> OnHideChild { get; set; }
//
//        public Action<GameObject, int> OnFocusChild { get; set; }
//
//        public Action<GameObject> OnStreamingOut { get; set; }
//
//        public int VisibleElementCount
//        {
//            get
//            {
//                RetriveTemplateSize();
//                return visibleElementCount;
//            }
//        }
//        /// <summary>
//        /// 是否为预定义子节点
//        /// </summary>
//        public bool IsPredefined => isPredefined;
//
//        /// <summary>
//        /// 获取预定义的子节点
//        /// </summary>
//        public GameObject[] PredefinedChildren => predefinedChildren;
//
//        MemoryPool<GameObject>[] childPool;
//        LayoutGroup layoutGroup;
//        Transform[] poolTransform;
//        RectTransform cachedTransform;
//
//        ValueTypeList<ChildEntry> recycleChildren = new ValueTypeList<ChildEntry>();
//        internal List<Child> childList = new List<Child>();
//        protected bool childListDirty = false;
//        bool initialized = false;
//
//        public int ChildCount
//        {
//            get
//            {
//                return (recycleModeEnabled || isPredefinedRecycle) ? recycleChildren.Count : childList.Count;
//            }
//        }
//
//        internal int RealChildCount => childList.Count;
//        protected override void Awake()
//        {
//            Initialize();
//        }
//
//        public void Initialize()
//        {
//            if (!initialized)
//            {
//                CurrentInstantiationPerFrame = InstantiationPerFrame;
//                initialized = true;
//                cachedTransform = (RectTransform)gameObject.transform;
//                if (isPredefined)
//                {
//                    nameIndexMapping = new Dictionary<string, int>();
//                    if (isPredefinedRecycle)
//                    {
//                        layoutPadding = new RectOffset();
//                        templatePivot = ((RectTransform)templateNode.transform).pivot;
//                        templatePivot.y = 1 - templatePivot.y;
//                        templatePivot.x = -templatePivot.x;
//                        streamingIn = new Queue<int>();
//                        childPool = new MemoryPool<GameObject>[1];
//                        poolTransform = new Transform[1];
//                        templateSize = new Vector2[1];
//                        streamingOut = new List<int>[1];
//                        streamingOut[0] = new List<int>();
//                        if (templateNode)
//                        {
//                            InitializeTemplate(templateNode, 0);
//                        }
//                        else
//                        {
//                            Logger.Error("{0} is missing template", this);
//                        }
//                        foreach (var i in predefinedChildrenInfo)
//                        {
//                            nameIndexMapping[i.Name] = recycleChildren.Count;
//                            var child = new ChildEntry()
//                            {
//                                TemplateIndex = 0,
//                                ObjectIndex = -1,
//                                Position = i.Position,
//                                Size = i.Size,
//                            };
//                            recycleChildren.Add(ref child);
//                        }
//                        recycleDirty = true;
//                        needCalcLayout = true;
//                    }
//                    else
//                    {
//                        foreach (var i in predefinedChildren)
//                        {
//                            nameIndexMapping[i.name] = childList.Count;
//
//                            childList.Add(new Child { GameObject = i });
//                        }
//                        childListDirty = true;
//                    }
//                }
//                else
//                {
//                    if (multiTemplateEnabled)
//                    {
//                        childPool = new MemoryPool<GameObject>[templateNodes.Length];
//                        poolTransform = new Transform[templateNodes.Length];
//                        templateSize = new Vector2[templateNodes.Length];
//                        streamingOut = new List<int>[templateNodes.Length];
//
//                        for (int i = 0; i < templateNodes.Length; i++)
//                        {
//                            InitializeTemplate(templateNodes[i], i);
//                        }
//                    }
//                    else
//                    {
//                        childPool = new MemoryPool<GameObject>[1];
//                        poolTransform = new Transform[1];
//                        templateSize = new Vector2[1];
//                        streamingOut = new List<int>[1];
//                        if (templateNode)
//                        {
//                            InitializeTemplate(templateNode, 0);
//                        }
//                        else
//                        {
//                            Logger.Error("{0} is missing template", this);
//                        }
//                    }
//                }
//            }
//        }
//
//        void InitializeTemplate(GameObject templateNode, int templateIdx)
//        {
//            templateNode.SetActive(false);
//            childPool[templateIdx] = new MemoryPool<GameObject>(200);
//            GameObject temp = new GameObject();
//            temp.name = "pool_" + gameObject.name;
//            temp.SetActive(false);
//            poolTransform[templateIdx] = temp.transform;
//            poolTransform[templateIdx].SetParent(cachedTransform);
//            firstSibling = cachedTransform.childCount;
//
//            if (recycleModeEnabled)
//            {
//                if (templateNode.GetComponent<LayoutElement>() == null)
//                {
//                    templateNode.AddComponent<LayoutElement>();
//                }
//
//                if (templateNode.GetComponent<CanvasGroup>() == null)
//                {
//                    templateNode.AddComponent<CanvasGroup>();
//                }
//
//                SetGameObjecTransparentCull(templateNode);
//            }
//        }
//
//        public void ShareObjectPool(UIContainer origin)
//        {
//            if (sharedPool)
//                throw new NotSupportedException("Can not change shared pool");
//            if (origin.templateNode && templateNode && origin.templateNode.name == templateNode.name)
//            {
//                DisposeChildren(true);
//                foreach (var i in poolTransform)
//                    Destroy(i.gameObject);
//                needInvokePoolEvent = true;
//                origin.needInvokePoolEvent = true;
//                sharedPool = true;
//                childPool = origin.childPool;
//                poolTransform = origin.poolTransform;
//            }
//            else
//                throw new NotSupportedException("Only pools with same template can be shared");
//        }
//
//        public Vector2 GetPivot(int idx)
//        {
//            if (recycleModeEnabled||isPredefinedRecycle)
//            {
//                ref var child = ref recycleChildren.GetRef(idx);
//                var go = multiTemplateEnabled ? templateNodes[child.TemplateIndex] : templateNode;
//                var rt = go.transform as RectTransform;
//                if (rt)
//                    return rt.pivot;
//                else
//                    return default;
//            }
//            else
//            {
//                var child = GetChildByIndexInternal(idx);
//                if (child.GameObject)
//                {
//                    var rt = ((RectTransform)child.GameObject.transform);
//                    return rt.pivot;
//                }
//                else
//                    return default;
//            }
//        }
//
//        public Rect GetChildRect(int idx)
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                ref var child = ref recycleChildren.GetRef(idx);
//                var pos = child.Position;
//                if (!layoutGroup)
//                {
//                    pos = pos + child.Size * templatePivot;
//                }
//                return new Rect(pos + new Vector2(layoutPadding.left, -layoutPadding.top), child.Size);
//            }
//            else
//            {
//                var child = GetChildByIndexInternal(idx);
//                if (child.GameObject)
//                {
//                    var rt = ((RectTransform)child.GameObject.transform);
//                    var pos = rt.anchoredPosition;
//                    var pivot = rt.pivot;
//                    pivot.y = 1 - pivot.y;
//                    pivot.x = -pivot.x;
//                    pos = pos + rt.rect.size * pivot;
//                    return new Rect(pos, rt.rect.size);
//                }
//                else
//                    return default;
//            }
//        }
//
//        public void RemoveChild(int idx)
//        {
//            if (recycleModeEnabled)
//            {
//                if (idx >= 0 && idx < recycleChildren.Count)
//                {
//                    ref var toRemove = ref recycleChildren.GetRef(idx);
//                    if (!templateSizeGot)
//                        RetriveTemplateSize();
//                    for (int i = idx + 1; i < recycleChildren.Count; i++)
//                    {
//                        ref var nextChild = ref recycleChildren.GetRef(i);
//                        if (templateSizeGot)
//                        {
//                            if (i == idx + 1)
//                            {
//                                ref var lastChild = ref recycleChildren.GetRef(i - 1);
//                                nextChild.Position = lastChild.Position;
//                            }
//                            else
//                                nextChild.Position = GetNextItemPosition(i - 1);
//                        }
//                        else
//                            nextChild.Position = Vector2.zero;
//                    }
//                    if (toRemove.ObjectIndex >= 0)
//                    {
//                        streamingOut[toRemove.TemplateIndex].Add(toRemove.ObjectIndex);
//                    }
//                    recycleChildren.RemoveAt(idx);
//                    recycleDirty = true;
//                    needCalcLayout = true;
//                    if (!templateSizeGot)
//                        positionDirty = true;
//                }
//                else
//                    throw new ArgumentOutOfRangeException();
//            }
//            else
//            {
//                if (isPredefined)
//                    throw new NotSupportedException("Predefined UI Container cannot remove child manually");
//                DoRemoveChild(idx);
//            }
//        }
//
//        public void RemoveChild(GameObject go)
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//                throw new NotSupportedException("Cannot remove child by object in recycle mode");
//            for (int i = 0; i < childList.Count; i++)
//            {
//                if (childList[i].GameObject == go)
//                {
//                    DoRemoveChild(i);
//                    return;
//                }
//            }
//        }
//
//        void DoRemoveChild(int idx, bool destroy = false)
//        {
//            if (isPredefined && !isPredefinedRecycle)
//                throw new NotSupportedException("Predefined UI Container cannot remove child manually");
//            var childData = GetChildByIndexInternal(idx);
//            var child = childData.GameObject;
//            if (!child)
//                return;
//            childListDirty = true;
//            childList.RemoveAt(idx);
//            DoDisposeGameObject(destroy, childData);
//        }
//
//        void DoDisposeGameObject(bool destroy, Child childData)
//        {
//            var child = childData.GameObject;
//            var trans = child.transform as RectTransform;
//            if (!destroy && childPool[childData.TemplateIndex].Free(child))
//            {
//                if (needInvokePoolEvent)
//                    OnPoolSharedChild?.Invoke(child, false);
//                OnDisposeChild?.Invoke(child, false);
//
//                Vector3 sc = trans.localScale;
//                Vector3 ps = trans.localPosition;
//                if (recycleModeEnabled && disableParentChange)
//                {
//                    trans.GetComponent<LayoutElement>().ignoreLayout = true;
//                    SetGameObjectVisible(child, false);
//                }
//                else
//                {
//                    trans.SetParent(poolTransform[childData.TemplateIndex]);
//                }
//                trans.localScale = sc;
//                trans.localPosition = ps;
//                if (recycleModeEnabled)
//                    trans.anchoredPosition = new Vector2(10000, 10000);
//                OnHideChild?.Invoke(child);
//                return;
//            }
//            OnDisposeChild?.Invoke(child, true);
//            //trans.SetParent(null);
//            GameObject.Destroy(child);
//        }
//        public GameObject AddChild(object data = null, int templateIdx = 0)
//        {
//            if (recycleModeEnabled)
//            {
//                AddChildRecycledInternal(templateIdx, data);
//                return null;
//            }
//            else
//            {
//                if (isPredefined)
//                {
//                    throw new NotSupportedException("Predefined UI Container cannot add child manually");
//                }
//                var idx = ChildCount;
//                var go = DoAddChild(out _, templateIdx);
//                if (go)
//                {
//                    OnRefreshChild?.Invoke(go, idx, data);
//                }
//                return go;
//            }
//        }
//
//        void AddChildRecycledInternal(int templateIdx, object data)
//        {
//            if (recycleModeEnabled)
//            {
//                if (!templateSizeGot)
//                    RetriveTemplateSize();
//                var child = new ChildEntry()
//                {
//                    TemplateIndex = templateIdx,
//                    ObjectIndex = -1,
//                    Position = templateSizeGot ? GetNextItemPosition() : Vector2.zero,
//                    Data = data,
//                    Size = templateSize[templateIdx]
//                };
//                recycleChildren.Add(ref child);
//                if (!templateSizeGot)
//                    positionDirty = true;
//                recycleDirty = true;
//                needCalcLayout = true;
//            }
//            else
//            {
//                throw new NotSupportedException("Only recycle mode is supported");
//            }
//        }
//
//        [Obsolete]
//        public void AddChildRecycled(object data = null)
//        {
//            AddChildRecycledInternal(0, data);
//        }
//
//        public void RefreshItems()
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                var cnt = recycleChildren.Count;
//                for (int i = 0; i < cnt; i++)
//                {
//                    ref var child = ref recycleChildren.GetRef(i);
//                    if (child.ObjectIndex >= 0)
//                    {
//                        var go = childList[child.ObjectIndex].GameObject;
//                        OnRefreshChild?.Invoke(go, i, child.Data);
//                        recycleDirty = true;
//                        needCalcLayout = true;
//                    }
//                }
//            }
//            else
//            {
//                throw new NotSupportedException("Only recycle mode is supported");
//            }
//        }
//
//
//        GameObject DoAddChildGameObject(out bool isPool, int templateIdx)
//        {
//            templateIdx = multiTemplateEnabled ? templateIdx : 0;
//            GameObject go = childPool[templateIdx].Alloc();
//            isPool = false;
//            if (go != null)
//            {
//                var trans = go.transform as RectTransform;
//                Vector3 sc = trans.localScale;
//                Vector3 ps = trans.anchoredPosition3D;
//                if (recycleModeEnabled && disableParentChange)
//                {
//                    trans.GetComponent<LayoutElement>().ignoreLayout = false;
//                    SetGameObjectVisible(go, true);
//                }
//                trans.SetParent(cachedTransform);
//                trans.localScale = sc;
//                trans.anchoredPosition3D = ps;
//                trans.localRotation = Quaternion.identity;
//                isPool = true;
//            }
//            else
//            {
//                var node = multiTemplateEnabled ? templateNodes[templateIdx] : templateNode;
//#if DEBUG
//                if (instiantiateSampler == null)
//                    instiantiateSampler = CustomSampler.Create($"Instantiate Cell:{node.name}");
//                instiantiateSampler.Begin(node);
//#endif
//                go = CloneChild(cachedTransform, node);
//                go.SetActive(true);
//#if DEBUG
//                instiantiateSampler.End();
//#endif
//            }
//            return go;
//        }
//
//
//        GameObject DoAddChild(out bool isPool, int templateIdx)
//        {
//            if (isPredefined && !isPredefinedRecycle)
//                throw new NotSupportedException("Predefined UI Container cannot add child manually");
//            var go = DoAddChildGameObject(out isPool, templateIdx);
//            if (recycleModeEnabled || (isPredefined && isPredefinedRecycle))
//                streamingOut[templateIdx].Add(childList.Count);
//            childList.Add(new Child { TemplateIndex = templateIdx, GameObject = go });
//#if DEBUG
//            if (!ignoreRecycleCheck && !DiscardRecycleModeCheck && !SupressRecyleModeCheck && childList.Count > RecycleModeCheckThreshold && !isPredefined && !recycleModeEnabled)
//                Burner.Logger.Error($"对象数大于{RecycleModeCheckThreshold}应使用循环模式({cachedTransform.GetFullPathName(BurnerUIManager.Instance.RootNode.transform)})");
//#endif
//            childListDirty = true;
//            if (isPool && needInvokePoolEvent)
//                OnPoolSharedChild?.Invoke(go, true);
//            OnCreateChild?.Invoke(go, !isPool);
//            return go;
//        }
//
//        GameObject CloneChild(Transform parent, GameObject template)
//        {
//            GameObject go = GameObject.Instantiate(template);
//            Vector3 oldScale = go.transform.localScale;
//            go.transform.SetParent(parent);
//            go.transform.localScale = oldScale;
//            go.transform.localPosition = Vector3.zero;
//            go.transform.localRotation = Quaternion.identity;
//            return go;
//        }
//
//        void SetGameObjecTransparentCull(GameObject obj)
//        {
//            CanvasRenderer[] rendererList = obj.GetComponentsInChildren<CanvasRenderer>(true);
//            foreach (CanvasRenderer renderer in rendererList)
//                renderer.cullTransparentMesh = true;
//        }
//
//        void SetGameObjectVisible(GameObject obj, bool visible)
//        {
//            obj.GetComponent<CanvasGroup>().alpha = visible ? 1f : 0f;
//        }
//
//        public GameObject GetChildByIndex(int idx)
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                if (recycleChildren.Count > idx && idx >= 0)
//                {
//                    ref var child = ref recycleChildren.GetRef(idx);
//                    if (child.ObjectIndex >= 0)
//                    {
//                        return GetChildByIndexInternal(child.ObjectIndex).GameObject;
//                    }
//                }
//                else
//                    return null;
//            }
//            return GetChildByIndexInternal(idx).GameObject;
//        }
//
//        internal Child GetChildByIndexInternal(int idx)
//        {
//            if (childList.Count > idx && idx >= 0)
//                return childList[idx];
//            else
//                return default;
//        }
//
//        public void SetRecycleMode(bool enabled)
//        {
//            if (recycleModeEnabled != enabled)
//            {
//                if (ChildCount > 0)
//                {
//                    Logger.Error("Cannot switch recycle mode while child count > 0");
//                    return;
//                }
//                if (enabled && isPredefined)
//                {
//                    Logger.Error("Cannot use recycle mode with predefined ui container");
//                    return;
//                }
//                recycleModeEnabled = enabled;
//                if (enabled)
//                {
//                    streamingIn = new Queue<int>();
//                    var lg = GetComponent<LayoutGroup>();
//                    if (lg)
//                    {
//                        containerLayoutElement = gameObject.AddComponent<LayoutElement>();                        
//                        layoutGroup = lg;
//                        lg.childAlignment = TextAnchor.UpperLeft;
//                        cachedTransform.pivot = new Vector2(0, 1);
//                        if (multiTemplateEnabled)
//                        {
//                            for (int i = 0; i < templateNodes.Length; i++)
//                            {
//                                var tn = (RectTransform)templateNodes[i].transform;
//                                tn.pivot = new Vector2(0, 1);
//                            }
//                        }
//                        else
//                            ((RectTransform)templateNode.transform).pivot = new Vector2(0, 1);
//                        layoutPadding = new RectOffset(lg.padding.left, lg.padding.right, lg.padding.top, lg.padding.bottom);
//                        if (lg is HorizontalLayoutGroup hlg)
//                        {
//                            layoutType = LayoutTypes.Horizontal;
//                            layoutSpacing = new Vector2(hlg.spacing, 0);
//                        }
//                        else if (lg is VerticalLayoutGroup vlg)
//                        {
//                            layoutType = LayoutTypes.Vertical;
//                            layoutSpacing = new Vector2(0, vlg.spacing);
//                        }
//                        else if (lg is GridLayoutGroup glg)
//                        {
//                            layoutType = LayoutTypes.Grid;
//                            layoutSpacing = glg.spacing;
//                            if (multiTemplateEnabled)
//                            {
//                                for (int i = 0; i < templateNodes.Length; i++)
//                                {
//                                    templateSize[i] = glg.cellSize;
//
//                                    var le = templateNodes[i].GetComponent<LayoutElement>();
//                                    if (!le)
//                                        le = templateNodes[i].AddComponent<LayoutElement>();
//                                    le.preferredWidth = templateSize[0].x;
//                                    le.preferredHeight = templateSize[0].y;
//                                }
//                            }
//                            else
//                            {
//                                templateSize[0] = glg.cellSize;
//                                var le = templateNode.GetComponent<LayoutElement>();
//                                if (!le)
//                                    le = templateNode.AddComponent<LayoutElement>();
//                                le.preferredWidth = templateSize[0].x;
//                                le.preferredHeight = templateSize[0].y;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        layoutPadding = new RectOffset();
//                    }
//                    if (multiTemplateEnabled)
//                    {
//                        streamingOut = new List<int>[templateNodes.Length];
//                        templatePivot = templateNodes.Length > 0 ? ((RectTransform)templateNodes[0].transform).pivot : new Vector2(0, 1);
//                        for (int i = 0; i < templateNodes.Length; i++)
//                        {
//                            streamingOut[i] = new List<int>();
//                            if (templateNodes[i].GetComponent<LayoutElement>() == null)
//                                templateNodes[i].AddComponent<LayoutElement>();
//
//                            if (templateNodes[i].GetComponent<CanvasGroup>() == null)
//                                templateNodes[i].AddComponent<CanvasGroup>();
//
//                            SetGameObjecTransparentCull(templateNodes[i]);
//                        }
//                    }
//                    else
//                    {
//                        templatePivot = ((RectTransform)templateNode.transform).pivot;
//                        streamingOut = new List<int>[1];
//                        streamingOut[0] = new List<int>();
//                        if (templateNode.GetComponent<LayoutElement>() == null)
//                            templateNode.AddComponent<LayoutElement>();
//
//                        if (templateNode.GetComponent<CanvasGroup>() == null)
//                            templateNode.AddComponent<CanvasGroup>();
//
//                        SetGameObjecTransparentCull(templateNode);
//                    }
//                    templatePivot.y = 1 - templatePivot.y;
//                    templatePivot.x = -templatePivot.x;
//                    RetriveParentScrollRect();
//                    RetriveTemplateSize();
//                }
//            }
//        }
//
//        public int GetChildIndexByName(string name)
//        {
//            if (nameIndexMapping.TryGetValue(name, out var idx))
//            {
//                return idx;
//            }
//            else
//                return -1;
//        }
//
//        public void SetChildDataByName(string name, object data, bool setPos = false, float x = 0, float y = 0)
//        {
//            if (!isPredefined)
//                throw new NotSupportedException("Only predefined mode can set data by name");
//            if (nameIndexMapping.TryGetValue(name, out var idx))
//            {
//                SetChildData(idx, data, setPos, x, y);
//            }
//        }
//
//        public void SetChildPosition(int idx, float x = 0, float y = 0)
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                ref var child = ref recycleChildren.GetRef(idx);
//                child.Position = new Vector2(x, y);
//                recycleDirty = true;
//                needCalcLayout = true;
//            }
//            else
//            {
//                GameObject go = childList[idx].GameObject;
//                RectTransform rt = go.transform as RectTransform;
//                rt.anchoredPosition = new Vector2(x, y);
//            }
//        }
//
//        public void SetChildData(int idx, ref UIContainerData data, bool setPos = false)
//        {
//            bool needPos = setPos && !layoutGroup;
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                UpdateChildData(idx, ref data, needPos);
//            }
//            else
//                SetChildData(idx, data.Data, needPos, data.Position.x, data.Position.y);
//        }
//
//        public void SetChildData(int idx, object data, bool setPos = false, float x = 0, float y = 0)
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                ref var child = ref recycleChildren.GetRef(idx);
//                child.Data = data;
//                if (setPos)
//                {
//                    child.Position = new Vector2(x, y);
//                    recycleDirty = true;
//                    needCalcLayout = true;
//                }
//                if (child.ObjectIndex >= 0)
//                {
//                    GameObject go = childList[child.ObjectIndex].GameObject;
//                    OnRefreshChild?.Invoke(go, idx, data);
//                    recycleDirty = true;
//                    needCalcLayout = true;
//                }
//            }
//            else
//            {
//                GameObject go = childList[idx].GameObject;
//                if (setPos)
//                {
//                    RectTransform rt = go.transform as RectTransform;
//                    rt.anchoredPosition = new Vector2(x, y);
//                }
//                OnRefreshChild?.Invoke(go, idx, data);
//            }
//        }
//
//        void RetriveParentScrollRect()
//        {
//            var cur = transform.parent;
//            while (cur && !parentScrollRect)
//            {
//                parentScrollRect = cur.gameObject.GetComponent<ScrollRect>();
//                cur = cur.parent;
//            }
//            if (parentScrollRect)
//            {
//                scrollRectViewport = parentScrollRect.viewport;
//                parentScrollRect.onValueChanged.RemoveListener(OnScroll);
//                parentScrollRect.onValueChanged.AddListener(OnScroll);
//            }
//        }
//
//        bool RetriveTemplateSizeByIndex(int templateIdx)
//        {
//            bool templateSizeGot = false;
//            var templateNode = multiTemplateEnabled ? templateNodes[templateIdx] : this.templateNode;
//            var t = templateNode.transform as RectTransform;
//            if (t && scrollRectViewport)
//            {
//                var rect = t.rect;
//                var layoutElement = templateNode.GetComponent<LayoutElement>();
//                if (layoutElement)
//                {
//                    if (layoutElement.preferredWidth > 0 || layoutElement.minWidth > 0)
//                        rect.width = Mathf.Max(layoutElement.preferredWidth, layoutElement.minWidth);
//                    if (layoutElement.preferredHeight > 0 || layoutElement.minHeight > 0)
//                        rect.height = Mathf.Max(layoutElement.preferredHeight, layoutElement.minHeight);
//                }
//                else
//                    templateNode.AddComponent<LayoutElement>();
//
//                /*if (rect.width <= 0 && mustSet)
//                    rect.width = 100;
//                if (rect.height <= 0 && mustSet)
//                    rect.height = 100;*/
//                if (rect.width > 0 || rect.height > 0)
//                {
//                    switch (layoutType)
//                    {
//                        case LayoutTypes.Horizontal:
//                            noCheckSize = false;
//                            if (rect.width > 0 && (rect.height > 0 || cachedTransform.rect.height > 0))
//                            {
//                                templateSizeGot = true;
//                                if (rect.height <= 0)
//                                {
//                                    rect.height = cachedTransform.rect.height - layoutPadding.top - layoutPadding.bottom;
//                                }
//                                templateSize[templateIdx] = rect.size;
//                                visibleElementCount = (int)((scrollRectViewport.rect.width - layoutPadding.left - layoutPadding.right) / (templateSize[templateIdx].x + layoutSpacing.x)) + 2;
//                            }
//                            break;
//                        case LayoutTypes.Vertical:
//                            noCheckSize = false;
//                            if (rect.height > 0 && (rect.width > 0 || cachedTransform.rect.width > 0))
//                            {
//                                templateSizeGot = true;
//                                if (rect.width <= 0)
//                                {
//                                    rect.width = cachedTransform.rect.width - layoutPadding.left - layoutPadding.right;
//                                }
//                                templateSize[templateIdx] = rect.size;
//                                visibleElementCount = (int)((scrollRectViewport.rect.height - layoutPadding.top - layoutPadding.bottom) / (templateSize[templateIdx].y + layoutSpacing.y)) + 2;
//                            }
//                            break;
//                        case LayoutTypes.Grid:
//                            if (cachedTransform.rect.width > 0)
//                            {
//                                templateSizeGot = true;
//                                noCheckSize = true;
//                                //GridLayout的成员尺寸由Layout决定，因此templateSize不由Prefab决定
//                                var rowCount = (int)((scrollRectViewport.rect.height - layoutPadding.top - layoutPadding.bottom + layoutSpacing.y) / (templateSize[templateIdx].y + layoutSpacing.y)) + 2;
//                                visibleElementColumnCount = (int)((cachedTransform.rect.width - layoutPadding.left - layoutPadding.right + layoutSpacing.x) / (templateSize[templateIdx].x + layoutSpacing.x));
//                                visibleElementCount = visibleElementColumnCount * rowCount;
//                            }
//                            break;
//                    }
//                }
//            }
//            return templateSizeGot;
//        }
//
//        void RetriveTemplateSize()
//        {
//            if (isPredefined)
//            {
//                templateSizeGot = true;
//            }
//            else
//            {
//                if (multiTemplateEnabled)
//                {
//                    bool allGot = true;
//                    for (int i = 0; i < templateNodes.Length; i++)
//                    {
//                        if (!RetriveTemplateSizeByIndex(i))
//                            allGot = false;
//                    }
//                    if (allGot)
//                        templateSizeGot = true;
//                }
//                else
//                {
//                    if (RetriveTemplateSizeByIndex(0))
//                        templateSizeGot = true;
//                }
//            }
//        }
//
//        Vector2 GetNextItemPosition(int index = -1)
//        {
//            Vector2 pos = default(Vector2);
//            //if (!templateSizeGot)
//            //    RetriveTemplateSize(true);
//            if (!templateSizeGot)
//                throw new NotSupportedException("Cannot get default template size on " + name);
//            if (index < 0)
//                index = recycleChildren.Count - 1;
//            if (index >= 0 && index < recycleChildren.Count)
//            {
//                ref var lastChild = ref recycleChildren.GetRef(index);
//                switch (layoutType)
//                {
//                    case LayoutTypes.Horizontal:
//                        pos.x = lastChild.Position.x + lastChild.Size.x + layoutSpacing.x;
//                        break;
//                    case LayoutTypes.Vertical:
//                        pos.y = lastChild.Position.y - lastChild.Size.y - layoutSpacing.y;
//                        break;
//                    case LayoutTypes.Grid:
//                        var nextX = lastChild.Position.x + lastChild.Size.x + layoutSpacing.x;
//                        int x = Mathf.RoundToInt(nextX / (lastChild.Size.x + layoutSpacing.x));
//                        if (x >= visibleElementColumnCount)
//                        {
//                            pos.y = lastChild.Position.y - lastChild.Size.y - layoutSpacing.y;
//                        }
//                        else
//                        {
//                            pos.x = nextX;
//                            pos.y = lastChild.Position.y;
//                        }
//                        break;
//                }
//            }
//            return pos;
//        }
//
//        public void SetChildrenData(IList<UIContainerData> children)
//        {
//            SetChildrenDataInternal(children, 0);
//        }
//
//        void UpdateChildData(int idx, ref UIContainerData childData, bool needSetPosition)
//        {
//            ref var child = ref recycleChildren.GetRef(idx);
//            if (child.TemplateIndex != childData.TemplateIndex && child.ObjectIndex >= 0)
//            {
//                streamingOut[child.TemplateIndex].Add(child.ObjectIndex);
//                child.TemplateIndex = childData.TemplateIndex;
//                child.ObjectIndex = -1;
//            }
//            if (needSetPosition)
//                child.Position = childData.Position;
//            if (child.Data != childData.Data)
//            {
//                child.Data = childData.Data;
//                child.TemplateIndex = childData.TemplateIndex;
//                if (child.ObjectIndex >= 0)
//                {
//                    var c = childList[child.ObjectIndex];
//                    OnRefreshChild?.Invoke(c.GameObject, idx, child.Data);
//                }
//            }
//        }
//
//        void SetChildrenDataInternal(IList<UIContainerData> children, int count)
//        {
//            if (children != null)
//                count = children.Count;
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                bool needSetPosition = !layoutGroup;
//
//
//                if (isPredefined && recycleChildren.Count != children.Count)
//                {
//                    throw new NotSupportedException("Can not change children count in predefined mode");
//                }
//                if (recycleChildren.Count > count)
//                {
//                    recycleDirty = true;
//                    needCalcLayout = true;
//                    if (children != null)
//                    {
//                        for (int i = 0; i < count; i++)
//                        {
//                            var newData = children[i];
//                            UpdateChildData(i, ref newData, needSetPosition);
//                        }
//                    }
//                    for (int i = count; i < recycleChildren.Count; i++)
//                    {
//                        ref var child = ref recycleChildren.GetRef(i);
//                        if (child.ObjectIndex >= 0)
//                        {
//                            streamingOut[child.TemplateIndex].Add(child.ObjectIndex);
//                        }
//                    }
//                    recycleChildren.RemoveRange(count, recycleChildren.Count - count);
//                }
//                else
//                {
//                    recycleDirty = true;
//                    needCalcLayout = true;
//                    if (children != null)
//                    {
//                        for (int i = 0; i < recycleChildren.Count; i++)
//                        {
//                            ref var child = ref recycleChildren.GetRef(i);
//                            var childData = children[i];
//                            if (child.TemplateIndex != childData.TemplateIndex && child.ObjectIndex >= 0)
//                            {
//                                streamingOut[child.TemplateIndex].Add(child.ObjectIndex);
//                                child.TemplateIndex = childData.TemplateIndex;
//                                child.ObjectIndex = -1;
//                            }
//                            if (needSetPosition)
//                                child.Position = childData.Position;
//                            if (child.Data != childData.Data)
//                            {
//                                child.Data = childData.Data;
//                                child.TemplateIndex = childData.TemplateIndex;
//                                if (child.ObjectIndex >= 0)
//                                {
//                                    var c = childList[child.ObjectIndex];
//                                    OnRefreshChild?.Invoke(c.GameObject, i, child.Data);
//                                }
//                            }
//                        }
//                    }
//                    int cur = recycleChildren.Count;
//                    var diff = count - recycleChildren.Count;
//                    if (!templateSizeGot)
//                        RetriveTemplateSize();
//                    for (int i = 0; i < diff; i++)
//                    {
//                        ChildEntry child;
//                        if (children != null)
//                        {
//                            var childData = children[cur + i];
//                            child = new ChildEntry()
//                            {
//                                TemplateIndex = childData.TemplateIndex,
//                                ObjectIndex = -1,
//                                Position = needSetPosition ? childData.Position : (templateSizeGot ? GetNextItemPosition() : Vector2.zero),
//                                Size = templateSize[childData.TemplateIndex],
//                                Data = childData.Data
//                            };
//                        }
//                        else
//                            child = new ChildEntry()
//                            {
//                                TemplateIndex = 0,
//                                ObjectIndex = -1,
//                                Position = needSetPosition ? Vector2.zero : (templateSizeGot ? GetNextItemPosition() : Vector2.zero),
//                                Size = templateSize[0]
//                            };
//                        recycleChildren.Add(ref child);
//                    }
//                }
//                if (!templateSizeGot)
//                {
//                    positionDirty = true;
//                }
//                recycleDirty = true;
//                needCalcLayout = true;
//            }
//            else
//            {
//                int cur = ChildCount;
//                if (cur < count)
//                {
//                    if (children != null)
//                    {
//                        for (int i = 0; i < cur; i++)
//                        {
//                            var childData = children[i];
//                            var child = GetChildByIndexInternal(i);
//                            if (child.TemplateIndex != childData.TemplateIndex)
//                            {
//                                DoDisposeGameObject(false, child);
//                                //child.GameObject = DoAddChildGameObject(out _, childData.TemplateIndex);
//                                //childList[i] = child;
//
//                                //移除TemplateIndex变化，更新streamingOut
//                                if (recycleModeEnabled || (isPredefined && isPredefinedRecycle))
//                                {
//                                    if (streamingOut[child.TemplateIndex].Remove(i))
//                                    {
//                                        streamingOut[childData.TemplateIndex].Add(i);
//                                    }
//                                }
//
//                                //当模板不同时，实例化GameObject后没有走正常的流程（即DoAddChild中DoAddChildGameObject之后的逻辑，导致新创建的go的GameUILogic为空，进而导致HandleRefreshChild中的OnRefreshItem没有调用，所以显示异常。暂先改为以下逻辑
//                                var go = DoAddChildGameObject(out var isPool, childData.TemplateIndex);
//                                childListDirty = true;
//                                if (isPool && needInvokePoolEvent)
//                                    OnPoolSharedChild?.Invoke(go, true);
//                                OnCreateChild?.Invoke(go, !isPool);
//                                child.GameObject = go;
//                                child.TemplateIndex = childData.TemplateIndex;
//                                childList[i] = child;
//                                childListDirty = true;
//                            }
//                            OnRefreshChild?.Invoke(child.GameObject, i, childData.Data);
//                        }
//                    }
//                    int diff = count - cur;
//                    for (int i = 0; i < diff; i++)
//                    {
//                        if (children != null)
//                        {
//                            var childData = children[cur + i];
//                            var go = DoAddChild(out _, childData.TemplateIndex);
//                            OnRefreshChild?.Invoke(go, cur + i, childData.Data);
//                        }
//                        else
//                            DoAddChild(out _, 0);
//                    }
//                }
//                else
//                {
//                    int diff = cur - count;
//                    for (int i = cur - 1; i >= cur - diff; i--)
//                    {
//                        DoRemoveChild(i);
//                    }
//                    if (children != null)
//                    {
//                        for (int i = 0; i < count; i++)
//                        {
//                            var childData = children[i];
//                            var child = GetChildByIndexInternal(i);
//                            if (child.TemplateIndex != childData.TemplateIndex)
//                            {
//                                DoDisposeGameObject(false, child);
//                                //child.TemplateIndex = childData.TemplateIndex;
//                                //child.GameObject = DoAddChildGameObject(out _, childData.TemplateIndex);
//                                //childList[i] = child;
//
//                                //移除TemplateIndex变化，更新streamingOut
//                                if (recycleModeEnabled || (isPredefined && isPredefinedRecycle))
//                                {
//                                    if (streamingOut[child.TemplateIndex].Remove(i))
//                                    {
//                                        streamingOut[childData.TemplateIndex].Add(i);
//                                    }
//                                }
//
//                                //当模板不同时，实例化GameObject后没有走正常的流程（即DoAddChild中DoAddChildGameObject之后的逻辑，导致新创建的go的GameUILogic为空，进而导致HandleRefreshChild中的OnRefreshItem没有调用，所以显示异常。暂先改为以下逻辑
//                                var go = DoAddChildGameObject(out var isPool, childData.TemplateIndex);
//                                childListDirty = true;
//                                if (isPool && needInvokePoolEvent)
//                                    OnPoolSharedChild?.Invoke(go, true);
//                                OnCreateChild?.Invoke(go, !isPool);
//                                child.GameObject = go;
//                                child.TemplateIndex = childData.TemplateIndex;
//                                childList[i] = child;
//                                childListDirty = true;
//                            }
//                            OnRefreshChild?.Invoke(child.GameObject, i, childData.Data);
//                        }
//                    }
//                }
//            }
//        }
//
//        public void EnsureSize(int count)
//        {
//            if (isPredefined)
//                throw new NotSupportedException($"Predefined UI Container add child manually({name})");
//            if (multiTemplateEnabled)
//                throw new NotSupportedException("Please use SetChildrenData instead for multi template mode");
//            SetChildrenDataInternal(null, count);
//        }
//
//        public void Clear(bool destroy)
//        {
//            if (isPredefined)
//                return;
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                recycleChildren.Clear();
//            }
//            var cur = childList.Count;
//            for (int i = cur - 1; i >= 0; i--)
//            {
//                DoRemoveChild(i, destroy);
//            }
//            if (streamingOut != null)
//            {
//                cur = streamingOut.Length;
//                for (int i = 0; i < cur; i++)
//                {
//                    if (streamingOut[i] != null)
//                        streamingOut[i].Clear();
//                }
//            }
//            if (streamingIn != null)
//                streamingIn.Clear();
//        }
//
//        public void DisposeChildren(bool destroy)
//        {
//            Clear(destroy);
//            if (!sharedPool && childPool != null && destroy)
//            {
//                foreach (var i in childPool)
//                {
//                    while (i.Count > 0)
//                    {
//                        var child = i.Alloc();
//                        OnDisposeChild?.Invoke(child, true);
//                        GameObject.Destroy(child);
//                    }
//                }
//            }
//        }
//
//        protected override void OnRectTransformDimensionsChange()
//        {
//            base.OnRectTransformDimensionsChange();
//            if (recycleModeEnabled || isPredefinedRecycle)
//                recycleDirty = true;
//        }
//
//        void OnScroll(Vector2 pos)
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//                recycleDirty = true;
//        }
//        bool CheckSizeChange(ref ChildEntry child, out Vector2 newSize)
//        {
//            if (noCheckSize)
//            {
//                newSize = child.Size;
//                return false;
//            }
//            var childGO = childList[child.ObjectIndex].GameObject;
//            var rt = childGO.transform as RectTransform;
//            newSize = new Vector2(LayoutUtility.GetPreferredWidth(rt), LayoutUtility.GetPreferredHeight(rt));
//            if (newSize.x < 0.001f)
//                newSize.x = LayoutUtility.GetFlexibleWidth(rt);
//            if(newSize.y < 0.001f)
//                newSize.y = LayoutUtility.GetFlexibleHeight(rt);
//            if (newSize.magnitude > 0.1f && Vector2.Distance(newSize, child.Size) > 0.1f)
//                return true;
//            else
//                return false;
//        }
//
//        public Vector2 ChildVisibleRectShiftBufferSize
//        {
//            set
//            {
//                childVisibleRectShiftBuffSize = value;
//            }
//        }
//
//        bool updating;
//        int noSizeCnt = 0;
//        void UpdateChildrenVisibility(bool immediately)
//        {
//            if (updating)
//                return;
//            if (!parentScrollRect)
//                RetriveParentScrollRect();
//            if (!templateSizeGot)
//            {
//                RetriveTemplateSize();
//                if (!templateSizeGot)
//                {
//                    recycleDirty = true;
//                    if (gameObject.activeInHierarchy)
//                        noSizeCnt++;
//                    if (noSizeCnt == 30)
//                    {
//                        Logger.Error($"Cannot get template size for container:{cachedTransform.GetFullPathName(BurnerUIManager.Instance.RootNode.transform)}, please check the templates have correct LayoutElement settings");
//                    }
//                    return;
//                }
//            }
//            noSizeCnt = 0;
//            bool isFreePos = !layoutGroup;
//            if (layoutGroup && disableLayoutGroup)
//            {
//                if (updatedLayoutGroup == null)
//                    updatedLayoutGroup = new HashSet<LayoutGroup>();
//                layoutGroup.enabled = false;
//                foreach(var i in affectingLayoutGroups)
//                {
//                    i.enabled = false;
//                }
//            }
//            updating = true;
//            try
//            {
//                var cnt = recycleChildren.Count;
//                if (positionDirty)
//                {
//                    positionDirty = false;
//                    for (int i = 0; i < cnt; i++)
//                    {
//                        ref var child = ref recycleChildren.GetRef(i);
//                        if (child.Size.magnitude < 1)
//                            child.Size = templateSize[child.TemplateIndex];
//                        if (!isFreePos)
//                            child.Position = i > 0 ? GetNextItemPosition(i - 1) : Vector2.zero;
//                    }
//                }
//
//                int firstVisible = -1;
//                int lastVisible = -1;
//                int firstSizeChange = -1;
//                Vector2 sizeDiff = Vector2.zero;
//                var viewportRect = scrollRectViewport.rect;
//                Rect viewportRectShift = new Rect(Vector2.zero-childVisibleRectShiftBuffSize/2, scrollRectViewport.rect.size + childVisibleRectShiftBuffSize);
//                int curFocus = -1;
//                
//                float focusDis = float.MaxValue;
//                UnityEngine.Profiling.Profiler.BeginSample("Visibility Calculation");
//                //刷新成员尺寸，并计算新进入视野和出视野的格子
//                for (int i = 0; i < cnt; i++)
//                {
//                    ref var child = ref recycleChildren.GetRef(i);
//                    if (child.ObjectIndex >= 0 && CheckSizeChange(ref child, out var newSize))
//                    {
//                        if (firstSizeChange < 0)
//                            firstSizeChange = i;
//                        else if (i < firstSizeChange)
//                            firstSizeChange = i;
//
//                        sizeDiff += (newSize - child.Size);
//                        needCalcLayout = true;
//                        child.Size = newSize;
//                        if (!isFreePos)
//                        {
//                            for (int j = i + 1; j < cnt; j++)
//                            {
//                                ref var childToModify = ref recycleChildren.GetRef(j);
//                                childToModify.Position = GetNextItemPosition(j - 1);
//                            }
//                        }
//                    }
//                    Vector2 childPos, viewportPos;
//                    if (isFreePos)
//                    {
//                        childPos = child.Position + child.Size * templatePivot;
//                        viewportPos = cachedTransform.GetRelativePosWithAnchor(scrollRectViewport, childPos) + new Vector2(layoutPadding.left, -layoutPadding.top);
//                    }
//                    else
//                    {
//                        childPos = child.Position;
//                        viewportPos = cachedTransform.GetRelativePos(scrollRectViewport, childPos) + new Vector2(layoutPadding.left, -layoutPadding.top);
//                    }
//                    var halfChildSize = child.Size / 2;
//                    var centerPos = viewportPos - viewportRectShift.size / 2 + halfChildSize;
//                    halfChildSize += layoutSpacing / 2;
//                    float dis = Mathf.Abs(centerPos.x);
//                    if (dis < halfChildSize.x)
//                    {
//                        if (child.ObjectIndex >= 0)
//                        {
//                            if (dis < focusDis)
//                            {
//                                curFocus = i;
//                                focusDis = dis;
//                            }
//                        }
//                    }
//                    viewportPos.y = -viewportPos.y;
//                    Rect rect = new Rect(viewportPos, child.Size);
//                    
//                    /*var tSize = templateSize;
//                    //tSize.y = -tSize.y;
//                    viewportRect.min -= tSize;
//                    viewportRect.max += tSize;*/
//                    bool isVisible = viewportRectShift.Intersect(rect);
//                    if (isVisible)
//                    {
//                        if (firstVisible < 0)
//                            firstVisible = i;
//                        if (child.ObjectIndex < 0)
//                            streamingIn.Enqueue(i);
//                        if (i > lastVisible)
//                            lastVisible = i;
//                    }
//                    else
//                    {
//                        if (child.ObjectIndex >= 0)
//                        {
//                            streamingOut[child.TemplateIndex].Add(child.ObjectIndex);
//                            child.ObjectIndex = -1;
//                        }
//                    }
//                }
//
//                if (curFocus >= 0)
//                {
//                    ref var child = ref recycleChildren.GetRef(curFocus);
//                    if (child.ObjectIndex >= 0)
//                    {
//                        var go = childList[child.ObjectIndex].GameObject;
//                        OnFocusChild?.Invoke(go, curFocus);
//                    }
//                }
//                UnityEngine.Profiling.Profiler.EndSample();
//                UnityEngine.Profiling.Profiler.BeginSample("Streaming in cells");
//                int instantiateCnt = 0;
//                //将进入视野的格子刷新出来，优先从刚出视野的格子里取，如果没有可用格子则新创建格子
//                while (streamingIn.Count > 0)
//                {
//                    if (!isFreePos)
//                    {
//                        if (layoutGroup.enabled)
//                            layoutGroup.enabled = false;
//                    }
//                    needCalcLayout = true;
//                    recycleDirty = true;
//                    var idx = streamingIn.Dequeue();
//                    ref var child = ref recycleChildren.GetRef(idx);
//                    int goIdx = 0;
//                    var templateIdx = child.TemplateIndex;
//                    var newGo = false;
//                    if (streamingOut[templateIdx].Count > 0)
//                    {
//                        goIdx = GetNextStreamingOutIndex(templateIdx);
//                        UnityEngine.Profiling.Profiler.BeginSample("Streaming out cell data");
//                        if ( recycleModeEnabled || isPredefinedRecycle)
//                            OnStreamingOut?.Invoke(childList[goIdx].GameObject);
//                        UnityEngine.Profiling.Profiler.EndSample();
//
//                    }
//                    else
//                    {
//                        DoAddChild(out var fromPool, templateIdx);
//                        if (!fromPool)
//                            instantiateCnt++;
//                        goIdx = GetNextStreamingOutIndex(templateIdx);
//                        newGo = true;
//                    }
//                    child.ObjectIndex = goIdx;
//                    var go = childList[goIdx].GameObject;
//                    var rt = go.transform as RectTransform;
//                    if (isFreePos)
//                    {
//                        rt.anchoredPosition = child.Position;
//                        rt.sizeDelta = child.Size;
//                    }
//
//                    if (newGo)
//                    {
//                        OnShowChild?.Invoke(go);
//                    }
//
//                    UnityEngine.Profiling.Profiler.BeginSample("Refreshing cell data");
//                    if (!isPredefinedRecycle || child.Data != null)
//                        OnRefreshChild?.Invoke(go, idx, child.Data);
//                    UnityEngine.Profiling.Profiler.EndSample();
//                    UnityEngine.Profiling.Profiler.BeginSample("Rebuilding Layout");
//                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
//                    UnityEngine.Profiling.Profiler.EndSample();
//                    UnityEngine.Profiling.Profiler.BeginSample("Refreshing size and position");
//                    if (CheckSizeChange(ref child, out var newSize))
//                    {
//                        if (firstSizeChange < 0)
//                            firstSizeChange = idx;
//                        else if (idx < firstSizeChange)
//                            firstSizeChange = idx;
//
//                        sizeDiff += (newSize - child.Size);
//                        child.Size = newSize;
//                        if (layoutGroup)
//                        {
//                            for (int j = idx + 1; j < cnt; j++)
//                            {
//                                ref var childToModify = ref recycleChildren.GetRef(j);
//                                childToModify.Position = GetNextItemPosition(j - 1);
//                            }
//                        }
//                    }
//                    UnityEngine.Profiling.Profiler.EndSample();
//                    if (instantiateCnt >= currentInstantiationPerFrame)
//                    {
//                        recycleDirty = recycleDirty || streamingIn.Count > 0;
//                        streamingIn.Clear();
//                    }
//                }
//                UnityEngine.Profiling.Profiler.EndSample();
//                UnityEngine.Profiling.Profiler.BeginSample("Streaming out cells");
//                //将依然不可见的格子删除，放入缓存
//                for (int k = 0; k < streamingOut.Length; k++)
//                {
//                    var outList = streamingOut[k];
//                    cnt = outList.Count;
//                    for (int i = 0; i < cnt; i++)
//                    {
//                        if (!isFreePos)
//                        {
//                            if (layoutGroup.enabled)
//                                layoutGroup.enabled = false;
//                        }
//                        needCalcLayout = true;
//
//                        var idx = outList[i];
//                        UnityEngine.Profiling.Profiler.BeginSample("Streaming out cell data");
//                        if (recycleModeEnabled || isPredefinedRecycle)
//                            OnStreamingOut?.Invoke(childList[idx].GameObject);
//                        UnityEngine.Profiling.Profiler.EndSample();
//                        DoRemoveChild(idx);
//                        FixStreamingOutIndexAbove(idx);
//                        for (int j = 0; j < recycleChildren.Count; j++)
//                        {
//                            ref var child = ref recycleChildren.GetRef(j);
//                            if (child.ObjectIndex > idx)
//                                child.ObjectIndex--;
//                        }
//                    }
//                    outList.Clear();
//                }
//                UnityEngine.Profiling.Profiler.EndSample();
//                UnityEngine.Profiling.Profiler.BeginSample("Resetting sibling index");
//                //刷新格子顺序
//                cnt = recycleChildren.Count;
//                for (int i = 0, exprectedSlidingIndex = firstSibling; i < cnt; i++)
//                {
//                    ref var child = ref recycleChildren.GetRef(i);
//                    if (child.ObjectIndex >= 0)
//                    {
//                        var go = childList[child.ObjectIndex].GameObject;
//                        int slibingIndex = go.transform.GetSiblingIndex();
//                        if (slibingIndex >= exprectedSlidingIndex)
//                        {
//                            exprectedSlidingIndex = slibingIndex + 1;
//                        }
//                        else
//                        {
//                            if (!isFreePos)
//                            {
//                                if (layoutGroup.enabled)
//                                    layoutGroup.enabled = false;
//                            }
//
//                            go.transform.SetSiblingIndex(exprectedSlidingIndex);
//                            ++exprectedSlidingIndex;
//                        }
//                    }
//                }
//                UnityEngine.Profiling.Profiler.EndSample();
//
//                if (!isFreePos)
//                {
//                    UnityEngine.Profiling.Profiler.BeginSample("Calculating Padding");
//                    int left = layoutPadding.left, right = layoutPadding.right, top = layoutPadding.top, bottom = layoutPadding.bottom;
//                    //处理占位符
//                    switch (layoutType)
//                    {
//                        case LayoutTypes.Horizontal:
//                            if (firstVisible < 0 && lastVisible < 0)
//                            {
//                                if (recycleChildren.Count > 0)
//                                {
//                                    ref var firstChild = ref recycleChildren.GetRef(0);
//                                    ref var lastChild = ref recycleChildren.GetRef(recycleChildren.Count - 1);
//                                    left = (int)(layoutPadding.left + (lastChild.Position.x + lastChild.Size.x - firstChild.Position.x));
//                                    right = layoutPadding.right;
//                                }
//                                else
//                                {
//                                    left = layoutPadding.left;
//                                    right = layoutPadding.right;
//                                }
//                            }
//                            else
//                            {
//                                if (firstVisible > 0)
//                                {
//                                    ref var child = ref recycleChildren.GetRef(firstVisible);
//                                    left = (int)child.Position.x + layoutPadding.left;
//                                }
//                                else
//                                    left = layoutPadding.left;
//                                if (lastVisible >= 0 && lastVisible < recycleChildren.Count - 1)
//                                {
//                                    ref var child = ref recycleChildren.GetRef(lastVisible);
//                                    ref var lastChild = ref recycleChildren.GetRef(recycleChildren.Count - 1);
//                                    right = layoutPadding.right + (int)((lastChild.Position.x + lastChild.Size.x) - (child.Position.x + child.Size.x));
//                                }
//                                else
//                                    right = layoutPadding.right;
//                            }
//                            break;
//                        case LayoutTypes.Vertical:
//                        case LayoutTypes.Grid:
//                            if (firstVisible < 0 && lastVisible < 0)
//                            {
//                                if (recycleChildren.Count > 0)
//                                {
//                                    ref var firstChild = ref recycleChildren.GetRef(0);
//                                    ref var lastChild = ref recycleChildren.GetRef(recycleChildren.Count - 1);
//                                    top = (int)(layoutPadding.top - (lastChild.Position.y - lastChild.Size.y - firstChild.Position.y));
//                                    bottom = layoutPadding.bottom;
//                                }
//                                else
//                                {
//                                    top = layoutPadding.top;
//                                    bottom = layoutPadding.bottom;
//                                }
//                            }
//                            else
//                            {
//                                if (firstVisible > 0)
//                                {
//                                    ref var child = ref recycleChildren.GetRef(firstVisible);
//                                    top = -(int)child.Position.y + layoutPadding.top;
//                                }
//                                else
//                                    top = layoutPadding.top;
//                                if (lastVisible >= 0 && lastVisible < recycleChildren.Count - 1)
//                                {
//                                    ref var child = ref recycleChildren.GetRef(lastVisible);
//                                    ref var lastChild = ref recycleChildren.GetRef(recycleChildren.Count - 1);
//                                    bottom = layoutPadding.bottom - (int)((lastChild.Position.y - lastChild.Size.y) - (child.Position.y - child.Size.y));
//                                }
//                                else
//                                    bottom = layoutPadding.bottom;
//                            }
//                            break;
//                    }
//                    bool paddingDirty = false;
//                    if (left != layoutGroup.padding.left)
//                    {
//                        paddingDirty = true;
//                    }
//                    if (right != layoutGroup.padding.right)
//                    {
//                        paddingDirty = true;
//                    }
//                    if (top != layoutGroup.padding.top)
//                    {
//                        paddingDirty = true;
//                    }
//                    if (bottom != layoutGroup.padding.bottom)
//                    {
//                        paddingDirty = true;
//                    }
//                    if (paddingDirty)
//                        layoutGroup.padding = new RectOffset(left, right, top, bottom);//force rebuild layout
//
//                    if (!layoutGroup.enabled && !disableLayoutGroup)
//                        layoutGroup.enabled = true;
//
//                    if (needCalcLayout && disableLayoutGroup)
//                    {
//                        needCalcLayout = false;
//                        layoutGroup.CalculateLayoutInputHorizontal();
//                        layoutGroup.SetLayoutHorizontal();
//                        layoutGroup.CalculateLayoutInputVertical();
//                        layoutGroup.SetLayoutVertical();
//                        containerLayoutElement.preferredHeight = layoutGroup.preferredHeight;
//                        containerLayoutElement.preferredWidth = layoutGroup.preferredWidth;
//
//                        if(affectingLayoutGroups!= null && affectingLayoutGroups.Length > 0)
//                        {
//                            foreach (var l in affectingLayoutGroups)
//                            {
//                                if (immediately)
//                                {
//                                    UpdateLayoutGroup(l);
//                                }
//                                else
//                                    updatedLayoutGroup.Add(l);
//                            }
//                        }
//                    }
//                    UnityEngine.Profiling.Profiler.EndSample();
//                }
//                UnityEngine.Profiling.Profiler.BeginSample("Fixing scrolling progress");
//                if (sizeDiff.magnitude > 0.1f)
//                {
//                    ref var firstChild = ref recycleChildren.GetRef(firstSizeChange);
//                    var childPos = firstChild.Position;
//                    childPos.y = -childPos.y;
//                    childPos += new Vector2(layoutPadding.left, layoutPadding.top);
//                    childPos -= viewportRect.size;
//                    if (childPos.x < 0)
//                        childPos.x = 0;
//                    if (childPos.y <= 0)
//                        childPos.y = 0;
//                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentScrollRect.content);
//                    var curSize = parentScrollRect.content.rect.size - viewportRect.size;
//                    var norPos = parentScrollRect.normalizedPosition;
//                    norPos.y = 1f - norPos.y;
//                    var scrollPos = curSize * norPos;
//                    curSize.x = Mathf.Max(curSize.x, 1f);
//                    curSize.y = Mathf.Max(curSize.y, 1f);
//                    var oldPos = childPos + firstChild.Size - sizeDiff;
//
//                    bool hasTouch = true;
//                    /* 手动拖住滑动后松手惯性情况下，以下修复会导致跳变，因此暂时注释，待其他地方出问题再看这块咋改
//#if UNITY_STANDALONE || UNITY_EDITOR
//                    hasTouch = Input.GetMouseButton(0) || Input.GetMouseButton(1);
//                    Debug.LogWarning($"hasTouch = {hasTouch}");
//
//#else
//                    hasTouch = Input.touchCount > 0;
//#endif
//                    */
//
//                    if (((scrollPos.x > childPos.x && scrollPos.x > oldPos.x) || (scrollPos.y > childPos.y && scrollPos.y > oldPos.y)) && parentScrollRect.velocity.magnitude > 1f && hasTouch)
//                    {
//                        var newScrollPos = scrollPos + sizeDiff;
//                        norPos = newScrollPos / curSize;
//                        norPos.y = 1f - norPos.y;
//                        parentScrollRect.SetNormalizedPositionAndKeepDrag(norPos, sizeDiff);
//                    }
//                }
//                UnityEngine.Profiling.Profiler.EndSample();
//            }
//            finally
//            {
//                updating = false;
//            }
//        }
//
//        void FixStreamingOutIndexAbove(int index)
//        {
//            for (int k = 0; k < streamingOut.Length; k++)
//            {
//                var outList = streamingOut[k];
//                int cnt = outList.Count;
//                for (int i = 0; i < cnt; i++)
//                {
//                    if (outList[i] > index)
//                        outList[i]--;
//                }
//            }
//        }
//
//        int GetNextStreamingOutIndex(int templateIdx)
//        {
//            var outList = streamingOut[templateIdx];
//            int idx = outList.Count - 1;
//            int res = outList[idx];
//            outList.RemoveAt(idx);
//            return res;
//        }
//
//        private void LateUpdate()
//        {
//            if (recycleDirty)
//            {
//                recycleDirty = false;
//                UpdateChildrenVisibility(false);
//            }
//        }
//
//        protected override void OnDisable()
//        {
//            if (disableLayoutGroup && updatedLayoutGroup != null)
//            {
//                if (affectingLayoutGroups != null && affectingLayoutGroups.Length > 0)
//                {
//                    foreach (var l in affectingLayoutGroups)
//                    {
//                        updatedLayoutGroup.Add(l);
//                    }
//                }
//            }
//        }
//
//        protected override void OnEnable()
//        {
//            if (disableLayoutGroup)
//                needCalcLayout = true;
//            if (disableLayoutGroup && updatedLayoutGroup != null)
//            {
//                if (affectingLayoutGroups != null && affectingLayoutGroups.Length > 0)
//                {
//                    foreach (var l in affectingLayoutGroups)
//                    {
//                        updatedLayoutGroup.Add(l);
//                    }
//                }
//            }
//        }
//
//        public void RefreshLayout()
//        {
//            if (recycleModeEnabled || isPredefinedRecycle)
//            {
//                recycleDirty = true;
//                needCalcLayout = true;
//                UpdateChildrenVisibility(true);
//            }
//            LayoutRebuilder.ForceRebuildLayoutImmediate(cachedTransform);
//        }
//
//        static void UpdateLayoutGroup(LayoutGroup l)
//        {
//            if (!l)
//                return;
//            if (!l.gameObject.activeInHierarchy)
//                return;
//            var rt = l.transform as RectTransform;
//            l.CalculateLayoutInputHorizontal();
//            l.CalculateLayoutInputVertical();
//            LayoutElement le = l.GetComponent<LayoutElement>();
//            if (!le)
//                le = l.gameObject.AddComponent<LayoutElement>();
//            le.preferredHeight = l.preferredHeight;
//            le.preferredWidth = l.preferredWidth;
//            //在SetLayoutXXXX之前如果不设置Size会导致Children的position计算错误
//            var oldSize = rt.sizeDelta;
//            float w = (l is HorizontalLayoutGroup) ? Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x) ? le.preferredWidth : oldSize.x : oldSize.x;
//            float h = !(l is HorizontalLayoutGroup) ? Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y) ? le.preferredHeight : oldSize.y : oldSize.y;
//
//            rt.sizeDelta = new Vector2(w, h);
//            l.SetLayoutHorizontal();
//            l.SetLayoutVertical();
//
//            
//        }
//
//        static void UpdateLayoutGroups()
//        {
//            if (updatedLayoutGroup != null)
//            {
//                foreach(var l in updatedLayoutGroup)
//                {
//                    UpdateLayoutGroup(l);
//                }
//                foreach (var l in updatedLayoutGroup)
//                {
//                    UpdateLayoutGroup(l);
//                }
//                updatedLayoutGroup.Clear();
//            }
//        }
//
//        [RuntimeInitializeOnLoadMethod]
//        private static void RegisterCallbackFunction()
//        {
//            // Prepare the function for using player loop.
//            var myPlayerLoopSystem = new PlayerLoopSystem()
//            {
//                type = typeof(UIContainer),     // Identifier for Profiler Hierarchy view.
//                updateDelegate = UpdateLayoutGroups    // Register the function.
//            };
//
//
//            // Get the default player loop.
//            var playerLoopSystem =
//#if UNITY_2019_3_OR_NEWER
//                PlayerLoop.GetCurrentPlayerLoop();
//#else
//                PlayerLoop.GetDefaultPlayerLoop();
//#endif
//
//            var playerLoopIndex = -1;
//            for (var i = 0; i < playerLoopSystem.subSystemList.Length; i++)
//            {
//                if (playerLoopSystem.subSystemList[i].type != typeof(PostLateUpdate))
//                {
//                    continue;
//                }
//
//                playerLoopIndex = i;
//                break;
//            }
//
//            if (playerLoopIndex < 0)
//            {
//                Debug.LogError("UIContainer : Failed to add processing to PlayerLoop.");
//                return;
//            }
//
//            // Get the "PostLateUpdate" system.
//            var playerLoopSubSystem = playerLoopSystem.subSystemList[playerLoopIndex];
//            var subSystemList = playerLoopSubSystem.subSystemList;
//
//
//            // Register the model update function after "PreLateUpdate" system.
//            Array.Resize(ref subSystemList, subSystemList.Length + 1);
//            Array.Copy(subSystemList, 0, subSystemList, 1, subSystemList.Length - 1);
//            subSystemList[0] = myPlayerLoopSystem;
//
//
//            // Restore the "PreLateUpdate" sytem.
//            playerLoopSubSystem.subSystemList = subSystemList;
//            playerLoopSystem.subSystemList[playerLoopIndex] = playerLoopSubSystem;
//            PlayerLoop.SetPlayerLoop(playerLoopSystem);
//        }
//
//        public ScrollRect GetScrollRect()
//        {
//            RetriveParentScrollRect();
//            return ScrollRect;
//        }
//    }
//}
