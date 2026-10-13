using System;
using System.IO;
using Ember.Basic;
using Ember.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Module.Editor
{
    /// <summary>只读检查入口。旧档导入及新库删除都不通过文件名通配符隐式执行。</summary>
    public static class PlayerDataSaveTools
    {
        #region 外部方法
        [MenuItem("Ember/存档/选择存储配置")]
        public static void SelectConfiguration()
        {
            var config = Resources.Load<PlayerDataConfig>(PlayerDataModule.CONFIG_PATH);
            Selection.activeObject = config;
            if (config != null) EditorGUIUtility.PingObject(config);
        }

        [MenuItem("Ember/存档/查看存储目录")]
        public static void RevealDirectory()
        {
            try
            {
                var config = Resources.Load<PlayerDataConfig>(PlayerDataModule.CONFIG_PATH);
                var root = config.CreateOptions(Application.persistentDataPath).RootDirectory;
                if (Directory.Exists(root)) EditorUtility.RevealInFinder(root);
                else EmberDebug.Log(LogTags.Game, "存储目录尚未创建：" + root);
            }
            catch (Exception exception) { EmberDebug.LogError(LogTags.Game, exception.Message); }
        }

        [MenuItem("Ember/存档/选择存储配置", true)]
        [MenuItem("Ember/存档/查看存储目录", true)]
        public static bool ValidateMenu() => EmberModuleCollector.Instance.IsModuleEnabled<PlayerDataModule>();
        #endregion
    }
}
