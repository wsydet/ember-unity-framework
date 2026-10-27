using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.LevelAuthoring.Editor
{
    public static class EmberLevelObjectEditing
    {
        #region 外部方法
        public static GameObject Place(EmberLevelMaterial material, Scene scene, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || material == null || !scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("请选择素材与已加载的编辑场景。");
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) ||
                !float.IsFinite(scale.x) || !float.IsFinite(scale.y) || !float.IsFinite(scale.z) || scale.x == 0 || scale.y == 0 || scale.z == 0)
                throw new ArgumentException("位置和缩放必须有限，缩放不能为零。");
            if (!material.AllowRotation && rotation != Quaternion.identity || !material.AllowScale && scale != Vector3.one ||
                !material.AllowMirror && (scale.x < 0 || scale.y < 0 || scale.z < 0)) throw new InvalidOperationException("素材不支持此变换。");
            var adapter = EmberLevelAdapters.GetObject(material.AdapterId);
            if (material.Kind == EmberLevelMaterialKind.Mechanism && adapter == null) throw new InvalidOperationException("缺少机关编辑适配器：" + material.AdapterId);
            adapter?.ValidatePlacement(material, scene);
            if (!material.HasPhysics && material.Prefab != null && material.Prefab.GetComponentInChildren<Collider2D>(true) != null)
                throw new InvalidOperationException("素材包含未声明的碰撞组件；请明确物理用途。");
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                var instance = material.Prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(material.Prefab, scene) : new GameObject(material.DisplayName);
                if (material.Prefab == null) { SceneManager.MoveGameObjectToScene(instance, scene); instance.AddComponent<SpriteRenderer>().sprite = material.Sprite; }
                Undo.RegisterCreatedObjectUndo(instance, "放置素材");
                instance.transform.SetPositionAndRotation(position, rotation); instance.transform.localScale = scale;
                adapter?.OnCreated(instance, false); EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = instance; return instance;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static GameObject Duplicate(GameObject source, IEmberLevelObjectAdapter adapter = null)
        {
            if (source == null || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请选择编辑对象。");
            adapter ??= EmberLevelAdapters.ResolveObject(source);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                var copy = GameObjectUtility.DuplicateGameObject(source);
                copy.name = source.name; Undo.RegisterCreatedObjectUndo(copy, "复制素材"); adapter?.OnCreated(copy, true);
                EditorSceneManager.MarkSceneDirty(copy.scene); Undo.CollapseUndoOperations(group); return copy;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static void Delete(GameObject instance, IEmberLevelObjectAdapter adapter = null)
        {
            if (instance == null || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请选择编辑对象。");
            adapter ??= EmberLevelAdapters.ResolveObject(instance);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try { adapter?.BeforeDelete(instance); var scene = instance.scene; Undo.DestroyObjectImmediate(instance); EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(group); }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        public static void ConfigureRenderer(SpriteRenderer renderer, Color color, int sortingLayer, int order, Vector2 size, bool tiled)
        {
            if (renderer == null || renderer.sprite == null) throw new ArgumentException("需要带图片的 SpriteRenderer。");
            // 未打包Sprite的packingMode并不表示其MeshType；白盒FullRect也可能返回Tight。
            if (tiled && (renderer.sprite.vertices.Length != 4 || renderer.sprite.packed &&
                (renderer.sprite.packingMode == SpritePackingMode.Tight || renderer.sprite.packingRotation != SpritePackingRotation.None)))
                throw new InvalidOperationException("平铺需矩形网格图片，且不能使用紧密或旋转打包的Sprite。");
            if (!(size.x > 0 && size.y > 0) || !float.IsFinite(size.x) || !float.IsFinite(size.y)) throw new ArgumentException("覆盖尺寸必须为正有限值。");
            Undo.RecordObject(renderer, "配置背景/装饰"); renderer.color = color; renderer.sortingLayerID = sortingLayer; renderer.sortingOrder = order;
            renderer.drawMode = tiled ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
            if (tiled) renderer.size = size;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
        }

        public static void SetEditorVisibility(GameObject instance, bool visible, bool locked)
        {
            if (visible) SceneVisibilityManager.instance.Show(instance, true); else SceneVisibilityManager.instance.Hide(instance, true);
            if (locked) SceneVisibilityManager.instance.DisablePicking(instance, true); else SceneVisibilityManager.instance.EnablePicking(instance, true);
        }
        #endregion
    }
}
