using System;
using System.Collections.Generic;
using System.Text;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    [Serializable]
    public sealed class NovelTextBinding
    {
        #region 编辑器面板参数
        [SerializeField] private string _token, _variableId;
        [SerializeField] private NovelVariableScope _scope;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Token => _token;
        public string VariableId => _variableId;
        public NovelVariableScope Scope => _scope;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelTextBinding(string token, string variableId, NovelVariableScope scope = NovelVariableScope.Global)
        { _token = token; _variableId = variableId; _scope = scope; }
        #endregion
    }

    public static class NovelTextBindings
    {
        #region 外部方法
        [HasGC]
        public static string Validate(NovelCommand command, IReadOnlyDictionary<string, NovelValue> locals,
            IReadOnlyDictionary<string, NovelValue> globals)
        {
            if (command.TextBindings.Count == 0) return null;
            if (command.Kind != NovelCommandKind.Say) return "文字变量绑定只用于对白";
            if (command.TextBeats.Count > 0) return "动态文字不能使用固定字符位置的节奏；请拆为独立台词";
            var tokens = new List<string>();
            foreach (var binding in command.TextBindings)
            {
                if (binding == null || string.IsNullOrEmpty(binding.Token) ||
                    string.IsNullOrWhiteSpace(binding.VariableId) || !Enum.IsDefined(typeof(NovelVariableScope), binding.Scope)) return "文字绑定字段无效";
                var source = binding.Scope == NovelVariableScope.Global ? globals : locals;
                if (source == null || !source.ContainsKey(binding.VariableId)) return "文字绑定变量未声明：" + binding.VariableId;
                if (command.Text == null || !command.Text.Contains(binding.Token)) return "正文不包含绑定占位符：" + binding.Token;
                foreach (var token in tokens)
                    if (token.Contains(binding.Token) || binding.Token.Contains(token)) return "文字绑定占位符重复或互相包含";
                tokens.Add(binding.Token);
            }
            return null;
        }

        [HasGC]
        public static string Resolve(NovelCommand command, IReadOnlyDictionary<string, NovelValue> locals,
            IReadOnlyDictionary<string, NovelValue> globals)
        {
            string error = Validate(command, locals, globals);
            if (error != null) throw new InvalidOperationException(error);
            return Resolve(command.Text, command.TextBindings, locals, globals);
        }

        /// <summary>
        /// 对给定文本做占位符替换。多语言译文走同一个入口，所以译文里的占位符与原文按同一套绑定替换；
        /// 译文没有出现某个占位符时那处就不替换（不报错），译文因此可以自由调整语序、省略称呼。
        /// </summary>
        [HasGC]
        public static string Resolve(string text, IReadOnlyList<NovelTextBinding> bindings,
            IReadOnlyDictionary<string, NovelValue> locals, IReadOnlyDictionary<string, NovelValue> globals)
        {
            if (string.IsNullOrEmpty(text) || bindings == null || bindings.Count == 0) return text;
            var result = new StringBuilder();
            for (int i = 0; i < text.Length;)
            {
                NovelTextBinding match = null;
                foreach (var binding in bindings)
                    if (binding != null && !string.IsNullOrEmpty(binding.Token) &&
                        i + binding.Token.Length <= text.Length &&
                        string.CompareOrdinal(text, i, binding.Token, 0, binding.Token.Length) == 0)
                    { match = binding; break; }
                if (match == null) result.Append(text[i++]);
                else
                {
                    // Values are appended once, never reinterpreted as further placeholders.
                    result.Append((match.Scope == NovelVariableScope.Global ? globals : locals)[match.VariableId].ToString());
                    i += match.Token.Length;
                }
            }
            return result.ToString();
        }
        #endregion
    }
}
