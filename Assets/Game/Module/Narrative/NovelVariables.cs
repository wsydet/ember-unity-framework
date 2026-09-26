using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Game.Narrative
{
    /// <summary>结构化整数运算；不解释策划单元格中的代码或表达式。</summary>
    public static class NovelVariableRules
    {
        #region 内部方法
        private static bool IsInteger(IReadOnlyDictionary<string, NovelValue> values, string id)
            => !string.IsNullOrWhiteSpace(id) && values != null && values.TryGetValue(id, out var v) && v.Type == NovelValueType.Int;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static string Validate(NovelCommand command, IReadOnlyDictionary<string, NovelValue> locals,
            IReadOnlyDictionary<string, NovelValue> globals)
        {
            if (command.Kind != NovelCommandKind.CalculateVariable && command.Kind != NovelCommandKind.RandomVariable) return null;
            if (!Enum.IsDefined(typeof(NovelVariableScope), command.Scope) ||
                !IsInteger(command.Scope == NovelVariableScope.Global ? globals : locals, command.VariableId))
                return "目标必须是已声明的整数变量";
            if (command.Kind == NovelCommandKind.RandomVariable)
                return command.RandomMin > command.RandomMax ? "随机整数下限不能大于上限（两端均包含）" : null;
            if (!Enum.IsDefined(typeof(NovelIntegerOperation), command.IntegerOperation)) return "未知整数运算";
            if (!string.IsNullOrEmpty(command.OperandVariableId))
            {
                if (!Enum.IsDefined(typeof(NovelVariableScope), command.OperandScope) ||
                    !IsInteger(command.OperandScope == NovelVariableScope.Global ? globals : locals, command.OperandVariableId))
                    return "来源必须是已声明的整数变量";
            }
            else if ((command.IntegerOperation == NovelIntegerOperation.Divide || command.IntegerOperation == NovelIntegerOperation.Modulo) && command.IntegerOperand == 0)
                return "除数不能为零";
            return null;
        }
        #endregion
    }

    public sealed partial class NarrativeRunner
    {
        #region 内部方法
        // xorshift32: state is nonzero and belongs to this session, never UnityEngine.Random.
        private uint NextRandom()
        {
            uint x = _randomState;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            return _randomState = x;
        }

        private int RandomInteger(int min, int max)
        {
            if (min == max) return min;
            // xorshift produces 1..uint.MaxValue. Rejection removes modulo bias.
            ulong width = (ulong)((long)max - min + 1);
            if (width == 1UL << 32)
                return unchecked((int)(((uint)RandomInteger(0, 65535) << 16) | (uint)RandomInteger(0, 65535)));
            ulong limit = uint.MaxValue - (ulong)uint.MaxValue % width;
            ulong sample;
            do { sample = (ulong)NextRandom() - 1; } while (sample >= limit);
            return (int)(min + (long)(sample % width));
        }

        private bool ExecuteVariable(NovelCommand command)
        {
            string error = NovelVariableRules.Validate(command, _variables, _globals);
            if (error != null) { Fault("BadVariableOperation", error); return false; }
            var target = command.Scope == NovelVariableScope.Global ? _globals : _variables;
            try
            {
                int result;
                if (command.Kind == NovelCommandKind.RandomVariable) result = RandomInteger(command.RandomMin, command.RandomMax);
                else
                {
                    int operand = string.IsNullOrEmpty(command.OperandVariableId) ? command.IntegerOperand :
                        (command.OperandScope == NovelVariableScope.Global ? _globals : _variables)[command.OperandVariableId].Int;
                    int current = target[command.VariableId].Int;
                    result = command.IntegerOperation switch
                    {
                        NovelIntegerOperation.Assign => operand,
                        NovelIntegerOperation.Add => checked(current + operand),
                        NovelIntegerOperation.Subtract => checked(current - operand),
                        NovelIntegerOperation.Multiply => checked(current * operand),
                        NovelIntegerOperation.Divide => checked(current / operand),
                        NovelIntegerOperation.Modulo => current % operand,
                        _ => throw new InvalidOperationException("未知整数运算")
                    };
                }
                target[command.VariableId] = new NovelValue(result);
                return true;
            }
            catch (ArithmeticException ex)
            {
                // Leave the failing target unchanged and preserve exact node/command diagnostics.
                Fault("VariableArithmetic", "整数计算失败：" + ex.Message); return false;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>
        /// 宿主或自定义节点写入剧情变量。目标必须已声明且类型一致：不做隐式转换、不自动创建变量，
        /// 失败时给出可定位的原因。变量写入不属于执行位置变化，因此不推进位置版本，
        /// 也就不会作废正在等待中的完成令牌。
        /// </summary>
        [HasGC]
        public bool TrySetVariable(NovelVariableScope scope, string id, NovelValue value, out string error)
        {
            error = null;
            if (!Enum.IsDefined(typeof(NovelVariableScope), scope)) { error = "变量作用域无效"; return false; }
            if (string.IsNullOrWhiteSpace(id)) { error = "变量 ID 为空"; return false; }
            if (!Enum.IsDefined(typeof(NovelValueType), value.Type)) { error = "变量类型无效"; return false; }
            var target = scope == NovelVariableScope.Global ? _globals : _variables;
            if (!target.TryGetValue(id, out NovelValue declared)) { error = "变量未声明：" + id; return false; }
            if (declared.Type != value.Type) { error = "变量类型不符：" + id + " 需要 " + declared.Type; return false; }
            if (_busy) { error = "运行器正忙，变量未写入"; return false; }
            return Mutate(() => target[id] = value, false);
        }

        /// <summary>读取剧情变量；未声明时返回 false。</summary>
        [NoGC]
        public bool TryGetVariable(NovelVariableScope scope, string id, out NovelValue value)
        {
            value = default;
            if ((uint)scope > (uint)NovelVariableScope.Global || string.IsNullOrWhiteSpace(id)) return false;
            return (scope == NovelVariableScope.Global ? _globals : _variables).TryGetValue(id, out value);
        }
        #endregion
    }
}
