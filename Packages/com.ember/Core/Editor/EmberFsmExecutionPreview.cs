using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine.UIElements;

namespace Ember.Core.Editor
{
    /// <summary>通过接口映射和匹配的调试符号读取真实实现；绝不创建或调用状态。</summary>
    public static class EmberFsmExecutionPreview
    {
        #region 内部参数
        private static readonly string[] METHODS = { "Initialize", "CanEnter", "Enter", "Update", "FixedUpdate", "Pause", "Resume", "Exit", "Dispose" };
        private static readonly string[] TIMING = {
            "准备：首次 Start 创建全图对象后初始化；不是每次 Enter。",
            "进入预检：可能允许、拒绝或重定向；拒绝不意味着旧分支已经 Exit。",
            "激活：提交时先对离开分支叶→父 Exit，再对新分支父→叶 Enter；保留祖先不重复进出。",
            "活动更新：宿主独立重复驱动 Update，按活动路径父→子；转换/版本变化可能短路旧路径派发。",
            "物理更新：宿主独立重复驱动 FixedUpdate，不与 Update 固定交替；父→子派发可能短路。",
            "可选控制：显式 Pause 对活动路径回调，暂停通常 Update/FixedUpdate；不自动暂停世界物理。",
            "可选控制：显式 Resume 恢复；不是每次激活的必经步骤。",
            "激活结束：清理本次激活，下一次仍可 Enter；Stop/异常有额外清理路径。",
            "对象释放：对象寿命结束时清理，不是每次 Exit 都立即 Dispose。" };
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string TypeName(Type type) => type == null ? "缺失" : !type.IsGenericType ? type.FullName
            : type.GetGenericTypeDefinition().FullName.Split('`')[0] + "<" + string.Join(", ",type.GetGenericArguments().Select(TypeName)) + ">";
        private static object Property(object value, string name) => value?.GetType().GetProperty(name)?.GetValue(value);
        private static MethodInfo Implementation(Type type, string name)
        {
            var contract = name == "Dispose" ? typeof(IDisposable) : type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEmberFsmState<>));
            if (contract == null || !contract.IsAssignableFrom(type)) return null;
            var map = type.GetInterfaceMap(contract);
            int index = Array.FindIndex(map.InterfaceMethods, m => m.Name == name);
            return index < 0 ? null : map.TargetMethods[index];
        }
        private static void ReadSource(MethodInfo method, MethodPreview result)
        {
            object module = null;
            try
            {
                if (method == null || method.Module.Assembly.IsDynamic) { result.Status = "源码不可用：无可靠方法元数据"; return; }
                var cecil = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Mono.Cecil");
                if (cecil == null) { result.Status = "源码不可用：调试符号读取器不可用"; return; }
                var moduleType = cecil.GetType("Mono.Cecil.ModuleDefinition");
                var readerType = cecil.GetType("Mono.Cecil.ReaderParameters");
                var reader = Activator.CreateInstance(readerType);
                readerType.GetProperty("ReadSymbols").SetValue(reader, true);
                readerType.GetProperty("InMemory").SetValue(reader, true);
                module = moduleType.GetMethod("ReadModule", new[] { typeof(string), readerType })
                    .Invoke(null, new[] { method.Module.FullyQualifiedName, reader });
                if (!Equals(Property(module,"Mvid"), method.Module.ModuleVersionId)) { result.Status="源码不可用：程序集符号版本不匹配"; return; }
                var definition = moduleType.GetMethod("LookupToken", new[] { typeof(int) }).Invoke(module, new object[] { method.MetadataToken });
                var debug = Property(definition, "DebugInformation");
                var points = ((IEnumerable)Property(debug,"SequencePoints"))?.Cast<object>()
                    .Where(p => !(bool)Property(p,"IsHidden")).ToArray();
                if (points == null || points.Length == 0) { result.Status="源码不可用：没有方法序列点（不能据此认定为空实现）"; return; }
                var document = Property(points[0],"Document");
                var path = (string)Property(document,"Url");
                if (points.Any(p => (string)Property(Property(p,"Document"),"Url") != path) || !File.Exists(path))
                { result.Status="源码不可用：多文档映射或文件缺失"; return; }
                result.Path = path;
                var expected = Property(document,"Hash") as byte[];
                var algorithm = Property(document,"HashAlgorithm")?.ToString();
                byte[] actual;
                using (HashAlgorithm hash = algorithm == "SHA256" ? SHA256.Create() : algorithm == "SHA1" ? SHA1.Create() : null)
                {
                    if (hash == null || expected == null || expected.Length == 0) { result.Status="源码不可用：符号不含可核实的源码校验值"; return; }
                    actual = hash.ComputeHash(File.ReadAllBytes(path));
                }
                if (!actual.SequenceEqual(expected)) { result.Status="源码已改变：与已编译符号不一致，请编译后刷新"; return; }
                int first = points.Min(p => (int)Property(p,"StartLine")), last = points.Max(p => (int)Property(p,"EndLine"));
                var lines = File.ReadAllLines(path);
                if (first < 1 || last > lines.Length || last < first) { result.Status="源码不可用：符号范围无效"; return; }
                result.Line = first;
                result.Source = string.Join("\n", lines.Skip(first-1).Take(last-first+1));
                result.Status = "已核实的实际方法源码（调试符号范围，可能省略签名）";
            }
            catch (Exception ex) { result.Status="源码不可用：" + (ex.InnerException ?? ex).GetType().Name; }
            finally { (module as IDisposable)?.Dispose(); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public sealed class MethodPreview
        {
            public string Name, Timing, Declaration, ImplementationKind, Source, Path, Status;
            public int Line;
        }
        public static IReadOnlyList<MethodPreview> Inspect(Type runtimeType)
        {
            var results = new List<MethodPreview>();
            for (int i=0; i<METHODS.Length; i++)
            {
                var method = runtimeType == null ? null : Implementation(runtimeType, METHODS[i]);
                var result = new MethodPreview { Name=METHODS[i], Timing=TIMING[i],
                    Declaration=method == null ? "无匹配接口实现" : TypeName(method.DeclaringType),
                    ImplementationKind=method == null ? "不可用" : method.DeclaringType != runtimeType ? "继承实际实现"
                        : method.GetBaseDefinition() != method ? "本类型重写" : "本类型实现" };
                ReadSource(method,result); results.Add(result);
            }
            return results;
        }
        public static bool OpenSource(MethodPreview method)
            => method != null && method.Line > 0 && !string.IsNullOrEmpty(method.Path)
                && UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(method.Path, method.Line);
        #endregion
    }

    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 内部参数
        private VisualElement _preview;
        public IReadOnlyList<EmberFsmExecutionPreview.MethodPreview> ExecutionPreview { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ShowExecutionPreview(EmberFsmNodeDescription info)
        {
            if (_preview == null) return;
            _preview.Clear(); _preview.style.display = DisplayStyle.Flex;
            var type = info.Definition != null ? info.Definition.RuntimeStateType : null;
            var title = new Label("执行预览 · 只读静态源码\n" + Display(info.Id) + " [" + info.Id + "]\n运行类型：" + (type?.FullName ?? "缺失")
                + "\n定义 SO：" + info.Definition?.GetType().FullName + "\n结构路径（非活动路径）：" + StatePath(info.Id));
            title.AddToClassList("fsm-preview-title"); title.style.whiteSpace = WhiteSpace.Normal; _preview.Add(title);
            ExecutionPreview = EmberFsmExecutionPreview.Inspect(type);
            foreach (var method in ExecutionPreview)
            {
                var fold = new Foldout { text=method.Name + " · " + method.ImplementationKind, value=method.Name=="FixedUpdate" };
                var note = new Label(method.Timing + "\n实际声明：" + method.Declaration + "\n" + method.Status);
                note.AddToClassList("fsm-method-note"); note.style.whiteSpace=WhiteSpace.Normal; fold.Add(note);
                if (!string.IsNullOrEmpty(method.Source))
                {
                    var source = new TextField { value=method.Source, multiline=true, isReadOnly=true };
                    source.AddToClassList("fsm-code"); source.style.whiteSpace=WhiteSpace.Normal; source.style.fontSize=12; source.style.unityTextAlign=UnityEngine.TextAnchor.UpperLeft;
                    var textInput=source.Q<VisualElement>(className:"unity-text-input");
                    if(textInput!=null) textInput.style.unityTextAlign=UnityEngine.TextAnchor.UpperLeft;
                    fold.Add(source);
                    fold.Add(new Button(() => EmberFsmExecutionPreview.OpenSource(method)) { text="定位实际方法源码 · 行 " + method.Line });
                }
                else if (!string.IsNullOrEmpty(method.Path)) fold.Add(new Button(() => UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(method.Path,1)) { text="打开候选文件（正文未核实）", tooltip=method.Path });
                _preview.Add(fold);
            }
        }
        #endregion
    }
}
