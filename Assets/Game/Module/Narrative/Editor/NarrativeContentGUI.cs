using System;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative.Editor
{
    internal static class NarrativeContentGUI
    {
        #region 内部方法
        private static readonly string[] CommandNames = { "对白 / 旁白", "设置变量", "等待", "背景", "立绘", "背景音乐", "音效", "独立配音", "透明度动作", "等待动作组", "人物移动", "人物缩放", "人物旋转", "人物镜像", "人物层级", "跳动 / 点头", "角色强调", "舞台 / 人物震动", "遮罩淡入淡出", "短暂闪光", "图片交叉淡化", "播放粒子效果", "停止粒子效果", "停止背景音乐", "播放环境循环音", "停止环境循环音", "环境音量", "剧情对白显隐", "舞台镜头", "背景擦除转场" };
        private static readonly string[] SlotNames = { "左侧", "居中", "右侧" };
        private static readonly string[] ActionNames = { "显示", "替换", "隐藏" };
        private static void EnumField(SerializedProperty parent, string field, string label, string[] names)
        {
            var property = parent.FindPropertyRelative(field);
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup(label, property.enumValueIndex, names);
            if (EditorGUI.EndChangeCheck()) property.enumValueIndex = selected;
        }
        private static string Seconds(SerializedProperty item) => item.FindPropertyRelative("_duration").floatValue.ToString("0.##") + " 秒";
        private static void Field(SerializedProperty parent, string name, string label)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }
        private static void Id(SerializedProperty item, string field, string label)
        {
            using (new EditorGUI.DisabledScope(true)) Field(item, field, label);
        }
        private static void Key(SerializedProperty item, string field, string label, NarrativeTableCatalog catalog, NovelCommandKind kind, bool character = false)
        {
            var property = item.FindPropertyRelative(field);
            Field(item, field, label);
            if (kind == NovelCommandKind.EffectPlay)
            { EditorGUILayout.LabelField("Resources/Effects/Narrative/资源键.prefab", EditorStyles.wordWrappedMiniLabel); return; }
            if (kind == NovelCommandKind.AmbientPlay) kind = NovelCommandKind.SFX;
            if (catalog?.IsReady != true) return;
            IEnumerable<string> source = character ? catalog.Characters.Select(r => r.Id) : kind switch
            {
                NovelCommandKind.Background => catalog.Backgrounds.Select(r => r.Id),
                NovelCommandKind.Character => catalog.Portraits.Select(r => r.Id),
                _ => catalog.Audio.Where(r => r.Category == kind.ToString()).Select(r => r.Id)
            };
            var keys = source.OrderBy(k => k, StringComparer.Ordinal).ToList();
            keys.Insert(0, "");
            int index = keys.IndexOf(property.stringValue);
            // Unknown keys remain editable; drawing the popup never silently replaces them.
            var labels = keys.Select(k => string.IsNullOrEmpty(k) ? "（留空）" : k).ToList();
            if (index < 0) { index = keys.Count; keys.Add(property.stringValue); labels.Add("（未找到）" + property.stringValue); }
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("从已导出配表选择", index, labels.ToArray());
            if (EditorGUI.EndChangeCheck()) property.stringValue = keys[selected];
            if (!character && catalog.TryResolve(kind, property.stringValue, out string path))
                EditorGUILayout.LabelField("Resources / " + path, EditorStyles.wordWrappedMiniLabel);
        }
        private static void Command(SerializedProperty item, NarrativeTableCatalog catalog)
        {
            var previousKind = (NovelCommandKind)item.FindPropertyRelative("_kind").enumValueIndex;
            EnumField(item, "_kind", "这一步做什么", CommandNames);
            var kind = (NovelCommandKind)item.FindPropertyRelative("_kind").enumValueIndex;
            if (kind != previousKind && (kind == NovelCommandKind.EffectPlay || kind == NovelCommandKind.EffectStop))
            {
                item.FindPropertyRelative("_duration").floatValue = 0; item.FindPropertyRelative("_delay").floatValue = 0;
                if (kind == NovelCommandKind.EffectPlay) item.FindPropertyRelative("_scale").vector2Value = Vector2.one;
            }
            if (kind != previousKind && kind == NovelCommandKind.Camera)
            { item.FindPropertyRelative("_cameraZoom").floatValue = 1; item.FindPropertyRelative("_position").vector2Value = Vector2.zero; }
            if (kind != previousKind && kind == NovelCommandKind.Wipe) item.FindPropertyRelative("_targetKind").enumValueIndex = (int)NovelTargetKind.Background;
            if (kind != previousKind && NovelActorRules.IsAction(kind))
            {
                var id = item.FindPropertyRelative("_actionId");
                if (string.IsNullOrWhiteSpace(id.stringValue)) id.stringValue = Guid.NewGuid().ToString("N");
                if (NovelScreenRules.IsAction(kind))
                {
                    if (item.FindPropertyRelative("_duration").floatValue <= 0) item.FindPropertyRelative("_duration").floatValue = .6f;
                    if (kind == NovelCommandKind.Flash)
                    { item.FindPropertyRelative("_color").colorValue = Color.white; item.FindPropertyRelative("_opacity").floatValue = .35f; }
                    if (kind == NovelCommandKind.Cover) item.FindPropertyRelative("_color").colorValue = Color.black;
                    if (kind == NovelCommandKind.CrossFade && item.FindPropertyRelative("_targetKind").enumValueIndex != (int)NovelTargetKind.Character)
                        item.FindPropertyRelative("_targetKind").enumValueIndex = (int)NovelTargetKind.Background;
                    if (kind == NovelCommandKind.Shake)
                    {
                        item.FindPropertyRelative("_direction").vector2Value = Vector2.right;
                        item.FindPropertyRelative("_frequency").floatValue = 12;
                        item.FindPropertyRelative("_decay").boolValue = true;
                        if (item.FindPropertyRelative("_targetKind").enumValueIndex != (int)NovelTargetKind.Character)
                            item.FindPropertyRelative("_targetKind").enumValueIndex = (int)NovelTargetKind.Stage;
                    }
                }
            }
            if (kind == NovelCommandKind.Say)
            {
                Key(item, "_characterId", "角色键（留空为旁白）", catalog, kind, true);
                var text = item.FindPropertyRelative("_text");
                EditorGUILayout.LabelField("台词");
                EditorGUI.BeginChangeCheck();
                string edited = EditorGUILayout.TextArea(text.stringValue, EditorStyles.textArea, GUILayout.MinHeight(70));
                if (EditorGUI.EndChangeCheck()) text.stringValue = edited;
                Key(item, "_resourceKey", "配音键（可留空）", catalog, NovelCommandKind.Voice);
                EnumField(item, "_textMode", "显示方式", new[] { "普通对白", "居中标题 / 章节卡", "全屏旁白" });
                var beats = item.FindPropertyRelative("_textBeats");
                int previousCount = beats.arraySize;
                Field(item, "_textBeats", "结构化文字节奏");
                for (int index = previousCount; index < beats.arraySize; index++)
                {
                    var beat = beats.GetArrayElementAtIndex(index);
                    beat.FindPropertyRelative("_at").intValue = index == 0 ? 0 : beats.GetArrayElementAtIndex(index - 1).FindPropertyRelative("_at").intValue + 1;
                    beat.FindPropertyRelative("_pause").floatValue = 0;
                    beat.FindPropertyRelative("_speed").floatValue = 1;
                    beat.FindPropertyRelative("_instant").intValue = 0;
                }
                EditorGUILayout.LabelField("正文字数（Unicode 标量）", NovelTextRules.Length(text.stringValue).ToString());
                EditorGUILayout.HelpBox("At：从 0 起的正文 Unicode 标量索引（包含换行与标点）；Pause：到达位置后停顿秒数；Speed：此处起速度倍率；Instant：停顿后即时显示的字数。按 At 递增。正文按纯文本显示，不写控制标记。长文自动分页，末页才进入整句稳定点。", MessageType.Info);
            }
            else if (kind == NovelCommandKind.DialogueVisibility)
            {
                Field(item, "_dialogueVisible", "显示剧情对白层");
                EditorGUILayout.HelpBox("只隐藏对白层，不暂停演出。下一句、选择或结局自动恢复；玩家手动隐藏独立处理。", MessageType.Info);
            }
            else if (kind == NovelCommandKind.SetVariable) { EnumField(item, "_scope", "作用域", new[] { "本章节", "全局" }); Field(item, "_variableId", "变量 ID"); Field(item, "_value", "赋值"); }
            else if (kind == NovelCommandKind.Wait) Field(item, "_duration", "等待秒数");
            else if (kind == NovelCommandKind.Camera)
            {
                Field(item, "_cameraZoom", "舞台缩放（1–3）"); Field(item, "_position", "舞台平移（屏幕归一化）");
                Field(item, "_duration", "动作秒数"); Field(item, "_delay", "开始延迟秒数");
                Field(item, "_ease", "缓动"); Field(item, "_parallel", "并行启动"); Field(item, "_actionId", "动作 ID");
                EditorGUILayout.HelpBox("背景、人物与绑定粒子一起变换，对白和菜单不动。每轴平移不得超过 (缩放−1)/2；复位填缩放 1、平移 (0,0)。同镜头新动作从当前值接管。", MessageType.Info);
            }
            else if (kind == NovelCommandKind.Opacity)
            {
                Field(item, "_targetKind", "目标类型"); Field(item, "_instanceId", "人物实例 ID");
                Field(item, "_actionId", "动作 ID"); Field(item, "_opacity", "最终透明度（0–1）");
                Field(item, "_duration", "动作秒数"); Field(item, "_delay", "开始延迟秒数");
                Field(item, "_parallel", "并行启动（不等待）");
                EditorGUILayout.HelpBox("同目标 Opacity 立即接管（含延迟阶段），旧动作取消并解除等待；新动作从当前值开始。人物须已显示；舞台/背景无需填写实例 ID。遮罩使用独立 Cover / Flash 指令；粒子实例使用播放/停止粒子效果。", MessageType.Info);
            }
            else if (kind == NovelCommandKind.WaitActions) Field(item, "_waitActions", "等待动作 ID 列表（全部结束）");
            else if (kind == NovelCommandKind.Emphasis)
            {
                Field(item, "_emphasisMode", "强调模式（默认关闭）");
                if (item.FindPropertyRelative("_emphasisMode").enumValueIndex == (int)NovelEmphasisMode.Manual)
                    Field(item, "_instanceId", "强调实例 ID");
                Field(item, "_dimFactor", "其他人物亮度（0–1）");
                EditorGUILayout.HelpBox("Auto 按对白角色键匹配在场实例；旁白或说话者不在场时全部恢复亮度。", MessageType.Info);
            }
            else if (kind == NovelCommandKind.Wipe)
            {
                Key(item, "_resourceKey", "新背景键", catalog, NovelCommandKind.Background);
                EnumField(item, "_wipeDirection", "擦除方向", new[] { "左 → 右", "右 → 左", "下 → 上", "上 → 下" });
                Field(item, "_duration", "动作秒数"); Field(item, "_delay", "开始延迟秒数");
                Field(item, "_ease", "缓动"); Field(item, "_parallel", "并行启动"); Field(item, "_actionId", "动作 ID");
                EditorGUILayout.HelpBox("目标为已有背景；新图加载后逐步覆盖旧图，与交叉淡化互相接管。切换是同场画面过渡，不触发换场清理。", MessageType.Info);
            }
            else if (NovelScreenRules.IsAction(kind))
            {
                if (kind == NovelCommandKind.Shake || kind == NovelCommandKind.CrossFade)
                {
                    Field(item, "_targetKind", "目标类型");
                    if (item.FindPropertyRelative("_targetKind").enumValueIndex == (int)NovelTargetKind.Character)
                        Field(item, "_instanceId", "已显示的人物实例 ID");
                }
                Field(item, "_actionId", "动作 ID");
                if (kind == NovelCommandKind.Shake)
                {
                    Field(item, "_strength", "强度（1 = 舞台尺寸的 2%）");
                    Field(item, "_direction", "方向（自动归一化）"); Field(item, "_frequency", "频率 Hz（0.1–60）");
                    Field(item, "_decay", "随时间衰减");
                    EditorGUILayout.HelpBox("目标仅支持 Stage / Character。临时偏移与 Move 叠加，结束/取消/快进归零，不移动对白。", MessageType.Info);
                }
                else if (kind == NovelCommandKind.CrossFade)
                {
                    var target = (NovelTargetKind)item.FindPropertyRelative("_targetKind").enumValueIndex;
                    Key(item, "_resourceKey", "新图片键", catalog, target == NovelTargetKind.Background ? NovelCommandKind.Background : NovelCommandKind.Character);
                    EditorGUILayout.HelpBox("目标须已有图片。新图加载后启动双图淡化；接管会先收束旧动作到其新图，再向下一张淡化。加载失败保留原画面。", MessageType.Info);
                }
                else
                {
                    Field(item, "_color", "颜色（闪白请选择白色）"); Field(item, "_opacity", "目标透明度（乘颜色 Alpha）");
                    Field(item, "_hold", "保持秒数"); Field(item, "_wholeReader", "覆盖整个阅读页（包括对白）");
                    EditorGUILayout.HelpBox("Cover 保留最终颜色；Flash 在淡入、保持、淡出后恢复原遮罩。Flash 的时长是淡入淡出合计。两者共用遮罩通道。", MessageType.Info);
                }
                Field(item, "_duration", "动作秒数（不含保持）"); Field(item, "_delay", "开始延迟秒数");
                Field(item, "_ease", "缓动（震动使用自身波形）"); Field(item, "_parallel", "并行启动（不等待）");
            }
            else if (NovelActorRules.IsAction(kind))
            {
                Field(item, "_instanceId", "人物实例 ID"); Field(item, "_actionId", "动作 ID");
                if (kind == NovelCommandKind.Move)
                {
                    Field(item, "_positionMode", "目标位置类型");
                    if (item.FindPropertyRelative("_positionMode").enumValueIndex == (int)NovelPositionMode.Named)
                        EnumField(item, "_slot", "目标命名位置", SlotNames);
                    else Field(item, "_position", "归一化目标坐标");
                    Field(item, "_exitAfterMove", "移动完成后退场");
                }
                if (kind == NovelCommandKind.Scale) Field(item, "_scale", "最终缩放（各轴 0.05–5）");
                if (kind == NovelCommandKind.Rotate) Field(item, "_rotation", "最终旋转角度");
                if (kind == NovelCommandKind.Mirror) Field(item, "_mirror", "水平镜像");
                if (kind == NovelCommandKind.Layer) Field(item, "_layer", "人物层级（-100–100）");
                if (kind == NovelCommandKind.Gesture) { Field(item, "_gesture", "动作预设"); Field(item, "_strength", "强度（0–5）"); }
                Field(item, "_duration", "动作秒数（镜像/层级须为 0）"); Field(item, "_delay", "开始延迟秒数");
                Field(item, "_ease", "缓动"); Field(item, "_parallel", "并行启动（不等待）");
                EditorGUILayout.HelpBox("同实例同属性接管，不同属性可并行。命名目标立即预占，已占用则报错；交换位置先将一人移到归一化临时位置。跳动/点头结束后清除临时姿态。", MessageType.Info);
            }
            else if (NovelMediaRules.IsMedia(kind))
            {
                if (kind != NovelCommandKind.BGM && kind != NovelCommandKind.BGMStop) Field(item, "_instanceId", "实例 ID");
                if (NovelMediaRules.NeedsResource(kind)) Key(item, "_resourceKey", "资源键", catalog, kind);
                if (kind == NovelCommandKind.EffectPlay)
                {
                    Field(item, "_persistent", "持续效果"); Field(item, "_bindingId", "绑定人物 ID（留空为舞台）");
                    Field(item, "_position", "归一化位置 / 人物局部偏移"); Field(item, "_scale", "缩放"); Field(item, "_layer", "同绑定效果层级");
                }
                if (kind == NovelCommandKind.EffectPlay || kind == NovelCommandKind.AmbientPlay) Field(item, "_keepOnSceneChange", "换背景/章节时保留");
                if (NovelMediaRules.IsAudio(kind))
                {
                    Field(item, "_volume", "剧情音量（叠乘玩家设置）"); Field(item, "_duration", "渐变秒数"); Field(item, "_delay", "渐变延迟");
                    Field(item, "_ease", "缓动"); Field(item, "_actionId", "动作 ID（留空使用指令 ID）"); Field(item, "_parallel", "并行渐变");
                }
                EditorGUILayout.HelpBox("持续效果与循环音不等待生命周期；仅有限音量渐变参与动作等待。相同实例接管；BGM 使用双音轨交叉渐变。停止不存在的实例安全忽略。", MessageType.Info);
            }
            else if (kind != NovelCommandKind.Character && kind != NovelCommandKind.Background)
                Key(item, "_resourceKey", "配表资源键", catalog, kind);
            if (kind == NovelCommandKind.Character || kind == NovelCommandKind.Background)
            {
                if (kind == NovelCommandKind.Character)
                {
                    EnumField(item, "_slot", "画面位置", SlotNames);
                    Field(item, "_instanceId", "实例 ID（旧剧情可留空）");
                    EditorGUILayout.HelpBox("Show 在指定空槽创建实例；Replace/Hide 按实例寻址，忽略位置。留空保留旧槽位语义。", MessageType.Info);
                }
                EnumField(item, "_visualAction", "动作", ActionNames);
                if (kind == NovelCommandKind.Character && item.FindPropertyRelative("_visualAction").enumValueIndex == (int)NovelVisualAction.Show)
                {
                    Field(item, "_positionMode", "入场位置类型");
                    if (item.FindPropertyRelative("_positionMode").enumValueIndex == (int)NovelPositionMode.Normalized)
                        Field(item, "_position", "归一化入场坐标（须显式实例 ID）");
                }
                if ((NovelVisualAction)item.FindPropertyRelative("_visualAction").enumValueIndex == NovelVisualAction.Hide)
                    EditorGUILayout.HelpBox(kind == NovelCommandKind.Character
                        ? "让这个位置的人物退场；位置原本为空时保持为空。隐藏不需要图片，资源键留空是正常的。"
                        : "移除当前背景。隐藏不需要图片，资源键留空是正常的。", MessageType.Info);
                else Key(item, "_resourceKey", "使用的图片资源键", catalog, kind);
                Field(item, "_duration", "过渡秒数");
            }
            var advanced = item.FindPropertyRelative("_commandId");
            advanced.isExpanded = EditorGUILayout.Foldout(advanced.isExpanded, "标识与修订", true);
            if (advanced.isExpanded)
            {
                Id(item, "_commandId", "指令 ID");
                if (kind == NovelCommandKind.Say) { Id(item, "_lineId", "台词 ID"); Field(item, "_textRevision", "台词修订"); }
            }
        }
        private static string Summary(SerializedProperty item, bool command, NarrativeTableCatalog catalog)
        {
            if (!command) return item.FindPropertyRelative("_text").stringValue;
            var kind = (NovelCommandKind)item.FindPropertyRelative("_kind").enumValueIndex;
            if (kind == NovelCommandKind.Say)
            {
                string character = item.FindPropertyRelative("_characterId").stringValue;
                string name = string.IsNullOrEmpty(character) ? "旁白" : character;
                if (catalog != null && catalog.TryGetCharacter(character, out var row)) name = row.DisplayName;
                string mode = item.FindPropertyRelative("_textMode").enumValueIndex switch { 1 => "[标题] ", 2 => "[全屏旁白] ", _ => "" };
                return mode + name + "：" + item.FindPropertyRelative("_text").stringValue;
            }
            if (kind == NovelCommandKind.Opacity) return "透明度 · " + item.FindPropertyRelative("_targetKind").enumDisplayNames[item.FindPropertyRelative("_targetKind").enumValueIndex] +
                "/" + item.FindPropertyRelative("_instanceId").stringValue + " → " + item.FindPropertyRelative("_opacity").floatValue +
                " · " + item.FindPropertyRelative("_actionId").stringValue + (item.FindPropertyRelative("_parallel").boolValue ? "（并行）" : "（等待）");
            if (kind == NovelCommandKind.DialogueVisibility) return item.FindPropertyRelative("_dialogueVisible").boolValue ? "恢复剧情对白层" : "隐藏剧情对白层 · 下一句自动恢复";
            if (kind == NovelCommandKind.Camera) return "舞台镜头 · " + item.FindPropertyRelative("_cameraZoom").floatValue.ToString("0.##") + "X · " + item.FindPropertyRelative("_position").vector2Value;
            if (kind == NovelCommandKind.WaitActions) return "等待全部指定动作完成或取消";
            if (kind == NovelCommandKind.Emphasis) return "角色强调 · " + item.FindPropertyRelative("_emphasisMode").enumDisplayNames[item.FindPropertyRelative("_emphasisMode").enumValueIndex];
            if (NovelActorRules.IsAction(kind)) return CommandNames[(int)kind] + " · " + item.FindPropertyRelative("_instanceId").stringValue +
                " · " + item.FindPropertyRelative("_actionId").stringValue + (item.FindPropertyRelative("_parallel").boolValue ? "（并行）" : "（等待）");
            if (kind == NovelCommandKind.Wait) return "暂停剧情推进 · " + Seconds(item);
            if (kind == NovelCommandKind.SetVariable)
            {
                var value = item.FindPropertyRelative("_value");
                string text = value.FindPropertyRelative("_type").enumValueIndex switch
                {
                    0 => value.FindPropertyRelative("_bool").boolValue ? "是（true）" : "否（false）",
                    1 => value.FindPropertyRelative("_int").intValue.ToString(),
                    _ => "“" + value.FindPropertyRelative("_string").stringValue + "”"
                };
                return (item.FindPropertyRelative("_scope").enumValueIndex == 0 ? "本章节" : "全局") + " · " + item.FindPropertyRelative("_variableId").stringValue + " = " + text;
            }
            if (NovelMediaRules.IsMedia(kind)) return CommandNames[(int)kind] + " · " + item.FindPropertyRelative("_instanceId").stringValue + " · " + item.FindPropertyRelative("_resourceKey").stringValue;
            string key = item.FindPropertyRelative("_resourceKey").stringValue;
            if (kind == NovelCommandKind.Background || kind == NovelCommandKind.Character)
            {
                int action = item.FindPropertyRelative("_visualAction").enumValueIndex;
                string target = kind == NovelCommandKind.Character ? SlotNames[item.FindPropertyRelative("_slot").enumValueIndex] + "立绘" : "背景";
                string resource = "";
                if (action != (int)NovelVisualAction.Hide)
                {
                    resource = string.IsNullOrEmpty(key) ? " · ⚠ 尚未选择图片" : " · " + key;
                    if (kind == NovelCommandKind.Character && catalog != null)
                    {
                        var portrait = catalog.Portraits.FirstOrDefault(p => p.Id == key);
                        if (portrait != null && catalog.TryGetCharacter(portrait.CharacterId, out var character))
                            resource = " · " + character.DisplayName + "（" + key + "）";
                    }
                }
                return ActionNames[action] + target + resource + " · " + Seconds(item);
            }
            return "播放" + CommandNames[(int)kind] + " · " + (string.IsNullOrEmpty(key) ? "尚未选择音频" : key);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Draw(UnityEngine.Object asset, NarrativeSnapshot snapshot, NarrativeTableCatalog catalog = null)
        {
            if (!asset) return;
            using var data = new SerializedObject(asset);
            Draw(data, snapshot, catalog);
        }
        internal static void Draw(SerializedObject data, NarrativeSnapshot snapshot, NarrativeTableCatalog catalog = null, bool settings = false)
        {
            var asset = data.targetObject;
            if (!asset) return;
            using var disabled = new EditorGUI.DisabledScope(!NarrativeGraphModel.CanEdit(asset));
            data.UpdateIfRequiredOrScript();
            if (asset is NarrativeChapterSO)
            {
                EditorGUILayout.PropertyField(data.FindProperty("_displayName"), new GUIContent("章节名称"));
                EditorGUILayout.PropertyField(data.FindProperty("_assetPrefix"), new GUIContent("新节点文件前缀"));
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(data.FindProperty("_chapterId"), new GUIContent("章节 ID"));
                EditorGUILayout.PropertyField(data.FindProperty("_storyRevision"), new GUIContent("剧情修订"));
                EditorGUILayout.PropertyField(data.FindProperty("_variables"), new GUIContent("初始变量"), true);
                data.ApplyModifiedProperties(); return;
            }
            var node = (NarrativeNodeSO)asset;
            if (settings)
            {
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(data.FindProperty("_nodeId"), new GUIContent("节点 ID"));
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(data.FindProperty("_chapterId"), new GUIContent("所属章节 ID"));
                EditorGUILayout.PropertyField(data.FindProperty("_contentRevision"), new GUIContent("内容修订"));
                foreach (string field in new[] { "_endingId", "_next", "_fallback" })
                {
                    var prop = data.FindProperty(field);
                    if (prop != null) EditorGUILayout.PropertyField(prop, new GUIContent(field switch {
                        "_prompt" => "选择提示", "_endingId" => "结局 ID", "_next" => "后续节点", _ => "兜底节点" }));
                }
                data.ApplyModifiedProperties(); return;
            }
            var prompt = data.FindProperty("_prompt");
            if (prompt != null) EditorGUILayout.PropertyField(prompt, new GUIContent("选择提示"));
            if (node is NarrativeChapterExitSO) EditorGUILayout.HelpBox("到达这里后结束本章；下一章和条件在章节总览中配置。", MessageType.Info);
            if (node is NarrativeEndingSO ending) EditorGUILayout.HelpBox("剧情结束：" + ending.EndingId, MessageType.Info);
            var list = data.FindProperty("_commands") ?? data.FindProperty("_options") ?? data.FindProperty("_branches");
            Action pending = null;
            if (list != null)
            {
                bool commands = list.name == "_commands";
                EditorGUILayout.LabelField(commands ? "段内步骤（从上到下执行）" : "按顺序判定的选项 / 分流", EditorStyles.boldLabel);
                if (commands && GUILayout.Button("插入演出预设…"))
                { data.ApplyModifiedProperties(); NarrativePresetWindow.Open((NarrativeDialogueSO)asset); data.Update(); }
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 折叠只影响编辑器显示，运行观察时也可操作。
                    bool enabled = GUI.enabled; GUI.enabled = true;
                    if (GUILayout.Button("全部收起")) for (int j = 0; j < list.arraySize; j++) list.GetArrayElementAtIndex(j).isExpanded = false;
                    if (GUILayout.Button("全部展开")) for (int j = 0; j < list.arraySize; j++) list.GetArrayElementAtIndex(j).isExpanded = true;
                    GUI.enabled = enabled;
                }
                for (int i = 0; i < list.arraySize; i++)
                {
                    var item = list.GetArrayElementAtIndex(i); int index = i;
                    string id = item.FindPropertyRelative(commands ? "_commandId" : "_optionId").stringValue;
                    bool current = snapshot != null && snapshot.NodeId == node.NodeId &&
                        snapshot.ChapterId == node.ChapterId && (commands ? snapshot.CommandId == id : snapshot.Error?.OptionId == id);
                    Color previous = GUI.backgroundColor;
                    if (current) GUI.backgroundColor = new Color(.25f, 1f, .65f);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool editable = GUI.enabled;
                            GUI.enabled = true;
                            item.isExpanded = EditorGUILayout.Foldout(item.isExpanded, (current ? "▶ " : "") + (i + 1) + ". " + (commands ? CommandNames[item.FindPropertyRelative("_kind").enumValueIndex] : "选项 / 分流"), true);
                            GUI.enabled = editable;
                            using (new EditorGUI.DisabledScope(i == 0))
                                if (GUILayout.Button("↑", GUILayout.Width(24))) pending = () => list.MoveArrayElement(index, index - 1);
                            using (new EditorGUI.DisabledScope(i == list.arraySize - 1))
                                if (GUILayout.Button("↓", GUILayout.Width(24))) pending = () => list.MoveArrayElement(index, index + 1);
                            if (GUILayout.Button("复制", GUILayout.Width(40)))
                                pending = () => { data.ApplyModifiedProperties(); NarrativeGraphModel.AddItem(node, list.name, index); data.Update(); };
                            if (GUILayout.Button("−", GUILayout.Width(24))) pending = () => list.DeleteArrayElementAtIndex(index);
                        }
                        EditorGUILayout.LabelField(Summary(item, commands, catalog), EditorStyles.wordWrappedLabel);
                        if (item.isExpanded)
                        {
                            if (commands)
                            {
                                Command(item, catalog);
                                if (NovelActorRules.IsAction((NovelCommandKind)item.FindPropertyRelative("_kind").enumValueIndex))
                                    for (int j = i - 1; j >= 0; j--)
                                    {
                                        var earlier = list.GetArrayElementAtIndex(j);
                                        var priorKind = (NovelCommandKind)earlier.FindPropertyRelative("_kind").enumValueIndex;
                                        if (priorKind == NovelCommandKind.WaitActions) break;
                                        var currentKind = (NovelCommandKind)item.FindPropertyRelative("_kind").enumValueIndex;
                                        bool maskPair = (priorKind == NovelCommandKind.Cover || priorKind == NovelCommandKind.Flash) &&
                                            (currentKind == NovelCommandKind.Cover || currentKind == NovelCommandKind.Flash);
                                        if ((!maskPair && priorKind != currentKind) || !earlier.FindPropertyRelative("_parallel").boolValue) continue;
                                        if (NovelScreenRules.IsAction(currentKind))
                                        {
                                            if (maskPair || earlier.FindPropertyRelative("_targetKind").enumValueIndex == item.FindPropertyRelative("_targetKind").enumValueIndex &&
                                                (item.FindPropertyRelative("_targetKind").enumValueIndex != (int)NovelTargetKind.Character ||
                                                earlier.FindPropertyRelative("_instanceId").stringValue == item.FindPropertyRelative("_instanceId").stringValue))
                                            { EditorGUILayout.HelpBox("可能接管第 " + (j + 1) + " 步的同属性画面动作。", MessageType.Warning); break; }
                                            continue;
                                        }
                                        if ((priorKind != NovelCommandKind.Opacity && earlier.FindPropertyRelative("_instanceId").stringValue == item.FindPropertyRelative("_instanceId").stringValue) ||
                                            priorKind == NovelCommandKind.Opacity && earlier.FindPropertyRelative("_targetKind").enumValueIndex == item.FindPropertyRelative("_targetKind").enumValueIndex &&
                                            (item.FindPropertyRelative("_targetKind").enumValueIndex != (int)NovelTargetKind.Character ||
                                            earlier.FindPropertyRelative("_instanceId").stringValue == item.FindPropertyRelative("_instanceId").stringValue))
                                        { EditorGUILayout.HelpBox("可能接管第 " + (j + 1) + " 步仍在运行的同属性动作；请确认这是预期行为。", MessageType.Warning); break; }
                                    }
                            }
                            else
                            {
                                Id(item, "_optionId", "选项 ID"); Field(item, "_text", "文字");
                                Field(item, "_condition", "条件（空为无条件）"); Field(item, "_target", "后续节点");
                            }
                        }
                    }
                    GUI.backgroundColor = previous;
                }
                if (GUILayout.Button(commands ? "+ 新指令" : "+ 新选项 / 分流"))
                    pending = () => { data.ApplyModifiedProperties(); NarrativeGraphModel.AddItem(node, list.name); data.Update(); };
            }
            pending?.Invoke(); data.ApplyModifiedProperties();
        }
        #endregion
    }

    [CustomPropertyDrawer(typeof(NovelValue))]
    internal sealed class NovelValueDrawer : PropertyDrawer
    {
        #region 外部方法
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2 + 4;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;
            var type = property.FindPropertyRelative("_type"); EditorGUI.PropertyField(position, type, label);
            position.y += position.height + 3;
            EditorGUI.PropertyField(position, property.FindPropertyRelative(type.enumValueIndex switch { 0 => "_bool", 1 => "_int", _ => "_string" }), new GUIContent("值"));
            EditorGUI.EndProperty();
        }
        #endregion
    }

    [CustomEditor(typeof(NarrativeNodeSO), true)]
    internal sealed class NarrativeNodeInspector : UnityEditor.Editor
    {
        private int _tab;
        #region 外部方法
        public override void OnInspectorGUI()
        {
            if (!NarrativeEditorAvailability.Enabled) { DrawDefaultInspector(); return; }
            if (GUILayout.Button("打开流程窗口")) NarrativeGraphWindow.OpenFor(target);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorGUILayout.HelpBox("运行期间剧情定义只读。", MessageType.Info);
            _tab = GUILayout.Toolbar(_tab, new[] { "对话内容", "SO 设置" });
            NarrativeContentGUI.Draw(serializedObject, NarrativeObservation.Current?.Snapshot, settings: _tab == 1);
        }
        #endregion
    }

    [CustomEditor(typeof(NarrativeChapterSO))]
    internal sealed class NarrativeChapterInspector : UnityEditor.Editor
    {
        #region 外部方法
        public override void OnInspectorGUI()
        {
            if (!NarrativeEditorAvailability.Enabled) { DrawDefaultInspector(); return; }
            if (GUILayout.Button("打开流程窗口")) NarrativeGraphWindow.OpenFor(target);
            NarrativeContentGUI.Draw(serializedObject, null);
            using (new EditorGUI.DisabledScope(!NarrativeGraphModel.CanEdit(target)))
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_entry"), new GUIContent("入口"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_nodes"), new GUIContent("节点目录"), true);
                serializedObject.ApplyModifiedProperties();
            }
        }
        #endregion
    }
}
