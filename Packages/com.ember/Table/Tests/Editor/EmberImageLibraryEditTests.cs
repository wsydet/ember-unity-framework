using System;
using NUnit.Framework;
using UnityEngine;

namespace Ember.Table.Editor.Tests
{
    public sealed class EmberImageLibraryEditTests
    {
        [Test]
        public void ManualFoldersAreScopedBySourceCharacterAndKey()
        {
            var folders = ScriptableObject.CreateInstance<EmberImageLibraryFolders>();
            try
            {
                folders.Assign("portraits/resourcePath", "alice", "normal", "服装/日常");
                folders.Assign("backgrounds/resourcePath", "", "normal", "学校");
                Assert.AreEqual("服装/日常", folders.GetFolder("portraits/resourcePath", "alice", "normal"));
                Assert.AreEqual("", folders.GetFolder("portraits/resourcePath", "bob", "normal"));
                Assert.AreEqual("学校", folders.GetFolder("backgrounds/resourcePath", "", "normal"));
                folders.Assign("portraits/resourcePath", "alice", "normal", "");
                Assert.AreEqual("", folders.GetFolder("portraits/resourcePath", "alice", "normal"));
                Assert.AreEqual("学校", folders.GetFolder("backgrounds/resourcePath", "", "normal"));
            }
            finally { UnityEngine.Object.DestroyImmediate(folders); }
        }

        [Test]
        public void FolderPersistenceKeepsEmptyDirectoriesAndAssignments()
        {
            var original = ScriptableObject.CreateInstance<EmberImageLibraryFolders>();
            var restored = ScriptableObject.CreateInstance<EmberImageLibraryFolders>();
            try
            {
                original.AddFolder("images/path", "", "空目录/子目录");
                original.Assign("images/path", "", "key", "已归档");
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(original), restored);
                Assert.AreEqual(2, restored.Folders.Count);
                Assert.AreEqual("已归档", restored.GetFolder("images/path", "", "key"));
            }
            finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(restored); }
        }

        [TestCase("../bad")]
        [TestCase("/bad")]
        [TestCase("bad//child")]
        [TestCase("bad\\child")]
        [TestCase("bad/ child")]
        public void InvalidFolderPathsAreRejected(string path)
            => Assert.Throws<InvalidOperationException>(() => EmberImageLibraryFolders.ValidatePath(path));

        [Test]
        public void AutoGroupsDoNotMutateSourceOrInterpretSlashesAsHierarchy()
        {
            var document = EmberTableSourceDocument.Parse("Assets/test.csv", "id,characterId,resourcePath\nnormal,a/b,Images/normal\n");
            var source = new EmberImageSource { Label = "立绘", TableId = "portraits", GroupColumns = new[] { "characterId" } };
            source.Validate(document);
            Assert.AreEqual("a%2Fb", source.AutoGroup(document, document.Rows[0]));
            Assert.AreEqual("a/b", document.Get(document.Rows[0], "characterId"));
            Assert.IsFalse(document.Dirty);
        }

        [Test]
        public void MissingRegisteredImageColumnIsRejected()
        {
            var document = EmberTableSourceDocument.Parse("Assets/test.csv", "id,audioPath\nkey,Audio/test\n");
            var source = new EmberImageSource { Label = "图片", TableId = "images" };
            Assert.Throws<InvalidOperationException>(() => source.Validate(document));
        }
    }
}
