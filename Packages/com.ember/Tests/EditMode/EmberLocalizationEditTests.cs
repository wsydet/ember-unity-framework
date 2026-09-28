using System;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EmberLocalizationEditTests
    {
        [Test]
        public void GenericResolverSupportsArbitraryLanguagesAndActualSourceFallback()
        {
            var table = ScriptableObject.CreateInstance<EmberLocalizationTable>();
            try
            {
                table.SourceLanguage = "fr";
                table.Languages = new[] { "fr", "de", "en" };
                table.Entries = new[] { new EmberLocalizationTable.Entry { Key = "test.greeting", Values = new[] { "Bonjour", "", "Hello" } } };
                string current = "en";
                var resolver = new EmberTextLocalizer("fr", table.Languages, () => current);
                resolver.AddTable(table);
                Assert.IsTrue(resolver.TryGet("test.greeting", out var value)); Assert.AreEqual("Hello", value);
                current = "de";
                Assert.IsTrue(resolver.TryGet("test.greeting", out value)); Assert.AreEqual("Bonjour", value);
                Assert.IsTrue(resolver.TryGet("test.greeting", "unknown", out value)); Assert.AreEqual("Bonjour", value);
                Assert.IsFalse(resolver.TryGet("missing", out value));
                Assert.Throws<InvalidOperationException>(() => resolver.AddTable(table));
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }
        [TestCase("Hello {player}", "你好")]
        [TestCase("<b>Hello</b>", "你好")]
        [TestCase("Hello\nWorld", "你好世界")]
        [TestCase("Score %d", "分数 %s")]
        public void TranslationRejectsLostTokens(string source, string translated)
            => Assert.Throws<InvalidOperationException>(() => EmberTranslationRequest.ValidateTokens(source, translated));
        [Test]
        public void TranslationAllowsReorderedPlaceholders()
            => Assert.DoesNotThrow(() => EmberTranslationRequest.ValidateTokens("{name}: {score}", "{score} 分，{name}"));
    }
}
