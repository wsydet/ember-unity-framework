using System;
using System.IO;
using System.Linq;
using System.Text;
using Ember.Basic;
using Ember.Table.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>Idempotent sample import through Unity prefab APIs and the existing table center pipeline.</summary>
    internal static class NarrativeE3Assets
    {
        #region 内部方法
        [InitializeOnLoadMethod]
        private static void Schedule() => EditorApplication.delayCall += Import;
        private static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NarrativeEditorAvailability.Visible) return;
            try
            {
                Create("lastlight_motes", true, false);
                Create("lastlight_spark", false, false);
                Create("rain", true, true);
                string output = "Assets/GameResource/Resources/Config/Tables/novel_audio.bytes";
                if (File.Exists(output) && Encoding.UTF8.GetString(File.ReadAllBytes(output)).Contains("lastlight_rooftop_wind") &&
                    Encoding.UTF8.GetString(File.ReadAllBytes(output)).Contains("lastlight_evening")) return;
                AssetDatabase.ImportAsset("Assets/GameResource/TableSources/novel_audio.etable.csv", ImportAssetOptions.ForceUpdate);
                var definition = EmberTablePipeline.FindAllDefinitions().Single(d => d.TableId == "novel_audio");
                if (!EmberTablePipeline.BakeCurrent(definition).Succeeded) throw new InvalidOperationException("novel_audio 配表中心导出失败");
            }
            catch (Exception ex) { EmberDebug.LogError("Narrative.E3", "E3 示例导入未完成：" + ex); }
        }
        private static void Create(string key, bool loop, bool rain)
        {
            string path = "Assets/GameResource/Resources/Effects/Narrative/" + key + ".prefab";
            if (File.Exists(path)) return; // Never overwrite an author's customized prefab.
            if (!AssetDatabase.IsValidFolder("Assets/GameResource/Resources/Effects"))
                AssetDatabase.CreateFolder("Assets/GameResource/Resources", "Effects");
            if (!AssetDatabase.IsValidFolder("Assets/GameResource/Resources/Effects/Narrative"))
                AssetDatabase.CreateFolder("Assets/GameResource/Resources/Effects", "Narrative");
            var root = new GameObject(key);
            try
            {
                var particles = root.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.playOnAwake = false; main.loop = loop; main.duration = loop ? 4 : .2f;
                main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = 128;
                main.startLifetime = rain ? 1.4f : loop ? 6 : .45f;
                main.startSpeed = 0;
                main.startSize3D = true;
                main.startSizeX = rain ? .0012f : loop ? .0025f : .007f;
                main.startSizeY = rain ? .035f : loop ? .0035f : .009f;
                main.startSizeZ = .001f;
                main.startColor = rain ? new Color(.7f, .8f, 1, .25f) : new Color(1, .84f, .48f, loop ? .35f : .7f);
                main.stopAction = ParticleSystemStopAction.None;
                var emission = particles.emission; emission.rateOverTime = loop ? rain ? 65 : 7 : 0;
                if (!loop) emission.SetBursts(new[] { new ParticleSystem.Burst(0, 14) });
                var shape = particles.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = loop ? new Vector3(1.1f, rain ? .02f : .9f, 0) : new Vector3(.035f, .035f, 0);
                var velocity = particles.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = rain ? -.08f : .018f; velocity.y = rain ? -.9f : loop ? .025f : .09f;
                var color = particles.colorOverLifetime; color.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) });
                color.color = gradient;
                root.GetComponent<ParticleSystemRenderer>().enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        #endregion
    }
}
