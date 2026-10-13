using System.Linq;
using Ember.Core.Fsm;

namespace Ember.Core.Editor
{
    internal static class EmberFsmBlueprintRules
    {
        internal static string Describe(EmberFsmTransition transition)
        {
            if (transition == null) return "";
            if (transition.Sites.Count == 0) return "\n旧式主动请求：调用时机/来源分支请查看真实生命周期代码。";
            return "\n" + string.Join("\n\n", transition.Sites.OrderBy(s => s.Segment).ThenBy(s => s.Order).Select(s =>
                $"请求位置 {s.Id} · {s.Phase} / {s.Segment} / {s.Order}\n许可：{s.Condition}\n"
                + (s.Continuation == EmberFsmContinuation.UntilMatched
                    ? "匹配后结束本回调；目标普通拒绝不阻止原子节点派发。"
                    : "普通拒绝继续；成功或协议错误结束本轮派发。")
                + $"\n提交：{s.CommitDescription}"));
        }

        internal static string Describe(EmberFsmAttemptRecord record)
        {
            if (record == null) return "旧记录未采集请求位置";
            string query = record.SkipReason != null ? "短路未查询：" + record.SkipReason
                : !record.Evaluated ? "未查询" : !record.Allowed.HasValue ? "条件查询异常"
                : record.Allowed.Value ? "条件满足" : "条件不满足";
            return $"位置 {record.SiteId ?? "未解析"} · {record.ActualPhase} #{record.DispatchId} · 激活 {record.Generation} · {query}\n"
                + $"提交区：{(record.CommitCompleted ? "完成" : record.CommitStarted ? "已开始，未完成" : "未开始")} · 原请求目标 {record.RequestedTarget}\n"
                + "预检：" + string.Join(" → ", record.Entries.Select(e => e.NodeId + ":" + e.Decision
                    + (e.RedirectSlot == null ? "" : "(" + e.RedirectSlot + ")")))
                + (record.RuleIdentity == null ? "" : "\n基础许可：" + record.BaseAllowed + " · 附加树（冻结记录）\n"
                    + string.Join("\n", record.Conditions.Select(c => c.Description + " [" + c.NodeId + "]："
                        + (c.Result.HasValue ? c.Result.Value.ToString() : "未求值") + " " + c.Fact)));
        }
    }
}
