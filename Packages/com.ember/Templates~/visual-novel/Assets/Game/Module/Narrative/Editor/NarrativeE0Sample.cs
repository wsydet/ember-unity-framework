using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>Independent presentation sample, using existing table resources and formal reader UI.</summary>
    public static class NarrativeE0Sample
    {
        #region 内部参数
        private const string ROOT = "Assets/GameResource/Resources/Config/Narrative/PresentationE0/";
        [Serializable] private sealed class Commands { public List<NovelCommand> _commands; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static T Asset<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, ROOT + name + ".asset"); return asset;
        }
        private static void References(UnityEngine.Object asset, string field, params UnityEngine.Object[] values)
        {
            using var data = new SerializedObject(asset); var p = data.FindProperty(field);
            if (p.isArray) { p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            else p.objectReferenceValue = values[0];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string CreateMissing()
        {
            NarrativeEditorAvailability.RequireEnabled();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NarrativeGraphModel.IsTemplateActive()) throw new InvalidOperationException("仅在 visual-novel 编辑模式制作");
            if (File.Exists(ROOT + "Story.asset")) return ROOT + "Story.asset";
            Directory.CreateDirectory(ROOT); AssetDatabase.Refresh();
            var story = Asset<NarrativeStorySO>("Story"); var chapter = Asset<NarrativeChapterSO>("Chapter");
            var talk = Asset<NarrativeDialogueSO>("Demonstration"); var end = Asset<NarrativeEndingSO>("End");
            JsonUtility.FromJsonOverwrite("{\"_storyId\":\"presentation_e0\",\"_displayName\":\"E0 演出执行基础\"}", story);
            JsonUtility.FromJsonOverwrite("{\"_chapterId\":\"presentation_e0\",\"_displayName\":\"透明度、并行与等待\"}", chapter);
            JsonUtility.FromJsonOverwrite("{\"_chapterId\":\"presentation_e0\",\"_nodeId\":\"demo\"}", talk);
            JsonUtility.FromJsonOverwrite("{\"_chapterId\":\"presentation_e0\",\"_nodeId\":\"end\",\"_endingId\":\"e0_complete\"}", end);
            NovelCommand Say(string id, string text) => new(id, NovelCommandKind.Say, text: text, lineId: id);
            NovelCommand Fade(string id, string target, float alpha, float duration, float delay = 0) =>
                new(id, NovelCommandKind.Opacity, duration: duration, instanceId: target, targetKind: NovelTargetKind.Character,
                    actionId: id, parallel: true, opacity: alpha, delay: delay);
            var commands = new List<NovelCommand>
            {
                new("background", NovelCommandKind.Background, resourceKey: "campus"),
                new("one", NovelCommandKind.Character, resourceKey: "alice_neutral", instanceId: "alice-main", slot: NovelPortraitSlot.Left),
                new("two", NovelCommandKind.Character, resourceKey: "alice_smile", instanceId: "alice-memory", slot: NovelPortraitSlot.Right),
                Say("intro", "E0 独立演示：同一人物有两个实例，身份不依赖左/右位置。下一句同时启动两侧透明度动作。"),
                Fade("left-dim", "alice-main", .2f, 8), Fade("right-dim", "alice-memory", .4f, 6, 1),
                Say("parallel", "两侧正在同时淡出。打开菜单会暂停，2X/3X 会加速。此处存档记录最终透明度；快速读档直接恢复目标状态。"),
                new("join", NovelCommandKind.WaitActions, waitActions: new[] { "left-dim", "right-dim" }),
                Say("joined", "两个动作均已结束。下一段先变亮，再从中间状态接管左侧动作。"),
                Fade("old", "alice-main", 1, 6), new("timer", NovelCommandKind.Wait, duration: 1),
                Fade("takeover", "alice-main", .5f, 3, .5f),
                new("join-takeover", NovelCommandKind.WaitActions, waitActions: new[] { "old", "takeover" }),
                new("expression", NovelCommandKind.Character, resourceKey: "alice_smile", instanceId: "alice-memory", visualAction: NovelVisualAction.Replace),
                new("stage", NovelCommandKind.Opacity, actionId: "stage-dim", opacity: .7f, duration: 2),
                Say("final", "旧动作已取消，等待组正常结束。右侧按实例换表情，舞台透明度不影响阅读 UI。重读可检查已读快进；返回菜单后再次进入应无残留。")
            };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands { _commands = commands }), talk);
            References(talk, "_next", end); References(chapter, "_entry", talk); References(chapter, "_nodes", talk, end);
            References(story, "_entry", chapter); References(story, "_chapters", chapter);
            foreach (var asset in new UnityEngine.Object[] { story, chapter, talk, end }) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
            return ROOT + "Story.asset";
        }
        #endregion
    }
}
