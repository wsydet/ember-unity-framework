#if UNITY_EDITOR
using System;

namespace Game.Player
{
    /// <summary>仅编辑器观察元数据；不参与配置解析、存档或运行决策。</summary>
    internal sealed class PlayerSchemeObservation
    {
        internal static Func<PlayerScheme, string> FingerprintProvider;
        internal PlayerScheme Source { get; }
        internal string SourceName { get; }
        internal string Fingerprint { get; }
        internal Guid CaptureId { get; } = Guid.NewGuid();

        private PlayerSchemeObservation(PlayerScheme source, string fingerprint)
        { Source = source; SourceName = source.name; Fingerprint = fingerprint; }

        internal static PlayerSchemeObservation Capture(PlayerScheme source)
        {
            if (source == null) return null;
            // 观察不可改变准备结果；编辑器服务缺失/失败时明确显示未知。
            string fingerprint = null;
            try { fingerprint = FingerprintProvider?.Invoke(source); }
            catch (Exception) { }
            return new PlayerSchemeObservation(source, fingerprint);
        }
    }
}
#endif
