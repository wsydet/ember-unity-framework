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
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Security.Cryptography;
//using System.Threading;
//using UnityEngine;
//using UnityEngine.LowLevel;
//using UnityEngine.PlayerLoop;
//
//namespace Burner.Extensions
//{
//    class SingleThreadSynchronizationContext : SynchronizationContext
//    {
//        public static SingleThreadSynchronizationContext Instance { get; } = new SingleThreadSynchronizationContext();
//
//        [RuntimeInitializeOnLoadMethod]
//
//        static void Initialize()
//        {
//            // Prepare the function for using player loop.
//            var myPlayerLoopSystem = new PlayerLoopSystem()
//            {
//                type = typeof(SingleThreadSynchronizationContext),     // Identifier for Profiler Hierarchy view.
//                updateDelegate = Instance.Update    // Register the function.
//            };
//
//
//            // Get the default player loop.
//            var playerLoopSystem =
//#if UNITY_2019_3_OR_NEWER
//                PlayerLoop.GetCurrentPlayerLoop();
//#else
//                PlayerLoop.GetDefaultPlayerLoop();
//#endif
//
//            var playerLoopIndex = -1;
//            for (var i = 0; i < playerLoopSystem.subSystemList.Length; i++)
//            {
//                if (playerLoopSystem.subSystemList[i].type != typeof(PreLateUpdate))
//                {
//                    continue;
//                }
//
//                playerLoopIndex = i;
//                break;
//            }
//
//            if (playerLoopIndex < 0)
//            {
//                Debug.LogError("SingleThreadSynchronizationContext : Failed to add processing to PlayerLoop.");
//                return;
//            }
//
//            // Get the "PreLateUpdate" system.
//            var playerLoopSubSystem = playerLoopSystem.subSystemList[playerLoopIndex];
//            var subSystemList = playerLoopSubSystem.subSystemList;
//
//
//            // Register the model update function after "PreLateUpdate" system.
//            Array.Resize(ref subSystemList, subSystemList.Length + 1);
//            subSystemList[subSystemList.Length - 1] = myPlayerLoopSystem;
//
//
//            // Restore the "PreLateUpdate" sytem.
//            playerLoopSubSystem.subSystemList = subSystemList;
//            playerLoopSystem.subSystemList[playerLoopIndex] = playerLoopSubSystem;
//            PlayerLoop.SetPlayerLoop(playerLoopSystem);
//        }
//
//        private readonly int mainThreadId = Thread.CurrentThread.ManagedThreadId;
//
//        // 线程同步队列,发送接收socket回调都放到该队列,由poll线程统一执行
//        private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
//
//        private Action a;
//
//        List<AggregateTaskCompletionSource> whenAllQueue = new List<AggregateTaskCompletionSource>();
//        List<AggregateTaskCompletionSource> whenAnyQueue = new List<AggregateTaskCompletionSource>();
//
//        public void QueueWhenAll(AggregateTaskCompletionSource tcs)
//        {
//            whenAllQueue.Add(tcs);
//        }
//
//        public void QueueWhenAny(AggregateTaskCompletionSource tcs)
//        {
//            whenAnyQueue.Add(tcs);
//        }
//
//        public void Update()
//        {
//            int completed = 0;
//            for(int i = 0; i < whenAllQueue.Count; i++)
//            {
//                var tcs = whenAllQueue[i];
//                if (tcs.Task.IsCompleted)
//                {
//                    if (i == completed)
//                        completed++;
//                }
//                else
//                {
//                    tcs.CheckAllCompleted();
//                    //如果完成了下帧删除
//                }
//            }
//            if (completed > 0)
//                whenAllQueue.RemoveRange(0, completed);
//
//            completed = 0;
//            for (int i = 0; i < whenAnyQueue.Count; i++)
//            {
//                var tcs = whenAnyQueue[i];
//                if (tcs.Task.IsCompleted)
//                {
//                    if (i == completed)
//                        completed++;
//                }
//                else
//                {
//                    tcs.CheckAllCompleted();
//                    //如果完成了下帧删除
//                }
//            }
//            if (completed > 0)
//                whenAnyQueue.RemoveRange(0, completed);
//            while (true)
//            {
//                if (!this.queue.TryDequeue(out a))
//                {
//                    return;
//                }
//                a();
//            }
//        }
//
//        public override void Post(SendOrPostCallback callback, object state)
//        {
//            if (Thread.CurrentThread.ManagedThreadId == this.mainThreadId)
//            {
//                callback(state);
//                return;
//            }
//
//            this.queue.Enqueue(() => { callback(state); });
//        }
//    }
//}
