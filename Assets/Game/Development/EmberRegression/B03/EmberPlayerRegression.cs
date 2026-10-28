using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Ember.Regression
{
    // Opt-in direct regression fixture. Does not represent product lifecycle acceptance.
    public sealed class EmberPlayerRegression : MonoBehaviour
    {
        [SerializeField] private Ember.UIExtension.EmberFontSkinCatalog _fonts;
#if UNITY_EDITOR
        [SerializeField] private string _editorOutput;
#endif
        private string _output;
        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-ember-regression-output");
            if (index >= 0) _output = args[index + 1];
#if UNITY_EDITOR
            else _output = _editorOutput;
#endif
            if (string.IsNullOrEmpty(_output)) yield break;
            Directory.CreateDirectory(_output);
            Application.logMessageReceived += OnLog;
            DontDestroyOnLoad(gameObject);
            for (int cycle = 0; cycle < 2; cycle++)
            {
#if UNITY_EDITOR
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    "Assets/Game/Development/EmberRegression/B03/CacheFixture.unity", new LoadSceneParameters(LoadSceneMode.Additive));
#else
                yield return SceneManager.LoadSceneAsync("CacheFixture", LoadSceneMode.Additive);
#endif
                yield return null;
                var scene = SceneManager.GetSceneByName("CacheFixture");
                var maps = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Tilemap>(true)).ToArray();
                var map = maps.First(m => m.name == "B02 Mixed Rule And Ordinary");
                var cameraObject = new GameObject("RegressionCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                camera.orthographic = true;
                camera.orthographicSize = 3.5f;
                camera.transform.position = new Vector3(2, -1.5f, -10);
                camera.backgroundColor = Color.gray;
                camera.clearFlags = CameraClearFlags.SolidColor;
                if (_fonts)
                {
                    var canvasObject = new GameObject("OwnedFontCanvas", typeof(Canvas));
                    SceneManager.MoveGameObjectToScene(canvasObject, scene);
                    var canvas = canvasObject.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1;
                    for (int i = 0; i < _fonts.Skins.Count; i++)
                    {
                        var font = _fonts.Skins[i].Fonts[0].Font;
                        var labelObject = new GameObject("OwnedFont-" + i, typeof(RectTransform));
                        labelObject.transform.SetParent(canvas.transform, false);
                        var label = labelObject.AddComponent<TMPro.TextMeshProUGUI>();
                        label.font = font;
                        label.fontSharedMaterial = font.material;
                        label.text = i == 0 ? "验收中文 ★ 123" : "验收中文 ▶ 123";
                        label.fontSize = 24;
                        label.rectTransform.sizeDelta = new Vector2(550, 40);
                        label.rectTransform.anchoredPosition = new Vector2(0, 200 - 40 * i);
                        label.ForceMeshUpdate();
                        File.AppendAllText(Path.Combine(_output, "fonts.txt"), "cycle=" + cycle + " skin=" + _fonts.Skins[i].Id +
                            " glyphs=" + font.characterTable.Count + " characters=" + label.textInfo.characterCount +
                            " missing=" + label.textInfo.characterInfo.Take(label.textInfo.characterCount).Count(c => c.character != ' ' && (c.textElement == null || c.textElement.unicode != c.character)) +
                            " faces=" + string.Join(";",label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>c.character!=' ').Select(c=>c.fontAsset.name)) + "\n");
                    }
                }
                foreach (var observed in maps) Write(observed, cycle, "initial");
                yield return null;
                yield return null;
                yield return new WaitForEndOfFrame();
                var target = new RenderTexture(960, 540, 24);
                camera.targetTexture = target;
                yield return null;
                yield return new WaitForEndOfFrame();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(_output, "cycle-" + cycle + ".png"), pixels.EncodeToPNG());
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                Destroy(target);
                Destroy(pixels);
                yield return null;
                yield return null;
                if (Array.IndexOf(args, "-ember-diagnostic-refresh") >= 0)
                {
                    map.RefreshAllTiles();
                    Write(map, cycle, "after-diagnostic-refresh");
                }
                yield return SceneManager.UnloadSceneAsync(scene);
                yield return Resources.UnloadUnusedAssets();
            }
            File.WriteAllText(Path.Combine(_output, "completed.txt"), "direct fixture finished; process exit must be checked separately");
            if (!Application.isEditor) Application.Quit();
        }

        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
                File.AppendAllText(Path.Combine(_output, "diagnostics.txt"), type + ": " + message + "\n" + stack + "\n");
        }

        private void Write(Tilemap map, int cycle, string phase)
        {
            var occupied = new List<Vector3Int>();
            foreach (var point in map.cellBounds.allPositionsWithin)
                if (map.HasTile(point)) occupied.Add(point);
            string samples = string.Join("\n", occupied.Select(point =>
            {
                var sprite = map.GetSprite(point);
                return point + " sprite=" + (sprite ? sprite.name : "NULL") + " color=" + map.GetColor(point) +
                    " matrix=" + map.GetTransformMatrix(point) +
                    " vertices=" + (sprite ? string.Join(";", sprite.vertices.Select(v => v.ToString("F6"))) : "NULL") +
                    " uv=" + (sprite ? string.Join(";", sprite.uv.Select(v => v.ToString("F6"))) : "NULL");
            }));
            File.AppendAllText(Path.Combine(_output, "samples.txt"), "map=" + map.name + " cells=" + map.GetTilesBlock(map.cellBounds).Count(t => t) +
                " paths=" + (map.GetComponent<CompositeCollider2D>() ? map.GetComponent<CompositeCollider2D>().pathCount : 0) + " cycle=" + cycle + " phase=" + phase +
                " grid=" + map.layoutGrid.cellSize + " anchor=" + map.tileAnchor + "\n" + samples + "\n");
        }
    }
}
