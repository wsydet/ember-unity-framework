// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using TMPro;
using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// FPS 显示组件 —— 挂到任意节点上，逐帧把其子节点上的 TextMeshProUGUI 刷新为当前帧率。
    ///
    /// <para>用环形缓冲区计算最近 <see cref="_avgFrameNum"/> 帧的平均 FPS；
    /// 预缓存 0~999 的字符串，热路径（Update）零 GC 分配。</para>
    /// </summary>
    public class FPSDisplay : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField, Range(10, 120)] private int _avgFrameNum = 60;
        [SerializeField] private TextMeshProUGUI _fpsText;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private const int FPS_NUMBER_LIMIT = 1000;

        private double[] _lastTimeArr;
        private int _curFrameIndex;
        private int _sampleCount;
        private bool _hasFrameBoundary;
        public float FramesPerSecond { get; private set; }
        private string[] _fpsNumbers;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            if (!_fpsText)
                _fpsText = GetComponentInChildren<TextMeshProUGUI>(true);

            // 初始化环形缓冲：先填满当前时间，避免首帧算出离谱的 FPS
            _lastTimeArr = new double[_avgFrameNum];
            for (int i = 0; i < _avgFrameNum; i++)
                _lastTimeArr[i] = Time.realtimeSinceStartupAsDouble;

            // 预缓存 0~999 的字符串（零 GC 热路径）
            _fpsNumbers = new string[FPS_NUMBER_LIMIT];
            for (int i = 0; i < FPS_NUMBER_LIMIT; i++)
                _fpsNumbers[i] = i.ToString();

            if (_fpsText)
                _fpsText.text = "...";
        }

        private void Update()
        {
            if (!_fpsText) return;
            if(!_hasFrameBoundary)
            {
                _hasFrameBoundary=true;
                for(int i=0;i<_lastTimeArr.Length;i++)_lastTimeArr[i]=Time.realtimeSinceStartupAsDouble;
                return;
            }

            // 环形缓冲：写入当前帧时间，计算最近 _avgFrameNum 帧的总耗时
            _curFrameIndex = (_curFrameIndex + 1) % _lastTimeArr.Length;
            double currentFrameTime = Time.realtimeSinceStartupAsDouble;
            double totalTimeOverWindow = currentFrameTime - _lastTimeArr[_curFrameIndex];
            _lastTimeArr[_curFrameIndex] = currentFrameTime;

            if (totalTimeOverWindow <= 0) return;

            _sampleCount = Mathf.Min(_sampleCount + 1, _lastTimeArr.Length);
            float fps = (float)(_sampleCount / totalTimeOverWindow);
            FramesPerSecond = fps;

            int fpsIndex = Mathf.FloorToInt(fps);
            if (fpsIndex >= FPS_NUMBER_LIMIT) fpsIndex = FPS_NUMBER_LIMIT - 1;
            else if (fpsIndex < 0) fpsIndex = 0;

            _fpsText.text = _fpsNumbers[fpsIndex];
            _fpsText.color = GetFpsColor(fps);
        }

        private void OnEnable()
        {
            if (_lastTimeArr == null) return;
            _sampleCount = 0; _curFrameIndex = 0; FramesPerSecond = 0; _hasFrameBoundary=false;
            for (int i = 0; i < _lastTimeArr.Length; i++) _lastTimeArr[i] = Time.realtimeSinceStartupAsDouble;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static Color GetFpsColor(float fps)
        {
            if (fps >= 60f) return Color.green;
            if (fps >= 30f) return Color.yellow;
            return Color.red;
        }

        #endregion
    }
}
