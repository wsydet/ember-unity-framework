namespace Borodar.RainbowHierarchy
{
    internal sealed class HierarchyRulesetWrapper
    {
        #pragma warning disable 414
        public int Version = HierarchyRulesetV2.VERSION;
        #pragma warning restore 414

        public string RulesetJson;
    }
}