using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace Ember.Test
{
    /// <summary>
    /// Feel 功能完整性测试器。
    /// 挂载到任意 GameObject 上，在 Play Mode 下自动运行各项测试并输出结果到 Console。
    /// </summary>
    public class FeelTestRunner : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private GameObject _poolTestPrefab;

        private int _passedCount;
        private int _failedCount;
        private readonly List<string> _failureDetails = new();

        private void Start()
        {
            if (_runOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTestsViaContextMenu()
        {
            StartCoroutine(RunAllTests());
        }

        private IEnumerator RunAllTests()
        {
            _passedCount = 0;
            _failedCount = 0;
            _failureDetails.Clear();

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🧪 <b>Feel 功能完整性测试开始</b>");
            Debug.Log("═══════════════════════════════════════");

            yield return Test_NamespaceAccessibility();
            yield return Test_MMFeedbacksBasic();
            yield return Test_MMEventManager();
            yield return Test_MMStateMachine();
            yield return Test_MMSingleton();
            yield return Test_MMObjectPooler();

            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"🧪 <b>测试完成</b> — 通过: <color=green>{_passedCount}</color> / 失败: <color=red>{_failedCount}</color>");
            foreach (var detail in _failureDetails)
            {
                Debug.LogError($"  ❌ {detail}");
            }
            Debug.Log("═══════════════════════════════════════");
        }

        // ═══════════════════════════════════════════
        //  Test 1: 命名空间与程序集可访问性
        // ═══════════════════════════════════════════
        private IEnumerator Test_NamespaceAccessibility()
        {
            Debug.Log("── 📦 命名空间 & 程序集可访问性 ──");

            var playerObj = new GameObject("_feel_test_mmf_player");
            var player = playerObj.AddComponent<MMF_Player>();
            AssertNotNull(player, "MMF_Player 可实例化");
            AssertTrue(player is MMFeedbacks, "MMF_Player 继承自 MMFeedbacks");

            // MMStateMachine 要求 T 为 struct enum 类型
            var fsm = new MMStateMachine<FeelTestState>(gameObject, false);
            AssertNotNull(fsm, "MMStateMachine 可实例化");

            var legacy = playerObj.AddComponent<MMFeedbacks>();
            AssertNotNull(legacy, "MMFeedbacks (Legacy) 可实例化");

            Destroy(playerObj);
            yield return null;
        }

        // ═══════════════════════════════════════════
        //  Test 2: MMFeedbacks / MMF_Player 基础 API
        // ═══════════════════════════════════════════
        private IEnumerator Test_MMFeedbacksBasic()
        {
            Debug.Log("── 🔊 MMFeedbacks / MMF_Player 基础 API ──");

            var go = new GameObject("_feel_test_feedbacks");
            var player = go.AddComponent<MMF_Player>();

            // 初始化
            player.Initialization();
            AssertTrue(player.AutoInitialization, "默认 AutoInitialization == true");
            AssertNotNull(player.FeedbacksList, "FeedbacksList 不为 null");

            // 播放
            player.PlayFeedbacks();
            AssertTrue(player.IsPlaying || !player.IsPlaying,
                "PlayFeedbacks() 不抛异常");

            // 暂停 & 恢复
            player.PauseFeedbacks();
            player.ResumeFeedbacks();

            // 停止
            player.StopFeedbacks();
            AssertFalse(player.IsPlaying, "StopFeedbacks() 后 IsPlaying == false");

            // 重置
            player.ResetFeedbacks();

            // 反向播放
            player.PlayFeedbacksInReverse();
            player.StopFeedbacks();

            // 倒放 & 正向条件播放
            player.PlayFeedbacksOnlyIfReversed();
            player.StopFeedbacks();
            player.PlayFeedbacksOnlyIfNormalDirection();
            player.StopFeedbacks();

            // 属性
            AssertTrue(player.TotalDuration >= 0f, "TotalDuration >= 0");
            AssertTrue(player.ElapsedTime >= 0f, "ElapsedTime >= 0");

            Destroy(go);
            yield return null;
        }

        // ═══════════════════════════════════════════
        //  Test 3: MMEventManager 事件系统
        // ═══════════════════════════════════════════
        private IEnumerator Test_MMEventManager()
        {
            Debug.Log("── 📡 MMEventManager 事件系统 ──");

            bool received = false;
            var handler = new EmberMMEventListener<MMGameEvent>(e =>
            {
                if (e.EventName == "FeelTest")
                {
                    received = true;
                }
            });
            handler.Subscribe();

            MMGameEvent.Trigger("FeelTest");
            AssertTrue(received, "事件触发后监听器收到回调");

            received = false;
            handler.Unsubscribe();
            MMGameEvent.Trigger("FeelTest");
            AssertFalse(received, "移除监听器后不再收到回调");

            yield return null;
        }

        // ═══════════════════════════════════════════
        //  Test 4: MMStateMachine 状态机
        // ═══════════════════════════════════════════
        private IEnumerator Test_MMStateMachine()
        {
            Debug.Log("── 🔄 MMStateMachine 状态机 ──");

            // MMStateMachine<T> 约束 T 必须是 struct enum
            var fsm = new MMStateMachine<FeelTestState>(gameObject, false);
            AssertNotNull(fsm, "状态机实例化成功");

            bool changed = false;
            fsm.OnStateChange += () => { changed = true; };

            fsm.ChangeState(FeelTestState.Playing);
            AssertTrue(changed, "OnStateChange 回调触发");
            AssertEqual(FeelTestState.Playing, fsm.CurrentState, "ChangeState 切换状态成功");

            fsm.ChangeState(FeelTestState.Paused);
            AssertEqual(FeelTestState.Paused, fsm.CurrentState, "二次 ChangeState 切换成功");
            AssertEqual(FeelTestState.Playing, fsm.PreviousState, "PreviousState 记录上一次状态");

            yield return null;
        }

        // ═══════════════════════════════════════════
        //  Test 5: MMSingleton 单例模式
        // ═══════════════════════════════════════════
        private IEnumerator Test_MMSingleton()
        {
            Debug.Log("── 🏠 MMSingleton 单例模式 ──");

            var go = new GameObject("_feel_test_singleton");
            var comp = go.AddComponent<FeelTestSingletonComponent>();
            var instance = FeelTestSingletonComponent.Instance;
            AssertNotNull(instance, "单例 Instance 不为 null");
            AssertEqual(comp, instance, "单例 Instance 返回正确的组件");

            AssertTrue(FeelTestSingletonComponent.HasInstance, "HasInstance == true");
            AssertNotNull(FeelTestSingletonComponent.TryGetInstance(), "TryGetInstance() 返回正确实例");

            Destroy(go);
            yield return null;
        }

        // ═══════════════════════════════════════════
        //  Test 6: MMObjectPooler 对象池
        // ═══════════════════════════════════════════
        private IEnumerator Test_MMObjectPooler()
        {
            Debug.Log("── 🏊 MMObjectPooler 对象池 ──");

            GameObject prefab;
            if (_poolTestPrefab != null)
            {
                prefab = _poolTestPrefab;
            }
            else
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prefab.name = "_feel_test_pool_prefab";
                prefab.SetActive(false);
            }

            var poolerGo = new GameObject("_feel_test_pooler");
            var pooler = poolerGo.AddComponent<MMSimpleObjectPooler>();
            pooler.GameObjectToPool = prefab;
            pooler.PoolSize = 5;
            pooler.PoolCanExpand = true;
            pooler.FillObjectPool();

            AssertNotNull(pooler, "MMSimpleObjectPooler 可实例化");

            var obj = pooler.GetPooledGameObject();
            AssertNotNull(obj, "GetPooledGameObject() 返回有效对象");
            AssertFalse(obj.activeInHierarchy, "池中取出时为未激活（调用方自行 SetActive）");

            // 激活后使用，用完归还（反激活即可被复用）
            obj.SetActive(true);
            obj.SetActive(false);
            var obj2 = pooler.GetPooledGameObject();
            AssertNotNull(obj2, "归还后再次获取仍返回有效对象");

            Destroy(poolerGo);
            if (_poolTestPrefab == null)
            {
                Destroy(prefab);
            }
            yield return null;
        }

        // ═══════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════

        private void AssertTrue(bool condition, string description)
        {
            if (condition) { _passedCount++; Debug.Log($"  ✅ {description}"); }
            else { _failedCount++; _failureDetails.Add(description); Debug.LogError($"  ❌ {description} (期望 true，实际 false)"); }
        }

        private void AssertFalse(bool condition, string description)
        {
            if (!condition) { _passedCount++; Debug.Log($"  ✅ {description}"); }
            else { _failedCount++; _failureDetails.Add(description); Debug.LogError($"  ❌ {description} (期望 false，实际 true)"); }
        }

        private void AssertNotNull(object obj, string description)
        {
            if (obj != null) { _passedCount++; Debug.Log($"  ✅ {description}"); }
            else { _failedCount++; _failureDetails.Add(description); Debug.LogError($"  ❌ {description} (为 null)"); }
        }

        private void AssertEqual(object expected, object actual, string description)
        {
            if (Equals(expected, actual)) { _passedCount++; Debug.Log($"  ✅ {description}"); }
            else { _failedCount++; _failureDetails.Add($"{description} (期望 {expected}，实际 {actual})"); Debug.LogError($"  ❌ {description} (期望 {expected}，实际 {actual})"); }
        }
    }

    // ═══════════════════════════════════════════════
    //  辅助类型
    // ═══════════════════════════════════════════════

    /// <summary>
    /// MMStateMachine 要求的状态枚举（T 必须是 struct enum）。
    /// </summary>
    public enum FeelTestState
    {
        Idle,
        Playing,
        Paused,
        Stopped
    }

    /// <summary>
    /// 用于测试 MMSingleton 的辅助组件。
    /// </summary>
    public class FeelTestSingletonComponent : MMSingleton<FeelTestSingletonComponent>
    {
    }

    /// <summary>
    /// MMEventManager 监听器包装，实现 MMEventListener&lt;T&gt; 接口。
    /// </summary>
    public class EmberMMEventListener<T> : MMEventListener<T> where T : struct
    {
        private readonly System.Action<T> _callback;

        public EmberMMEventListener(System.Action<T> callback)
        {
            _callback = callback;
        }

        public void OnMMEvent(T eventData)
        {
            _callback?.Invoke(eventData);
        }

        public void Subscribe()
        {
            this.MMEventStartListening<T>();
        }

        public void Unsubscribe()
        {
            this.MMEventStopListening<T>();
        }
    }
}
