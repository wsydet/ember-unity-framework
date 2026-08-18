using UnityEngine;
using Ember.Core;

namespace Game.Module
{
    /// <summary>
    /// 玩家偏好存储模块 —— 封装 UnityEngine.PlayerPrefs，提供类型安全的读写。
    ///
    /// Phase = <see cref="ModulePhase.Global"/>：Init 状态启动，常驻到游戏退出。
    /// 由 <see cref="EmberModuleCollector"/> 自动初始化/销毁，业务代码直接
    /// <c>PlayerPrefsModule.Instance.GetInt(...)</c> 访问。
    ///
    /// 设计要点：
    /// - bool 以 int 0/1 存储（PlayerPrefs 不支持 bool）
    /// - OnDestroy 时 Save() 刷写，确保退出前落盘
    /// - 无热重启状态（ResetModuleData 空实现）
    /// </summary>
    public class PlayerPrefsModule : EmberSingleton<PlayerPrefsModule>, IEmberModule
    {
        public int Phase => ModulePhase.Global;

        #region 生命周期

        void IEmberModule.OnInit() { }

        void IEmberModule.OnDestroy()
        {
            PlayerPrefs.Save();
        }

        void IEmberModule.ResetModuleData() { }

        #endregion

        // ============================================================

        #region 外部方法

        // ---- int ----

        public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        // ---- float ----

        public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);

        // ---- string ----

        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

        // ---- bool ----

        public bool GetBool(string key, bool defaultValue = false)
            => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;

        public void SetBool(string key, bool value)
            => PlayerPrefs.SetInt(key, value ? 1 : 0);

        // ---- 键管理 ----

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

        public void DeleteAll() => PlayerPrefs.DeleteAll();

        // ---- 刷写 ----

        public void Save() => PlayerPrefs.Save();

        #endregion
    }
}
