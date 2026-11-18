using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>RawImage/Texture 地址加载，借入占位/失败/手动图不销毁。</summary>
    [DisallowMultipleComponent,RequireComponent(typeof(RawImage))]
    [AddComponentMenu("UI/EUI/Raw Image Loader")]
    [EUIExtension(typeof(EUIRawImageLoader),AutoDetect=false)]
    public sealed class EUIRawImageLoader:MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Texture _placeholder;
        [SerializeField] private Texture _failureTexture;
        [SerializeField] private EUIImageLoadingDisplay _loadingDisplay;
        [SerializeField] private bool _keepPreviousOnFailure;
        [SerializeField] private bool _keepNativeSize;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private RawImage _image;
        private EUIAssetImageSlot<Texture> _state;
        private Texture _expected;
        private bool _writing,_awake;
        public Texture Placeholder {get=>_placeholder;set{_placeholder=value;Configure();}}
        public Texture FailureTexture {get=>_failureTexture;set{_failureTexture=value;Configure();}}
        public EUIImageLoadingDisplay LoadingDisplay {get=>_loadingDisplay;set{_loadingDisplay=value;Configure();}}
        public bool KeepPreviousOnFailure {get=>_keepPreviousOnFailure;set{_keepPreviousOnFailure=value;Configure();}}
        public bool KeepNativeSize {get=>_keepNativeSize;set{_keepNativeSize=value;Configure();}}
        public bool IsLoading=>_state?.IsLoading ?? false;
        public string CurrentPath=>_state?.CurrentPath;
        public string LoadingPath=>_state?.LoadingPath;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake(){_awake=true;_state?.MarkAwake();}
        private void LateUpdate(){_state?.CheckManual();}
        private void OnDisable(){_state?.Retire(EUIImageLoadStatus.Disabled,false);}
        private void OnDestroy(){if(_image)_image.UnregisterDirtyVerticesCallback(OnDirty);_state?.Destroyed();}
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Ensure()
        {
            if(_state!=null)return;
            _image=GetComponent<RawImage>();_expected=_image.texture;
            _state=new EUIAssetImageSlot<Texture>(this,()=>_image?_image.texture:null,Write,Detach,External,()=>null,_awake);
            _image.RegisterDirtyVerticesCallback(OnDirty);Configure();
        }
        private void Configure(){if(_state==null)return;_state.Placeholder=_placeholder;_state.Failure=_failureTexture;_state.LoadingDisplay=_loadingDisplay;_state.KeepPreviousOnFailure=_keepPreviousOnFailure;_state.KeepNativeSize=_keepNativeSize;}
        private bool External(){if(_writing || !_image)return false;bool changed=_image.texture!=_expected;if(changed)_expected=_image.texture;return changed;}
        private void OnDirty(){if(!_writing)_state?.CheckManual();}
        private void Detach(Texture owned){if(_image && _image.texture==owned)Write(_placeholder,false);}
        private void Write(Texture texture,bool native)
        {if(!_image)return;_writing=true;try{_image.texture=texture;if(native)_image.SetNativeSize();_expected=_image.texture;}finally{_writing=false;}}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public long Load(string path,Action<EUIImageLoadResult> completed=null){Ensure();Configure();return _state.Load(path,completed);}
        public void CancelLoading(){_state?.CancelLoading();}
        public void Clear(){_state?.Retire(EUIImageLoadStatus.Cleared,true);}
        public void SetTexture(Texture borrowed){Ensure();_state.SetBorrowed(borrowed);}
        #endregion
    }
}
