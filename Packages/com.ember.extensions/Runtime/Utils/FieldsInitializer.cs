//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using UnityEngine;
//
//#if UNITY_EDITOR
//namespace Burner.Extensions
//{
//    [Obsolete("check follow document to know why it's obsoleted: https://burner.feishu.cn/docs/doccn7VGRMW54iFNm6CK8JeSq7b")]
//    public class FieldsInitialValueAttribute : Attribute
//    {
//        public readonly Type InitType;
//        public FieldsInitialValueAttribute(Type initType)
//        {
//            InitType = initType;
//        }
//    }
//
//    /// <summary>
//    /// https://burner.feishu.cn/docs/doccn7VGRMW54iFNm6CK8JeSq7b
//    ///
//    /// You can use this FieldsInitializer utility to set the MonoBehaviour's SerializeField by Reset function.
//    /// The purpose is to reduce duplicated work about setting to different default value that each project needed.
//    ///
//    /// To call FieldsInitializer.OnTypeReset(this) in MonoBehaviour.Reset method (or other places),
//    /// it will search all types with [public static field] and [FieldsInitialValueAttribute] in C# all assemblies,
//    /// and set them by same name for initializing when you attach MonoBehaviour to GameObject.
//    ///
//    /// About MonoBehaviour.Reset method please visit:
//    /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Reset.html
//    ///
//    ///
//    /// For example.
//    ///
//    /// class SomeClass : MonoBehaviour
//    /// {
//    ///     [SerializeField]
//    ///     private float sameFloatValue = 1f;
//    ///     ...
//    ///
//    ///     protected override void Reset()
//    ///     {
//    ///         FieldsInitializer.OnTypeReset(this);
//    ///     }
//    /// }
//    ///
//    /// Project A:
//    ///
//    /// public static class IWantToInitializeSomeValue
//    /// {
//    ///     [FieldsInitialValueAttribute(typeof(SomeClass))]
//    ///     public static float sameFloatValue = 0.8f;
//    /// }
//    ///
//    /// ------------------------------------------------------------
//    ///
//    /// Project B:
//    ///
//    /// public static class IWantToInitializeSomeValue
//    /// {
//    ///     [FieldsInitialValueAttribute(typeof(SomeClass))]
//    ///     public static float sameFloatValue = 1.5f;
//    /// }
//    ///
//    /// </summary>
//    [Obsolete("check follow document to know why it's obsoleted: https://burner.feishu.cn/docs/doccn7VGRMW54iFNm6CK8JeSq7b")]
//    public static class FieldsInitializer
//    {
//        private static Dictionary<Type, List<Tuple<FieldInfo, object>>> _fields = null;
//
//        public static void OnTypeReset(MonoBehaviour mono)
//        {
//            if(_fields == null)
//            {
//                _fields = new Dictionary<Type, List<Tuple<FieldInfo,object>>>();
//
//                bool setFailed = false;
//
//                AppDomain.CurrentDomain.GetAssemblies()
//                .SelectMany(t => t.GetTypes())
//                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
//                .Where(f => f.IsDefined(typeof(FieldsInitialValueAttribute)) || f.IsDefined(typeof(SerializeField)))
//                .Select(f => (f, f.GetCustomAttributes().First(a => a is FieldsInitialValueAttribute) as FieldsInitialValueAttribute))
//                .ForEach(fa =>
//                {
//                    var fields = fa.Item2.InitType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//                    Tuple<FieldInfo, object> fv = null;
//                    var field = fields.FirstOrDefault(f => f.Name == fa.f.Name);
//                    if(field != null)
//                    {
//                        var val = fa.f.GetValue(null);
//                        if(val.GetType() == field.FieldType)
//                        {
//                            fv = new Tuple<FieldInfo, object>(field, fa.f.GetValue(null));
//                        }
//                        else
//                        {
//                            setFailed = true;
//                            Debug.LogError($"[Burner]: FieldsInitializer get error value type for '{fa.Item2.InitType}.{fa.f.Name}', " +
//                                $"original type is {field.FieldType}, but get value is {val.GetType()}");
//                        }
//
//                    }
//                    else
//                    {
//                        setFailed = true;
//                        Debug.LogError($"[Burner]: Cannot find FieldsInitializer with type '{fa.Item2.InitType}' and field '{fa.f.Name}', " +
//                            $"please check the spell.");
//                    }
//
//                    if(fv != null)
//                    {
//                        if(_fields.TryGetValue(fa.Item2.InitType, out var list))
//                        {
//                            list.Add(fv);
//                        }
//                        else
//                        {
//                            _fields.Add(fa.Item2.InitType, new List<Tuple<FieldInfo, object>> { fv });
//                        }
//                    }
//                });
//
//                if(setFailed)
//                {
//                    _fields = null;
//                }
//            }
//
//            if(_fields != null)
//            {
//                Type type = mono.GetType();
//                if(_fields.TryGetValue(type, out var list))
//                {
//                    list.ForEach(fv =>
//                    {
//                        fv.Item1.SetValue(mono, fv.Item2);
//                        Debug.Log($"[Burner]: FieldsInitializer set {type}.{fv.Item1.Name} as {fv.Item2}");
//                    });
//                }
//            }
//        }
//    }
//}
//#endif
