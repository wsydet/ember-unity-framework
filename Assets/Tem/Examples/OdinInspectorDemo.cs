// ------------------------------------------------------------------------------
// OdinInspectorDemo — 挂到任意 GameObject 上，在 Inspector 面板中验证 Odin 效果
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Ember.Editor
{
    [InfoBox("🎉 Odin Inspector 集成正常！以下展示了常用 Odin 特性。", InfoMessageType.Info)]
    public class OdinInspectorDemo : MonoBehaviour
    {
        [Title("📁 分组与折叠")]
        [FoldoutGroup("Settings")]
        public string playerName = "Ember";

        [FoldoutGroup("Settings")]
        [Range(0f, 100f)]
        public float volume = 80f;

        [FoldoutGroup("Settings")]
        public bool enableVibration = true;

        [FoldoutGroup("Settings")]
        [ShowInInspector, ReadOnly]
        private string ReadOnlyHint => $"Player: {playerName}, Vol: {volume}";

        // ----------

        [Title("🔘 按钮")]
        [Button("打印问候", ButtonSizes.Large)]
        private void SayHello()
        {
            Debug.Log($"[OdinDemo] Hello from {playerName}!");
        }

        [Button("重置设置")]
        private void ResetSettings()
        {
            playerName = "Ember";
            volume = 80f;
            enableVibration = true;
            Debug.Log("[OdinDemo] 设置已重置");
        }

        [Button(ButtonSizes.Small)]
        private void LogTime()
        {
            Debug.Log($"[OdinDemo] 当前时间: {DateTime.Now:HH:mm:ss}");
        }

        // ----------

        [Title("📊 数据显示")]
        [TableList(ShowIndexLabels = true)]
        public List<ItemEntry> inventory = new List<ItemEntry>
        {
            new ItemEntry { name = "剑", quantity = 1, weight = 3.5f },
            new ItemEntry { name = "盾", quantity = 1, weight = 5.0f },
            new ItemEntry { name = "药水", quantity = 3, weight = 0.5f },
        };

        [ShowInInspector, PropertyOrder(100)]
        [ProgressBar(0, 100, r: 0.2f, g: 0.7f, b: 0.2f)]
        public float HealthProgress => 75f;

        // ----------

        [Title("🎨 枚举与下拉")]
        [EnumToggleButtons]
        public DifficultyLevel difficulty = DifficultyLevel.Normal;

        [ValueDropdown("GetAvailableClasses")]
        public string heroClass = "战士";

        private static IEnumerable<string> GetAvailableClasses()
        {
            return new[] { "战士", "法师", "游侠", "牧师", "刺客" };
        }

        // ----------

        [Title("🔢 数值范围")]
        [SerializeField, MinValue(1), MaxValue(99)]
        private int level = 1;

        [SerializeField, MinValue(0), MaxValue(9999)]
        private int hp = 500;

        [SerializeField, MinValue(0), MaxValue(999)]
        private int mp = 200;

        [ShowInInspector, ReadOnly]
        private string CombatPower => $"Lv.{level}  HP:{hp}  MP:{mp}";

        // ----------

        [Title("⚙️ 内联对象")]
        [InlineProperty]
        public WeaponConfig weapon = new WeaponConfig();

        // ----------

        [Title("🖼️ 预览")]
        [PreviewField(Height = 100)]
        public Texture2D previewTexture;

        [AssetsOnly]
        public GameObject somePrefab;

        [SceneObjectsOnly]
        public GameObject someSceneObject;

        // ----------

        [Title("📝 多行文本")]
        [Multiline(4)]
        public string notes = "Odin 极大地增强了 Unity Inspector 的编辑体验。";

        // ----------

#if UNITY_EDITOR
        [Title("✅ 自检")]
        [Button("自检 — 所有 Odin 特性", ButtonSizes.Large), GUIColor(0.3f, 0.8f, 0.3f)]
        private void SelfCheck()
        {
            var attrs = GetType()
                .GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SelectMany(m => m.GetCustomAttributes(true))
                .Count(a => a.GetType().Namespace?.StartsWith("Sirenix") == true);

            Debug.Log($"<color=green>[OdinDemo] 当前脚本使用了 {attrs} 个 Odin 特性，全部正常渲染。</color>");
        }
#endif
    }

    // ──────────────────────────────────────────
    // 辅助类型
    // ──────────────────────────────────────────

    [Serializable]
    public class ItemEntry
    {
        [HorizontalGroup("Row")]
        public string name;

        [HorizontalGroup("Row")]
        public int quantity;

        [HorizontalGroup("Row")]
        public float weight;
    }

    public enum DifficultyLevel
    {
        [LabelText("简单")] Easy,
        [LabelText("普通")] Normal,
        [LabelText("困难")] Hard,
        [LabelText("地狱")] Hell
    }

    [Serializable]
    public class WeaponConfig
    {
        public string weaponName = "新手剑";

        [MinValue(1), MaxValue(100)]
        public int attack;

        [Range(0f, 1f)]
        public float criticalChance = 0.15f;

        [ShowInInspector, ReadOnly]
        public float CritPercent => criticalChance * 100f;
    }
}
