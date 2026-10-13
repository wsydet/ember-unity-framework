using System;
using System.IO;
using System.Linq;
using Game.PlayerData;
using UnityEngine;

namespace Game.Module
{
    /// <summary>只配置通用存储，不包含业务默认进度；每个项目使用独立目录和文档族。</summary>
    [CreateAssetMenu(fileName = "PlayerDataConfig", menuName = "Game/Save/Module Config")]
    public sealed class PlayerDataConfig : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _directoryName = "BaseProgress";
        [SerializeField] private string _dataTypeId = "ember.base.progress";
        [SerializeField] private int[] _slotIds = { 0, 1, 2 };
        [SerializeField] private int _queueCapacity = 16;
        [SerializeField] private int _maxPayloadBytes = 4 * 1024 * 1024;
        [SerializeField] private int _maxIndexBytes = 4 * 1024 * 1024;
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        public int QueueCapacity => _queueCapacity;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public ProgressStoreOptions CreateOptions(string persistentRoot)
        {
            if (string.IsNullOrWhiteSpace(persistentRoot) || !Path.IsPathFullyQualified(persistentRoot))
                throw new ArgumentException("持久化根目录必须是完整绝对路径。");
            if (string.IsNullOrWhiteSpace(_directoryName) || _directoryName.Length > 64 ||
                _directoryName.Any(c => !(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')
                    && !(c >= '0' && c <= '9') && c != '-' && c != '_'))
                throw new ArgumentException("目录名只允许 1—64 个英文字母、数字、横线和下划线。");
            if (_queueCapacity < 1 || _queueCapacity > 1024) throw new ArgumentOutOfRangeException(nameof(_queueCapacity));
            return new ProgressStoreOptions(Path.Combine(persistentRoot, "EmberProgress", _directoryName),
                _dataTypeId, _slotIds, _maxPayloadBytes, _maxIndexBytes);
        }

        /// <summary>用于项目装配/测试，模块 OnInit 时复制配置；修改不影响已准备服务。</summary>
        public void Configure(string directoryName, string dataTypeId, int[] slotIds, int queueCapacity = 16,
            int maxPayloadBytes = 4 * 1024 * 1024, int maxIndexBytes = 4 * 1024 * 1024)
        {
            _directoryName = directoryName; _dataTypeId = dataTypeId;
            _slotIds = slotIds == null ? Array.Empty<int>() : (int[])slotIds.Clone();
            _queueCapacity = queueCapacity; _maxPayloadBytes = maxPayloadBytes; _maxIndexBytes = maxIndexBytes;
        }
        #endregion
    }
}
