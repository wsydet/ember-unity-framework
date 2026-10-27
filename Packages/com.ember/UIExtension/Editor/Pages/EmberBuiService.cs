// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ember.Basic.LitJson;
using Ember.Core.Editor;
using Ember.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using J = Ember.Basic.LitJson.JsonData;

namespace Ember.UIExtension.Editor
{
    /// <summary>BUI v3 + mandatory EUI map consumer. Explicit mappings, immutable plans and resumable generation.</summary>
    public static partial class EmberBuiService
    {
        #region 内部参数
        public const string VERSION = "1.0.0";
        private static readonly string[] Capabilities = { "ember-eui-map-v1", "dynamic-list-v1" };
        private sealed class Input
        {
            internal EmberBuiRequest request;
            internal J map, bui;
            internal Dictionary<string, J> nodes, targets;
            internal Dictionary<string, EUIContract> contracts = new();
        }
        #endregion

        #region 外部方法
        public static EmberBuiPlan Analyze(string requestPath)
        {
            var plan = new EmberBuiPlan { requestPath = requestPath, projectRoot = Path.GetFullPath(".").Replace('\\', '/') };
            try
            {
                plan.requestDigest = Hash(requestPath);
                var input = Read(requestPath, plan);
                CheckAccess(input.request);
                var receipt = ReadReceipt(input.request);
                if (receipt != null)
                {
                    Require(receipt.requestDigest == plan.requestDigest, "RECEIPT_REQUEST_CHANGED");
                    Require(receipt.status == "completed", "RESUME_REQUIRED: " + receipt.status);
                    foreach (var state in receipt.after) Require(StateMatches(state), "OUTPUT_CHANGED: " + state.path);
                    plan.noChanges = true;
                }
                foreach (var target in input.request.targets)
                    AnalyzeTarget(input, target, plan, plan.noChanges);
                plan.limitations.Add("ArialMT remains design identity. Existing fonts/materials/skin are retained; new static text uses the explicitly configured font. No Arial/TMP equivalence is claimed.");
                plan.limitations.Add("No native Photoshop effects, mixed text runs, rotation, v4 adaptation, virtual/multi-template lists or arbitrary business-expression execution.");
                plan.limitations.Add("Candidate creation does not migrate external consumers or delete the old asset. Existing object mappings and scoped nested-source replacement are explicit.");
                plan.valid = true;
            }
            catch (Exception e) { plan.issues.Add(e.Message); }
            plan.digest = "";
            plan.digest = Digest(JsonUtility.ToJson(plan));
            return plan;
        }

        public static EmberBuiReceipt Apply(EmberBuiPlan plan)
        {
            Require(plan != null && plan.valid, "PLAN_INVALID");
            var fresh = Analyze(plan.requestPath);
            Require(fresh.valid && fresh.digest == plan.digest, "PLAN_STALE: " + string.Join(";", fresh.issues));
            var input = Read(plan.requestPath, new EmberBuiPlan());
            CheckAccess(input.request); RequireIdle();
            if (plan.noChanges) return ReadReceipt(input.request);
            Preview(plan); // Validate all scoped edits before importing or saving any formal asset.
            Require(!File.Exists(input.request.receiptPath), "RECEIPT_EXISTS: query/resume original operation");
            var receipt = new EmberBuiReceipt { requestPath = plan.requestPath, requestDigest = plan.requestDigest,
                planDigest = plan.digest, status = "reserved", backupPath = ".utmp/EmberBui/" + plan.digest };
            Directory.CreateDirectory(receipt.backupPath);
            foreach (var path in plan.writes.Where(File.Exists))
            {
                var dest = Path.Combine(receipt.backupPath, path);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)); File.Copy(path, dest, false);
            }
            Write(input.request.receiptPath, receipt);
            try
            {
                foreach (var target in Ordered(input))
                {
                    receipt.status = "writing:" + target.targetId; Write(input.request.receiptPath, receipt);
                    if (target.mode == "create" || target.mode == "candidate")
                    {
                        var created = EUICreationService.Create(Creation(input, target));
                        Require(created.Success, "CREATE_FAILED: " + created.Error);
                        Require(created.PrefabPath == target.prefabPath, "CREATE_PATH_CHANGED");
                    }
                    ImportImages(input, target);
                    var root = PrefabUtility.LoadPrefabContents(target.prefabPath);
                    try
                    {
                        Edit(input, target, root, false, null);
                        Require(!EUIBindingEditorUtility.ValidateBinding(root.GetComponent<EUIBinding>()).HasError, "BINDING_INVALID");
                        PrefabUtility.SaveAsPrefabAsset(root, target.prefabPath, out var saved);
                        Require(saved, "PREFAB_SAVE_FAILED");
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    receipt.saved.Add(target.prefabPath);
                    receipt.status = "saved"; CaptureAfter(plan, receipt); Write(input.request.receiptPath, receipt);
                    Generate(target.prefabPath, receipt); CaptureAfter(plan, receipt); Write(input.request.receiptPath, receipt);
                }
                Verify(input, receipt);
                receipt.status = "completed";
                CaptureAfter(plan, receipt); Write(input.request.receiptPath, receipt);
            }
            catch (Exception e)
            {
                receipt.error = e.ToString();
                // A writing marker deliberately remains unknown; never replay a possibly saved mutation.
                if (!receipt.status.StartsWith("writing:", StringComparison.Ordinal)) receipt.status = "partial";
                CaptureAfter(plan, receipt);
                try { Write(input.request.receiptPath, receipt); }
                catch (Exception reportError) { receipt.error += "\nRECEIPT_WRITE_FAILED: " + reportError.Message; }
            }
            return receipt;
        }

        public static EmberBuiReceipt ResumeGeneration(string requestPath)
        {
            var request = JsonUtility.FromJson<EmberBuiRequest>(File.ReadAllText(requestPath));
            CheckAccess(request); RequireIdle();
            var receipt = ReadReceipt(request);
            Require(receipt != null && receipt.requestDigest == Hash(requestPath), "RECOVERY_IDENTITY_CHANGED");
            Require(receipt.status == "saved" || receipt.status == "partial", "RECOVERY_REQUIRES_KNOWN_SAVE: " + receipt.status);
            Require(receipt.saved.Count == request.targets.Count, "PARTIAL_TARGET_SET: inspect original receipt; do not replay Apply");
            foreach (var state in receipt.after) Require(StateMatches(state), "RECOVERY_OUTPUT_CHANGED: " + state.path);
            foreach (var path in receipt.saved.Where(p => !receipt.generated.Contains(p))) Generate(path, receipt);
            var input = Read(requestPath, new EmberBuiPlan());
            Verify(input, receipt);
            receipt.status = "completed"; receipt.error = null;
            foreach (var state in receipt.after) state.sha256 = Hash(state.path);
            Write(request.receiptPath, receipt);
            return receipt;
        }

        /// <summary>Repairs only Unity's nested replacement root-name reset, using the original saved receipt and exact source GUID.</summary>
        public static EmberBuiReceipt RecoverNestedNames(string requestPath)
        {
            var input=Read(requestPath,new EmberBuiPlan()); CheckAccess(input.request);RequireIdle();
            var receipt=ReadReceipt(input.request);
            Require(receipt!=null&&receipt.status=="partial"&&receipt.saved.Count==input.request.targets.Count,"RECOVERY_STATE_INVALID");
            foreach(var state in receipt.after)Require(StateMatches(state),"RECOVERY_OUTPUT_CHANGED: "+state.path);
            foreach(var target in input.request.targets.Where(t=>t.nodes.Any(n=>!string.IsNullOrEmpty(n.replaceSourceGuid))))
            {
                var root=PrefabUtility.LoadPrefabContents(target.prefabPath);
                try
                {
                    foreach(var op in target.nodes.Where(n=>!string.IsNullOrEmpty(n.replaceSourceGuid)))
                    {
                        if(Find(root,op.path))continue;
                        var parent=Find(root,op.path.Substring(0,op.path.LastIndexOf('/')));
                        var guid=S(Get(input.nodes[op.nodeId],"prefabRef"),"assetGuid");
                        var candidates=Enumerable.Range(0,parent.childCount).Select(parent.GetChild).Where(t=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)))==guid).ToArray();
                        Require(candidates.Length==1,"RECOVERY_AMBIGUOUS_NESTED_SOURCE");
                        candidates[0].name=Path.GetFileName(op.path);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(candidates[0].gameObject);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,target.prefabPath,out var saved);Require(saved,"RECOVERY_SAVE_FAILED");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                receipt.generated.Remove(target.prefabPath);
            }
            foreach(var state in receipt.after)state.sha256=Hash(state.path);
            Write(input.request.receiptPath,receipt);
            return ResumeGeneration(requestPath);
        }

        /// <summary>Edits only isolated Prefab contents and transient textures, then unloads everything.</summary>
        public static string Preview(EmberBuiPlan plan)
        {
            Require(plan != null && plan.valid, "PLAN_INVALID");
            Require(Analyze(plan.requestPath).digest == plan.digest, "PLAN_STALE");
            var input = Read(plan.requestPath, new EmberBuiPlan()); CheckAccess(input.request); RequireIdle();
            var objects = new List<UnityEngine.Object>();
            try
            {
                foreach (var target in Ordered(input))
                {
                    var root = PrefabUtility.LoadPrefabContents(target.mode == "update" ? target.prefabPath : target.sourceSkeletonPath);
                    try { Edit(input, target, root, true, objects); }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                return "All scoped nodes constructed in isolated contents and unloaded; no formal assets saved. Visual verification remains separate.";
            }
            finally { foreach (var obj in objects) if (obj) UnityEngine.Object.DestroyImmediate(obj); }
        }

        public static void Write(string path, object value)
        {
            SafePath(path); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var temp = path + ".pending";
            File.WriteAllText(temp, JsonUtility.ToJson(value, true), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }

        /// <summary>Read back existing outputs without generating or changing assets.</summary>
        public static EmberBuiReceipt VerifySaved(string requestPath)
        {
            var input=Read(requestPath,new EmberBuiPlan());CheckAccess(input.request);RequireIdle();
            var receipt=ReadReceipt(input.request);
            Require(receipt!=null&&receipt.requestDigest==Hash(requestPath),"VERIFY_RECEIPT_MISSING_OR_CHANGED");
            foreach(var state in receipt.after)Require(StateMatches(state),"VERIFY_OUTPUT_CHANGED: "+state.path);
            var result=JsonUtility.FromJson<EmberBuiReceipt>(JsonUtility.ToJson(receipt));
            result.verification.Clear();Verify(input,result);return result;
        }
        #endregion

        #region 内部方法
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
        private static J Get(J obj, string key) => obj != null && obj.IsObject && obj.ContainsKey(key) ? obj[key] : null;
        private static string S(J obj, string key) => Get(obj, key)?.ToString() ?? "";
        private static IEnumerable<J> Items(J value)
        { if (value == null) yield break; Require(value.IsArray, "ARRAY_REQUIRED"); for (var i = 0; i < value.Count; i++) yield return value[i]; }
        private static float F(J value) => float.Parse(value.ToString(), CultureInfo.InvariantCulture);
        private static Vector2 V(J value)
        {
            Require(value != null && value.IsArray && value.Count == 2, "VECTOR_INVALID");
            var result = new Vector2(F(value[0]), F(value[1]));
            Require(float.IsFinite(result.x) && float.IsFinite(result.y), "GEOMETRY_NONFINITE"); return result;
        }
        private static string Digest(string value)
        { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
        private static string Hash(string path)
        { if (!File.Exists(path)) return ""; using var sha = SHA256.Create(); using var file = File.OpenRead(path); return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant(); }
        private static string SafePath(string path)
        {
            Require(!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) && !path.Contains('\\')
                && path.Split('/').All(p => p != ".." && p != "." && p.Length > 0), "PATH_INVALID: " + path);
            var full = Path.GetFullPath(path); var root = Path.GetFullPath(".") + Path.DirectorySeparatorChar;
            Require(full.StartsWith(root, StringComparison.OrdinalIgnoreCase), "PATH_ESCAPE");
            for (var cursor = full; cursor.Length >= root.Length; cursor = Path.GetDirectoryName(cursor))
                if (File.Exists(cursor) || Directory.Exists(cursor)) Require((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) == 0, "REPARSE_PATH: " + path);
            return path;
        }
        private static void Pin(EmberBuiPlan plan, string path, string expected = null)
        {
            SafePath(path); var hash = Hash(path);
            Require(hash.Length > 0 && (expected == null || hash == expected), "INPUT_CHANGED_OR_MISSING: " + path);
            if (!plan.inputs.Any(s => s.path == path)) plan.inputs.Add(new EmberBuiFileState { path = path, sha256 = hash });
        }
        private static void CheckAccess(EmberBuiRequest request)
        {
            Require(!string.IsNullOrEmpty(request.ownerTemplateId) && EmberProjectSetup.IsTemplateActive(request.ownerTemplateId), "TEMPLATE_INACTIVE");
            if (!string.IsNullOrEmpty(request.moduleType))
            {
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(request.moduleType)).FirstOrDefault(t => t != null);
                Require(type != null, "MODULE_MISSING");
                var attribute = type.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == "EmberModuleAttribute");
                Require(attribute != null && (bool)(attribute.GetType().GetProperty("Enabled")?.GetValue(attribute)
                    ?? attribute.GetType().GetField("Enabled")?.GetValue(attribute) ?? false), "MODULE_DISABLED");
            }
        }
        private static void RequireIdle()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating, "EDITOR_BUSY");
            Require(PrefabStageUtility.GetCurrentPrefabStage() == null, "PREFAB_STAGE_OPEN");
        }
        private static EmberBuiReceipt ReadReceipt(EmberBuiRequest request)
        { SafePath(request.receiptPath); return File.Exists(request.receiptPath) ? JsonUtility.FromJson<EmberBuiReceipt>(File.ReadAllText(request.receiptPath)) : null; }
        private static void CaptureAfter(EmberBuiPlan plan, EmberBuiReceipt receipt)
        { receipt.after = plan.writes.Where(File.Exists).Select(p => new EmberBuiFileState { path = p, sha256 = Hash(p) }).ToList(); }
        private static bool StateMatches(EmberBuiFileState state)
        {
            if(Hash(state.path)==state.sha256)return true;
            // The project's encoding importer prepends UTF-8 BOM to generated C#. Accept only that exact byte transformation.
            if(!state.path.EndsWith(".Binding.cs",StringComparison.Ordinal)||!File.Exists(state.path))return false;
            var bytes=File.ReadAllBytes(state.path);
            if(bytes.Length<3||bytes[0]!=239||bytes[1]!=187||bytes[2]!=191)return false;
            using var sha=SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes,3,bytes.Length-3)).Replace("-","").ToLowerInvariant()==state.sha256;
        }
        private static void Generate(string path, EmberBuiReceipt receipt)
        {
            var binding = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EUIBinding>();
            if (!binding.NoCodeGeneration) Require(EUIBindingCodeGenUtility.TryRegenerateCode(binding, out var error), "GENERATION_FAILED: " + error);
            receipt.generated.Add(path);
        }
        #endregion
    }
}
