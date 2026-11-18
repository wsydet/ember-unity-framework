using System;
using Ember.Basic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>单行往返跑马灯；只位移公开 TMP 网格，不改布局、字体、材质或 Shader。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(TextMeshProUGUI))]
    [AddComponentMenu("UI/EUI/TMP Marquee")]
    [EUIExtension(typeof(TMPMarquee), AutoDetect=false)]
    public sealed class TMPMarquee : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private bool _marqueeEnabled=true;
        [SerializeField, Min(0)] private float _speed=30;
        [SerializeField, Min(0)] private float _startDelay=0.5f;
        [SerializeField, Min(0)] private float _endDelay=0.5f;
        [SerializeField] private bool _useUnscaledTime=true;
        [SerializeField] private RectTransform _viewport;
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        private TextMeshProUGUI _text;
        private Vector3[][] _baseline=Array.Empty<Vector3[]>();
        private int[] _counts=Array.Empty<int>();
        private readonly Vector3[] _corners=new Vector3[4];
        private bool _dirty=true, _subscribed, _overflow, _returning, _captured;
        private float _distance, _travel, _baseShift, _pause;
        private Rect _window;
        private Behaviour _clip;
        private string _diagnostic, _lastWarning;
        private TextMeshProUGUI Text => _text ? _text : _text=GetComponent<TextMeshProUGUI>();
        public bool IsRunning => isActiveAndEnabled && _marqueeEnabled && _overflow && _diagnostic==null && Speed>0;
        public float Speed { get=>NonNegative(_speed); set=>_speed=NonNegative(value); }
        public float StartDelay { get=>_startDelay; set=>_startDelay=NonNegative(value); }
        public float EndDelay { get=>_endDelay; set=>_endDelay=NonNegative(value); }
        public bool UseUnscaledTime { get=>_useUnscaledTime; set=>_useUnscaledTime=value; }
        public RectTransform Viewport { get=>_viewport; set { _viewport=value;Refresh(); } }
        public string Diagnostic => _diagnostic;
        #endregion

        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable() { Subscribe();Refresh(); }
        private void OnDisable() { Unsubscribe();Restore();_overflow=false; }
        private void OnDestroy() { Unsubscribe();_baseline=Array.Empty<Vector3[]>();_counts=Array.Empty<int>(); }
        private void OnRectTransformDimensionsChange() { _dirty=true; }
        private void OnTransformParentChanged() { _dirty=true; }
        private void OnValidate() { _dirty=true; }
        private void LateUpdate()
        {
            if(!_marqueeEnabled || !Text.isActiveAndEnabled)return;
            var window=GetWindow();
            if(window!=_window || Text.havePropertiesChanged)_dirty=true;
            if(_dirty)Rebuild();
            var problem=ConfigurationProblem();
            if(problem!=null || !_clip || !_clip.isActiveAndEnabled)
            {
                if(_captured)Apply(0);
                Diagnose(problem ?? "需要启用的祖先 Mask 或 RectMask2D。");return;
            }
            if(_diagnostic!=null || !_overflow)return;
            float dt=_useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime;
            float speed=Speed;
            if(speed<=0)return;
            if(_pause>0){_pause=Mathf.Max(0,_pause-dt);return;}
            _travel=Mathf.MoveTowards(_travel,_returning?0:_distance,speed*dt);
            if(_returning && _travel<=0){_returning=false;_pause=NonNegative(_startDelay);}
            else if(!_returning && _travel>=_distance){_returning=true;_pause=NonNegative(_endDelay);}
            Apply(_baseShift-_travel);
        }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private float NonNegative(float value) => float.IsFinite(value)?Mathf.Max(0,value):0;
        private void Subscribe() { if(_subscribed)return;Text.OnPreRenderText+=Capture;_subscribed=true; }
        private void Unsubscribe() { if(!_subscribed)return;if(_text)_text.OnPreRenderText-=Capture;_subscribed=false; }
        private Rect GetWindow()
        {
            var rect=Text.rectTransform.rect;var margin=Text.margin;
            rect.xMin+=margin.x;rect.xMax-=margin.z;
            if(_viewport && Text.transform.IsChildOf(_viewport))
            {
                _viewport.GetWorldCorners(_corners);float min=float.PositiveInfinity,max=float.NegativeInfinity;
                for(int i=0;i<4;i++){float x=Text.transform.InverseTransformPoint(_corners[i]).x;min=Mathf.Min(min,x);max=Mathf.Max(max,x);}
                rect.xMin=Mathf.Max(rect.xMin,min);rect.xMax=Mathf.Min(rect.xMax,max);
            }
            return rect;
        }
        private string ConfigurationProblem()
        {
            if(Text.enableAutoSizing)return "跑马灯需要关闭 AutoSize。";
            if(Text.textWrappingMode!=TextWrappingModes.NoWrap && Text.textWrappingMode!=TextWrappingModes.PreserveWhitespaceNoWrap)return "跑马灯需要 NoWrap。";
            if(Text.overflowMode!=TextOverflowModes.Overflow)return "跑马灯需要 Overflow 模式。";
            if(Text.isRightToLeftText)return "跑马灯不支持 RTL。";
            if(_viewport && !Text.transform.IsChildOf(_viewport))return "Viewport 必须是文本的祖先裁剪节点。";
            return null;
        }
        private void FindClip()
        {
            _clip=null;
            for(var parent=Text.transform.parent;parent;parent=parent.parent)
            {
                if(_viewport && parent!=_viewport)continue;
                var rectMask=parent.GetComponent<RectMask2D>();if(rectMask && rectMask.isActiveAndEnabled){_clip=rectMask;break;}
                var mask=parent.GetComponent<Mask>();if(mask && mask.isActiveAndEnabled){_clip=mask;break;}
                if(_viewport)break;
            }
        }
        private void Diagnose(string problem)
        {
            _diagnostic=problem;
            if(problem==null){_lastWarning=null;return;}
            if(problem==_lastWarning)return;
            _lastWarning=problem;EmberDebug.LogWarning(LogTags.UIManager,$"TMPMarquee ({name}): {problem}");
        }
        private void Rebuild()
        {
            Subscribe();
            _dirty=false;FindClip();_window=GetWindow();
            Text.ForceMeshUpdate(true);
        }
        private void Capture(TMP_TextInfo info)
        {
            int count=info.materialCount;
            if(_baseline.Length!=count){Array.Resize(ref _baseline,count);Array.Resize(ref _counts,count);}
            for(int m=0;m<count;m++)
            {
                var mesh=info.meshInfo[m];int n=mesh.vertexCount;
                if(_baseline[m]==null || _baseline[m].Length<n)_baseline[m]=new Vector3[n];
                _counts[m]=n;Array.Copy(mesh.vertices,_baseline[m],n);
            }
            _captured=true;_travel=0;_returning=false;_pause=NonNegative(_startDelay);
            _window=GetWindow();FindClip();
            var problem=ConfigurationProblem();
            if(problem==null && info.lineCount>1)problem="跑马灯仅支持单行文本。";
            if(problem==null && (!_clip || !_clip.isActiveAndEnabled))problem="需要启用的祖先 Mask 或 RectMask2D。";
            Diagnose(problem);
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            for(int i=0;i<info.characterCount;i++)
            {
                var ch=info.characterInfo[i];if(!ch.isVisible)continue;
                var vertices=_baseline[ch.materialReferenceIndex];
                for(int k=0;k<4;k++){float x=vertices[ch.vertexIndex+k].x;min=Mathf.Min(min,x);max=Mathf.Max(max,x);}
            }
            if(info.lineCount==1 && float.IsFinite(min))max=Mathf.Max(max,info.lineInfo[0].maxAdvance);
            float width=max-min;
            _overflow=problem==null && float.IsFinite(width) && _window.width>0 && width>_window.width+0.5f;
            _distance=_overflow?width-_window.width:0;_baseShift=_overflow?_window.xMin-min:0;
            if(_marqueeEnabled && isActiveAndEnabled && _overflow)ApplyTo(info,_baseShift);
        }
        private void ApplyTo(TMP_TextInfo info,float shift)
        {
            for(int m=0;m<_baseline.Length && m<info.materialCount;m++)
            {
                var vertices=info.meshInfo[m].vertices;
                int count=Mathf.Min(_counts[m],vertices.Length);
                for(int i=0;i<count;i++){vertices[i]=_baseline[m][i];vertices[i].x+=shift;}
            }
        }
        private void Apply(float shift)
        {
            if(!_captured || !_text)return;
            ApplyTo(Text.textInfo,shift);Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
        private void Restore()
        {
            if(!_text)return;
            // 有更新时生成当前原始几何，不能恢复旧文本/字体。
            if(Text.havePropertiesChanged){Text.ForceMeshUpdate(true);_captured=false;}
            else Apply(0);
            _travel=0;_pause=NonNegative(_startDelay);
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>重复调用仍更新速度；速度为零暂停。前置：单行 NoWrap/Overflow、关闭 AutoSize、祖先裁剪。</summary>
        public void SetMarqueeEnabled(bool enabled,float speed=30)
        {
            Speed=speed;
            if(_marqueeEnabled==enabled)return;
            _marqueeEnabled=enabled;
            if(enabled)Refresh();else Restore();
        }
        public void ResetPosition() { _travel=0;_returning=false;_pause=NonNegative(_startDelay);if(_captured)Apply(_marqueeEnabled && _overflow?_baseShift:0); }
        public void Refresh() { _dirty=true;if(isActiveAndEnabled && _marqueeEnabled)Rebuild(); }
        #endregion
    }
}
