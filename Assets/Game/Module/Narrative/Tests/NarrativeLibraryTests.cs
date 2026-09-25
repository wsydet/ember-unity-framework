using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeLibraryTests
    {
        private string _folder;
        private NarrativeLibrarySO _library;
        private NarrativeStorySO _story;

        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/Game/Module/Narrative/Tests/Library_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Game/Module/Narrative/Tests", System.IO.Path.GetFileName(_folder));
            _story = ScriptableObject.CreateInstance<NarrativeStorySO>();
            _library = ScriptableObject.CreateInstance<NarrativeLibrarySO>();
            AssetDatabase.CreateAsset(_story, _folder + "/Original.asset");
            AssetDatabase.CreateAsset(_library, _folder + "/Library.asset");
            using var data = new SerializedObject(_library);
            data.FindProperty("_current").objectReferenceValue = _story;
            var stories = data.FindProperty("_stories"); stories.arraySize = 1;
            stories.GetArrayElementAtIndex(0).objectReferenceValue = _story;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(_library);
        }

        /// <summary>
        /// 删掉临时夹具后必须把资源管线刷干净。
        /// 否则残留的待处理导入会让紧随其后的 Play Mode 用例卡在
        /// EnterPlayMode 上（过渡等不到装配重载），表现为运行期用例整批失败。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_folder);
            AssetDatabase.Refresh();
        }

        [Test]
        public void RenamingAndMovingStoryPreservesEntryAndStableLookup()
        {
            string id = _story.StoryId;
            AssetDatabase.CreateFolder(_folder, "Moved");
            Assert.IsEmpty(AssetDatabase.MoveAsset(_folder + "/Original.asset", _folder + "/Moved/我的小说.asset"));
            AssetDatabase.ImportAsset(_folder + "/Library.asset", ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<NarrativeLibrarySO>(_folder + "/Library.asset");
            Assert.AreEqual(_story, loaded.Current);
            Assert.AreEqual(_story, loaded.Find(id));
            Assert.AreEqual("我的小说", loaded.Current.name);
        }

        [Test]
        public void MissingIdNeverFallsBackToCurrentStory()
        {
            Assert.IsNull(_library.Find("missing"));
            Assert.AreEqual(_story, _library.Current);
        }

        [Test]
        public void DuplicateStoryIdsAreRejectedRatherThanChoosingArbitrarily()
        {
            var duplicate = UnityEngine.Object.Instantiate(_story);
            AssetDatabase.CreateAsset(duplicate, _folder + "/Duplicate.asset");
            try
            {
                using var data = new SerializedObject(_library);
                var stories = data.FindProperty("_stories"); stories.arraySize = 2;
                stories.GetArrayElementAtIndex(1).objectReferenceValue = duplicate;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => _library.Find(_story.StoryId));
            }
            finally { AssetDatabase.DeleteAsset(_folder + "/Duplicate.asset"); }
        }
    }
}
