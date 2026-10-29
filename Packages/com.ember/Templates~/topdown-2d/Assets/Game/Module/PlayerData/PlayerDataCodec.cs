using System;
using System.Text;
using Ember.Basic.LitJson;
using Game.PlayerData;

namespace Game.Module
{
    /// <summary>base 示例适配器，解析完整字段与业务版本；格式/兼容错误不会生成默认进度。</summary>
    public sealed class PlayerDataCodec : IProgressCodec<PlayerData>
    {
        #region 内部参数
        public string DataTypeId => "ember.base.progress";
        private readonly UTF8Encoding _utf8 = new(false, true);
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public EncodedProgress Encode(PlayerData snapshot)
        {
            if (snapshot == null || snapshot.version != 1) throw new ArgumentException("base codec 仅支持业务版本 1。");
            return new EncodedProgress(_utf8.GetBytes(JsonMapper.ToJson(snapshot)), snapshot.version);
        }

        public DecodeResult<PlayerData> Decode(ReadOnlyMemory<byte> payload, int dataVersion)
        {
            if (dataVersion != 1) return new DecodeResult<PlayerData>(DecodeStatus.DataVersionUnsupported);
            try
            {
                var json = JsonMapper.ToObject(_utf8.GetString(payload.ToArray()));
                if (json == null || !json.IsObject || !json.ContainsKey("version") || !json.ContainsKey("level") || !json.ContainsKey("coins")
                    || !json["version"].IsInt || !json["level"].IsInt || !json["coins"].IsInt || (int)json["version"] != dataVersion)
                    return new DecodeResult<PlayerData>(DecodeStatus.DecodeFailed, error: "缺少完整字段、类型错误或负载内外业务版本不一致。");
                return new DecodeResult<PlayerData>(DecodeStatus.Decoded,
                    new PlayerData { version = (int)json["version"], level = (int)json["level"], coins = (int)json["coins"] });
            }
            catch (Exception error) { return new DecodeResult<PlayerData>(DecodeStatus.DecodeFailed, error: error.ToString()); }
        }
        #endregion
    }
}
