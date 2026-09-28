using System.Linq;
using Ember.Table.Editor;
using Ember.UIExtension.Editor;
using UnityEditor;

namespace Game.Narrative.Editor
{
    /// <summary>小说对通用文案工具的注册适配，框架不引用小说表名。</summary>
    public static class NovelContentToolsSetup
    {
        #region 外部方法
        public static void RegisterTables()
        {
            foreach (string id in new[] { "novel_ui_text", "novel_content_text" })
            {
                var definition = EmberTablePipeline.FindAllDefinitions().First(d => d.TableId == id);
                var source = EmberLocalizationEditorService.Register(id, id == "novel_ui_text" ? "小说 UI 文案" : "小说剧情文案",
                    AssetDatabase.GetAssetPath(definition.Source), definition);
                source.TranslationContext = id == "novel_ui_text" ? "游戏界面文案，保持简洁，保留变量。" : "视觉小说对白与角色名称；遵循人物称呼和前后台词语境。";
                EmberLocalizationEditorService.Save(source, EmberTableSourceDocument.Load(AssetDatabase.GetAssetPath(source.Source)));
            }
        }
        #endregion
    }
}
