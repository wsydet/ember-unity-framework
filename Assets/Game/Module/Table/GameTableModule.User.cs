// Project-owned Table Module extensions. This file is never generator-owned.

using Game.Narrative;

namespace Game.Module
{
    public sealed partial class GameTableModule
    {
        #region 内部参数
        public NarrativeTableCatalog NovelCatalog { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        partial void OnGameTableModuleInitialized()
        {
            NovelCatalog = new NarrativeTableCatalog(Database);
            // 多语言与皮肤跟着配表一起装配：缺表时安装方法内部按未接入处理，行为与接入前一致。
            NovelLocalization.Install(NovelCatalog);
            NovelSkin.Install(NovelCatalog);
        }

        partial void OnGameTableModuleDestroying()
        {
            NovelLocalization.Uninstall();
            NovelSkin.Uninstall();
            NovelCatalog = null;
        }

        partial void OnResetGameTableModuleData()
        {
            NovelLocalization.Uninstall();
            NovelCatalog = null;
        }
        #endregion
    }
}
