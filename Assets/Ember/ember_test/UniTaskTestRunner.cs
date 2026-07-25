using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Ember.Test
{
    /// <summary>
    /// UniTask 功能完整性测试器。
    /// 挂载到任意 GameObject 上，Play Mode 下自动运行各项测试并输出结果到 Console。
    /// </summary>
    public class UniTaskTestRunner : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = true;

        private int _passedCount;
        private int _failedCount;
        private readonly List<string> _failureDetails = new();

        private void Start()
        {
            if (_runOnStart)
            {
                RunAllTests().Forget();
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTestsViaContextMenu()
        {
            RunAllTests().Forget();
        }

        private async UniTaskVoid RunAllTests()
        {
            _passedCount = 0;
            _failedCount = 0;
            _failureDetails.Clear();

            Debug.Log("═══════════════════════════════");
            Debug.Log("🧪 <b>UniTask 功能测试开始</b>");
            Debug.Log("═══════════════════════════════");

            await Test_Delay();
            await Test_Yield();
            await Test_WhenAll();
            await Test_WhenAny();
            await Test_CancellationToken();
            await Test_RunOnThreadPool();
            await Test_AsyncLazy();
            await Test_AsyncReactiveProperty();
            await Test_Forget();
            await Test_SwitchToMainThread();

            Debug.Log("═══════════════════════════════");
            Debug.Log($"🧪 <b>测试完成</b> — 通过: <color=green>{_passedCount}</color> / 失败: <color=red>{_failedCount}</color>");
            foreach (var detail in _failureDetails)
            {
                Debug.LogError($"  ❌ {detail}");
            }
            Debug.Log("═══════════════════════════════");
        }

        // ═══════════════════════════════════
        //  Test 1: UniTask.Delay
        // ═══════════════════════════════════
        private async UniTask Test_Delay()
        {
            Debug.Log("── ⏱ UniTask.Delay ──");

            var start = Time.time;
            await UniTask.Delay(200); // 200ms
            var elapsed = (Time.time - start) * 1000f;

            // Time.time 精度有限，允许较大容差
            AssertTrue(elapsed >= 100f, $"Delay 200ms 实际等待 {elapsed:F0}ms ≥ 100ms");
            AssertTrue(elapsed <= 500f, $"Delay 200ms 实际等待 {elapsed:F0}ms ≤ 500ms(含帧开销)");

            // DelayFrame
            var frame = Time.frameCount;
            await UniTask.DelayFrame(3);
            var framesPassed = Time.frameCount - frame;
            AssertTrue(framesPassed >= 3, $"DelayFrame(3) 实际经过 {framesPassed} 帧 ≥ 3");

            // Cancel 后不抛异常的 Delay
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await UniTask.Delay(1000, cancellationToken: cts.Token).SuppressCancellationThrow();
            AssertTrue(true, "取消后的 Delay + SuppressCancellationThrow 不抛异常");
        }

        // ═══════════════════════════════════
        //  Test 2: Yield
        // ═══════════════════════════════════
        private async UniTask Test_Yield()
        {
            Debug.Log("── 🔄 UniTask.Yield ──");

            var frame = Time.frameCount;
            await UniTask.Yield(); // 等一帧
            AssertTrue(Time.frameCount > frame, "Yield() 后帧号递增");

            var initFrame = Time.frameCount;
            await UniTask.WaitForEndOfFrame();
            AssertTrue(Time.frameCount >= initFrame, "WaitForEndOfFrame() 不抛异常");
        }

        // ═══════════════════════════════════
        //  Test 3: WhenAll
        // ═══════════════════════════════════
        private async UniTask Test_WhenAll()
        {
            Debug.Log("── 📦 UniTask.WhenAll ──");

            bool a = false, b = false, c = false;

            async UniTask TaskA() { await UniTask.Delay(50); a = true; }
            async UniTask TaskB() { await UniTask.Delay(80); b = true; }
            async UniTask TaskC() { await UniTask.Delay(120); c = true; }

            var start = Time.time;
            await UniTask.WhenAll(TaskA(), TaskB(), TaskC());
            var elapsed = (Time.time - start) * 1000f;

            AssertTrue(a && b && c, "WhenAll 三个子任务全部执行完毕");
            AssertTrue(elapsed < 500f, $"WhenAll 总耗时 {elapsed:F0}ms ≈ 最慢子任务(120ms)");
        }

        // ═══════════════════════════════════
        //  Test 4: WhenAny
        // ═══════════════════════════════════
        private async UniTask Test_WhenAny()
        {
            Debug.Log("── 🏁 UniTask.WhenAny ──");

            int winner = 0;

            async UniTask Fast() { await UniTask.Delay(30); winner = 1; }
            async UniTask Slow() { await UniTask.Delay(500); winner = 2; }

            await UniTask.WhenAny(Fast(), Slow());
            AssertEqual(1, winner, "WhenAny 最快完成的子任务先返回 (winner=1)");
        }

        // ═══════════════════════════════════
        //  Test 5: CancellationToken
        // ═══════════════════════════════════
        private async UniTask Test_CancellationToken()
        {
            Debug.Log("── 🛑 CancellationToken 取消 ──");

            // 正常取消
            var cts = new CancellationTokenSource();
            bool cancelled = false;

            var task = UniTask.Delay(5000, cancellationToken: cts.Token);
            try
            {
                cts.CancelAfter(50);
                await UniTask.Delay(60);
                await task;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            AssertTrue(cancelled, "CancellationToken.CancelAfter 触发 OperationCanceledException");

            // 链式取消
            var parentCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);
            bool linkedCancelled = false;

            var linkedTask = UniTask.Delay(5000, cancellationToken: linkedCts.Token);
            parentCts.CancelAfter(30);
            await UniTask.Delay(60);
            linkedCancelled = linkedTask.Status == UniTaskStatus.Canceled;

            AssertTrue(linkedCancelled, "LinkedTokenSource 跟随父 Token 一起取消");
        }

        // ═══════════════════════════════════
        //  Test 6: RunOnThreadPool
        // ═══════════════════════════════════
        private async UniTask Test_RunOnThreadPool()
        {
            Debug.Log("── 🧵 RunOnThreadPool ──");

            int mainThread = Thread.CurrentThread.ManagedThreadId;
            int poolThread = mainThread;

            await UniTask.RunOnThreadPool(() =>
            {
                poolThread = Thread.CurrentThread.ManagedThreadId;
            });

            AssertTrue(poolThread != mainThread,
                $"RunOnThreadPool 在线程池执行 (main={mainThread}, pool={poolThread})");
        }

        // ═══════════════════════════════════
        //  Test 7: AsyncLazy
        // ═══════════════════════════════════
        private async UniTask Test_AsyncLazy()
        {
            Debug.Log("── 💤 AsyncLazy ──");

            int callCount = 0;
            var lazy = new AsyncLazy<int>(async () =>
            {
                callCount++;
                await UniTask.Delay(30);
                return 42;
            });

            var r1 = await lazy.Task;
            var r2 = await lazy.Task; // 第二次应直接返回缓存值
            AssertEqual(42, r1, "AsyncLazy 返回正确值");
            AssertEqual(42, r2, "第二次访问返回相同值");
            AssertEqual(1, callCount, "工厂方法只执行一次");
        }

        // ═══════════════════════════════════
        //  Test 8: AsyncReactiveProperty
        // ═══════════════════════════════════
        private async UniTask Test_AsyncReactiveProperty()
        {
            Debug.Log("── 📡 AsyncReactiveProperty ──");

            var prop = new AsyncReactiveProperty<int>(0);

            int received1 = -1;
            prop.Subscribe(v => received1 = v);

            prop.Value = 100;
            await UniTask.Yield(); // Subscribe 异步触发，等一帧
            AssertEqual(100, received1, "Subscribe 回调收到新值");

            // WithoutCurrent
            int received2 = -1;
            prop.WithoutCurrent().Subscribe(v => received2 = v);
            AssertEqual(-1, received2, "WithoutCurrent 不推送当前值");
            prop.Value = 200;
            await UniTask.Yield();
            AssertEqual(200, received2, "变化后 WithoutCurrent 收到新值");
        }

        // ═══════════════════════════════════
        //  Test 9: Forget (UniTaskVoid)
        // ═══════════════════════════════════
        private async UniTask Test_Forget()
        {
            Debug.Log("── 🔥 Forget (UniTaskVoid) ──");

            bool executed = false;

            FireAndForgetTest().Forget();

            async UniTaskVoid FireAndForgetTest()
            {
                await UniTask.Delay(50);
                executed = true;
            }

            await UniTask.Delay(200);
            AssertTrue(executed, "Forget() 启动的 UniTaskVoid 正常执行完成");
        }

        // ═══════════════════════════════════
        //  Test 10: SwitchToMainThread
        // ═══════════════════════════════════
        private async UniTask Test_SwitchToMainThread()
        {
            Debug.Log("── 🔀 SwitchToMainThread ──");

            int mainThread = Thread.CurrentThread.ManagedThreadId;
            int afterPoolThread = mainThread;

            await UniTask.RunOnThreadPool(() =>
            {
                afterPoolThread = Thread.CurrentThread.ManagedThreadId;
            });
            AssertTrue(afterPoolThread != mainThread, "RunOnThreadPool 在非主线程");

            await UniTask.SwitchToMainThread();
            int backOnMainThread = Thread.CurrentThread.ManagedThreadId;
            AssertEqual(mainThread, backOnMainThread, "SwitchToMainThread 后回到主线程");
        }

        // ═══════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════

        private void AssertTrue(bool condition, string description)
        {
            if (condition) { _passedCount++; Debug.Log($"  ✅ {description}"); }
            else { _failedCount++; _failureDetails.Add(description); Debug.LogError($"  ❌ {description} (期望 true)"); }
        }

        private void AssertFalse(bool condition, string description)
        {
            if (!condition) { _passedCount++; Debug.Log($"  ✅ {description}"); }
            else { _failedCount++; _failureDetails.Add(description); Debug.LogError($"  ❌ {description} (期望 false)"); }
        }

        private void AssertEqual(object expected, object actual, string description)
        {
            if (Equals(expected, actual)) { _passedCount++; Debug.Log($"  ✅ {description}"); }
            else { _failedCount++; _failureDetails.Add($"{description} (期望 {expected}，实际 {actual})"); Debug.LogError($"  ❌ {description} (期望 {expected}，实际 {actual})"); }
        }
    }
}
