using System.Reflection;
using Game.Narrative.Editor;
using NUnit.Framework;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeTemplateIdentityTests
    {
        [TestCase(false, "visual-novel", null, null)]
        [TestCase(false, "visual-novel", "base", "base")]
        [TestCase(false, null, "visual-novel", "visual-novel")]
        [TestCase(true, null, "visual-novel", null)]
        [TestCase(true, "visual-novel", "base", "visual-novel")]
        [TestCase(false, "base", "novel-child", "novel-child")]
        public void ModeSelectsOnlyItsFormalRecord(bool embedded, string editing, string deployed, string expected)
        {
            var method = typeof(NarrativeGraphModel).GetMethod("SelectTemplateIdentity",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            Assert.AreEqual(expected, method.Invoke(null, new object[] { embedded, editing, deployed }));
        }
    }
}
