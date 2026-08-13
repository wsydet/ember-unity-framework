// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.Rendering;

namespace Ember.UIExtension
{
    /// <summary>
    /// 可动画化 Shader 属性描述。
    /// 指定一个 Shader 属性的名称和类型，配合 <see cref="EUIGraphicAnimation"/> 在运行时同步材质属性。
    /// </summary>
    [Serializable]
    public class AnimatableProperty
    {
        #region 编辑器面板参数

        [SerializeField]
        [LabelText("属性名")]
        [Tooltip("Shader 属性名称，如 _MainTex、_Color")]
        private string _name = string.Empty;

        [SerializeField]
        [LabelText("属性类型")]
        private ShaderPropertyType _type = ShaderPropertyType.Vector;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private int _id = -1;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>Shader 属性名称</summary>
        public string Name => _name;

        /// <summary>Shader 属性类型</summary>
        public ShaderPropertyType Type => _type;

        /// <summary>Shader.PropertyToID 缓存值</summary>
        public int PropertyId
        {
            get
            {
                if (_id <= 0)
                    _id = Shader.PropertyToID(_name);
                return _id;
            }
        }

        #endregion
    }
}
