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
    }
}
