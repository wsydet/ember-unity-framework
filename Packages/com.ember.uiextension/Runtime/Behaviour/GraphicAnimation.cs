// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 图形序列帧/材质动画组件。
    /// 在 Update 中将 Renderer 的 MaterialPropertyBlock 中的动画属性值同步到 Graphic 的材质上，
    /// 使 Animator / Animation 可以通过 MaterialPropertyBlock 驱动 UI 材质的 Shader 属性变化。
    ///
    /// <para>使用场景：UI 元素的 Shader 特效动画（溶解、扫光、扰动等），
    /// 在 Animation Clip 中 K 帧驱动 MaterialPropertyBlock → 本组件每帧同步到 Graphic.materialForRendering。</para>
    /// </summary>
    [AddComponentMenu("UI/Ember/Graphic Animation")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic), typeof(Renderer))]
    public class EmberGraphicAnimation : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField]
        [HideInInspector]
        private Material _material;

        [SerializeField]
        [LabelText("动画属性列表")]
        [Tooltip("需要从 MaterialPropertyBlock 同步到材质上的 Shader 属性")]
        private AnimatableProperty[] _animatedProperties = new AnimatableProperty[0];

#if UNITY_EDITOR
        /// <summary>编辑器下关联的 AnimationClip（仅展示用）</summary>
        public AnimationClip[] EditorClips;
#endif

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private Graphic _graphic;
        private Renderer _renderer;
        private Material _instanceMat;
        private MaterialPropertyBlock _mpb;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _renderer = GetComponent<Renderer>();
            _renderer.enabled = false;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (_graphic == null)
            {
                _graphic = GetComponent<Graphic>();
                _renderer = GetComponent<Renderer>();
                _renderer.enabled = false;
            }

            if (_material == null && _graphic.material != null && !_graphic.material.name.Contains("Clone"))
                Material = _graphic.material;
            else
#endif
                Bind();
        }

        private void OnDestroy()
        {
            if (_instanceMat != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_instanceMat);
#else
                Destroy(_instanceMat);
#endif
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (_graphic == null)
                return;
#endif
            if (_graphic == null)
                return;

            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_mpb);
            foreach (var prop in _animatedProperties)
            {
                CopyMaterialProperty(_graphic.materialForRendering, ref _mpb, prop);
            }

            _graphic.SetMaterialDirty();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>动画材质。设置后自动 Instantiate 并绑定到 Graphic。</summary>
        public Material Material
        {
            get => _material;
            set
            {
                if (_material == value)
                    return;
                _material = value;
                Bind();
            }
        }

        /// <summary>动画属性列表</summary>
        public AnimatableProperty[] AnimatedProperties
        {
            get => _animatedProperties;
            set => _animatedProperties = value;
        }

        /// <summary>检查指定材质是否为当前使用的实例材质</summary>
        public bool IsValidMaterial(Material mat)
        {
            return mat == _instanceMat;
        }

        /// <summary>重新绑定材质：销毁旧实例，Instantiate 新材质并应用到 Graphic 和 Renderer。</summary>
        public void Bind()
        {
            if (_instanceMat != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_instanceMat);
#else
                Destroy(_instanceMat);
#endif
            }

            if (_material == null)
            {
                _instanceMat = null;
            }
            else
            {
                _instanceMat = Instantiate(_material);
            }

            _graphic.material = _instanceMat;
            _renderer.sharedMaterial = _instanceMat;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void CopyMaterialProperty(Material mat, ref MaterialPropertyBlock mpb, AnimatableProperty property)
        {
            var id = property.PropertyId;
            switch (property.Type)
            {
                case ShaderPropertyType.Color:
                    mat.SetColor(id, mpb.GetColor(id));
                    break;
                case ShaderPropertyType.Vector:
                    mat.SetVector(id, mpb.GetVector(id));
                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    mat.SetFloat(id, mpb.GetFloat(id));
                    break;
                case ShaderPropertyType.Texture:
                    mat.SetTexture(id, mpb.GetTexture(id));
                    break;
            }
        }

        #endregion
    }
}
