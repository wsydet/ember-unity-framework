//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using Burner.Basic;
//using UnityEngine;
//
//namespace Burner
//{
//    public static class Logger
//    {
//        // private static int s_writeLogIndex = -1; //default log file "game.log" is -1
//
//        //public static bool NoLog { get; set; } = false;
//        //public static bool NoLogWarning { get; set; } = false;
//
//        public static Action<string> GameLogAction;
//        public static Action<string> GameWarnAction;
//        public static Action<string> GameErrorAction;
//        public static Action<bool, string> GameAssertAction;
//        public static Action<Exception> GameExceptionAction;
//
//        static Logger()
//        {
//            // Don't write log to file in build project process.
//            if(Application.isBatchMode) return;
//
//#if !UNITY_EDITOR && UNITY_WEBGL
//            // Don't write log to file in WebGL build process.
//            return;
//#endif
//
//// #if UNITY_EDITOR
////             s_writeLogIndex = FileStreamNI.fsni_log_add_file("game_editor.log") - 1;
//// #endif
////
////             Application.logMessageReceivedThreaded += (logStr, stackTrace, logType) =>
////             {
////                 // if AppConst.NoLog was set in runtime
////                 if(logType == LogType.Log && NoLog)
////                 {
////                     return;
////                 }
////
////                 if(logType == LogType.Warning && NoLogWarning)
////                 {
////                     return;
////                 }
////
////                 FileStreamNI.fsni_log_to(s_writeLogIndex, logStr);
////
////
////                 if(!stackTrace.IsNullOrEmpty()
////                    && (logType == LogType.Exception
////                        || logType == LogType.Assert
////                        || logType == LogType.Error))
////                 {
////                     FileStreamNI.fsni_log_to(s_writeLogIndex, stackTrace);
////
////                 }
////             };
//         }
//
//        private static string TAG = "[BurnerUI]: ";
//        // log for only debug environment
//        public static void Log(string msg, params object[] args)
//        {
//            //if(NoLog) return;
//
//            if (args.IsNullOrEmpty()) GameLogAction?.Invoke(TAG + msg);
//            else GameLogAction?.Invoke(TAG + string.Format(msg, args));
//
//        }
//
//        // log for only debug environment, with color specified
//        public static void ColorLog(string color, string msg, params object[] args)
//        {
//            //if(NoLog) return;
//
//            msg = $"<color={color}>{msg}</color>";
//            Log(msg, args);
//        }
//
//        // log for both debug and release environment
//        public static void Info(string msg, params object[] args)
//        {
//            Log(msg, args);
//        }
//
//        public static void Warn(string msg, params object[] args)
//        {
//            //if(NoLogWarning) return;
//
//            if(args.IsNullOrEmpty()) GameWarnAction?.Invoke(TAG+msg);
//            else GameWarnAction?.Invoke(TAG + string.Format(msg, args));
//        }
//
//        public static void Error(string msg, params object[] args)
//        {
//            if(args.IsNullOrEmpty()) GameErrorAction?.Invoke(TAG + msg);
//            else GameErrorAction?.Invoke(TAG + string.Format(msg, args));
//        }
//
//        public static void Assert(bool condition, string msg, params object[] args)
//        {
//            GameAssertAction?.Invoke(condition,string.Format(msg, args));
//            //if (condition) return;
//            //throw (new Exception(string.Format(TAG + msg, args)));
//        }
//
//        public static void Exception(Exception exception, string tag = "")
//        {
//            GameExceptionAction?.Invoke(exception);
////#if UNITY_EDITOR
////            // it can be output in Editor's Console and you can click on the lines
////            Debug.LogException(exception);
////#else
////            // in real device, it must output only one error to the console(Android.LogCat or XCode.Console)
////            if(tag.IsNullOrEmpty())
////            {
////                Error($"Exception Caught: {exception}");
////            }
////            else
////            {
////                Error($"{tag}: {exception}");
////            }
////#endif
//        }
//
//        public static void Warn(Exception exception)
//        {
//            Warn($"Warning Exception: {exception}");
//        }
//
//        public static void ClearConsole()
//        {
//#if UNITY_EDITOR
//            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
//            var type = assembly.GetType("UnityEditor.LogEntries");
//            var method = type.GetMethod("Clear");
//            method.Invoke(new object(), null);
//#endif
//        }
//    }
//}
