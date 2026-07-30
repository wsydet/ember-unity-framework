//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//namespace Burner.Extensions
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Threading;
//    using System.Threading.Tasks;
//    using UnityEngine;
//
//    /// <summary>
//    /// A thread pool can create and destroy threads automatically for running tasks.
//    /// Idle task will exit by itself when pool's total numbers of threads is over than a specific number (minThreads).
//    /// </summary>
//    public class ThreadPool : IDisposable
//    {
//        public static bool EnableLog = true;
//
//        static void Log(string format, params object[] parm)
//        {
//            if(EnableLog)
//            {
//                Debug.Log(string.Format(format, parm));
//            }
//        }
//
//        static void Warn(string format, params object[] parm)
//        {
//            if(EnableLog)
//            {
//                Debug.LogWarning(string.Format(format, parm));
//            }
//        }
//
//        public class Task<T>
//        {
//            public TaskStatus Status { get; internal set; } = TaskStatus.Created;
//            public T Result { get; internal set; }
//
//            public bool IsCompleted => Status == TaskStatus.RanToCompletion || Status == TaskStatus.Faulted;
//
//            internal readonly Func<T> _run;
//            private readonly ThreadPool _pool;
//
//            private Task(Func<T> run, ThreadPool pool)
//            {
//                _run = run;
//                _pool = pool;
//            }
//
//            /// <summary>
//            /// create a task with T result, you need to call Task.Start to start it.
//            /// </summary>
//            /// <param name="pool">a specific thread pool</param>
//            /// <param name="run">process function</param>
//            /// <returns> Task to start or get result </returns>
//            public static Task<T> CreateTask(ThreadPool pool, Func<T> run) => new Task<T>(run, pool);
//
//            /// <summary>
//            /// create and run a task
//            /// </summary>
//            /// <param name="pool"></param>
//            /// <param name="run"></param>
//            /// <returns></returns>
//            public static Task<T> RunTask(ThreadPool pool, Func<T> run)
//            {
//                var t = CreateTask(pool, run);
//                t.Start();
//                return t;
//            }
//
//            /// <summary>
//            /// start a task after created, Task.Status will become from Create to WaitingToRun
//            /// </summary>
//            public void Start()
//            {
//                if(_pool.Disposed)
//                {
//                    throw new Exception("[Burner]: cannot start tasks when thread pool was disposed.");
//                }
//
//                if(Status != TaskStatus.Created)
//                {
//                    throw new Exception("[Burner]: cannot start tasks again.");
//                }
//
//                Status = TaskStatus.WaitingToRun;
//
//                _pool.StartThread<T>();
//                lock(_pool._tasks)
//                {
//                    _pool._tasks.Enqueue(this);
//                }
//
//                _pool._sema.Release();
//            }
//        }
//
//        public const int LimitMaxThreads = 100;
//
//        public int MinThreads { get; set; }
//        public int MaxThreads { get; set; }
//
//        public int IdleTimeInMilliSec { get; set; }
//
//        public bool IsBackground { get; set; } = true;
//        public System.Threading.ThreadPriority Priority { get; set; } = System.Threading.ThreadPriority.BelowNormal;
//
//        public string Name { get; set; } = "BohTPool " + (new System.Random().Next(5000));
//
//        public int CurrThreads => _threads.Count;
//        public bool Disposed { get; private set; } = false;
//
//        private readonly SemaphoreSlim _sema;
//        private readonly CancellationTokenSource _semaToken = new CancellationTokenSource();
//        private readonly Queue<object> _tasks = new Queue<object>();
//
//        private readonly List<Thread> _threads;
//
//        /// <summary>
//        /// Set it true if you don't want this pool to call <see cref="Thread.Interrupt()"/>when this pool is going to be disposed
//        /// there are some progresses cannot be interrupted by threads
//        /// or they will cause weird problem, such as <see cref="System.Net.HttpWebRequest"/> which might use global static variables in System.Net
//        /// </summary>
//        public bool GentlyDispose { get; set; } = false;
//
//        /// <summary>
//        /// get quantity of queuing (not running yet) threads in the pool
//        /// </summary>
//        public int CurrQueuingTasks => _tasks.Count;
//
//        /// <summary>
//        /// get the number of running tasks (less or equals than <see cref="MaxThreads"/>)
//        /// </summary>
//        public int CurrRunningTasks { get; private set; }
//
//        /// <summary>
//        /// get all threads quantity of all Burner.ThreadPool
//        /// </summary>
//        public static int AllAliveThreads => _allAliveThreads;
//        private static int _allAliveThreads = 0;
//        private static object _allAliveThreadsLocker = new object();
//        private static void ThreadLive() { lock(_allAliveThreadsLocker) _allAliveThreads++; }
//        private static void ThreadDead() { lock(_allAliveThreadsLocker) _allAliveThreads--; }
//
//
//        /// <summary>
//        /// a new Thread() pool for multi-thread process requirement.
//        /// the number of threads of this pool will be kept between minThread and maxThread
//        /// </summary>
//        /// <param name="minThreads"> minimum alive threads in the pool </param>
//        /// <param name="maxThreads"> maximum work threads in the pool</param>
//        /// <param name="idleTimeInMilliSec">
//        /// if the number of current threads is more than minThread without any task to run
//        /// These threads will be exit by itself automatically after idleTimeInMilliSec
//        /// And no thread exits when it is set as 0
//        /// </param>
//        public ThreadPool(int minThreads, int maxThreads, int idleTimeInMilliSec)
//        {
//            if(minThreads < 0 || maxThreads <= 0)
//            {
//                throw new ArgumentException("[Burner]: cannot set max/min threads as a negative or zero number!");
//            }
//
//            if(minThreads > maxThreads)
//            {
//                throw new ArgumentException("[Burner]: min threads cannot be greater than max threads!");
//            }
//
//            if(maxThreads > LimitMaxThreads)
//            {
//                throw new ArgumentException($"[Burner]: max threads cannot be set over limit {LimitMaxThreads}!");
//            }
//
//            if(idleTimeInMilliSec < 0)
//            {
//                throw new ArgumentException("[Burner]: cannot set timeout as a negative or zero number!");
//            }
//
//            MinThreads = minThreads;
//            MaxThreads = maxThreads;
//            IdleTimeInMilliSec = idleTimeInMilliSec;
//
//            _sema = new SemaphoreSlim(0, int.MaxValue);
//            _threads = new List<Thread>(maxThreads);
//        }
//
//
//        private bool TryEndThread(Thread thread, bool justRemove)
//        {
//            lock(_threads)
//            {
//                if(Disposed)
//                {
//                    if(_threads.Count > 0)
//                    {
//                        _threads.Remove(thread);
//
//                        if(!justRemove && _threads.Count == 0)
//                        {
//                            _semaToken.Dispose();
//                            _sema.Dispose();
//                        }
//                    }
//
//                    return true;
//                }
//                else
//                {
//                    if(_threads.Count <= MinThreads)
//                    {
//                        return false;
//                    }
//                    _threads.Remove(thread);
//                    return true;
//                }
//            }
//        }
//
//        public void InterruptAll()
//        {
//            if(!Disposed)
//            {
//                if(!GentlyDispose)
//                {
//                    lock(_threads)
//                    {
//                        foreach(var t in _threads) t.Interrupt();
//                    }
//                }
//            }
//        }
//
//        /// <summary>
//        /// dispose this thread pool, all running task will exit peacefully by itself
//        /// </summary>
//        public void Dispose()
//        {
//            if(Disposed) return;
//            Disposed = true;
//
//            GC.SuppressFinalize(this);
//            InterruptAll();
//
//            _semaToken.Cancel();
//
//            // CAUTION!
//            // cannot dispose the semaphore resource in Dispose method directly,
//            // otherwise those Wait threads will hang forever in sema.Wait, so that we need dispose semaphore
//            // after all threads quit by itself, please check {TryEndThread} method
//            //_semaToken.Dispose();
//            //_sema.Dispose();
//        }
//
//
//        private void StartThread<T>()
//        {
//            // delay new and start a thread
//            lock(_threads)
//            {
//                if(_threads.Count < MaxThreads)
//                {
//                    var t = new Thread(ThreadFunc<T>)
//                    {
//                        Name = Name + " Thread " + _threads.Count,
//                        IsBackground = IsBackground,
//                        Priority = Priority
//                    };
//
//                    t.Start();
//                    _threads.Add(t);
//
//                    Log("[Burner]: start a thread '{0}' in thread pool", t.Name);
//                }
//            }
//        }
//
//        private void ThreadFunc<T>()
//        {
//            ThreadLive();
//            try
//            {
//                var timeOut = IdleTimeInMilliSec <= 0 ? -1 : IdleTimeInMilliSec;
//                while(!Disposed)
//                {
//                    if(_sema.Wait(timeOut, _semaToken.Token))
//                    {
//                        Task<T> task;
//                        lock(_tasks)
//                        {
//                            if(_tasks.Count == 0) continue;
//
//                            task = _tasks.Dequeue() as Task<T>;
//                            if(task == null) continue;
//                            CurrRunningTasks++;
//                        }
//
//                        try
//                        {
//                            task.Status = TaskStatus.Running;
//                            task.Result = task._run.Invoke();
//                            task.Status = TaskStatus.RanToCompletion;
//                        }
//                        catch(Exception ex)
//                        {
//                            Debug.LogException(ex);
//                            task.Status = TaskStatus.Faulted;
//                        }
//                        finally
//                        {
//                            lock(_tasks) CurrRunningTasks--;
//                        }
//                    }
//                    else
//                    {
//                        if(TryEndThread(Thread.CurrentThread, false)) break;
//                    }
//                }
//
//                Log("[Burner]: '{0}' thread exits by itself.", Thread.CurrentThread.Name);
//            }
//            catch(OperationCanceledException)
//            {
//                Log("[Burner]: '{0}' thread has been Cancelled in thread pool", Thread.CurrentThread.Name);
//            }
//            catch(ObjectDisposedException)
//            {
//                Log("[Burner]: '{0}' thread has been Disposed in thread pool", Thread.CurrentThread.Name);
//            }
//            catch(ThreadInterruptedException)
//            {
//                Log("[Burner]: '{0}' thread has been Interrupted in thread pool", Thread.CurrentThread.Name);
//            }
//            catch(Exception ex)
//            {
//                Warn("[Burner]: '{0}' thread exception: {1} \n {2}",
//                    Thread.CurrentThread.Name, ex.GetType(), ex.ToString());
//            }
//            finally
//            {
//                ThreadDead();
//                TryEndThread(Thread.CurrentThread, true);
//            }
//        }
//
//        public void Run<T>(Func<T> run, int multi = -1)
//        {
//            if(multi == -1)
//            {
//                Task<T>.CreateTask(this, run).Start();
//            }
//            else
//            {
//                while(CurrQueuingTasks < multi && CurrQueuingTasks < MaxThreads)
//                {
//                    Task<T>.CreateTask(this, run).Start();
//                }
//            }
//
//        }
//
//        public void Run(Action run, int multi = -1)
//        {
//            if(multi == -1)
//            {
//                Task<int>.CreateTask(this, () => { run.Invoke(); return 0; }).Start();
//            }
//            else
//            {
//                while(CurrQueuingTasks < multi && CurrQueuingTasks < MaxThreads)
//                {
//                    Task<int>.CreateTask(this, () => { run.Invoke(); return 0; }).Start();
//                }
//            }
//        }
//
//        ~ThreadPool()
//        {
//            Dispose();
//        }
//    }
//}
