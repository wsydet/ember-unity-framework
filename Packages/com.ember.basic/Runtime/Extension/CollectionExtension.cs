//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System.Collections.Generic;
//using System;
//
//using System.Text;
//using System.Threading.Tasks;
//
//namespace Burner.Basic
//{
//    public static class CollectionExtension
//    {
//        public static void ForEach<K, V>(this IDictionary<K, V> dict, Action<K, V> act)
//        {
//            using var it = dict.GetEnumerator();
//            KeyValuePair<K, V> pair;
//            while(it.MoveNext())
//            {
//                pair = it.Current;
//                act(pair.Key, pair.Value);
//            }
//        }
//
//        public static void ForEach<T>(this IEnumerable<T> e, Action<T> act)
//        {
//            using IEnumerator<T> it = e.GetEnumerator();
//            while(it.MoveNext())
//            {
//                act(it.Current);
//            }
//        }
//
//        public static void ForEach<T>(this IEnumerable<T> e, Action<T,int> act)
//        {
//            int idx = 0;
//            using IEnumerator<T> it = e.GetEnumerator();
//            while(it.MoveNext())
//            {
//                act(it.Current, idx++);
//            }
//        }
//
//        public static HashSet<T> ToHashSetBase<T>(this IEnumerable<T> e) => new HashSet<T>(e);
//
//        static readonly object s_ParallelForEachLocker = new object();
//
//        public static void ParallelForEach<T>(this IEnumerable<T> list, Action<T> processor)
//        {
//            StringBuilder sb = null;
//            Parallel.ForEach(list, f =>
//            {
//                try
//                {
//                    processor(f);
//                }
//                catch(Exception ex)
//                {
//                    lock(s_ParallelForEachLocker)
//                    {
//                        if(sb == null)
//                        {
//                            sb = new StringBuilder();
//                        }
//                        sb.Append(ex);
//                    }
//                }
//            });
//
//            if(sb != null)
//            {
//                throw new Exception(sb.ToString());
//            }
//        }
//
//        public static void Add<K, V>(this Dictionary<K, List<V>> dict, K k, V v)
//        {
//            if (!dict.TryGetValue(k, out var list))
//            {
//                list = new List<V>();
//                dict.Add(k, list);
//            }
//            list.Add(v);
//        }
//
//        public static bool IsNullOrEmpty<T>(this ICollection<T> c)
//        {
//            return c == null || c.Count == 0;
//        }
//
//        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
//        {
//            if(items == null || hashSet == null)
//            {
//                return;
//            }
//
//            foreach(var item in items)
//            {
//                hashSet.Add(item);
//            }
//        }
//
//        public static int RemoveAll<T>(this HashSet<T> set, Predicate<T> pred)
//        {
//            var count = 0;
//            while(set.Count > 0)
//            {
//                bool found = false;
//                foreach(var s in set)
//                {
//                    if(pred(s))
//                    {
//                        set.Remove(s);
//                        found = true;
//                        count++;
//                        break;
//                    }
//                }
//                if(!found)
//                {
//                    break;
//                }
//            }
//
//            return count;
//        }
//
//        public static int RemoveAll<T, T1>(this Dictionary<T, T1> set, Predicate<T> pred)
//        {
//            var count = 0;
//            while(set.Count > 0)
//            {
//                bool found = false;
//                foreach(var s in set.Keys)
//                {
//                    if(pred(s))
//                    {
//                        count++;
//                        set.Remove(s);
//                        found = true;
//                        break;
//                    }
//                }
//                if(!found)
//                {
//                    break;
//                }
//            }
//
//            return count;
//        }
//
//        public static int RemoveAll<T>(this LinkedList<T> list, Predicate<T> match)
//        {
//            var count = 0;
//
//            if(list.Count > 0)
//            {
//                var node = list.First;
//                while(node != null)
//                {
//                    var next = node.Next;
//                    if(match(node.Value))
//                    {
//                        list.Remove(node);
//                        count++;
//                    }
//                    node = next;
//                }
//            }
//
//            return count;
//        }
//
//        /// <summary>
//        /// #Warn: Avoid Boxing
//        /// </summary>
//        public static T ConvertTo<T>(this object src)
//        {
//            try
//            {
//                return (T)Convert.ChangeType(src, typeof(T));
//            }
//            catch (Exception e)
//            {
//                Console.Error.WriteLine("[Burner]: ConvertToException for {0} ---> {1}", src, e.Message);
//            }
//
//            return default(T);
//        }
//
//        /// <summary>
//        /// join IEnumerable as string with seperator, "Implode" is from PHP
//        /// </summary>
//        public static string Implode<T, C>(this IEnumerable<T> e, C seperator)
//        {
//            var sb = new StringBuilder();
//            e.ForEach(s =>
//            {
//                if (sb.Length != 0)
//                {
//                    sb.Append(seperator);
//                }
//
//                sb.Append(s.ToString());
//            });
//            return sb.ToString();
//        }
//
//    }
//}
