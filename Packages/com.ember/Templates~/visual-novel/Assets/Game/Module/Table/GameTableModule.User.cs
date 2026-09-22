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
        }

        partial void OnGameTableModuleDestroying()
        {
            NovelCatalog = null;
        }

        partial void OnResetGameTableModuleData()
        {
            NovelCatalog = null;
        }
        #endregion
    }
}
