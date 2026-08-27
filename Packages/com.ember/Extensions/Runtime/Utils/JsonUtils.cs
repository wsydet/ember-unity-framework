// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember
// Migrated from Burner extensions with cleanup.

using Ember.Basic;
using Ember.Basic.LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Attribute = System.Attribute;

namespace Ember.Extensions
{
    public class JsonProp : Attribute
    {
        public readonly string Name;
        public JsonProp(string name = null) { Name = name; }
    }

    /// <summary>
    /// JSON serialization/deserialization utility based on LitJson with reflection support.
    /// Uses [JsonProp] attribute for opt-in field/property serialization.
    /// </summary>
    public static class JsonUtils
    {
        private const string TAG = LogTags.ExtensionJsonUtils;

        public static readonly BindingFlags BindingFlagsMask =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private static readonly Type DictType = typeof(Dictionary<string, object>);
        private static readonly Type ObjectType = typeof(System.Object);

        private static readonly Dictionary<Type, MethodInfo[]>[] s_callbackDict =
        {
            new Dictionary<Type, MethodInfo[]>(), // OnSerializing
            new Dictionary<Type, MethodInfo[]>(), // OnSerialized
            new Dictionary<Type, MethodInfo[]>(), // OnDeserializing
            new Dictionary<Type, MethodInfo[]>(), // OnDeserialized
        };

        private static readonly Type[] s_callbackDictType =
        {
            typeof(OnSerializingAttribute),
            typeof(OnSerializedAttribute),
            typeof(OnDeserializingAttribute),
            typeof(OnDeserializedAttribute),
        };

        private struct Members
        {
            public PropertyInfo p;
            public FieldInfo f;
            public JsonProp attr;
            public Type type;
        }

        private static readonly Dictionary<Type, Members[]> s_membersDict = new Dictionary<Type, Members[]>();

        #region 内部方法

        public static void ClearReflectionCache()
        {
            foreach (var dict in s_callbackDict)
            {
                lock (dict)
                {
                    dict.Clear();
                }
            }

            lock (s_membersDict) s_membersDict.Clear();
        }

        private static void SetItemValue<T>(object obj, T v, (PropertyInfo p, FieldInfo f) item, (IList list, object set, MethodInfo set_add, Array arr, int idx) list)
        {
            if (list.list != null) list.list.Add(v);
            else if (list.set != null) list.set_add.Invoke(list.set, new object[] { v });
            else if (list.arr != null) list.arr.SetValue(v, list.idx);
            else if (item.p != null) item.p.SetValue(obj, v);
            else item.f.SetValue(obj, v);
        }

        private static string GetItemName((PropertyInfo p, FieldInfo f) item)
        {
            return item.p == null ? item.f.Name : item.p.Name;
        }

        internal static bool TrySetValue(this JsonData j, object obj, Type type,
            (PropertyInfo p, FieldInfo f) item, (IList list, object set, MethodInfo set_add, Array arr, int idx) list)
        {
            if (j.IsLong)
            {
                if (type.IsEnum)
                {
                    SetItemValue(obj, Enum.ToObject(type, (int)(long)j), item, list);
                }
                else if (type == typeof(long))
                {
                    SetItemValue(obj, (long)j, item, list);
                }
                else if (type == typeof(ulong))
                {
                    SetItemValue(obj, checked((ulong)(long)j), item, list);
                }
                else if (type == typeof(int))
                {
                    SetItemValue(obj, checked((int)(long)j), item, list);
                }
                else if (type == typeof(uint))
                {
                    SetItemValue(obj, checked((uint)(long)j), item, list);
                }
                else if (type == typeof(short))
                {
                    SetItemValue(obj, checked((short)(long)j), item, list);
                }
                else if (type == typeof(ushort))
                {
                    SetItemValue(obj, checked((ushort)(long)j), item, list);
                }
                else if (type == typeof(char))
                {
                    SetItemValue(obj, checked((char)(long)j), item, list);
                }
                else if (type == typeof(byte))
                {
                    SetItemValue(obj, checked((byte)(long)j), item, list);
                }
                else if (type == typeof(sbyte))
                {
                    SetItemValue(obj, checked((sbyte)(long)j), item, list);
                }
                else if (type == typeof(float))
                {
                    SetItemValue(obj, (float)(long)j, item, list);
                }
                else if (type == typeof(double))
                {
                    SetItemValue(obj, (double)(long)j, item, list);
                }
                else
                {
                    EmberDebug.LogError(TAG, $"[Ember]: JsonData is long but field/property '{GetItemName(item)}' is '{type}'");
                    return false;
                }

                return true;
            }
            else if (j.IsInt)
            {
                if (type.IsEnum)
                {
                    SetItemValue(obj, Enum.ToObject(type, (int)j), item, list);
                }
                else if (type == typeof(long))
                {
                    SetItemValue(obj, (long)(int)j, item, list);
                }
                else if (type == typeof(ulong))
                {
                    SetItemValue(obj, checked((ulong)(int)j), item, list);
                }
                else if (type == typeof(int))
                {
                    SetItemValue(obj, (int)j, item, list);
                }
                else if (type == typeof(uint))
                {
                    SetItemValue(obj, checked((uint)(int)j), item, list);
                }
                else if (type == typeof(short))
                {
                    SetItemValue(obj, checked((short)(int)j), item, list);
                }
                else if (type == typeof(ushort))
                {
                    SetItemValue(obj, checked((ushort)(int)j), item, list);
                }
                else if (type == typeof(char))
                {
                    SetItemValue(obj, checked((char)(int)j), item, list);
                }
                else if (type == typeof(byte))
                {
                    SetItemValue(obj, checked((byte)(int)j), item, list);
                }
                else if (type == typeof(sbyte))
                {
                    SetItemValue(obj, checked((sbyte)(int)j), item, list);
                }
                else if (type == typeof(float))
                {
                    SetItemValue(obj, (float)(int)j, item, list);
                }
                else if (type == typeof(double))
                {
                    SetItemValue(obj, (double)(int)j, item, list);
                }
                else
                {
                    EmberDebug.LogError(TAG, $"[Ember]: JsonData is integer but field/property '{GetItemName(item)}' is '{type}'");
                    return false;
                }

                return true;
            }
            else if (j.IsBoolean)
            {
                if (type == typeof(bool))
                {
                    SetItemValue(obj, (bool)j, item, list);
                    return true;
                }
                else
                {
                    EmberDebug.LogError(TAG, $"[Ember]: JsonData is bool but field/property '{GetItemName(item)}' is '{type}'");
                    return false;
                }
            }
            else if (j.IsString)
            {
                if (type == typeof(string))
                {
                    SetItemValue(obj, (string)j, item, list);
                }
                else
                {
                    EmberDebug.LogError(TAG, $"[Ember]: JsonData is string but field/property '{GetItemName(item)}' is '{type}'");
                    return false;
                }

                return true;
            }
            else if (j.IsDouble)
            {
                if (type == typeof(float))
                {
                    SetItemValue(obj, (float)(double)j, item, list);
                }
                else if (type == typeof(double))
                {
                    SetItemValue(obj, (double)j, item, list);
                }
                else
                {
                    EmberDebug.LogError(TAG, $"[Ember]: JsonData is double but field/property '{GetItemName(item)}' is '{type}'");
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static Members[] GetAllReflectedData(object obj)
        {
            var type = obj.GetType();
            Members[] members;

            lock (s_membersDict)
            {
                if (!s_membersDict.TryGetValue(type, out members))
                {
                    IEnumerable<Members> results = null;

                    while (type != ObjectType)
                    {
                        var props = type.GetProperties(BindingFlagsMask)
                            .Where(p => p.IsDefined(typeof(JsonProp)))
                            .Select(p =>
                                new Members
                                {
                                    p = p,
                                    f = null,
                                    attr = p.GetCustomAttributes().First(a => a is JsonProp) as JsonProp,
                                    type = p.PropertyType
                                }
                            );

                        var fields = type.GetFields(BindingFlagsMask)
                            .Where(f => f.IsDefined(typeof(JsonProp)))
                            .Select(f =>
                                new Members
                                {
                                    p = null,
                                    f = f,
                                    attr = f.GetCustomAttributes().First(a => a is JsonProp) as JsonProp,
                                    type = f.FieldType
                                }
                            );

                        var res = fields.Concat(props);
                        if (results != null)
                        {
                            res = res.Concat(results);
                        }

                        results = res;
                        type = type.BaseType;
                    }

                    members = results.Distinct().ToArray();
                    s_membersDict.Add(obj.GetType(), members);
                }
            }

            return members;
        }

        private static void CallbackInvoke<T>(object obj)
        {
            var type = obj.GetType();
            var genericType = typeof(T);
            Dictionary<Type, MethodInfo[]> dict = null;

            for (var i = 0; i < s_callbackDictType.Length; i++)
            {
                if (s_callbackDictType[i] == genericType)
                {
                    dict = s_callbackDict[i];
                    break;
                }
            }

            if (dict == null)
            {
                throw new Exception("[Ember]: Internal Error! JsonUtils missed type: " + type);
            }

            MethodInfo[] methods;

            lock (dict)
            {
                if (!dict.TryGetValue(type, out methods))
                {
                    IEnumerable<MethodInfo> preMethods = null;
                    while (type != ObjectType)
                    {
                        var md = type.GetMethods(BindingFlagsMask).Where(m => m.IsDefined(typeof(T)));
                        if (preMethods != null)
                        {
                            md = md.Concat(preMethods);
                        }
                        preMethods = md;
                        type = type.BaseType;
                    }

                    methods = preMethods.Distinct().ToArray();
                    dict.Add(obj.GetType(), methods);
                }
            }

            foreach (var m in methods)
            {
                try
                {
                    m.Invoke(obj, m.GetParameters().IsNullOrEmpty() ? null : new[] { (object)null });
                }
                catch (Exception e)
                {
                    EmberDebug.LogException(TAG,e);
                }
            }
        }

        private static object ReadToObjectImpl(JsonData json, object obj)
        {
            CallbackInvoke<OnDeserializingAttribute>(obj);

            var all = GetAllReflectedData(obj);

            static object ParseArray(Type type, JsonData data, object obj)
            {
                object setObj;

                if (type.IsArray)
                {
                    Type elemType = type.GetElementType();
                    var arr = Array.CreateInstance(elemType, data.Count);

                    for (int i = 0; i < data.Count; i++)
                    {
                        var jsonItem = data[i];
                        if (!jsonItem.TrySetValue(obj, elemType, (null, null), (null, null, null, arr, i)))
                        {
                            arr.SetValue(
                                elemType == DictType
                                    ? ReadToDictTmpl(jsonItem)
                                    : ReadToObjectImpl(jsonItem, Activator.CreateInstance(elemType)), i);
                        }
                    }

                    setObj = arr;
                }
                else if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type generic = type.GetGenericArguments()[0];
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(generic));
                    for (int i = 0; i < data.Count; i++)
                    {
                        var jsonItem = data[i];
                        if (!jsonItem.TrySetValue(obj, generic, (null, null), (list, null, null, null, 0)))
                        {
                            list.Add(generic == DictType
                                ? ReadToDictTmpl(jsonItem)
                                : ReadToObjectImpl(jsonItem, Activator.CreateInstance(generic)));
                        }
                    }

                    setObj = list;
                }
                else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    Type generic = type.GetGenericArguments()[0];
                    var set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(generic));
                    var add = set.GetType().GetMethod("Add");
                    for (int i = 0; i < data.Count; i++)
                    {
                        var jsonItem = data[i];
                        if (!jsonItem.TrySetValue(obj, generic, (null, null), (null, set, add, null, 0)))
                        {
                            add.Invoke(set, new[]
                            {
                                generic == DictType
                                    ? ReadToDictTmpl(jsonItem)
                                    : ReadToObjectImpl(jsonItem, Activator.CreateInstance(generic))
                            });
                        }
                    }

                    setObj = set;
                }
                else
                {
                    throw new Exception($"[Ember]: Error generic list type {type.GetGenericTypeDefinition()}");
                }

                return setObj;
            }

            foreach (var item in all)
            {
                var name = item.attr.Name.IsNullOrEmpty() ? (item.p == null ? item.f.Name : item.p.Name) : item.attr.Name;
                if (json.TryGetValue(name, out var data))
                {
                    if (item.type.IsGenericType || item.type.IsArray)
                    {
                        object setObj;
                        if (data.IsArray)
                        {
                            setObj = ParseArray(item.type, data, obj);
                        }
                        else if (item.type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                        {
                            if (item.type == DictType)
                            {
                                setObj = ReadToObject<Dictionary<string, object>>(data);
                            }
                            else
                            {
                                var genericList = item.type.GetGenericArguments();
                                var genericValue = genericList[1];
                                var newDict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(genericList)) as IDictionary;
                                foreach (var key in data.Keys)
                                {
                                    var jsonItem = data[key];
                                    if (jsonItem.IsArray)
                                    {
                                        newDict[key] = ParseArray(genericValue, jsonItem, null);
                                    }
                                    else
                                    {
                                        newDict[key] = genericValue == DictType
                                            ? ReadToDictTmpl(jsonItem)
                                            : ReadToObjectImpl(jsonItem, Activator.CreateInstance(genericValue));
                                    }
                                }

                                setObj = newDict;
                            }
                        }
                        else
                        {
                            throw new Exception("[Ember]: Error List<>/HashSet<>/Dictionary<,> generic type");
                        }

                        if (item.p != null) item.p.SetValue(obj, setObj);
                        else item.f.SetValue(obj, setObj);
                    }
                    else
                    {
                        if (!data.TrySetValue(obj, item.type, (item.p, item.f), (null, null, null, null, 0)))
                        {
                            SetItemValue(obj, ReadToObjectImpl(data, Activator.CreateInstance(item.type)), (item.p, item.f), (null, null, null, null, 0));
                        }
                    }
                }
            }

            CallbackInvoke<OnDeserializedAttribute>(obj);

            return obj;
        }

        private static object GetJsonDataListForDict<T>(JsonData v)
        {
            if (v.Count > 0)
            {
                var list = new T[v.Count];
                for (var i = 0; i < v.Count; i++)
                {
                    list[i] = (T)Convert.ChangeType(GetJsonDataValueForDict(v[i]), typeof(T));
                }
                return list;
            }
            return null;
        }

        private static object GetJsonDataValueForDict(JsonData v)
        {
            if (v.IsObject) return ReadToObject<Dictionary<string, object>>(v);
            else if (v.IsString) return (string)v;
            else if (v.IsBoolean) return (bool)v;
            else if (v.IsDouble) return (double)v;
            else if (v.IsInt) return (int)v;
            else if (v.IsLong) return (long)v;
            else if (v.IsArray)
            {
                if (v.Count > 0)
                {
                    var v0 = v[0];

                    if (v0.IsObject) return GetJsonDataListForDict<Dictionary<string, object>>(v);
                    else if (v0.IsString) return GetJsonDataListForDict<string>(v);
                    else if (v0.IsBoolean) return GetJsonDataListForDict<bool>(v);
                    else if (v0.IsDouble) return GetJsonDataListForDict<double>(v);
                    else if (v0.IsInt)
                    {
                        for (int i = 0; i < v.Count; i++)
                        {
                            if (v[i].IsLong) return GetJsonDataListForDict<long>(v);
                        }
                        return GetJsonDataListForDict<int>(v);
                    }
                    else if (v0.IsLong) return GetJsonDataListForDict<long>(v);
                    else if (v0.IsArray)
                    {
                        if (v0.Count > 0)
                        {
                            var list = new object[v0.Count];
                            for (var i = 0; i < v0.Count; i++)
                            {
                                list[i] = GetJsonDataValueForDict(v0[i]);
                            }
                            return list;
                        }
                    }
                    else throw new Exception("[Ember]: Error LitJson Type!");
                }
                return null;
            }
            else throw new Exception("[Ember]: Error LitJson Type!");
        }

        private static bool IsBuildInType(Type type)
        {
            return type == typeof(int) || type == typeof(uint)
                || type == typeof(long) || type == typeof(ulong)
                || type == typeof(short) || type == typeof(ushort)
                || type == typeof(bool) || type == typeof(string)
                || type == typeof(float) || type == typeof(double)
                || type == typeof(sbyte) || type == typeof(byte)
                || type == typeof(char)
                || type.IsEnum;
        }

        private static object ConvertToLitJsonSupportType(object obj)
        {
            try
            {
                if (obj is short s) return (int)s;
                if (obj is float f) return (double)f;
                if (obj is uint u) return (long)u;
                if (obj is ushort us) return (int)us;
                if (obj is ulong ul) return checked((long)ul);
                if (obj is byte b) return (int)b;
                if (obj is sbyte sb) return (int)sb;
                if (obj is char ch) return (int)ch;
                if (obj is Enum) return Convert.ToInt32(obj);
            }
            catch (OverflowException ex)
            {
                EmberDebug.LogException(TAG,ex);
                throw ex;
            }

            return obj;
        }

        private static bool TryFillArrayJsonData(Type type, object array, out JsonData jd)
        {
            jd = null;

            if (array == null || array is string)
            {
                return false;
            }

            if (array is IDictionary dict)
            {
                jd = WriteToJsonDataImpl(dict);
                return true;
            }

            if (array is IEnumerable list)
            {
                Type generic = type.IsGenericType ? type.GetGenericArguments()[0] : null;

                var enumerator = list.GetEnumerator();

                var capacity = 0;
                if (list is Array arr) capacity = arr.Length;
                else if (list is IList lst) capacity = lst.Count;
                else
                {
                    while (enumerator.MoveNext()) capacity++;
                    enumerator = list.GetEnumerator();
                }


                jd = new JsonData
                {
                    Capacity = capacity
                };

                jd.SetJsonType(JsonType.Array);

                while (enumerator.MoveNext())
                {
                    if (generic != null && IsBuildInType(generic)) jd.Add(ConvertToLitJsonSupportType(enumerator.Current));
                    else jd.Add(WriteToJsonDataImpl(enumerator.Current));
                }

                return true;
            }

            return false;

        }

        private static JsonData WriteToJsonDataImpl(object obj)
        {
            JsonData json;

            if (obj is IDictionary dict)
            {
                json = new JsonData
                {
                    Capacity = dict.Count
                };

                var kv = dict.GetEnumerator();
                while (kv.MoveNext())
                {
                    Type type = kv.Value.GetType();
                    if (kv.Value is IDictionary)
                    {
                        json[kv.Key.ToString()] = WriteToJsonDataImpl(kv.Value);
                    }
                    else if (IsBuildInType(type))
                    {
                        json[kv.Key.ToString()] = new JsonData(ConvertToLitJsonSupportType(kv.Value));
                    }
                    else if (TryFillArrayJsonData(type, kv.Value, out var jd))
                    {
                        if (jd != null) json[kv.Key.ToString()] = jd;
                    }
                    else
                    {
                        throw new Exception($"[Ember]: JsonUtils cannot support to serialize {type} for Dictionary<string, object>");
                    }
                }
            }
            else
            {
                if (IsBuildInType(obj.GetType()))
                {
                    json = new JsonData(ConvertToLitJsonSupportType(obj));
                }
                else
                {
                    CallbackInvoke<OnSerializingAttribute>(obj);

                    var all = GetAllReflectedData(obj);

                    json = new JsonData
                    {
                        Capacity = all.Length
                    };

                    foreach (var item in all)
                    {
                        var name = item.attr.Name.IsNullOrEmpty() ? (item.p == null ? item.f.Name : item.p.Name) : item.attr.Name;
                        var v = item.p == null ? item.f.GetValue(obj) : item.p.GetValue(obj);
                        if (v != null)
                        {
                            if (TryFillArrayJsonData(item.type, v, out var jd))
                            {
                                if (jd != null) json[name] = jd;
                            }
                            else
                            {
                                json[name] = IsBuildInType(item.type) ?
                                   new JsonData(ConvertToLitJsonSupportType(v))
                                   : WriteToJsonDataImpl(v);
                            }
                        }
                    }

                    CallbackInvoke<OnSerializedAttribute>(obj);
                }
            }

            return json;
        }

        private static Dictionary<string, object> ReadToDictTmpl(JsonData json)
        {
            var keys = json.Keys;
            var dict = new Dictionary<string, object>(keys.Count);
            foreach (var k in keys)
            {
                var v = GetJsonDataValueForDict(json[k]);
                if (v != null)
                {
                    dict.Add(k, v);
                }
            }

            return dict;
        }

        #endregion

        #region 外部方法

        [Obsolete("please use JsonUtils.ReadToObject<T> or JsonUtils.ReadToDict(), don't use LitJson.JsonData directly.")]
        public static JsonData Load(string jsonStr)
        {
            if (jsonStr == null) return null;
            try
            {
                return JsonMapper.ToObject(jsonStr);
            }
            catch (Exception e)
            {
                EmberDebug.LogException(TAG,e);
                return null;
            }
        }

        /// <summary>
        /// Reads JSON data into an object by C# reflection.
        /// Only properties and fields marked with [JsonProp] are deserialized.
        /// Supports List&lt;T&gt;, T[], HashSet&lt;T&gt;, Dictionary&lt;string, T&gt;, and nested objects.
        /// </summary>
        [HasGC]
        public static T ReadToObject<T>(this JsonData json)
        {
            if (json == null || json.GetJsonType() == JsonType.None)
            {
                throw new JsonException("[Ember]: json format is error!");
            }

            var t = typeof(T);
            if (t == DictType)
            {
                return (T)(object)ReadToDictTmpl(json);
            }
            else
            {
                return (T)ReadToObjectImpl(json, Activator.CreateInstance(t));
            }
        }

        /// <summary>
        /// Reads JSON string into an object by C# reflection.
        /// Only properties and fields marked with [JsonProp] are deserialized.
        /// </summary>
        public static T ReadToObject<T>(string json)
        {
            try
            {
                return JsonMapper.ToObject(json).ReadToObject<T>();
            }
            catch (JsonException ex)
            {
                throw new JsonException($"[Ember]: string is not exact json format for {nameof(JsonUtils)}.{nameof(ReadToObject)}, please check it. " +
                    $"It may be a \"last comma\" problem!!\n{ex}");
            }
        }

        /// <summary>
        /// Reads JSON string into a Dictionary&lt;string, object&gt;.
        /// Only List&lt;T&gt;, T[] can be deserialized; Dictionary&lt;,&gt; and HashSet&lt;&gt; are not supported.
        /// </summary>
        public static Dictionary<string, object> ReadToDict(string json)
        {
            var jsonData = JsonMapper.ToObject(json);
            if (jsonData == null || jsonData.GetJsonType() == JsonType.None)
            {
                throw new JsonException("[Ember]: json format is error!");
            }

            return ReadToDictTmpl(jsonData);
        }

        /// <summary>
        /// Writes an object to a JSON string by C# reflection.
        /// Only properties and fields marked with [JsonProp] are serialized.
        /// </summary>
        [HasGC]
        public static string WriteToJsonString(object obj, bool format = false)
        {
            using (var sw = new StringWriter())
            {
                JsonWriter writer = new JsonWriter(sw);
                writer.Validate = false;
                writer.PrettyPrint = format;

                var json = WriteToJsonData(obj);
                if (json.GetJsonType() == JsonType.None) return "{}";

                json.ToJson(writer);
                return sw.ToString().Replace("\r", "");
            }
        }

        /// <summary>
        /// Writes an object to a JsonData by C# reflection.
        /// Only properties and fields marked with [JsonProp] are serialized.
        /// </summary>
        [HasGC]
        public static JsonData WriteToJsonData(object obj)
        {
            return WriteToJsonDataImpl(obj);
        }

        /// <summary>
        /// Writes a Dictionary&lt;string, object&gt; to JsonData.
        /// </summary>
        [HasGC]
        public static JsonData WriteDictToJsonData(Dictionary<string, object> dict)
        {
            return WriteToJsonData(dict);
        }

        /// <summary>
        /// Writes a Dictionary&lt;string, object&gt; to a JSON string.
        /// </summary>
        public static string WriteDictToJsonString(Dictionary<string, object> dict, bool format = false)
        {
            return WriteToJsonString(dict, format);
        }

        #endregion
    }

    #region JsonData Extension Methods

    public static class JsonDataExtensions
    {
        public static void ForEach(this JsonData json, Action<string, JsonData> act)
        {
            if (json == null || act == null) return;
            json.Keys.ForEach(key =>
            {
                act(key, json[key]);
            });
        }

        public static bool TryGetValue(this JsonData json, string name, out JsonData val)
        {
            if (json.ContainsKey(name))
            {
                val = json[name];
                return true;
            }
            else
            {
                val = null;
                return false;
            }
        }

        public static void ForEach<T>(this JsonData json, Action<string, T> act)
        {
            if (json == null || act == null) return;
            json.Keys.ForEach<string>(key =>
            {
                act(key, json[key].ToString().ConvertTo<T>());
            });
        }

        public static T Get<T>(this JsonData json, string key)
        {
            if (json == null || string.IsNullOrEmpty(key)) return default(T);
            try
            {
                return json[key].ToString().ConvertTo<T>();
            }
            catch (Exception e)
            {
                EmberDebug.LogException(LogTags.ExtensionJsonUtils, e);
                return default(T);
            }
        }

        [Obsolete("it will deleted in future")]
        public static bool SetValue(this JsonData j, object obj, FieldInfo f)
        {
            return j.TrySetValue(obj, f.FieldType, (null, f), (null, null, null, null, 0));
        }

        [Obsolete("it will deleted in future")]
        public static bool SetValue(this JsonData j, object obj, PropertyInfo p)
        {
            return j.TrySetValue(obj, p.PropertyType, (p, null), (null, null, null, null, 0));
        }
    }

    #endregion
}
