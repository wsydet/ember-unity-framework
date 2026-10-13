using System;

namespace Game.Module
{
    /// <summary>base 的完整示例进度模型；项目自行替换/扩展，通用模块不依赖此类型。</summary>
    [Serializable]
    public sealed class PlayerData
    {
        #region 编辑器面板参数
        public int version = 1;
        public int level = 1;
        public int coins;
        #endregion
    }
}
