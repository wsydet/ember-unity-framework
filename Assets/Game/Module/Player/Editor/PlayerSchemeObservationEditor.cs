using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    [InitializeOnLoad]
    internal static class PlayerSchemeObservationEditor
    {
        static PlayerSchemeObservationEditor() => PlayerSchemeObservation.FingerprintProvider = Fingerprint;

        internal static string Fingerprint(PlayerScheme scheme)
        {
            var text = new StringBuilder();
            // 引用身份和内容都参与比较；仅生成摘要，不创建另一套运行配置。
            foreach (var pair in PlayerSchemeWorkflow.Configuration(scheme).OrderBy(p => p.Key))
                text.Append(pair.Key).Append(':').Append(GlobalObjectId.GetGlobalObjectIdSlow(pair.Value))
                    .Append(':').Append(EditorJsonUtility.ToJson(pair.Value)).Append('\n');
            if (scheme.Graph != null) text.Append(EditorJsonUtility.ToJson(scheme.Graph));
            text.Append(JsonUtility.ToJson(Physics2D.gravity));
            return Hash128.Compute(text.ToString()).ToString();
        }

        internal static string Describe(PlayerSchemeObservation observation, PlayerScheme selected)
        {
            if (observation == null) return "未记录来源方案（旧装配或准备前）";
            string prefix = $"冻结方案 {observation.SourceName} · 捕获 {observation.CaptureId.ToString("N").Substring(0, 8)}";
            if (selected == null) return prefix + " · 未选编辑方案";
            if (observation.Source != selected) return prefix + " · 与编辑方案不同";
            if (observation.Fingerprint == null) return prefix + " · 同一来源，捕获内容无法核对";
            return prefix + (observation.Fingerprint == Fingerprint(selected)
                ? " · 编辑配置与捕获时一致" : " · 编辑配置已变化，运行仍用冻结值");
        }
    }
}
