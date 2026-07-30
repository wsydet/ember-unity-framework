//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//using System.Threading;
//
//namespace Burner.Basic.Tasks
//{
//    public partial struct STTask
//    {
//        public static STTask FromException(Exception ex)
//        {
//            STTaskCompletionSource tcs = new STTaskCompletionSource();
//            tcs.TrySetException(ex);
//            return tcs.Task;
//        }
//
//        public static STTask<T> FromException<T>(Exception ex)
//        {
//            var tcs = new STTaskCompletionSource<T>();
//            tcs.TrySetException(ex);
//            return tcs.Task;
//        }
//
//        public static STTask<T> FromResult<T>(T value)
//        {
//            return new STTask<T>(value);
//        }
//
//        public static STTask FromCanceled()
//        {
//            return CanceledSTTaskCache.Task;
//        }
//
//        public static STTask<T> FromCanceled<T>()
//        {
//            return CanceledSTTaskCache<T>.Task;
//        }
//
//        public static STTask FromCanceled(CancellationToken token)
//        {
//            STTaskCompletionSource tcs = new STTaskCompletionSource();
//            tcs.TrySetException(new OperationCanceledException(token));
//            return tcs.Task;
//        }
//
//        public static STTask<T> FromCanceled<T>(CancellationToken token)
//        {
//            var tcs = new STTaskCompletionSource<T>();
//            tcs.TrySetException(new OperationCanceledException(token));
//            return tcs.Task;
//        }
//
//        private static class CanceledSTTaskCache
//        {
//            public static readonly STTask Task;
//
//            static CanceledSTTaskCache()
//            {
//                STTaskCompletionSource tcs = new STTaskCompletionSource();
//                tcs.TrySetCanceled();
//                Task = tcs.Task;
//            }
//        }
//
//        private static class CanceledSTTaskCache<T>
//        {
//            public static readonly STTask<T> Task;
//
//            static CanceledSTTaskCache()
//            {
//                var taskCompletionSource = new STTaskCompletionSource<T>();
//                taskCompletionSource.TrySetCanceled();
//                Task = taskCompletionSource.Task;
//            }
//        }
//    }
//}
