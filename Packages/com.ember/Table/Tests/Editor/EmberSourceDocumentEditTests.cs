using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Ember.Table.Editor.Tests
{
    public sealed class EmberSourceDocumentEditTests
    {
        [Test]
        public void RoundTripPreservesCommasQuotesNewlinesAndUnknownColumns()
        {
            var doc = EmberTableSourceDocument.Parse("Assets/test.csv", "key,en,notes\r\nx,\"A, \"\"B\"\"\nC\",keep\r\n");
            var result = EmberTableSourceDocument.Parse(doc.Path, doc.Serialize());
            Assert.AreEqual("A, \"B\"\nC", result.Get(result.Rows[0], "en"));
            Assert.AreEqual("keep", result.Get(result.Rows[0], "notes"));
            Assert.IsFalse(doc.Dirty);
        }
        [Test]
        public void DuplicateAndBlankNewKeysAreRejected()
        {
            var doc = EmberTableSourceDocument.Parse("Assets/test.csv", "key,en\nx,hello\n");
            Assert.Throws<InvalidOperationException>(() => doc.Add("x", "key"));
            Assert.Throws<InvalidOperationException>(() => doc.Add(" ", "key"));
            doc.Add("y", "key"); Assert.IsTrue(doc.Dirty);
        }
        [Test]
        public void SaveRejectsExternalChangesAndPreservesDraft()
        {
            string path = "Assets/__EmberSourceTest_" + Guid.NewGuid().ToString("N") + ".csv";
            try
            {
                File.WriteAllText(path, "key,en\nx,original\n");
                var doc = EmberTableSourceDocument.Load(path);
                doc.Rows[0].Values[1] = "draft";
                File.WriteAllText(path, "key,en\nx,external\n");
                Assert.Throws<InvalidOperationException>(() => doc.Save());
                Assert.AreEqual("draft", doc.Rows[0].Values[1]);
                StringAssert.Contains("external", File.ReadAllText(path));
            }
            finally { if (File.Exists(path)) File.Delete(path); AssetDatabase.DeleteAsset(path); }
        }
    }
}
