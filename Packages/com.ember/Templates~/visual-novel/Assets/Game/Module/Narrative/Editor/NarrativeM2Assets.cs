using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Narrative.Editor
{
    /// <summary>维护 M2 测试夹具；图片复用 LastLight 正式素材；保留现有 SO、GUID 和稳定 ID，不覆盖已有音画文件。</summary>
    public static class NarrativeM2Assets
    {
        #region 内部参数
        private const string RESOURCE = "UI/Module/Narrative/Atlas/LastLight/";
        private const string AUDIO_RESOURCE = "Audio/Narrative/M1Sample/";
        [Serializable] private sealed class Commands { public List<NovelCommand> _commands; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void AudioFile(string name, bool music)
        {
            string path = "Assets/GameResource/Resources/" + AUDIO_RESOURCE + name + ".wav"; if (File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            int rate=22050, count=music?rate*4:rate/4;
            using(var stream=File.Create(path)) using(var writer=new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(rate); writer.Write(rate*2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count*2);
                for(int i=0;i<count;i++)
                {
                    double t=i/(double)rate;
                    double envelope=Math.Min(1,t*12)*Math.Min(1,(count-i)/(double)rate*12);
                    double wave=music?(Math.Sin(t*2*Math.PI*220)+Math.Sin(t*2*Math.PI*277.25)+Math.Sin(t*2*Math.PI*329.63))*.035
                        :Math.Sin(t*2*Math.PI*660)*.15*(1-i/(double)count);
                    writer.Write((short)(wave*envelope*short.MaxValue));
                }
            }
            AssetDatabase.ImportAsset(path);
        }
        private static void AddCommands(NarrativeDialogueSO node, params NovelCommand[] commands)
        {
            if (node.Commands.Any(c => c.CommandId.StartsWith("m2_", StringComparison.Ordinal))) return;
            var list = new List<NovelCommand>(commands); list.AddRange(node.Commands);
            Undo.RecordObject(node,"M2 样例演出");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Commands { _commands=list }),node);
            EditorUtility.SetDirty(node); AssetDatabase.SaveAssetIfDirty(node);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string CreateMissing()
        {
            NarrativeEditorAvailability.RequireEnabled();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NarrativeGraphModel.IsTemplateActive()) throw new InvalidOperationException("仅在 visual-novel 编辑模式制作");
            AudioFile("quiet_afternoon",true); AudioFile("choice_chime",false);
            const string sources="Assets/GameResource/TableSources/";
            // M1 表为空。只在没有数据行时填入本阶段已确认的占位映射。
            void Table(string name,string body)
            {
                string path=sources+name+".etable.csv";
                if(File.ReadAllLines(path).Count(l=>!string.IsNullOrWhiteSpace(l))>1) return;
                File.WriteAllText(path,body,new System.Text.UTF8Encoding(false)); AssetDatabase.ImportAsset(path);
            }
            Table("novel_backgrounds","id,resourcePath\ncampus,"+RESOURCE+"Backgrounds/rooftop_dusk\nevening,"+RESOURCE+"Backgrounds/rooftop_dusk\n");
            Table("novel_portraits","id,characterId,expression,resourcePath\nalice_neutral,alice,neutral,"+RESOURCE+"Portraits/lin_evening\nalice_smile,alice,smile,"+RESOURCE+"Portraits/lin_evening\nlin_neutral,lin,neutral,"+RESOURCE+"Portraits/zhou_evening\n");
            Table("novel_audio","id,category,resourcePath\nquiet_afternoon,BGM,"+AUDIO_RESOURCE+"quiet_afternoon\nchoice_chime,SFX,"+AUDIO_RESOURCE+"choice_chime\n");
            string inputPath="Assets/GameResource/Resources/Config/Narrative/NovelInput.inputactions";
            if(!File.Exists(inputPath))
            {
                var actions=ScriptableObject.CreateInstance<InputActionAsset>();
                try
                {
                    var ui=actions.AddActionMap("UI"); ui.AddAction("Idle",InputActionType.Button);
                    var novel=actions.AddActionMap("Novel");
                    var advance=novel.AddAction("Advance",InputActionType.Button); advance.AddBinding("<Keyboard>/space"); advance.AddBinding("<Keyboard>/enter");
                    novel.AddAction("Menu",InputActionType.Button,"<Keyboard>/escape");
                    File.WriteAllText(inputPath,actions.ToJson()); AssetDatabase.ImportAsset(inputPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(actions); }
            }
            var story=AssetDatabase.LoadAssetAtPath<NarrativeStorySO>("Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/M1StoryFixture.asset");
            foreach(var chapter in story.Chapters)
            {
                var intro=chapter.Entry as NarrativeDialogueSO;
                if(intro!=null) AddCommands(intro,
                    new NovelCommand("m2_"+chapter.ChapterId+"_background",NovelCommandKind.Background,resourceKey:chapter.AssetPrefix.Contains("Return")?"evening":"campus",duration:.35f),
                    new NovelCommand("m2_"+chapter.ChapterId+"_bgm",NovelCommandKind.BGM,resourceKey:"quiet_afternoon"),
                    new NovelCommand("m2_"+chapter.ChapterId+"_alice",NovelCommandKind.Character,resourceKey:"alice_neutral",slot:NovelPortraitSlot.Left,duration:.3f),
                    new NovelCommand("m2_"+chapter.ChapterId+"_lin",NovelCommandKind.Character,resourceKey:"lin_neutral",slot:NovelPortraitSlot.Right,duration:.3f));
                foreach(var node in chapter.Nodes.OfType<NarrativeDialogueSO>())
                    if(node.NodeId=="ask") AddCommands(node,new NovelCommand("m2_smile",NovelCommandKind.Character,resourceKey:"alice_smile",slot:NovelPortraitSlot.Left,visualAction:NovelVisualAction.Replace,duration:.2f),new NovelCommand("m2_chime",NovelCommandKind.SFX,resourceKey:"choice_chime"));
                    else if(node.NodeId=="leave") AddCommands(node,new NovelCommand("m2_hide",NovelCommandKind.Character,slot:NovelPortraitSlot.Left,visualAction:NovelVisualAction.Hide,duration:.2f));
            }
            return "M2 占位资源、输入与演出指令已创建；请预览并导出配表。";
        }
        #endregion
    }
}
