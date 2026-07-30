//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using Burner;
//using Burner.Basic;
//using Burner.Extensions;
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.UI;
//
//
//public class NodePostProcessManager : MonoBehaviour
//{
//    public class NodeListStageInfo
//    {
//        public List<Transform> mRawParentList = new List<Transform>();
//        public List<int> mRawSlblingIndexList = new List<int>();
//        public List<Vector3> mRawScaleList = new List<Vector3>();
//        public List<Vector2> mRawPosList = new List<Vector2>();
//
//        public GameObject mCameraGameObject = null;
//        public GameObject mCanvasGameObject = null;
//
//        public RenderTexture m_renderTarget;
//
//        public bool IsValid => mCameraGameObject.IsNotNull() && mCanvasGameObject.IsNotNull();
//    }
//
//    private static Rect GetNodeWorldRect(RectTransform _rectTransform)
//    {
//        Vector3[] worldCorners = new Vector3[4];
//        _rectTransform.GetWorldCorners(worldCorners);
//
//        Vector2 min = worldCorners[0];
//        Vector2 max = worldCorners[0];
//
//        for (int i = 1; i != 4; ++i)
//        {
//            min = Vector2.Min(min, worldCorners[i]);
//            max = Vector2.Max(max, worldCorners[i]);
//        }
//
//        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
//    }
//
//    private static Rect GetBoundRect(List<Rect> _rectList)
//    {
//        if (_rectList.Count == 0)
//            return Rect.zero;
//
//        Vector2 min = _rectList[0].min;
//        Vector2 max = _rectList[0].max;
//
//        for (int i = 1; i != _rectList.Count; ++i)
//        {
//            min = Vector2.Min(_rectList[i].min, min);
//            max = Vector2.Max(_rectList[i].max, max);
//        }
//
//        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
//    }
//
//    private static Rect GetRelativeRect(Rect _rawRect, Rect _parentRect)
//    {
//        Vector2 relativePos = (_rawRect.position - _parentRect.position) / _parentRect.size;
//        Vector2 relativeSize = _rawRect.size / _parentRect.size;
//
//        return new Rect(relativePos, relativeSize);
//    }
//
//    private static Rect GetAbsoluteRect(Rect _relativeRect, Rect _parentRect)
//    {
//        Vector2 absolutePos = _relativeRect.position * _parentRect.size + _parentRect.position;
//        Vector2 absoluteSize = _relativeRect.size * _parentRect.size;
//
//        return new Rect(absolutePos, absoluteSize);
//    }
//
//    private static NodeListStageInfo StageNodeListInfo(List<RectTransform> _gameObjectList, 
//        GameObject cameraGameObject, GameObject canvasGameObject, RenderTexture targetTexture)
//    {
//        NodeListStageInfo nodeListStageInfo = new NodeListStageInfo();
//
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            nodeListStageInfo.mRawParentList.Add(_gameObjectList[i].parent);
//            nodeListStageInfo.mRawSlblingIndexList.Add(_gameObjectList[i].GetSiblingIndex());
//            nodeListStageInfo.mRawScaleList.Add(_gameObjectList[i].localScale);
//            nodeListStageInfo.mRawPosList.Add(_gameObjectList[i].anchoredPosition);
//        }
//
//        nodeListStageInfo.mCameraGameObject = cameraGameObject;
//        nodeListStageInfo.mCanvasGameObject = canvasGameObject;
//        nodeListStageInfo.m_renderTarget = targetTexture;
//
//        return nodeListStageInfo;
//    }
//
//    private static void RestoreNodeListInfo(List<RectTransform> _gameObjectList, NodeListStageInfo _nodeListStageInfo)
//    {
//        if (
//            _gameObjectList.Count != _nodeListStageInfo.mRawParentList.Count ||
//            _gameObjectList.Count != _nodeListStageInfo.mRawSlblingIndexList.Count ||
//            _gameObjectList.Count != _nodeListStageInfo.mRawScaleList.Count ||
//            _gameObjectList.Count != _nodeListStageInfo.mRawPosList.Count ||
//            _nodeListStageInfo.mCameraGameObject.IsNull() ||
//            _nodeListStageInfo.mCanvasGameObject.IsNotNull() ||
//            _nodeListStageInfo.m_renderTarget.IsNotNull()
//            )
//        {
//            //Debug.LogError("[BurnerUI]: 错误的NodeListStageInfo");
//            return;
//        }
//
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            _gameObjectList[i].SetParent(_nodeListStageInfo.mRawParentList[i]);
//            _gameObjectList[i].localScale = _nodeListStageInfo.mRawScaleList[i];
//            _gameObjectList[i].anchoredPosition3D = _nodeListStageInfo.mRawPosList[i];
//        }
//
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            _gameObjectList[i].SetSiblingIndex(_nodeListStageInfo.mRawSlblingIndexList[i]);
//        }
//
//        if(_nodeListStageInfo.mCameraGameObject.IsNotNull())
//        {
//            Destroy(_nodeListStageInfo.mCameraGameObject);
//        }
//
//        if(_nodeListStageInfo.mCanvasGameObject.IsNotNull())
//        {
//            Destroy(_nodeListStageInfo.mCanvasGameObject);
//        }
//
//        if(_nodeListStageInfo.m_renderTarget.IsNotNull())
//        {
//            _nodeListStageInfo.m_renderTarget.Release();
//        }
//    }
//
//    public static void SetRawImagePos(RawImage _rawImage, Rect screenShotRect)
//    {
//        var canvasArr = _rawImage.GetComponentsInParent<Canvas>(true);
//        if (canvasArr.Length < 1)
//            return;
//
//        Canvas rootCanvas = canvasArr[0].rootCanvas;
//        if (!rootCanvas) 
//            return;
//        Rect rootCanvasRect = GetNodeWorldRect(rootCanvas.transform as RectTransform);
//
//        Rect rawImageWorldRect = GetNodeWorldRect(_rawImage.rectTransform);
//        Rect expectRawImageWorldRect = GetAbsoluteRect(screenShotRect, rootCanvasRect);
//
//        _rawImage.rectTransform.sizeDelta *= expectRawImageWorldRect.size / rawImageWorldRect.size;
//        rawImageWorldRect = GetNodeWorldRect(_rawImage.rectTransform);
//
//        _rawImage.rectTransform.anchoredPosition += (expectRawImageWorldRect.position - rawImageWorldRect.position) / _rawImage.rectTransform.lossyScale;
//    }
//
//    public static void GetNodeShotImage(List<RectTransform> _gameObjectList, ref RenderTexture _destTexture, ref Rect screenShotRect, bool useTempDestTexture, float RTScale=1.0f)
//    {
//        if(_gameObjectList.IsNullOrEmpty() || _gameObjectList[0].IsNull())
//        {
//            throw new ArgumentNullException("[SoftMask]: _gameObjectList is null or gameobject has been destroyed.");
//        }
//
//        var rootCanvas = _gameObjectList[0].GetComponentInParent<Canvas>();
//        if(rootCanvas.IsNull())
//        {
//            throw new Exception($"[SoftMask]: {_gameObjectList[0].gameObject.GetHierachyPath()} cannot find canvas!");
//        }
//
//        rootCanvas = rootCanvas.rootCanvas;
//        if(rootCanvas.IsNull())
//        {
//            throw new Exception($"[SoftMask]: {_gameObjectList[0].gameObject.GetHierachyPath()} cannot find its root canvas!");
//        }
//
//        Rect rootCanvasRect = GetNodeWorldRect(rootCanvas.transform as RectTransform);
//
//        List<Rect> gameObjectWorldRectList = new List<Rect>();
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            gameObjectWorldRectList.Add(GetNodeWorldRect(_gameObjectList[i]));
//        }
//
//        if (screenShotRect.size == Vector2.zero)
//        {
//            Rect absScreenShotRect = GetBoundRect(gameObjectWorldRectList);
//            screenShotRect = GetRelativeRect(absScreenShotRect, rootCanvasRect);
//        }
//
//        Rect absoluteScreenShotRect = GetAbsoluteRect(screenShotRect, rootCanvasRect);
//
//        List<Rect> gameObjectRelativeRectList = new List<Rect>();
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            gameObjectRelativeRectList.Add(GetRelativeRect(gameObjectWorldRectList[i], absoluteScreenShotRect));
//        }
//
//        Vector2 renderTargetSize = screenShotRect.size * (rootCanvas.transform as RectTransform).sizeDelta * RTScale;
//
//        if (_destTexture == null)
//        {
//            if (useTempDestTexture)
//                _destTexture = RenderTexture.GetTemporary((int)renderTargetSize.x, (int)renderTargetSize.y, 24);
//            else
//                _destTexture = new RenderTexture((int)renderTargetSize.x, (int)renderTargetSize.y, 24);
//        }
//
//        GameObject newCameraGameObject = new GameObject("CanvasTempCamera");
//        newCameraGameObject.transform.localPosition = new Vector3(100, 100, 100);
//        GameObject newCanvasGameObject = new GameObject("TempCanvas");
//
//        RenderTexture tempRenderTexture = RenderTexture.GetTemporary((int)renderTargetSize.x, (int)renderTargetSize.y, 24);
//
//        Camera camera = newCameraGameObject.AddComponent<Camera>();
//        camera.clearFlags = CameraClearFlags.SolidColor;
//        camera.backgroundColor = new Color(0, 0, 0, 0);
//        //camera.orthographic = true;
//        camera.targetTexture = tempRenderTexture;
//        if (rootCanvas.worldCamera)
//        {
//            camera.orthographic = rootCanvas.worldCamera.orthographic;
//            camera.depth = rootCanvas.worldCamera.depth;
//        }
//        else if (Burner.UIExtension.BurnerUIManager.Instance != null && Burner.UIExtension.BurnerUIManager.Instance.UICamera)
//            camera.orthographic = Burner.UIExtension.BurnerUIManager.Instance.UICamera.orthographic;
//        else
//            camera.orthographic = true;
//        //camera.transform.position = rootCanvas.worldCamera.transform.position;
//        //camera.transform.rotation = rootCanvas.worldCamera.transform.rotation;
//
//        Canvas canvas = newCanvasGameObject.AddComponent<Canvas>();
//        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent | AdditionalCanvasShaderChannels.TexCoord1;
//        canvas.renderMode = RenderMode.ScreenSpaceCamera;
//        canvas.worldCamera = camera;
//
//        newCanvasGameObject.transform.SetParent(newCameraGameObject.transform);
//
//        List<Transform> rawParentList = new List<Transform>();
//        List<int> rawSlblingIndexList = new List<int>();
//        List<Vector3> rawScaleList = new List<Vector3>();
//        List<Vector2> rawPosList = new List<Vector2>();
//        List<Vector2> rawOffsetMinList = new List<Vector2>();
//        List<Vector2> rawOffsetMaxList = new List<Vector2>();
//        List<Vector2> rawSizeDeltaList = new List<Vector2>();
//
//        Rect tempCanvasWorldRect = GetNodeWorldRect(newCanvasGameObject.transform as RectTransform);
//        {
//            for (int i = 0; i != _gameObjectList.Count; ++i)
//            {
//                rawParentList.Add(_gameObjectList[i].parent);
//                rawSlblingIndexList.Add(_gameObjectList[i].GetSiblingIndex());
//                rawScaleList.Add(_gameObjectList[i].localScale);
//                rawPosList.Add(_gameObjectList[i].anchoredPosition);
//                rawOffsetMinList.Add(_gameObjectList[i].offsetMin);
//                rawOffsetMaxList.Add(_gameObjectList[i].offsetMax);
//                rawSizeDeltaList.Add(_gameObjectList[i].sizeDelta);
//            }
//
//            for (int i = 0; i != _gameObjectList.Count; ++i)
//            {
//                Canvas selfCanvas = _gameObjectList[i].GetComponentInParent<Canvas>();
//
//                GameObject tempCanvasGameObject = new GameObject($"TempCanvas {i}");
//
//                RectTransform parentTransform = tempCanvasGameObject.AddComponent<RectTransform>();
//
//                Canvas parentCanvas = tempCanvasGameObject.AddComponent<Canvas>();
//
//                tempCanvasGameObject.transform.SetParent(newCanvasGameObject.transform);
//
//                parentCanvas.overrideSorting = true;
//                parentCanvas.sortingOrder = selfCanvas.sortingOrder;
//                parentCanvas.sortingLayerID = selfCanvas.sortingLayerID;
//
//                //重新设置Camera，强制planeDistance刷新
//                var cam = selfCanvas.worldCamera;
//                selfCanvas.worldCamera = null;
//                selfCanvas.worldCamera = cam;
//
//                _gameObjectList[i].SetParent(tempCanvasGameObject.transform, false);
//
//                parentTransform.localScale = Vector3.one;
//                parentTransform.localPosition = Vector3.zero;
//                parentTransform.anchoredPosition3D = Vector3.zero;
//            }
//
//            for (int i = 0; i != _gameObjectList.Count; ++i)
//            {
//                Rect worldRect = GetNodeWorldRect(_gameObjectList[i]);
//                Rect expectWorldRect = GetAbsoluteRect(gameObjectRelativeRectList[i], tempCanvasWorldRect);
//
//                Vector2 scale = (newCanvasGameObject.transform as RectTransform).localScale;
//
//                Vector3 localScale = _gameObjectList[i].localScale;
//                localScale.x *= expectWorldRect.size.x / worldRect.size.x;
//                localScale.y *= expectWorldRect.size.y / worldRect.size.y;
//
//                _gameObjectList[i].localScale = localScale;
//
//                worldRect = GetNodeWorldRect(_gameObjectList[i]);
//
//                _gameObjectList[i].anchoredPosition += (expectWorldRect.position - worldRect.position) / scale;
//            }
//        }
//
//        camera.Render();
//
//        {
//            for (int i = 0; i != _gameObjectList.Count; ++i)
//            {
//                GameObject parentTempCanvas = _gameObjectList[i].parent.gameObject;
//
//                _gameObjectList[i].SetParent(rawParentList[i]);
//                _gameObjectList[i].localScale = rawScaleList[i];
//                _gameObjectList[i].anchoredPosition3D = rawPosList[i];
//                _gameObjectList[i].offsetMin = rawOffsetMinList[i];
//                _gameObjectList[i].offsetMax = rawOffsetMaxList[i];
//                _gameObjectList[i].sizeDelta = rawSizeDeltaList[i];
//
//                Destroy(parentTempCanvas);
//            }
//
//            for (int i = 0; i != _gameObjectList.Count; ++i)
//            {
//                _gameObjectList[i].SetSiblingIndex(rawSlblingIndexList[i]);
//            }
//        }
//
//        Graphics.Blit(tempRenderTexture, _destTexture);
//
//        RenderTexture.ReleaseTemporary(tempRenderTexture);
//
//        Destroy(newCameraGameObject);
//        Destroy(newCanvasGameObject);
//    }
//    
//    public static void GetNodeRenderTexture(List<RectTransform> _gameObjectList, RawImage rawImage,
//        ref RenderTexture _destTexture, ref Rect screenShotRect, out NodeListStageInfo _nodeListStageInfo)
//    {
//        _nodeListStageInfo = null;
//
//        if (_gameObjectList.Count == 0)
//            return;
//
//        Canvas rootCanvas = _gameObjectList[0].GetComponentInParent<Canvas>().rootCanvas;
//        Rect rootCanvasRect = GetNodeWorldRect(rootCanvas.transform as RectTransform);
//
//        List<Rect> gameObjectWorldRectList = new List<Rect>();
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            gameObjectWorldRectList.Add(GetNodeWorldRect(_gameObjectList[i]));
//        }
//
//        if (screenShotRect.size == Vector2.zero)
//        {
//            Rect absScreenShotRect = GetBoundRect(gameObjectWorldRectList);
//            screenShotRect = GetRelativeRect(absScreenShotRect, rootCanvasRect);
//        }
//
//        Rect absoluteScreenShotRect = GetAbsoluteRect(screenShotRect, rootCanvasRect);
//
//        List<Rect> gameObjectRelativeRectList = new List<Rect>();
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            gameObjectRelativeRectList.Add(GetRelativeRect(gameObjectWorldRectList[i], absoluteScreenShotRect));
//        }
//
//        var rootCanvasSize = (rootCanvas.transform as RectTransform).sizeDelta;
//        
//        Vector2 renderTargetSize = screenShotRect.size * rootCanvasSize;
//        if (_destTexture == null)
//        {
//            _destTexture = new RenderTexture((int)renderTargetSize.x, (int)renderTargetSize.y, 1);
//        }
//
//        // comment following code or it will cause to print error in Unity Native code in some devices:
//        //   RenderTexture.Create failed: stencil texture format unsupported - R8 UInt(13)
//        // so that we have to keep it default, unity might set right stencil format when we set its depth.
//        //_destTexture.stencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UInt;
//
//        _destTexture.depth = 24;
//
//        GameObject newCameraGameObject = new GameObject("CanvasTempCamera");
//        GameObject newCanvasGameObject = new GameObject("TempCanvas");
//        newCanvasGameObject.transform.SetParent(newCameraGameObject.transform);
//        
//        Camera camera = newCameraGameObject.AddComponent<Camera>();
//        camera.clearFlags = CameraClearFlags.SolidColor;
//        camera.backgroundColor = new Color(0, 0, 0, 0);
//        camera.orthographic = true;
//        camera.targetTexture = _destTexture;
//        
//        GraphicsRaycastModifer graphicsRaycastModifer = newCameraGameObject.AddComponent<GraphicsRaycastModifer>();
//        graphicsRaycastModifer.ParentSize = rootCanvasSize;
//        graphicsRaycastModifer.RelativeRectPos = screenShotRect.position * rootCanvasSize;
//        graphicsRaycastModifer.RelativeRectSize = new Vector2(_destTexture.width, _destTexture.height) / renderTargetSize;
//
//        var _ = newCanvasGameObject.AddComponent<GraphicRaycaster>();
//
//        Canvas newCanvas = newCanvasGameObject.GetComponent<Canvas>();
//        newCanvas.renderMode = RenderMode.ScreenSpaceCamera;
//        newCanvas.worldCamera = camera;
//        newCanvas.sortingOrder = rootCanvas.sortingOrder + 1; // prior than raw image
//        
//        // set the size of camera view as same as screenShotRect
//        // please check following wiki for detail reason:
//        // https://burner.feishu.cn/docx/doxcnQ78dyI0ivU59vTR8Scqtpf#doxcn8yYGYUq02WugOCGMSRTlie
//        if(rootCanvas.renderMode == RenderMode.ScreenSpaceCamera
//           || rootCanvas.renderMode == RenderMode.WorldSpace)
//        {
//            var scale = rootCanvas.worldCamera.orthographicSize / (rootCanvasSize.y * 0.5f); 
//            camera.orthographicSize = renderTargetSize.y * 0.5f * scale;
//            
//            camera.nearClipPlane = -0.1f;
//            camera.farClipPlane = 0.1f;
//            newCanvas.planeDistance = 0;
//        }
//        else
//        {
//            camera.orthographicSize = renderTargetSize.y * 0.5f;
//        }
//
//        Rect tempCanvasWorldRect = GetNodeWorldRect(newCanvasGameObject.transform as RectTransform);
//        {
//            _nodeListStageInfo = StageNodeListInfo(_gameObjectList, newCameraGameObject, newCanvasGameObject, _destTexture);
//
//            for (int i = 0; i != _gameObjectList.Count; ++i)
//            {
//                _gameObjectList[i].SetParent(newCanvasGameObject.transform, false);
//            }
//
//            for (int i = 0; i != _gameObjectList.Count; ++i)
//            {
//                Rect worldRect = GetNodeWorldRect(_gameObjectList[i]);
//                Rect expectWorldRect = GetAbsoluteRect(gameObjectRelativeRectList[i], tempCanvasWorldRect);
//
//                Vector2 scale = (newCanvasGameObject.transform as RectTransform).localScale;
//
//                Vector3 localScale = _gameObjectList[i].localScale;
//                localScale.x *= expectWorldRect.size.x / worldRect.size.x;
//                localScale.y *= expectWorldRect.size.y / worldRect.size.y;
//
//                _gameObjectList[i].localScale = localScale;
//
//                worldRect = GetNodeWorldRect(_gameObjectList[i]);
//
//                _gameObjectList[i].anchoredPosition += (expectWorldRect.position - worldRect.position) / scale;
//            }
//        }
//        
//        camera.Render();
//        
//        SetRawImagePos(rawImage, screenShotRect);
//        rawImage.gameObject.SetActive(true);
//        rawImage.texture = _destTexture;
//
//        // set the same position of rawImage
//        newCameraGameObject.transform.position = rawImage.transform.position - Vector3.forward * 10;
//    }
//
//    public void RenderBlurImage(RenderTexture sourceTexture, RenderTexture destTexture, int downSampleNum, float blurSpreadSize, int blurIterations)
//    {
//        if (BlurShader != null)
//        {
//            //【0】参数准备
//            //根据向下采样的次数确定宽度系数。用于控制降采样后相邻像素的间隔
//            float widthMod = 1.0f / (1.0f * (1 << downSampleNum));
//            //Shader的降采样参数赋值
//            material.SetFloat("_DownSampleValue", blurSpreadSize * widthMod);
//            //设置渲染模式：双线性
//            sourceTexture.filterMode = FilterMode.Bilinear;
//            //通过右移，准备长、宽参数值
//            int renderWidth = sourceTexture.width >> downSampleNum;
//            int renderHeight = sourceTexture.height >> downSampleNum;
//
//            // 【1】处理Shader的通道0，用于降采样 ||Pass 0,for down sample
//            //准备一个缓存renderBuffer，用于准备存放最终数据
//            RenderTexture renderBuffer = RenderTexture.GetTemporary(renderWidth, renderHeight, 0, sourceTexture.format);
//            //设置渲染模式：双线性
//            renderBuffer.filterMode = FilterMode.Bilinear;
//            //拷贝sourceTexture中的渲染数据到renderBuffer,并仅绘制指定的pass0的纹理数据
//            Graphics.Blit(sourceTexture, renderBuffer, material, 0);
//
//            //【2】根据BlurIterations（迭代次数），来进行指定次数的迭代操作
//            for (int i = 0; i < blurIterations; i++)
//            {
//                //【2.1】Shader参数赋值
//                //迭代偏移量参数
//                float iterationOffs = (i * 1.0f);
//                //Shader的降采样参数赋值
//                material.SetFloat("_DownSampleValue", blurSpreadSize * widthMod + iterationOffs);
//
//                // 【2.2】处理Shader的通道1，垂直方向模糊处理 || Pass1,for vertical blur
//                // 定义一个临时渲染的缓存tempBuffer
//                RenderTexture tempBuffer = RenderTexture.GetTemporary(renderWidth, renderHeight, 0, sourceTexture.format);
//                // 拷贝renderBuffer中的渲染数据到tempBuffer,并仅绘制指定的pass1的纹理数据
//                Graphics.Blit(renderBuffer, tempBuffer, material, 1);
//                //  清空renderBuffer
//                RenderTexture.ReleaseTemporary(renderBuffer);
//                // 将tempBuffer赋给renderBuffer，此时renderBuffer里面pass0和pass1的数据已经准备好
//                renderBuffer = tempBuffer;
//
//                // 【2.3】处理Shader的通道2，竖直方向模糊处理 || Pass2,for horizontal blur
//                // 获取临时渲染纹理
//                tempBuffer = RenderTexture.GetTemporary(renderWidth, renderHeight, 0, sourceTexture.format);
//                // 拷贝renderBuffer中的渲染数据到tempBuffer,并仅绘制指定的pass2的纹理数据
//                Graphics.Blit(renderBuffer, tempBuffer, material, 2);
//
//                //【2.4】得到pass0、pass1和pass2的数据都已经准备好的renderBuffer
//                // 再次清空renderBuffer
//                RenderTexture.ReleaseTemporary(renderBuffer);
//                // 再次将tempBuffer赋给renderBuffer，此时renderBuffer里面pass0、pass1和pass2的数据都已经准备好
//                renderBuffer = tempBuffer;
//            }
//
//            //拷贝最终的renderBuffer到目标纹理，并绘制所有通道的纹理到屏幕
//            Graphics.Blit(renderBuffer, destTexture);
//            //清空renderBuffer
//            RenderTexture.ReleaseTemporary(renderBuffer);
//
//        }
//        //着色器实例为空，直接拷贝屏幕上的效果。此情况下是没有实现屏幕特效的
//        else
//        {
//            //直接拷贝源纹理到目标渲染纹理
//            Graphics.Blit(sourceTexture, destTexture);
//        }
//    }
//
//    public static void SetGameObjectListVisible(List<RectTransform> _gameObjectList, bool _visible)
//    {
//        for (int i = 0; i != _gameObjectList.Count; ++i)
//        {
//            if (_gameObjectList[i])
//                _gameObjectList[i].gameObject.SetActive(_visible);
//        }
//    }
//
//    public void EnableNodeBlur(List<RectTransform> _gameObjectList, ref Rect screenShotRect, RawImage rawImage, int downSampleNum, float blurSpreadSize, int blurIterations, float RTScale = 1.0f)
//    {
//        RenderTexture screenShotTexture = null;
//
//        GetNodeShotImage(_gameObjectList, ref screenShotTexture, ref screenShotRect, true, RTScale);
//
//        RenderTexture blurTexture = new RenderTexture(screenShotTexture.width, screenShotTexture.height, 1);
//
//        RenderBlurImage(screenShotTexture, blurTexture, downSampleNum, blurSpreadSize, blurIterations);
//
//        RenderTexture.ReleaseTemporary(screenShotTexture);
//
//        SetRawImagePos(rawImage, screenShotRect);
//        rawImage.gameObject.SetActive(true);
//        rawImage.texture = blurTexture;
//    }
//
//    public static void DisableNodeBlur(RawImage rawImage)
//    {
//        RenderTexture texture = rawImage.texture as RenderTexture;
//        rawImage.texture = null;
//        if (texture != null)
//            texture.Release();
//
//        rawImage.gameObject.SetActive(false);
//    }
//
//    public static void EnableNodeShotPostProcess(List<RectTransform> _gameObjectList, ref Rect screenShotRect, RawImage rawImage)
//    {
//        RenderTexture screenShotTexture = null;
//
//        GetNodeShotImage(_gameObjectList, ref screenShotTexture, ref screenShotRect, false);
//
//        RenderTexture.ReleaseTemporary(screenShotTexture);
//
//        SetRawImagePos(rawImage, screenShotRect);
//        rawImage.gameObject.SetActive(true);
//        rawImage.texture = screenShotTexture;
//    }
//
//    public static void DisableNodeShotPostProcess(RawImage rawImage)
//    {
//        RenderTexture texture = rawImage.texture as RenderTexture;
//        rawImage.texture = null;
//        if (texture != null)
//            texture.Release();
//
//        rawImage.gameObject.SetActive(false);
//    }
//
//    public static void EnableNodePostProcess(List<RectTransform> _gameObjectList, ref Rect screenShotRect, RawImage rawImage, out NodeListStageInfo NodeListStageInfo)
//    {
//        RenderTexture _ = null;
//        GetNodeRenderTexture(_gameObjectList, rawImage, ref _, ref screenShotRect, out NodeListStageInfo);
//    }
//
//    public static void DisableNodePostProcess(List<RectTransform> _gameObjectList, RawImage rawImage, NodeListStageInfo nodeListStageInfo)
//    {
//        RestoreNodeListInfo(_gameObjectList, nodeListStageInfo);
//
//        if(rawImage.IsNotNull())
//        {
//            rawImage.texture = null;
//            rawImage.gameObject.SetActive(false);
//        }
//    }
//
//    Shader BlurShader;
//    Material BlurMaterial;
//
//    Material material
//    {
//        get
//        {
//            if (BlurMaterial == null)
//            {
//                BlurMaterial = new Material(BlurShader);
//                BlurMaterial.hideFlags = HideFlags.HideAndDontSave;
//            }
//            return BlurMaterial;
//        }
//    }
//
//    private static readonly List<NodePostProcessManager> postProcessManagerList = new List<NodePostProcessManager>();
//    public static NodePostProcessManager Current => postProcessManagerList.Count == 0 ? null : postProcessManagerList[0];
//
//    private void OnEnable()
//    {
//        BlurShader = Shader.Find("UI/NodeBlurEffect");
//        postProcessManagerList.Add(this);
//    }
//
//    private void OnDisable()
//    {
//        postProcessManagerList.Remove(this);
//    }
//
//}
