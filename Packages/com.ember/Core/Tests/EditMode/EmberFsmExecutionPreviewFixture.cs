using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Ember.Core.Editor;
using NUnit.Framework;

namespace Ember.Core.Fsm.Tests
{
    public sealed partial class EmberFsmTests
    {
        // 与另一 partial 文件中同名重载区分；定位必须来自方法元数据而非文件名猜测。
        private sealed partial class PreviewDerived
        {
            public override void FixedUpdate(float deltaTime)
            {
                if (deltaTime > 0) return;
                throw new InvalidOperationException("never execute preview");
            }
        }

        [Test]
        public void ExecutionPreviewRuntimeWithoutSymbolsIsExplicitlyUnavailable()
        {
            var assembly=AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("FsmPreviewNoSymbols"+Guid.NewGuid().ToString("N")),AssemblyBuilderAccess.Run);
            var module=assembly.DefineDynamicModule("runtime");
            var type=module.DefineType("State",TypeAttributes.Public,typeof(EmberFsmState<object>));
            var method=type.DefineMethod("FixedUpdate",MethodAttributes.Public|MethodAttributes.Virtual,typeof(void),new[]{typeof(float)});
            method.GetILGenerator().Emit(OpCodes.Ret);
            var result=EmberFsmExecutionPreview.Inspect(type.CreateType()).Single(m=>m.Name=="FixedUpdate");
            Assert.That(result.ImplementationKind,Does.Contain("重写"));
            Assert.That(result.Source,Is.Null);
            Assert.That(result.Status,Does.Contain("不可用"));
        }
    }
}
