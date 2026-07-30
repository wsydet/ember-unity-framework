////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEngine;
////using UnityEditor;
////using System.Linq;
////using System.Collections.Generic;
////using UnityEngine.UI;
////using UnityEngine.Profiling;
////using static Burner.AnimatableProperty;
////using UnityEngine.Rendering;
////
////namespace Burner.Editor
////{
////	[CustomEditor(typeof(GraphicAnimation))]
////	public class GraphicAnimationEditor : UnityEditor.Editor
////	{
////		SerializedProperty m_spAnimatedProperties;
////        SerializedProperty m_Material;
////        //private AnimationClip m_Clip;
////        Dictionary<string, ShaderPropertyType> names;
////        HashSet<string> allBindings;
////        List<AnimatableProperty> animPropertys;
////        private void OnEnable()
////		{
////            m_spAnimatedProperties = serializedObject.FindProperty("m_AnimatedProperties");
////            m_Material = serializedObject.FindProperty("m_Material");
////            names = new Dictionary<string, ShaderPropertyType>();
////            allBindings = new HashSet<string>();
////            animPropertys = new List<AnimatableProperty>();
////            // m_Clip = (target as GraphicAnimation).m_Clip;
////        }
////
////        public override void OnInspectorGUI()
////        {
////            var imageAnim = target as GraphicAnimation;
////            var mat = EditorGUILayout.ObjectField("Material", imageAnim.material, typeof(Material), false ) as Material;
////            if (imageAnim.material != mat)
////                imageAnim.material = mat;
////            else if(!imageAnim.IsValidmatMaterial(imageAnim.GetComponent<Graphic>().material) && imageAnim.isActiveAndEnabled)
////                imageAnim.Bind();
////            base.OnInspectorGUI();
////            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
////            {
////                //(target as GraphicAnimation).m_Clip = EditorGUILayout.ObjectField("AnimationClip", m_Clip, typeof(AnimationClip), true) as AnimationClip;
////                Profiler.BeginSample("_genAnimationProperties");
////                _genAnimationProperties();
////                Profiler.EndSample();
////            }
////            imageAnim.Update();
////        }
////
////       private void _genAnimationProperties()
////        {
////			var imageAnim = target as GraphicAnimation;
////            var clips = (target as GraphicAnimation).m_Clip;
////            if (clips == null || clips.Length == 0)
////                return;
////            var mat = imageAnim.material;
////            if (mat == null)
////                return;
////            names.Clear();
////
////            for (int i = 0; i < ShaderUtil.GetPropertyCount(mat.shader); ++i)
////            {
////                var name = ShaderUtil.GetPropertyName(mat.shader, i);
////                var type = (UnityEngine.Rendering.ShaderPropertyType)ShaderUtil.GetPropertyType(mat.shader, i);
////
////                if (!names.ContainsKey(name))
////                    names.Add(name, type);
////                if (type == ShaderPropertyType.Texture)
////                {
////                    name += "_ST";
////                    if (!names.ContainsKey(name))
////                        names.Add(name, ShaderPropertyType.Vector);
////                }
////            }
////            allBindings.Clear();
////             foreach (var clip in clips)
////            {
////                if (clip == null)
////                    continue;
////                var bindings = AnimationUtility.GetCurveBindings(clip)
////               .Where(b => string.IsNullOrEmpty(b.path) || b.path.EndsWith(imageAnim.name))
////               .Select(b => b.propertyName.Replace("material.", "").Split('.')[0])
////               .Distinct();
////                foreach (var bind in bindings)
////                    allBindings.Add(bind);
////            }
////            animPropertys.Clear();
////            foreach (var it in names)
////            {
////                var name = it.Key;
////                if (allBindings.Contains(name))
////                {
////                    animPropertys.Add( new AnimatableProperty { m_Name = name, m_Type = it.Value });
////                }
////            }
////            animPropertys.Sort((a, b)=>
////            {
////                return string.Compare(a.m_Name, b.m_Name);
////            });
////            var same = true;
////            if (animPropertys.Count != imageAnim.m_AnimatedProperties.Length)
////                same = false;
////            else
////            {
////                for (var i = 0; i < imageAnim.m_AnimatedProperties.Length; i++)
////                {
////                    var oldProprty = imageAnim.m_AnimatedProperties[i];
////                    var newProprty = animPropertys[i];
////                    if ((oldProprty.m_Name != newProprty.m_Name) || (oldProprty.type != newProprty.m_Type))
////                    {
////                        same = false;
////                        break;
////                    }
////                }
////            }
////            if(!same)
////                imageAnim.m_AnimatedProperties = animPropertys.ToArray();
////            serializedObject.ApplyModifiedProperties();
////        }
////
////        private void _appendAnimationProperty(string name, int type)
////        {
////            int index = m_spAnimatedProperties.arraySize;
////            m_spAnimatedProperties.InsertArrayElementAtIndex(index);
////            var sp = m_spAnimatedProperties.GetArrayElementAtIndex(index);
////            sp.FindPropertyRelative("m_Name").stringValue = name;
////            sp.FindPropertyRelative("m_Type").intValue = (int)type;
////        }
////	}
////}
