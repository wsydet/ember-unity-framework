//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using UnityEngine;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Security.Cryptography;
//using System.Text;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using UnityEngine.Networking;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Debug = UnityEngine.Debug;
//
//namespace Burner.Extensions
//{
//    public static class Utility
//    {
//        /// <summary>
//        /// if the resources name starts with "http://" or "https://"
//        /// it will be loaded as remote asset
//        /// </summary>
//        /// <param name="name"></param>
//        /// <returns></returns>
//        public static bool IsRemote(string name)
//        {
//            return !name.IsNullOrEmpty() && (name.StartsWith("http://") || name.StartsWith("https://"));
//        }
//
//        private readonly static char[] s_urlAndSpliter = new char[] { '&' };
//        private readonly static char[] s_urlEquSpliter = new char[] { '=' };
//
//        /// <summary>
//        /// remove and parse url params
//        /// http://url.com?aaa#bbb => http://url.com
//        /// </summary>
//        /// <param name="url"></param>
//        /// <param name="param">aa=bb&cc=dd will be loaded into this dictionary</param>
//        /// <returns></returns>
//        public static string ParseRemoteUrl(string url, Dictionary<string,string> param = null)
//        {
//            int questionSignIdx = url.IndexOf('?');
//            string ret = questionSignIdx == -1 ? url : url.Substring(0, questionSignIdx);
//
//            if(param != null && questionSignIdx != -1 && questionSignIdx < url.Length - 1)
//            {
//                int sharpSignIdx = url.IndexOf('#');
//                if(sharpSignIdx == -1) sharpSignIdx = url.Length;
//
//                questionSignIdx += 1;
//
//                var par = url.Substring(questionSignIdx, sharpSignIdx - questionSignIdx);
//                if(!par.IsNullOrEmpty())
//                {
//                    var pars = par.Split(s_urlAndSpliter);
//
//                    foreach(var p in pars)
//                    {
//                        if(p.IsNullOrEmpty()) continue;
//
//                        if(p.Contains(s_urlEquSpliter[0]))
//                        {
//                            var pp = p.Split(s_urlEquSpliter);
//                            var key = UnityWebRequest.UnEscapeURL(pp[0]);
//                            if(param.ContainsKey(key)) continue;
//                            param.Add(key, pp.Length > 1 ? UnityWebRequest.UnEscapeURL(pp[1]) : string.Empty);
//                        }
//                        else
//                        {
//                            var key = UnityWebRequest.UnEscapeURL(p);
//                            if(param.ContainsKey(key)) continue;
//                            param.Add(key, string.Empty);
//                        }
//                    }
//                }
//            }
//
//            return ret;
//        }
//
//
//        static char[] ParseUrlHostChars = new char[] { ':', '?', '/' };
//
//        /// <summary>
//        /// parse host name from url:
//        /// http://example.com:111/index => example.com
//        /// </summary>
//        /// <param name="url"></param>
//        [HasGC]
//        public static string ParseUrlHost(string url)
//        {
//            int start = url.IndexOf(':');
//            if(start != -1 )
//            {
//                start += 3; // "://"
//
//                int end = url.IndexOfAny(ParseUrlHostChars, start);
//                if(end == -1)
//                {
//                    end = url.Length - start;
//                }
//                else
//                {
//                    end = end - start;
//                }
//
//                return url.Substring(start, end);
//            }
//
//            return url;
//        }
//
//        public const int MD5CodeLength_16Bytes = 16;
//        private static readonly Queue<MD5> s_md5List = new Queue<MD5>();
//        private static readonly Queue<byte[]> s_md5Buff = new Queue<byte[]>();
//
//        static MD5 PopMD5()
//        {
//            lock(s_md5List)
//            {
//                if(s_md5List.Count > 0)
//                {
//                    var md5 = s_md5List.Dequeue();
//                    md5.Initialize();
//                    return md5;
//                }
//                else
//                {
//                    return MD5.Create();
//                }
//            }
//        }
//
//        static void PushMD5(MD5 md5)
//        {
//            lock(s_md5List)
//            {
//                s_md5List.Enqueue(md5);
//            }
//        }
//
//        static byte[] PopBuff()
//        {
//            lock(s_md5Buff)
//            {
//                if(s_md5Buff.Count > 0) return s_md5Buff.Dequeue();
//                return new byte[64 * 1024];
//            }
//        }
//
//        static void PushBuff(byte[] buff)
//        {
//            lock(s_md5Buff)
//            {
//                s_md5Buff.Enqueue(buff);
//            }
//        }
//
//        public static void ClearMD5Buff()
//        {
//            lock(s_md5Buff)
//            {
//                s_md5Buff.Clear();
//            }
//
//            lock(s_md5List)
//            {
//                s_md5List.Clear();
//            }
//        }
//
//        public static string ComputeMD5_16Bits(byte[] bytes, int len = -1)
//        {
//            var md5 = PopMD5();
//            try
//            {
//                var res = BitConvertHash(md5.ComputeHash(bytes,0, len > 0 ? len : bytes.Length));
//
//#if UNITY_EDITOR
//                if(res.Length != MD5CodeLength_16Bytes)
//                {
//                    throw new Exception($"[Burner]: the result of {nameof(ComputeMD5_16Bits)} [{res}] is not {MD5CodeLength_16Bytes} length");
//                }
//#endif
//                return res;
//            }
//            finally
//            {
//                PushMD5(md5);
//            }
//        }
//
//        public static string GetMD5_16Bits(string s)
//        {
//            return ComputeMD5_16Bits(Encoding.UTF8.GetBytes(s));
//        }
//
//        public static string GetFileMD5_16Bits(string path)
//        {
//            using(FileStream fs = File.OpenRead(path))
//            {
//                var md5 = PopMD5();
//                try
//                {
//                    var res = BitConvertHash(md5.ComputeHash(fs));
//#if UNITY_EDITOR
//                    if(res.Length != MD5CodeLength_16Bytes)
//                    {
//                        throw new Exception($"[Burner]: the result of {nameof(ComputeMD5_16Bits)} [{res}] is not {MD5CodeLength_16Bytes} length");
//                    }
//#endif
//                    return res;
//                }
//                finally
//                {
//                    PushMD5(md5);
//                }
//            }
//        }
//
//        public static string BitConvertHash(byte[] hash)
//        {
//            return BitConverter.ToString(hash, 4, 8).Replace("-", "").ToLower();
//        }
//
//        public static string GetStreamMD5_16Bit(Stream fs, int len)
//        {
//            if(len <= 0)
//            {
//                throw new Exception("[Burner]: stream is empty for GetStreamMD5_16Bit");
//            }
//
//            var md5 = PopMD5();
//            var bytes = PopBuff();
//            try
//            {
//                var tryCount = 10;
//                while(len > 0 && tryCount > 0)
//                {
//                    if(fs.Position >= fs.Length)
//                    {
//                        break;
//                    }
//
//                    var read = fs.Read(bytes, 0, Mathf.Min(len, bytes.Length));
//                    if(read <= 0)
//                    {
//                        Thread.Sleep(50);
//                        tryCount--;
//                        continue;
//                    }
//
//                    len -= read;
//
//                    if(len > 0) md5.TransformBlock(bytes, 0, read, null, 0);
//                    else md5.TransformFinalBlock(bytes, 0, read);
//                }
//
//                if(tryCount <= 0)
//                {
//                    throw new Exception("[Burner]: error read bytes for GetStreamMD5_16Bit");
//                }
//
//                return BitConvertHash(md5.Hash);
//            }
//            finally
//            {
//                PushBuff(bytes);
//                PushMD5(md5);
//            }
//        }
//
//        /// <summary>
//        /// convert a path to one that can be load by <see cref="Resources.Load"/>
//        /// Assets/Game/Editor/Lang/Resources/English/Icon/FightFly.spriteatlas
//        /// ->
//        /// Assets/Game/Editor/Lang/Resources/English/Icon/FightFly
//        /// </summary>
//        public static string GetPathNoExt(string path)
//        {
//            var ext = Path.GetExtension(path);
//            var s = path.Substring(0, path.Length - ext.Length);
//            return s;
//        }
//
//        public static bool HasUpperChar(string str)
//        {
//            if(!str.IsNullOrEmpty())
//            {
//                for(int i = 0; i < str.Length; i++)
//                {
//                    if(str[i] >= 'A' && str[i] <= 'Z')
//                    {
//                        return true;
//                    }
//                }
//            }
//
//            return false;
//        }
//
//        // test it if it has less GC.Alloc than pure string.ToLower on the string all chars are lower'
//        [Obsolete("Please use StringExtension.ToAlphaLower instead")]
//        public static string ToLower(string str)
//        {
//            return HasUpperChar(str) ? str.ToLower() : str;
//        }
//
//        /// <summary>
//        /// return the current Linux time stamp (millisecond)
//        /// </summary>
//        public static long CurrTimeStamp()
//        {
//            return ConvertDateTime(DateTime.UtcNow);
//        }
//
//        /// <summary>
//        /// convert DateTime to Linux timestamp in milliseconds
//        /// </summary>
//        /// <param name="time"></param>
//        /// <returns></returns>
//        public static long ConvertDateTime(DateTime time)
//        {
//            return ConvertDataTimeTicks(time.Ticks);
//        }
//
//        /// <summary>
//        /// convert DateTime.Ticks to linux timestamp in milliseconds
//        /// </summary>
//        /// <param name="ticks"></param>
//        /// <returns></returns>
//        public static long ConvertDataTimeTicks(long ticks)
//        {
//            //
//            //long t_delta = System.DateTime.Parse("1970-01-01 00:00:00").Ticks;
//            //t_delta == 621355968000000000
//            //
//            return (ticks - 621355968000000000L) / 10000L;
//        }
//
//        public static (bool notEven, int word) ContinuousCheckSum(ref uint crc, byte[] content, int offset, int n, (bool notEven, int word) former)
//        {
//            if(n > content.Length)
//            {
//                throw new ArgumentOutOfRangeException();
//            }
//
//            if(n > 0)
//            {
//                int index = offset;
//
//                if(former.notEven)
//                {
//                    crc += (uint)(former.word | content[index++]);
//                }
//
//                while(index < n)
//                {
//                    int word = content[index++];
//                    word <<= 8;
//
//                    if(index < n)
//                    {
//                        word |= content[index++];
//                    }
//                    else
//                    {
//                        return (true, word);
//                    }
//
//                    crc += (uint)word;
//                }
//            }
//
//            return (false, 0);
//        }
//
//        public static void FinalCheckSum(ref uint crc, (bool notEven, int word) former)
//        {
//            if(former.notEven)
//            {
//                crc += (uint)former.word;
//            }
//
//            crc = (crc >> 16) + (crc & 0xffff);
//            crc = (ushort)(~crc);
//        }
//
//        public static uint CheckSum(byte[] content, int contentLen = -1)
//        {
//            uint crc = 0;
//            var former = ContinuousCheckSum(ref crc, content, 0, contentLen > 0 ? contentLen : content.Length, (false, 0));
//            FinalCheckSum(ref crc, former);
//            return crc;
//        }
//
//        public static uint CheckSum(string filePath)
//        {
//            return CheckSum(File.ReadAllBytes(filePath));
//        }
//
//        // remove comments lines (//, # in a line) for json, property format string
//        public static string RemoveComments(string str)
//        {
//            Const.sb.Length = 0;
//
//            int idx = 0;
//            int n = str.Length;
//            bool inQuota = false;
//            while(idx < n)
//            {
//                if(str[idx] == '"')
//                {
//                    inQuota = !inQuota;
//                }
//
//                if(!inQuota)
//                {
//                    if(str[idx] == '#' || (str[idx] == '/' && idx + 1 < n && str[idx + 1] == '/'))
//                    {
//                        while(idx < n && str[idx] != '\n') idx++;
//                        if(idx >= n) break;
//                    }
//                }
//
//                Const.sb.Append(str[idx++]);
//            }
//
//            return Const.sb.ToString();
//        }
//
//        private const long Kilobyte = 1024;
//        private const long Megabyte = Kilobyte * Kilobyte;
//        private const long Gigabyte = Kilobyte * Megabyte;
//
//        private const long Kilobyte1000 = 1000;
//        private const long Megabyte1000 = Kilobyte1000 * Kilobyte1000;
//        private const long Gigabyte1000 = Kilobyte1000 * Megabyte1000;
//
//        /// <summary>
//        /// get the size string
//        ///
//        /// case 1: if it dividing 1024
//        ///   123                         => 123B
//        ///   54 * 1024                   => 54.1KB
//        ///   1.23 * 1024 * 1024          => 1.23MB
//        ///   1.44 * 1024 * 1024 * 1024   => 1.44GB
//        ///
//        /// case 2: if it dividing 1000
//        ///   123                         => 123B
//        ///   54 * 1000                   => 54.1KiB
//        ///   1.23 * 1000 * 1000          => 1.23MiB
//        ///   1.44 * 1000 * 1000 * 1000   => 1.44GiB
//        /// </summary>
//        /// <param name="size"></param>
//        /// <param name="divide1024or1000"> divide by 1024 or 1000
//        /// it will display numbers which is divided by 1000 when it's used for disk capacity
//        /// https://www.zhihu.com/question/268670573
//        /// </param>
//        /// <returns></returns>
//        public static string GetSizeString(long size, bool divide1024or1000 = true)
//        {
//            bool negative = size < 0;
//            size = size < 0 ? (-size) : size;
//
//            string res;
//            if(divide1024or1000)
//            {
//                if(size < Kilobyte)
//                {
//                    res = size.ToString() + "B";
//                }
//                else if(size >= Kilobyte && size < Megabyte)
//                {
//                    res = (size / (float)Kilobyte).ToString("F3") + "KB";
//                }
//                else if(size >= Megabyte && size < Gigabyte)
//                {
//                    res = (size / (float)Megabyte).ToString("F3") + "MB";
//                }
//                else
//                {
//                    res = (size / (float)Gigabyte).ToString("F3") + "GB";
//                }
//            }
//            else
//            {
//                if(size < Kilobyte1000)
//                {
//                    res = size.ToString() + "B";
//                }
//                else if(size >= Kilobyte1000 && size < Megabyte1000)
//                {
//                    res = (size / (float)Kilobyte).ToString("F3") + "KiB";
//                }
//                else if(size >= Megabyte1000 && size < Gigabyte1000)
//                {
//                    res = (size / (float)Megabyte1000).ToString("F3") + "MiB";
//                }
//                else
//                {
//                    res = (size / (float)Gigabyte1000).ToString("F3") + "GiB";
//                }
//            }
//
//            return negative ? ("-" + res) : res;
//        }
//
//
//        public static bool HasMask(this int flag, int mask) => (flag & (1 << mask)) != 0;
//        public static void SetMask(this ref int flag, int mask) => flag |= (1 << mask);
//        public static void UnsetMask(this ref int flag, int mask) => flag &= ~(1 << mask);
//
//        public static bool HasMask(this uint flag, int mask) => ((int)flag).HasMask(mask);
//
//        private static System.Random s_rnd = new System.Random();
//
//        public static void SetRandomSeed(int seed)
//        {
//            s_rnd = new System.Random(seed);
//            Debug.Log("[Burner]: Seed == " + seed);
//        }
//
//        public static void SetTimeRandomSeed(int seed = 0)
//        {
//            if(seed == 0) seed = (int)(CurrTimeStamp() / 1000);
//            SetRandomSeed(seed);
//        }
//
//        public static int RandomRange(int min, int max)
//        {
//            var rnd = s_rnd;
//            lock(rnd)
//            {
//                return rnd.Next(min, max);
//            }
//        }
//
//        public static long RandomRange(long min, long max)
//        {
//            if(max < min)
//            {
//                (min, max) = (max, min);
//            }
//
//            var rnd = s_rnd;
//            lock(rnd)
//            {
//                return (long)(min + rnd.NextDouble() * (max - min));
//            }
//        }
//
//        [HasGC]
//        public static void Shuffle<T>(T[] array)
//        {
//            var rnd = s_rnd;
//            lock(rnd)
//            {
//                int n = array.Length;
//                while(n > 1)
//                {
//                    var k = rnd.Next(n--);
//                    if(k != n)
//                    {
//                        (array[n], array[k]) = (array[k], array[n]);
//                    }
//                }
//            }
//        }
//
//        [HasGC]
//        public static List<T> RandomSelectionList<T>(int selectionCount, T[] list)
//        {
//            T[] cpy = new T[list.Length];
//            list.CopyTo(cpy, 0);
//            Shuffle(cpy);
//
//            var newList = new List<T>(Mathf.Min(list.Length, selectionCount));
//            if(selectionCount >= cpy.Length)
//            {
//                newList.AddRange(cpy);
//            }
//            else
//            {
//                newList.AddRange(cpy.Where((_, i) => i < selectionCount));
//            }
//
//            return newList;
//        }
//
//        public static void DeleteDirectory(string path)
//        {
//            const int TryCount = 10;
//            int count = 0;
//            while(Directory.Exists(path) && count++ < TryCount)
//            {
//                try
//                {
//                    Directory.Delete(path, true);
//                }
//                catch(IOException e)
//                {
//                    Debug.LogWarning($"[Burner]: Delete Directory {path} Error:\n {e}");
//
//                    // just wait for a moment to try to avoid "IOException: Sharing violation"
//                    Thread.Sleep(100);
//                }
//            }
//
//            if(count > TryCount)
//            {
//                throw new Exception($"[Burner]: Cannot delete directory {path}");
//            }
//        }
//
//        public static void RecreateDirectory(string path)
//        {
//            DeleteDirectory(path);
//            Directory.CreateDirectory(path);
//        }
//
//        public static void DeleteFile(string path)
//        {
//            const int TryCount = 10;
//            int count = 0;
//            while(File.Exists(path) && count++ < TryCount)
//            {
//                try
//                {
//                    File.Delete(path);
//                }
//                catch(IOException e)
//                {
//                    Debug.LogWarning($"[Burner]: Delete File {path} Error {e}");
//
//                    // just wait for a moment to try to avoid "IOException: Sharing violation"
//                    Thread.Sleep(100);
//                }
//            }
//
//            if(count > TryCount)
//            {
//                throw new Exception($"[Burner]: Cannot delete file {path}");
//            }
//        }
//
//        /// <summary>
//        /// iterate all files through a directory and copy it one by one to another directory
//        /// instead of delete destination directory and create a new one
//        /// </summary>
//        /// <param name="srcFolder"></param>
//        /// <param name="dstFolder"></param>
//        /// <param name="mirror"> if delete extra file </param>
//        /// <param name="filter"> predict filter </param>
//        /// <exception cref="Exception"></exception>
//        [HasGC("You should call this in Editor rather than runtime as possible as you can")]
//        public static void CopyDirectory(string srcFolder, string dstFolder, bool mirror = false, Func<string, bool> filter = null)
//        {
//            if(!Directory.Exists(srcFolder))
//            {
//                throw new Exception($"[Burner]: Source folder {srcFolder} does not exist");
//            }
//
//            static string ReplacePath(string path)
//            {
//                return path.Replace("\\", "/");
//            }
//
//            srcFolder = ReplacePath(srcFolder);
//            dstFolder = ReplacePath(dstFolder);
//
//            var files = Directory.GetFiles(srcFolder, "*.*", SearchOption.AllDirectories)
//                .Where(f => filter == null || filter(f))
//                .Select(ReplacePath);
//
//            files.ParallelForEach(file =>
//            {
//                try
//                {
//                    var filename = Path.GetFileName(file);
//                    var dir = ReplacePath(Path.GetDirectoryName(file)).Replace(srcFolder, dstFolder);
//
//                    Directory.CreateDirectory(dir);
//                    File.Copy(file, dir + "/" + filename, true);
//                }
//                catch(Exception ex)
//                {
//                    Debug.LogError("[Burner]: Copy File Error:\n" + ex);
//                }
//            });
//
//            if(mirror)
//            {
//                var filesHashSet = files.ToHashSetBase();
//
//                var dstFiles = Directory.GetFiles(dstFolder, "*.*", SearchOption.AllDirectories)
//                    .Select(ReplacePath)
//                    .ToArray();
//
//                dstFiles.ParallelForEach(f =>
//                {
//                    var dst = f.Replace(dstFolder, srcFolder);
//                    if(!filesHashSet.Contains(dst))
//                    {
//                        try
//                        {
//                            File.Delete(f);
//                        }
//                        catch(Exception e)
//                        {
//                            Debug.LogError($"[Burner]: Delete file {f} Error: {e}");
//                        }
//                    }
//                });
//            }
//        }
//
//        /// <summary>
//        /// execute a shell command
//        /// </summary>
//        /// <param name="exePath"> c:/ddd/xx/dd/xx/cmd.exe </param>
//        /// <param name="args"> -c echo Hello!</param>
//        /// <param name="timeOut"> in milliseconds to</param>
//        /// <param name="ignoreErrorOutput"> sometime we have to ignore error, only judge succ or fail by exit code</param>
//        /// <param name="errorOutput"></param>
//        /// <returns></returns>
//        public static bool ExecuteCmd(string exePath, string args, int timeOut = 60 * 1000 * 1000, bool ignoreErrorOutput = false,
//            StringBuilder errorOutput = null, Action<string> outputHandler = null, Action<string> errorHandler = null)
//        {
//            var isSuccess = true;
//            var startInfo = new ProcessStartInfo()
//            {
//                FileName = exePath,
//                Arguments = args,
//                UseShellExecute = false,
//                CreateNoWindow = true, // Avoid create console window to flick at Unity Editor
//                RedirectStandardOutput = true,
//                RedirectStandardError = true
//            };
//
//            using var proc = new Process { StartInfo = startInfo };
//
//            var cb = new DataReceivedEventHandler((_, e) =>
//            {
//                var error = e.Data;
//                if(!string.IsNullOrEmpty(error))
//                {
//                    if(ignoreErrorOutput)
//                    {
//                        Debug.Log(error);
//                        outputHandler?.Invoke(error);
//                    }
//                    else
//                    {
//                        isSuccess = false;
//
//                        if(errorOutput != null)
//                        {
//                            lock(errorOutput) errorOutput.Append(error);
//                        }
//                        else
//                        {
//                            Debug.LogError(error);
//                        }
//
//                        errorHandler?.Invoke(error);
//                    }
//                }
//            });
//
//            var cb_output = new DataReceivedEventHandler((_, e) =>
//            {
//                var output = e.Data;
//                if(!string.IsNullOrEmpty(output))
//                {
//                    Debug.Log(output);
//                    outputHandler?.Invoke(output);
//                }
//            });
//
//            proc.ErrorDataReceived += cb;
//            proc.OutputDataReceived += cb_output;
//
//            try
//            {
//                proc.Start();
//                proc.BeginOutputReadLine();
//                proc.BeginErrorReadLine();
//
//                if(!proc.WaitForExit(timeOut))
//                {
//                    var error = "executing time out!";
//                    if(errorOutput != null)
//                    {
//                        lock(errorOutput) errorOutput.Append(error);
//                    }
//                    else
//                    {
//                        Debug.LogError(error);
//                    }
//
//                    errorHandler?.Invoke(error);
//                    isSuccess = false;
//                }
//
//                if(proc.ExitCode != 0)
//                {
//                    // wait for proc.ErrorDataReceived was called.
//                    int waitCount = 0;
//                    while(isSuccess && waitCount++ < 10)
//                    {
//                        Thread.Sleep(100);
//                    }
//
//                    if(isSuccess)
//                    {
//                        var error = "Execution Error ExitCode != 0, it cannot get error output, " +
//                                    "please copy and paste following command and run in your shell/cmd to get detail information:\n" +
//                                    $"{exePath} {args}\n\n";
//
//                        if(errorOutput != null)
//                        {
//                            lock(errorOutput) errorOutput.Append(error);
//                        }
//                        else
//                        {
//                            Debug.LogError(error);
//                        }
//
//                        errorHandler?.Invoke(error);
//                        isSuccess = false;
//                    }
//                }
//            }
//            finally
//            {
//                proc.ErrorDataReceived -= cb;
//                proc.OutputDataReceived -= cb_output;
//            }
//
//            return isSuccess;
//        }
//    }
//}
