using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>Image/Sprite 地址加载。首次 inactive Bind 可加载；Hide 后需 OnShow 显式重发。</summary>
    [DisallowMultipleComponent,RequireComponent(typeof(Image))]
    [AddComponentMenu("UI/EUI/Image Loader")]
    [EUIExtension(typeof(EUIImageLoader),AutoDetect=false)]
    public sealed class EUIImageLoader:MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Sprite _placeholder;
        [SerializeField] private Sprite _failureImage;
        [SerializeField] private EUIImageLoadingDisplay _loadingDisplay;
        [SerializeField] private bool _keepPreviousOnFailure;
        [SerializeField] private bool _keepNativeSize;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Image _image;
        private EUIAssetImageSlot<Sprite> _state;
        private Sprite _expectedSprite,_expectedOverride;
        private bool _writing,_awake;
        public Sprite Placeholder {get=>_placeholder;set{_placeholder=value;Configure();}}
        public Sprite FailureImage {get=>_failureImage;set{_failureImage=value;Configure();}}
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
            _image=GetComponent<Image>();Remember();
            _state=new EUIAssetImageSlot<Sprite>(this,()=>_image?_image.overrideSprite:null,Write,Detach,External,()=>_image is EUIImageEx ex && ex.Animated ? "Animated ImageEx cannot load an asynchronous image. Set Animated=false first.":null,_awake);
            _image.RegisterDirtyVerticesCallback(OnDirty);Configure();
        }
        private void Configure(){if(_state==null)return;_state.Placeholder=_placeholder;_state.Failure=_failureImage;_state.LoadingDisplay=_loadingDisplay;_state.KeepPreviousOnFailure=_keepPreviousOnFailure;_state.KeepNativeSize=_keepNativeSize;}
        private void Remember(){_expectedSprite=_image.sprite;_expectedOverride=_image.overrideSprite;}
        private bool External()
        {
            if(_writing || !_image)return false;
            bool changed=_expectedSprite!=_image.sprite || _expectedOverride!=_image.overrideSprite;
            if(changed)Remember();return changed;
        }
        private void OnDirty(){if(!_writing)_state?.CheckManual();}
        private void Detach(Sprite owned)
        {
            if(!_image || !owned)return;_writing=true;
            try{if(_image.overrideSprite==owned)_image.overrideSprite=null;if(_image.sprite==owned)_image.sprite=_placeholder;Remember();}
            finally{_writing=false;}
        }
        private void Write(Sprite sprite,bool native)
        {
            if(!_image)return;_writing=true;
            try{_image.overrideSprite=null;_image.sprite=sprite;if(native)_image.SetNativeSize();Remember();}
            finally{_writing=false;}
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public long Load(string path,Action<EUIImageLoadResult> completed=null){Ensure();Configure();return _state.Load(path,completed);}
        public void CancelLoading(){_state?.CancelLoading();}
        /// <summary>可复用；先卸下显示引用再释放。池借用绑定时使用 lease.Track(loader.Clear)。</summary>
        public void Clear(){_state?.Retire(EUIImageLoadStatus.Cleared,true);}
        /// <summary>借入手动图并接管，包括相同值。调用方须持有自己的有效资源引用。</summary>
        public void SetSprite(Sprite borrowed){Ensure();_state.SetBorrowed(borrowed);}
        #endregion
    }
}
